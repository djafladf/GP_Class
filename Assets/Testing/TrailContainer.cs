using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TrailContainer : MonoBehaviour
{
    [SerializeField] GameObject InstObj;
    List<Trailer> Trails = new();
    int PN = 3;

    Queue<int> UnUse = new();
    Queue<int> OnUse = new();

    private void Awake()
    {
        Trails.Add(InstObj.GetComponent<Trailer>()); UnUse.Enqueue(0);
        for(int i = 0; i <PN-1; i++) 
        { 
            var t = Instantiate(InstObj,transform);
            Trails.Add(t.GetComponent<Trailer>()); UnUse.Enqueue(i + 1);
        }
        for(int i = 0; i < PN; i++)
        {
            Trails[i].id = i; Trails[i].UseEnd = UseEnd;
        }
    }
    public void UseEnd(int i)
    {
        UnUse.Enqueue(i);
    }

    public void On()
    {
        if (!UnUse.TryDequeue(out var p)) return;
        Trails[p].On(); OnUse.Enqueue(p);
    }

    public void Off()
    {
        if (!OnUse.TryDequeue(out var p)) return;
         Trails[p].Off();
    }

    public void OnDestroy()
    {
        foreach (var j in Trails) Destroy(j.gameObject);
    }
}
