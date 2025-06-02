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


    [Header("Gun")]
    public List<WeaponInfo> Weapon;

    [Header("EXP")]
    public List<GameObject> Exp;

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


    public Tuple<int,int,GameObject> ReturnItem(Transform parent)
    {
        int ct = 0;
        while (++ct < 100)
        {
            float z = Random.Range(0f, 1f);
            if (z < Prob[0]) // Normal
            {
                if (norm.Count != 0)
                {
                    GameObject cnt = Instantiate(Normal[norm[0]], parent); int ind = norm[0]; norm.RemoveAt(0);
                    return new Tuple<int,int, GameObject>(0,ind,cnt);
                }
            }
            else if (z < Prob[1])   // Rare
            {
                if (rare.Count != 0)
                {
                    GameObject cnt = Instantiate(Rare[rare[0]], parent); int ind = rare[0]; rare.RemoveAt(0);
                    return new Tuple<int,int, GameObject>(1, ind, cnt);
                }
            }
            else if (legd.Count != 0)
            {
                GameObject cnt = Instantiate(Legend[legd[0]], parent); int ind = legd[0]; legd.RemoveAt(0);
                return new Tuple<int,int, GameObject>(2, ind,cnt);
            }
        }
        return null;
    }

    public void RemoveItem(int Rarity, int ind)
    {
        if(ind >=3)
        {
            if (Rarity == 0) Normal.RemoveAt(ind);
            else if (Rarity == 1) Rare.RemoveAt(ind);
            else Legend.RemoveAt(ind);
        }
        else if(ind != 2)   // Apply Weapon
        {
            int cnt = ind * 3 + Rarity;
            GameManager.instance.PlayerScript.WeaponLevelUp(cnt);
        }
    }
}
