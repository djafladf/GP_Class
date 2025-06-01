using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class DataManager : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] List<GameObject> Normal;
    [SerializeField] List<GameObject> Rare;
    [SerializeField] List<GameObject> Legend;
    [SerializeField] float[] Prob;
    List<int> norm, rare, legd;

    private void Awake()
    {
        GameManager.instance.Data = this;
    }

    public void ResetPool()
    {
        norm = Enumerable.Range(0, Normal.Count).OrderBy(x => Guid.NewGuid()).ToList();
        rare = Enumerable.Range(0, Rare.Count).OrderBy(x => Guid.NewGuid()).ToList();
        legd = Enumerable.Range(0, Legend.Count).OrderBy(x => Guid.NewGuid()).ToList();
    }


    public Tuple<int,GameObject> ReturnItem(Transform parent)
    {
        int ct = 0;
        while (++ct < 100)
        {
            float z = Random.Range(0f, 1f);
            if (z < Prob[0]) // Normal
            {
                if (norm.Count != 0)
                {
                    GameObject cnt = Instantiate(Normal[norm[0]], parent); norm.RemoveAt(0);
                    return new Tuple<int, GameObject>(0, cnt);
                }
            }
            else if (z < Prob[1])   // Rare
            {
                if (rare.Count != 0)
                {
                    GameObject cnt = Instantiate(Rare[rare[0]], parent); rare.RemoveAt(0);
                    return new Tuple<int, GameObject>(1, cnt);
                }
            }
            else if (legd.Count != 0)
            {
                GameObject cnt = Instantiate(Legend[legd[0]], parent); legd.RemoveAt(0);
                return new Tuple<int, GameObject>(2, cnt);
            }
        }
        return null;
    }
}
