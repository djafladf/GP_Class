
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;

public class SmokeTest : MonoBehaviour
{

    [SerializeField] Vector3Int Resolution = default;
    [Header("X Scale Base")]
    [SerializeField] int ResInt;
    [SerializeField] float RestoreSpeed;
    float rRes = 1;

    Texture3D Tex;
    Color[] densities;
    List<int> restorePoint = new();
    float[] maxdens;
    bool[] resCache;
    bool[] cPoint;
    Material Mat;


    
    IEnumerator cntEffect()
    {
        GetComponent<Collider>().enabled = false;
        List<Vector3Int> dfs = new();
        dfs.Add(Resolution/2);
        float mRes = ResInt * 0.5f;
        bool[,,] visit = new bool[Resolution.x,Resolution.y,Resolution.z];
        Vector3Int[] dp = new Vector3Int[26];
        int l = 0;
        for (int z = -1; z <= 1; z++)for (int y = -1; y <= 1; y++)for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0 && z == 0) continue;

                    dp[l++] = new Vector3Int(x, y, z);
                }
        int start = 0;
        float radiusSq;

        for (int i = 0; i < mRes; i++)
        {
            int end = dfs.Count;
            radiusSq = (i+1) * (i+1);
            for (int n = 0; n < end; n++)
            {
                var p = dfs[n];
                int id = Flatten(p.x, p.y, p.z);

                densities[id].r = Mathf.Min(
                    1f,
                    densities[id].r + 0.075f
                );
                maxdens[id] = Mathf.Max(maxdens[id], densities[id].r);
            }
            for (int n = start; n < end; n++)
            {
                var p = dfs[n];

                foreach (var dir in dp)
                {
                    var next = p + dir;

                    if (next.x < 0 || next.x >= Resolution.x ||
                        next.y < 0 || next.y >= Resolution.y ||
                        next.z < 0 || next.z >= Resolution.z)
                        continue;

                    if (visit[next.x, next.y, next.z])
                        continue;
                    if (new Vector3(next.x - mRes,next.y - mRes, next.z - mRes).sqrMagnitude > radiusSq) continue;

                    visit[next.x, next.y, next.z] = true;
                    dfs.Add(next);
                }
            }

            start = end;
            UploadGrid();
            yield return null;

        }
        // 그냥 편하게 첫 생성시에는 고려안함.
        GetComponent<Collider>().enabled = true;
    }

    private void Start()
    {
        CreateGrid();
        StartCoroutine(cntEffect());
    }

    // 3d -> 평탄화
    private int Flatten(int x, int y, int z)
    {
        return x + y * Resolution.x + z * Resolution.x * Resolution.y;
    }


    private void CreateGrid()
    {
        Resolution.x = ResInt; Resolution.y = ResInt * Mathf.FloorToInt(transform.localScale.y / transform.localScale.x); Resolution.z = ResInt * Mathf.FloorToInt(transform.localScale.z / transform.localScale.x);
        rRes = 1f / ResInt;
        Tex = new Texture3D(Resolution.x, Resolution.y, Resolution.z, TextureFormat.RGBA32, false);

        Tex.wrapMode = TextureWrapMode.Clamp;
        Tex.filterMode = FilterMode.Trilinear;

        int fl = Resolution.x * Resolution.y * Resolution.z;
        densities = new Color[fl];
        resCache = new bool[fl];
        cPoint = new bool[fl];
        maxdens = new float[fl];
        for (int z = 0; z < Resolution.z; z++) for (int y = 0; y < Resolution.y; y++) for (int x = 0; x < Resolution.x; x++) { densities[Flatten(x, y, z)] = new Color(0, 0, 0, 1); }
        if (!TryGetComponent<MeshRenderer>(out var t)) gameObject.AddComponent<MeshRenderer>();
        Mat = t.material;

        UploadGrid();
        Mat.SetTexture("_DensityTex", Tex);
    }

    // Texture 갱신
    private void UploadGrid()
    {
        Tex.SetPixels(densities);
        Tex.Apply(false, false);
    }

    private void OnDestroy()
    {
        if (Tex != null) Destroy(Tex); if (Mat != null) Destroy(Mat);
    }

    // 사전에 설정을 통해 LayerMask는 이미 되어있다고 가정
    HashSet<Collider> CurCol = new HashSet<Collider>();
    private void OnTriggerEnter(Collider other)
    {
        CurCol.Add(other);
    }
    private void OnTriggerExit(Collider other)
    {
        CurCol.Remove(other);
    }

    void CalcGrid(Bounds b)
    {
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new(
                (i & 1) == 0 ? b.min.x : b.max.x,
                (i & 2) == 0 ? b.min.y : b.max.y,
                (i & 4) == 0 ? b.min.z : b.max.z
            );
            Vector3 p = transform.InverseTransformPoint(corner);

            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        minG.x = Mathf.FloorToInt((min.x + 0.5f) * ResInt); minG.y = Mathf.FloorToInt((min.y + 0.5f) * ResInt); minG.z = Mathf.FloorToInt((min.z + 0.5f) * ResInt);
        maxG.x = Mathf.FloorToInt((max.x + 0.5f) * ResInt); maxG.y = Mathf.FloorToInt((max.y + 0.5f) * ResInt); maxG.z = Mathf.FloorToInt((max.z + 0.5f) * ResInt);
    }

    RenderTexture CreateDest(Vector3Int res)
    {
        var rt = new RenderTexture(res.x, res.y, 0, RenderTextureFormat.ARGBHalf)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = res.z, enableRandomWrite = true,
            filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp
        };
        rt.Create(); return rt;
    } 



    Vector3Int minG = default, maxG = default;
    bool calcN = false;
    int idt;

    private void Update()
    {
        calcN = false;
        Array.Clear(cPoint, 0, cPoint.Length);
        foreach (var j in CurCol)
        {
            CalcGrid(j.bounds);
            int mz = Mathf.Max(0, minG.z), Mz = Mathf.Min(Resolution.z - 1, maxG.z);
            int my = Mathf.Max(0, minG.y), My = Mathf.Min(Resolution.y - 1, maxG.y);
            int mx = Mathf.Max(0, minG.x), Mx = Mathf.Min(Resolution.x - 1, maxG.x);
            float mmz = (mz + Mz + 1) *0.5f, mmy = (my + My + 1) *0.5f, mmx = (mx + Mx + 1) *0.5f;

            float inf=0,dist=0;
            for (; mz <= Mz; mz++) for (int y = my; y <= My; y++) for (int x = mx; x <= Mx; x++)
            {
                idt = Flatten(x, y, mz);
                if (maxdens[idt] < 0) continue;
                cPoint[idt] = true;
                if (densities[idt].r > 0) 
                {
                    dist = Mathf.Pow((mz - mmz)*rRes, 2) + Mathf.Pow((y - mmy)*rRes, 2) + Mathf.Pow((x - mmx)*rRes, 2);
                    inf = 1 - Mathf.Clamp01(dist);

                    inf = 1 - inf * inf * (3f - 2f * inf);
                    densities[idt].r = Mathf.Min(densities[idt].r, inf);
                    calcN = true; 
                }
                if (!resCache[idt]) { restorePoint.Add(idt); resCache[idt] = true; }
            }
        }
        for (int i = restorePoint.Count - 1; i >= 0; i--)
        {
            idt = restorePoint[i];
            if (cPoint[idt]) continue;
            densities[idt].r += Time.deltaTime * RestoreSpeed;
            if (densities[idt].r >= maxdens[idt])
            {
                densities[idt].r = maxdens[idt]; resCache[idt] = false;
                int lst = restorePoint.Count - 1;
                restorePoint[i] = restorePoint[lst];
                restorePoint.RemoveAt(lst);
            }
            calcN = true;
        }
        if (calcN) UploadGrid();
    }
}
