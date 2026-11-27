using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

// CPU skinned-surface sampling drives NormalParticleAssembly VFX via a structured buffer.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class FragmentAssembly : MonoBehaviour
{
    // 날아오는 방향
    public Vector3 assemblyDirection = Vector3.right;
    public float cellSize3D = 0.05f;
    [HideInInspector] public Camera effectCamera;
    [Min(0.001f)] public float vfxSizeMultiplier = 1;
    public Color vfxTint = Color.white;
    [HideInInspector] public Material fragmentMaterial;
    [HideInInspector] public Mesh fragmentMesh;
    public bool playOnStart = false;
    [Min(0.1f)] public float duration = 2.5f;
    [Range(4, 64)] public float cellPixels = 18;
    [Range(0, 1)] public float cellDelay = 0.2f;
    [Range(100, 5000)] public int fragmentCount = 1600;
    [Min(0.01f)] public float scatterDistance = 1.4f;
    public Vector3 fragmentTravelDirection3D = Vector3.right;
    [Range(0, 0.3f)] public float streamSpread = 0.06f;
    [HideInInspector] public float fragmentSize = 0.025f;
    [HideInInspector] public Color fragmentColor = new Color(1, 0.72f, 0.84f, 1);
    
    MaterialPropertyBlock block;

    // VFX Datas
    const int VfxCapacity = 8192;
    const int VectorsPerParticle = 3;
    static readonly int AssemblyDataId = Shader.PropertyToID("AssemblyData");
    static readonly int BoundsCenterId = Shader.PropertyToID("AssemblyBoundsCenter");
    static readonly int BoundsSizeId = Shader.PropertyToID("AssemblyBoundsSize");
    [SerializeField] VisualEffect fragmentEffect;
    [SerializeField] Renderer fragmentRenderer;
    GraphicsBuffer graphicsBuffer;
    Vector4[] vfxData;

    // VertexDatas
    readonly List<Surface> surfaces = new List<Surface>();
    Sample[] samples;
    struct Sample 
    {
        public int surface, a, b, c; public Vector3 bary, offset, axis; public Color color; 
    }
    

    private void Awake()
    {
        Initialize(); build = false; elapsed = 1;
    }

    public void Begin(bool assembling)
    {
        if (build == assembling) return;
        elapsed = 1 - elapsed;
        build = assembling;

        direction = ToWorldDirection(assemblyDirection) * (build ? 1 : -1);
        travelDir = ToWorldDirection(fragmentTravelDirection3D) * (build ? 1 : -1);
        reference = Mathf.Abs(Vector3.Dot(travelDir, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        transverse = Vector3.Cross(travelDir, reference).normalized;
        depth = Vector3.Cross(travelDir, transverse).normalized;

        if (EffectCoroutine == null) EffectCoroutine = StartCoroutine(EffectCor());
    }

    bool First = true;
    private void OnEnable()
    {
        if (First) { First = false; return; }
        if (EffectCoroutine != null) StopCoroutine(EffectCoroutine);
        foreach (var s in surfaces)
        {
            if (!s.renderer) continue;
            s.renderer.GetPropertyBlock(block); block.SetFloat("_AssemblyEnabled", 1); s.renderer.SetPropertyBlock(block);
        }
        if (fragmentRenderer) fragmentRenderer.enabled = true;
        build = false; elapsed = 1; UpdateEffect();
        Begin(true);
    }

    void OnDisable() 
    { 
        if (!initialized) return;
        if (EffectCoroutine != null) StopCoroutine(EffectCoroutine);
        foreach (var s in surfaces)
        { 
            if (!s.renderer) continue; 
            s.renderer.GetPropertyBlock(block); block.SetFloat("_AssemblyEnabled", 0); s.renderer.SetPropertyBlock(block);
        } 
        VisibleFragments = 0; if (fragmentRenderer) fragmentRenderer.enabled = false; 
    }

    public void SetDirFromExtern(Transform StartFrom = null, Transform ToFrom = null)
    {
        if (!StartFrom) return;
        if (ToFrom == null) ToFrom = transform;
        assemblyDirection = (ToFrom.position - StartFrom.position).normalized;
    }


    int cUse = 0;
    [SerializeField] Transform SkinTrans;
    void Initialize()
    {
        if (initialized) return;
        effectCamera = Camera.main;
        block = new MaterialPropertyBlock();
        foreach (var r in SkinTrans.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null; 
            if (r is SkinnedMeshRenderer sk && sk.sharedMesh) { mesh = new Mesh();sk.BakeMesh(mesh, true); }
            if (!mesh) continue;
            // For Toon or etc( Tex = _MainTex )
            var s = new Surface { renderer = r, mesh = mesh, triangles = mesh.triangles, tex = r.sharedMaterial.GetTexture("_MainTex") as Texture2D }; s.Bake(); s.SetupSkinning(); surfaces.Add(s);
        }

        // 균등 분배 Algorithm
        var weights = new List<float>(); var ids = new List<Vector2Int>(); float total = 0;
       samples = new Sample[Mathf.Clamp(fragmentCount, 1, VfxCapacity)];

        for (int j = 0; j < surfaces.Count; j++)
        {
            var s = surfaces[j]; var m = s.renderer.localToWorldMatrix;
            for (int k = 0; k < s.triangles.Length; k += 3)
            {
                Vector3 a = m.MultiplyPoint3x4(s.vertices[s.triangles[k]]), b = m.MultiplyPoint3x4(s.vertices[s.triangles[k + 1]]), c = m.MultiplyPoint3x4(s.vertices[s.triangles[k + 2]]);
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f; weights.Add(total); ids.Add(new Vector2Int(j, k));
            }
        }
        Vector3 offset,sampleUV, bary;
        Vector2[] uv;
        float u, v,pick;
        int lo, hi,mid;
        for (int i = 0; i < samples.Length; i++)
        {
            // 이분
            pick = (float)GameManager.rng.NextDouble() * total;
            lo = 0;  hi = weights.Count - 1;
            while (lo < hi) { mid = (lo + hi) / 2; if (weights[mid] < pick) lo = mid + 1; else hi = mid; }

            // Offset
            var id = ids[lo]; var tr = surfaces[id.x].triangles;
            u = Mathf.Sqrt((float)GameManager.rng.NextDouble()); v = (float)GameManager.rng.NextDouble();
            offset.x = (float)GameManager.rng.NextDouble() * 2 - 1; offset.y = (float)GameManager.rng.NextDouble() * 2 - 1; offset.z = (float)GameManager.rng.NextDouble() * 2 - 1; offset.Normalize();
            bary = new Vector3(1 - u, u * (1 - v), u * v);

            samples[i] = new Sample
            {
                surface = id.x,a = tr[id.y], b = tr[id.y + 1],c = tr[id.y + 2],
                bary = bary, offset = offset * (0.6f + (float)GameManager.rng.NextDouble()),axis = offset
            };

            // Color
            uv = surfaces[id.x].mesh.uv; 
            sampleUV = uv[tr[id.y]] * bary.x + uv[tr[id.y + 1]] * bary.y + uv[tr[id.y + 2]] * bary.z;
            var colr = surfaces[id.x].tex.GetPixelBilinear(sampleUV.x, sampleUV.y);
            samples[i].color = colr;
        }

        // VFX Setting
        vfxData = new Vector4[VfxCapacity * VectorsPerParticle];
        graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vfxData.Length, 16);
        graphicsBuffer.SetData(vfxData); fragmentEffect.startSeed = unchecked((uint)GameManager.rdseed);
        fragmentEffect.SetGraphicsBuffer(AssemblyDataId, graphicsBuffer);
        fragmentEffect.SetVector3(BoundsCenterId, Vector3.zero); fragmentEffect.SetVector3(BoundsSizeId, Vector3.one * 10);
        fragmentEffect.initialEventName = "OnPlay";
        fragmentEffect.Reinit(); fragmentEffect.pause = false;
        cUse = fragmentCount * VectorsPerParticle;

        initialized = true;
    }
    

    [HideInInspector] public bool IsPlaying { get; private set; }
    [HideInInspector] public float Progress { get; private set; } = 1;
    public int VisibleFragments { get; private set; }
    bool build, initialized = false;
    float elapsed = 0;
    Coroutine EffectCoroutine = null;
    IEnumerator EffectCor()
    {
        if (!initialized) yield break;
        fragmentRenderer.enabled = true;
        Progress = 0; IsPlaying = true; UpdateEffect();
        var rd = 1f / Mathf.Max(duration, 0.1f);


       

        while (Progress < 1 && IsPlaying)
        {
            elapsed += Time.deltaTime * rd; Progress = Mathf.Clamp01(elapsed);
            UpdateEffect();
            yield return null;
        }
        fragmentRenderer.enabled = false;
        IsPlaying = false;
        EffectCoroutine = null;
    }

    Matrix4x4 vp;
    Vector4 screen;
    Matrix4x4 worldToGrid, gridToWorld;
    Vector3 revealOrigin, travelDir;
    Vector3 direction, transverse, depth, reference;
    float revealHalfWidth;
    void UpdateEffect()
    {
        // Set Bound
        Bounds bounds = surfaces[0].renderer.bounds; foreach (var s in surfaces) bounds.Encapsulate(s.renderer.bounds);
        worldToGrid = transform.worldToLocalMatrix; gridToWorld = transform.localToWorldMatrix;
        direction = ToWorldDirection(assemblyDirection) * (build ? 1 : -1);
        revealOrigin = bounds.center;
        revealHalfWidth = Mathf.Max(ProjectedExtent(bounds, direction), 0.00001f);
        foreach (var s in surfaces)
        {
            s.renderer.GetPropertyBlock(block); block.SetFloat("_AssemblyEnabled", 1); block.SetFloat("_AssemblyProgress", Progress); block.SetFloat("_AssemblyBuild", build ? 1 : 0); block.SetFloat("_AssemblyDirection", 1); block.SetFloat("_AssemblyCell", cellPixels); block.SetFloat("_AssemblyNoise", cellDelay); block.SetMatrix("_AssemblyVP", vp); block.SetVector("_AssemblyScreen", screen);
            block.SetFloat("_AssemblySpatial", 1); block.SetFloat("_AssemblyCell3D", Mathf.Max(cellSize3D, 0.001f));
            block.SetMatrix("_AssemblyWorldToGrid", worldToGrid); block.SetMatrix("_AssemblyGridToWorld", gridToWorld);
            block.SetVector("_AssemblyAxis", new Vector4(direction.x, direction.y, direction.z, revealHalfWidth));
            block.SetVector("_AssemblyOrigin", revealOrigin); s.renderer.SetPropertyBlock(block);
        }

        VisibleFragments = 0;

        foreach (var s in surfaces) s.UpdatePose();
       
        float halfWidth = ProjectedExtent(bounds, travelDir);

        Array.Clear(vfxData, 0, cUse);

        for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
        {
            var sample = samples[sampleIndex];
            var s = surfaces[sample.surface]; if (!s.renderer.enabled || !s.renderer.gameObject.activeInHierarchy) continue;
            Vector3 a = s.GetWorldVertex(sample.a), b = s.GetWorldVertex(sample.b), c = s.GetWorldVertex(sample.c);
            Vector3 target = a * sample.bary.x + b * sample.bary.y + c * sample.bary.z;
            float threshold = Threshold(target);
            float flight = build ? (threshold - Progress) / 0.18f : (Progress - threshold) / 0.18f;
            if (flight < 0 || flight > 1) continue;
            float ease = flight * flight * (3 - 2 * flight);
            float plane = (build ? -1 : 1) * (halfWidth + scatterDistance * (1 + sample.offset.x * 0.1f));
            float along = Vector3.Dot(target - bounds.center, travelDir);
            Vector3 offset = travelDir * (plane - along)
                + transverse * (sample.offset.y * scatterDistance * streamSpread)
                + depth * (sample.offset.z * scatterDistance * streamSpread * 0.5f);
            Vector3 position = target + offset * ease;
            int index = sampleIndex * VectorsPerParticle;
            vfxData[index] = new Vector4(position.x, position.y, position.z, vfxSizeMultiplier);
            vfxData[index + 1] = new Vector4(flight, 1, 0, 0);
            vfxData[index + 2] = sample.color;
            VisibleFragments++;
        }


        graphicsBuffer.SetData(vfxData,0,0, cUse);
        UpdateVfxBounds(bounds);
    }
    void UpdateVfxBounds(Bounds worldBounds)
    {
        // Includes the incoming/outgoing plane, small stream jitter and the graph's visual size.
        worldBounds.Expand(2 * (Mathf.Max(scatterDistance, 0) * (1.2f + streamSpread * 2) + Mathf.Max(vfxSizeMultiplier, 1)));
        var matrix = fragmentEffect.transform.worldToLocalMatrix;
        var localBounds = new Bounds(matrix.MultiplyPoint3x4(worldBounds.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            var corner = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            localBounds.Encapsulate(matrix.MultiplyPoint3x4(corner));
        }
        fragmentEffect.SetVector3(BoundsCenterId, localBounds.center);
        fragmentEffect.SetVector3(BoundsSizeId, localBounds.size);
    }
    float Threshold(Vector3 world)
    {
        float size = Mathf.Max(cellSize3D, 0.001f);
        Vector3 local = worldToGrid.MultiplyPoint3x4(world);
        Vector3 ThCell = new Vector3(Mathf.Floor(local.x / size), Mathf.Floor(local.y / size), Mathf.Floor(local.z / size));
        Vector3 center = gridToWorld.MultiplyPoint3x4((ThCell + Vector3.one * 0.5f) * size);
        float along = Mathf.Clamp01((Vector3.Dot(center - revealOrigin, direction) + revealHalfWidth) / (2 * revealHalfWidth));
        float hash = Mathf.Repeat(Mathf.Repeat(ThCell.x, 257) * 17 + Mathf.Repeat(ThCell.y, 257) * 131 + Mathf.Repeat(ThCell.z, 257) * 71 + 23, 257);
        float noise = Mathf.Repeat(hash * hash * 13 + hash * 7, 257) / 257f;
        return 0.2f + 0.6f * ((1 - cellDelay) * along + cellDelay * noise);

    }
    Vector3 ToWorldDirection(Vector3 value)
    {
        return transform.TransformDirection(value).normalized;
    }
    static float ProjectedExtent(Bounds bounds, Vector3 axis) { return Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z))); }
    
    void OnDestroy()
    {
        if (fragmentEffect) { fragmentEffect.enabled = false; Destroy(fragmentEffect.gameObject); }
        graphicsBuffer?.Dispose(); graphicsBuffer = null;
        foreach (var s in surfaces) if (s.mesh) Destroy(s.mesh);
    }
}
