using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public int id;
    public System.Action<int> UseEnd;
    [SerializeField] Transform B, T;
    [SerializeField] float LifeTime = 0.2f,rLife;
    [SerializeField] float MinDistance = 0.005f;
    MeshRenderer mesh;
    MeshFilter filter;
    Mesh m;
    Material mt;

    Queue<TrailPoint> points = new();
    List<Vector3> vert = new();
    List<Color> cols = new();
    List<Vector2> uvs = new();
    List<int> tra = new();

    private void Awake()
    {
        m = new Mesh(); m.name = "Trail"; m.MarkDynamic();
        filter = GetComponent<MeshFilter>(); filter.sharedMesh = m;
        mesh = GetComponent<MeshRenderer>(); mt = new Material(mesh.sharedMaterial);
        mesh.material = mt;
        rLife = 1f / LifeTime;
    }

    public void On()
    {
        points.Clear(); m.Clear(); IsEmit = true;
        if (Emiter == null) Emiter = StartCoroutine(Emit());
    }
    public void Off()
    {
        IsEmit = false;
    }
    bool IsEmit = false;
    Coroutine Emiter = null;
    IEnumerator Emit()
    {
        float now,msqr = MinDistance * MinDistance;
        Vector3 b, t;
        bool ad;
        TrailPoint lst = new();
        while (IsEmit)
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

        while(points.Count >=2)
        {
            now = Time.time;
            while (points.Count > 0)
            {
                if (now - points.Peek().Time > LifeTime) points.Dequeue();
                else break; // FIFO니까?
            }
            if (points.Count < 2) break;
            else BuildMesh(now);
            yield return null;
        }
        points.Clear();
        m.Clear(); Emiter = null;
        UseEnd?.Invoke(id);
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
            cols.Add(col); cols.Add(col);

            float v = l * pc;

            vert.Add(transform.InverseTransformPoint(p.Base));
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
        {
            Destroy(mt);
            Destroy(m);
        }
    }

}
