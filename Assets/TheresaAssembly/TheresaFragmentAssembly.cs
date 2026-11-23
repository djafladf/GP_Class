using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

// CPU skinned-surface sampling drives NormalParticleAssembly VFX via a structured buffer.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class TheresaFragmentAssembly : MonoBehaviour
{
    public enum AssemblyMode { Direction3D, ScreenHorizontal }
    public enum DirectionSpace { CharacterLocal, World }
    [Header("Assembly direction")]
    public AssemblyMode assemblyMode = AssemblyMode.Direction3D;
    public DirectionSpace directionSpace = DirectionSpace.CharacterLocal;
    [Tooltip("Travel direction, not a source position. (0,-1,0) assembles from above and exits below.")]
    public Vector3 assemblyDirection = Vector3.right;
    [Tooltip("3D cell size in the character root's local units. Independent of the camera and mesh UVs.")]
    [Min(0.001f)] public float cellSize3D = 0.05f;
    public Camera effectCamera;
    [Header("VFX appearance")]
    [Tooltip("Leave empty to load Resources/Theresa/NormalParticleAssembly automatically. Use the assembly copy, not the original graph.")]
    public VisualEffectAsset fragmentVfxAsset;
    [Min(0.001f)] public float vfxSizeMultiplier = 1;
    public Color vfxTint = Color.white;
    // Retained only for existing scene serialization / the original installer.
    [HideInInspector] public Material fragmentMaterial;
    [HideInInspector] public Mesh fragmentMesh;
    public bool playOnStart = false;
    [InspectorName("Reverse Direction")]
    public bool rightToLeft;
    [Min(0.1f)] public float duration = 2.5f;
    [Tooltip("Cell size in pixels, only used by Screen Horizontal mode.")]
    [Range(4,64)] public float cellPixels = 18;
    [Range(0,1)] public float cellDelay = 0.2f;
    [Range(100,5000)] public int fragmentCount = 1600;
    [Min(0.01f)] public float scatterDistance = 1.4f;
    [Tooltip("Use the assembly direction for fragment travel. Particles enter from its negative side and leave toward its positive side.")]
    public bool followRevealDirection = true;
    [Tooltip("Screen-space travel direction when Follow Reveal Direction is off. (1,0) travels right; (0,-1) travels down.")]
    public Vector2 fragmentTravelDirection = Vector2.right;
    [Tooltip("3D particle travel override when Follow Reveal Direction is off. Uses Direction Space.")]
    public Vector3 fragmentTravelDirection3D = Vector3.right;
    [Range(0,0.3f)] public float streamSpread = 0.06f;
    [HideInInspector] public float fragmentSize = 0.025f;
    [HideInInspector] public Color fragmentColor = new Color(1,0.72f,0.84f,1);
    public int seed = 173;
    public bool IsPlaying { get; private set; }
    public float Progress { get; private set; } = 1;
    public int VisibleFragments { get; private set; }
    bool build = true, initialized;
    float elapsed;
    Matrix4x4 vp;
    Vector4 screen;
    Matrix4x4 worldToGrid, gridToWorld;
    Vector3 revealDirection, revealOrigin;
    float revealHalfWidth;
    MaterialPropertyBlock block;
    // Must match the graph's single burst, capacity, and HLSL particleId modulus.
    const int VfxCapacity = 8192;
    const int VectorsPerParticle = 3;
    static readonly int AssemblyDataId = Shader.PropertyToID("AssemblyData");
    static readonly int BoundsCenterId = Shader.PropertyToID("AssemblyBoundsCenter");
    static readonly int BoundsSizeId = Shader.PropertyToID("AssemblyBoundsSize");
    VisualEffect fragmentEffect;
    Renderer fragmentRenderer;
    GraphicsBuffer graphicsBuffer;
    Vector4[] vfxData;
    readonly List<Surface> surfaces = new List<Surface>();
    Sample[] samples;
    struct Sample { public int surface,a,b,c; public Vector3 bary,offset,axis; public float rotation; }
    sealed class Surface {
        public Renderer renderer; public Mesh mesh; public bool owned;
        public readonly List<Vector3> vertices = new List<Vector3>();
        public int[] triangles;
        public void Bake() { if(renderer is SkinnedMeshRenderer sk) sk.BakeMesh(mesh, true); mesh.GetVertices(vertices); }
    }
    void Start(){ if(playOnStart) Assemble(); }
    void Initialize() {
        if(initialized)return;
        if(!effectCamera)effectCamera=Camera.main;
        if(!effectCamera && assemblyMode==AssemblyMode.ScreenHorizontal)throw new InvalidOperationException("Assign an effect camera for Screen Horizontal mode.");
        var asset=fragmentVfxAsset?fragmentVfxAsset:Resources.Load<VisualEffectAsset>("Theresa/NormalParticleAssembly");
        if(!asset)throw new InvalidOperationException("NormalParticleAssembly.vfx is missing. Assign its assembly copy in Fragment Vfx Asset.");
        block=new MaterialPropertyBlock();
        foreach(var r in GetComponentsInChildren<Renderer>(true)) {
            Mesh mesh=null; bool owned=false;
            if(r is SkinnedMeshRenderer sk && sk.sharedMesh) {mesh=new Mesh(); owned=true; sk.BakeMesh(mesh, true);}
            else if(r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var mf))mesh=mf.sharedMesh;
            if(!mesh)continue;
            var s=new Surface{renderer=r,mesh=mesh,owned=owned,triangles=mesh.triangles}; s.Bake(); surfaces.Add(s);
        }
        if(surfaces.Count==0)throw new InvalidOperationException("No mesh surfaces found.");
        // Area-weighted triangle selection across every skinned part, including the horns.
        var weights=new List<float>(); var ids=new List<Vector2Int>(); float total=0;
        for(int j=0;j<surfaces.Count;j++) {
            var s=surfaces[j]; var m=s.renderer.localToWorldMatrix;
            for(int k=0;k<s.triangles.Length;k+=3) {
                Vector3 a=m.MultiplyPoint3x4(s.vertices[s.triangles[k]]), b=m.MultiplyPoint3x4(s.vertices[s.triangles[k+1]]), c=m.MultiplyPoint3x4(s.vertices[s.triangles[k+2]]);
                total+=Vector3.Cross(b-a,c-a).magnitude*0.5f; weights.Add(total); ids.Add(new Vector2Int(j,k));
            }
        }
        if(total<=0)throw new InvalidOperationException("Mesh has no surface area.");
        var rng=new System.Random(seed); samples=new Sample[Mathf.Clamp(fragmentCount,1,VfxCapacity)];
        for(int i=0;i<samples.Length;i++) {
            float pick=(float)rng.NextDouble()*total; int lo=0,hi=weights.Count-1;
            while(lo<hi){int mid=(lo+hi)/2;if(weights[mid]<pick)lo=mid+1;else hi=mid;}
            var id=ids[lo];var tr=surfaces[id.x].triangles;float u=Mathf.Sqrt((float)rng.NextDouble()),v=(float)rng.NextDouble();
            var offset=new Vector3((float)rng.NextDouble()*2-1,(float)rng.NextDouble()*2-1,(float)rng.NextDouble()*2-1).normalized;
            samples[i]=new Sample{surface=id.x,a=tr[id.y],b=tr[id.y+1],c=tr[id.y+2],bary=new Vector3(1-u,u*(1-v),u*v),offset=offset*(0.6f+(float)rng.NextDouble()),axis=offset,rotation=(float)rng.NextDouble()*360};
        }
        vfxData=new Vector4[VfxCapacity*VectorsPerParticle];
        graphicsBuffer=new GraphicsBuffer(GraphicsBuffer.Target.Structured,vfxData.Length,16);
        graphicsBuffer.SetData(vfxData);
        var go=new GameObject("Theresa Assembly VFX");go.SetActive(false);go.layer=gameObject.layer;go.transform.SetParent(transform,false);
        fragmentEffect=go.AddComponent<VisualEffect>();fragmentEffect.visualEffectAsset=asset;
        fragmentEffect.resetSeedOnPlay=false;fragmentEffect.startSeed=unchecked((uint)seed);
        fragmentEffect.SetGraphicsBuffer(AssemblyDataId,graphicsBuffer);
        fragmentEffect.SetVector3(BoundsCenterId,Vector3.zero);fragmentEffect.SetVector3(BoundsSizeId,Vector3.one*10);
        fragmentRenderer=go.GetComponent<Renderer>();
        if(fragmentRenderer){fragmentRenderer.shadowCastingMode=ShadowCastingMode.Off;fragmentRenderer.receiveShadows=false;fragmentRenderer.enabled=false;}
        go.SetActive(true);
        fragmentEffect.Reinit();fragmentEffect.pause=false;
        initialized=true;
    }
    [ContextMenu("Assemble")] public void Assemble(){Begin(true);}
    [ContextMenu("Disassemble")] public void Disassemble(){Begin(false);}
    void Begin(bool assembling){Initialize();build=assembling;elapsed=0;Progress=0;IsPlaying=true;UpdateEffect();}
    // Deterministic scrubbing is also used by the Editor preview and verification.
    public void SetProgress(float progress,bool assembling){Initialize();build=assembling;Progress=Mathf.Clamp01(progress);IsPlaying=false;UpdateEffect();}
    void LateUpdate(){if(!initialized)return;if(IsPlaying){elapsed+=Time.deltaTime;Progress=Mathf.Clamp01(elapsed/Mathf.Max(duration,0.1f));if(Progress>=1)IsPlaying=false;}UpdateEffect();}
    void UpdateEffect(){
        bool spatial=assemblyMode==AssemblyMode.Direction3D;
        if(!spatial && !effectCamera)return;
        Bounds bounds=surfaces[0].renderer.bounds; foreach(var s in surfaces)bounds.Encapsulate(s.renderer.bounds);
        worldToGrid=transform.worldToLocalMatrix;gridToWorld=transform.localToWorldMatrix;
        revealDirection=ToWorldDirection(assemblyDirection)*(rightToLeft?-1:1);
        revealOrigin=bounds.center;
        revealHalfWidth=Mathf.Max(ProjectedExtent(bounds,revealDirection),0.00001f);
        if(!spatial){
            vp=effectCamera.projectionMatrix*effectCamera.worldToCameraMatrix;
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            for(int i=0;i<8;i++){var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var v=effectCamera.WorldToViewportPoint(p);left=Mathf.Min(left,v.x);right=Mathf.Max(right,v.x);}
            screen=new Vector4(Mathf.Max(1,effectCamera.pixelWidth),Mathf.Max(1,effectCamera.pixelHeight),left,right);
        }
        foreach(var s in surfaces){
            s.renderer.GetPropertyBlock(block);block.SetFloat("_AssemblyEnabled",1);block.SetFloat("_AssemblyProgress",Progress);block.SetFloat("_AssemblyBuild",build?1:0);block.SetFloat("_AssemblyDirection",rightToLeft?1:0);block.SetFloat("_AssemblyCell",cellPixels);block.SetFloat("_AssemblyNoise",cellDelay);block.SetMatrix("_AssemblyVP",vp);block.SetVector("_AssemblyScreen",screen);
            block.SetFloat("_AssemblySpatial",spatial?1:0);block.SetFloat("_AssemblyCell3D",Mathf.Max(cellSize3D,0.001f));
            block.SetMatrix("_AssemblyWorldToGrid",worldToGrid);block.SetMatrix("_AssemblyGridToWorld",gridToWorld);
            block.SetVector("_AssemblyAxis",new Vector4(revealDirection.x,revealDirection.y,revealDirection.z,revealHalfWidth));
            block.SetVector("_AssemblyOrigin",revealOrigin);s.renderer.SetPropertyBlock(block);
        }
        VisibleFragments=0;
        Array.Clear(vfxData,0,vfxData.Length);
        if(Progress>0 && Progress<1){
            foreach(var s in surfaces)s.Bake();
            Vector3 direction,transverse,depth;
            if(spatial){
                direction=followRevealDirection?revealDirection:ToWorldDirection(fragmentTravelDirection3D);
                Vector3 reference=Mathf.Abs(Vector3.Dot(direction,Vector3.up))>0.95f?Vector3.right:Vector3.up;
                transverse=Vector3.Cross(direction,reference).normalized;
                depth=Vector3.Cross(direction,transverse).normalized;
            }else{
                Vector2 travel=followRevealDirection ? (rightToLeft?Vector2.left:Vector2.right) : fragmentTravelDirection;
                if(travel.sqrMagnitude<0.0001f)travel=Vector2.right;
                travel.Normalize();
                direction=effectCamera.transform.right*travel.x+effectCamera.transform.up*travel.y;
                transverse=effectCamera.transform.right*(-travel.y)+effectCamera.transform.up*travel.x;
                depth=effectCamera.transform.forward;
            }
            // All origins lie on one plane beyond the character, rather than a sphere around each target.
            float halfWidth=ProjectedExtent(bounds,direction);
            for(int sampleIndex=0;sampleIndex<samples.Length;sampleIndex++){
                var sample=samples[sampleIndex];
                var s=surfaces[sample.surface]; if(!s.renderer.enabled || !s.renderer.gameObject.activeInHierarchy)continue;
                Vector3 target=s.renderer.transform.TransformPoint(s.vertices[sample.a]*sample.bary.x+s.vertices[sample.b]*sample.bary.y+s.vertices[sample.c]*sample.bary.z);
                float threshold=Threshold(target);
                float flight=build?(threshold-Progress)/0.18f:(Progress-threshold)/0.18f;
                if(flight<0 || flight>1)continue;
                float ease=flight*flight*(3-2*flight);
                float plane=(build?-1:1)*(halfWidth+scatterDistance*(1+sample.offset.x*0.1f));
                float along=Vector3.Dot(target-bounds.center,direction);
                Vector3 offset=direction*(plane-along)
                    +transverse*(sample.offset.y*scatterDistance*streamSpread)
                    +depth*(sample.offset.z*scatterDistance*streamSpread*0.5f);
                Vector3 position=target+offset*ease;
                int index=sampleIndex*VectorsPerParticle;
                // Stable slot identity keeps graph randomness and curves consistent while samples appear/disappear.
                vfxData[index]=new Vector4(position.x,position.y,position.z,vfxSizeMultiplier);
                // Surface distance doubles as curve time: assembly 1 -> 0, disassembly 0 -> 1.
                vfxData[index+1]=new Vector4(flight,1,0,0);
                vfxData[index+2]=new Vector4(vfxTint.r,vfxTint.g,vfxTint.b,vfxTint.a);
                VisibleFragments++;
            }
        }
        graphicsBuffer.SetData(vfxData);
        UpdateVfxBounds(bounds);
        // Keep GPU updates running; AssemblyRead supplies position and age each frame.
        if(fragmentRenderer)fragmentRenderer.enabled=VisibleFragments>0;
    }
    void UpdateVfxBounds(Bounds worldBounds){
        // Includes the incoming/outgoing plane, small stream jitter and the graph's visual size.
        worldBounds.Expand(2*(Mathf.Max(scatterDistance,0)*(1.2f+streamSpread*2)+Mathf.Max(vfxSizeMultiplier,1)));
        var matrix=fragmentEffect.transform.worldToLocalMatrix;
        var localBounds=new Bounds(matrix.MultiplyPoint3x4(worldBounds.center),Vector3.zero);
        for(int i=0;i<8;i++){
            var corner=worldBounds.center+Vector3.Scale(worldBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            localBounds.Encapsulate(matrix.MultiplyPoint3x4(corner));
        }
        fragmentEffect.SetVector3(BoundsCenterId,localBounds.center);
        fragmentEffect.SetVector3(BoundsSizeId,localBounds.size);
    }
    float Threshold(Vector3 world){
        if(assemblyMode==AssemblyMode.Direction3D){
            float size=Mathf.Max(cellSize3D,0.001f);
            Vector3 local=worldToGrid.MultiplyPoint3x4(world);
            Vector3 ThCell=new Vector3(Mathf.Floor(local.x/size),Mathf.Floor(local.y/size),Mathf.Floor(local.z/size));
            Vector3 center=gridToWorld.MultiplyPoint3x4((ThCell+Vector3.one*0.5f)*size);
            float along=Mathf.Clamp01((Vector3.Dot(center-revealOrigin,revealDirection)+revealHalfWidth)/(2*revealHalfWidth));
            // Bound each integer before hashing, matching the shader even for negative cells.
            float hash=Mathf.Repeat(Mathf.Repeat(ThCell.x,257)*17+Mathf.Repeat(ThCell.y,257)*131+Mathf.Repeat(ThCell.z,257)*71+23,257);
            float noise=Mathf.Repeat(hash*hash*13+hash*7,257)/257f;
            return 0.2f+0.6f*((1-cellDelay)*along+cellDelay*noise);
        }
        Vector4 q=vp*new Vector4(world.x,world.y,world.z,1);Vector2 uv=new Vector2(q.x,q.y)/Mathf.Max(q.w,0.00001f)*0.5f+Vector2.one*0.5f;
        float cell=Mathf.Max(cellPixels,1);float cx=Mathf.Floor(uv.x*screen.x/cell),cy=Mathf.Floor(uv.y*screen.y/cell);
        float x=Mathf.Clamp01(((cx+0.5f)*cell/screen.x-screen.z)/Mathf.Max(screen.w-screen.z,0.00001f));if(rightToLeft)x=1-x;
        float h=Mathf.Repeat(cx*17+cy*131+23,257);float n=Mathf.Repeat(h*h*13+h*7,257)/257f;return 0.2f+0.6f*((1-cellDelay)*x+cellDelay*n);
    }
    Vector3 ToWorldDirection(Vector3 value){
        if(value.sqrMagnitude<0.0001f)value=Vector3.right;
        return (directionSpace==DirectionSpace.CharacterLocal?transform.TransformDirection(value):value).normalized;
    }
    static float ProjectedExtent(Bounds bounds,Vector3 axis){return Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z)));}
    void OnDisable(){if(!initialized)return;foreach(var s in surfaces){if(!s.renderer)continue;s.renderer.GetPropertyBlock(block);block.SetFloat("_AssemblyEnabled",0);s.renderer.SetPropertyBlock(block);}VisibleFragments=0;if(fragmentRenderer)fragmentRenderer.enabled=false;}
    void OnDestroy(){
        if(fragmentEffect){fragmentEffect.enabled=false;Destroy(fragmentEffect.gameObject);}
        graphicsBuffer?.Dispose();graphicsBuffer=null;
        foreach(var s in surfaces)if(s.owned && s.mesh)Destroy(s.mesh);
    }
}
