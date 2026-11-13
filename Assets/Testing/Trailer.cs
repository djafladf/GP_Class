using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public struct TrailPoint
{
    public Vector3 Base, Tip;
    public float Time;

    public TrailPoint(Vector3 b, Vector3 t, float time)
    {
        Base = b;
        Tip = t;
        Time = time;
    }
}
public class Trailer : MonoBehaviour
{
    [SerializeField] Transform B, T;
    [SerializeField] float LifeTime = 0.2f,rLife;
    [SerializeField] float MinDistance = 0.005f;
    MeshRenderer mesh;
    MeshFilter filter;
    Mesh m;

    Queue<TrailPoint> points = new();
    List<Vector3> vert = new();
    List<Color> cols = new();
    List<Vector2> uvs = new();
    List<int> tra = new();

    private void Awake()
    {
        m = new Mesh(); m.name = "Trail"; m.MarkDynamic();
        filter = GetComponent<MeshFilter>(); filter.sharedMesh = m;
        mesh = GetComponent<MeshRenderer>();
        rLife = 1f / LifeTime;
    }

    private void Start()
    {
        StartCoroutine(Emit());
    }

    float strokeStartTime = 0;
    public void On()
    {
        strokeStartTime = Time.time;
        points.Clear(); m.Clear(); 
    }

    IEnumerator Emit()
    {
        float now,msqr = MinDistance * MinDistance;
        Vector3 b, t;
        bool ad;
        TrailPoint lst = new();
        while (true)
        {
            now = Time.time;

            b = B.position; t = T.position;
            if (points.Count == 0) { lst = new TrailPoint(b, t, now); points.Enqueue(lst); }
            else
            {
                ad = (b - lst.Base).sqrMagnitude > msqr || (t -  lst.Tip).sqrMagnitude > msqr;
                if (ad)
                {
                    lst = new TrailPoint(b, t, now); points.Enqueue(lst);
                }
            }

            while (points.Count > 0)
            {
                if (now - points.Peek().Time > LifeTime) points.Dequeue();
                else break; // FIFO니까?
            }
            if (points.Count < 2) m.Clear();
            else BuildMesh(now);

            yield return null;
        }
    }

    void BuildMesh(float now)
    {
        vert.Clear();
        cols.Clear();
        uvs.Clear();
        tra.Clear();
        int l = 0;
        float pc = 1f / (points.Count-1);
        Color col = Color.white;
        foreach (var p in points)
        {
            col.a = Mathf.Clamp01(1.4f - (now - p.Time)*rLife);
            col.a = 1;
            cols.Add(col); cols.Add(col);

            float v = l * pc;

            float taper = Mathf.SmoothStep(
                0f, 1f,
                Mathf.Clamp01(Mathf.Min(v, 1f - v) * 5f)
            );

            Vector3 b = Vector3.Lerp(p.Base, p.Tip, 0.4f);

            vert.Add(transform.InverseTransformPoint(b));
            vert.Add(transform.InverseTransformPoint(p.Tip));

            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));

            if (l > 0)
            {
                int n = l * 2;
                tra.Add(n - 2); tra.Add(n - 1); tra.Add(n);
                tra.Add(n - 1); tra.Add(n + 1); tra.Add(n);
            }
            l++;
        }
        m.Clear(); m.SetVertices(vert); m.SetColors(cols);
        m.SetUVs(0, uvs); m.SetTriangles(tra, 0);
        m.RecalculateBounds();
    }

    void OnDestroy()
    {
        if (m != null)
            Destroy(m);
    }

}
