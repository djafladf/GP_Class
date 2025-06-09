using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
[DefaultExecutionOrder(-999)]
public class DataManager : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] List<GameObject> Normal;
    [SerializeField] List<GameObject> Rare;
    [SerializeField] List<GameObject> Legend;
    [SerializeField] float[] Prob;
    public Color[] RarityColor;


    public GameObject Drone;

    [Header("Gun")]
    public List<WeaponInfo> Weapon;

    [Header("EXP")]
    public List<GameObject> Exp;

    List<int> norm, rare, legd;


    private void Awake()
    {
        GameManager.instance.Data = this;
    }

    private void Start()
    {
        RemoveItem(0, 0); RemoveItem(0, 1);
    }

    public void ResetPool()
    {
        norm = Enumerable.Range(0, Normal.Count).OrderBy(x => Guid.NewGuid()).ToList();
        rare = Enumerable.Range(0, Rare.Count).OrderBy(x => Guid.NewGuid()).ToList();
        legd = Enumerable.Range(0, Legend.Count).OrderBy(x => Guid.NewGuid()).ToList();
    }


    public Tuple<int, int, GameObject> ReturnItem(Transform parent)
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
                    return new Tuple<int, int, GameObject>(0, ind, cnt);
                }
            }
            else if (z < Prob[1])   // Rare
            {
                if (rare.Count != 0)
                {
                    GameObject cnt = Instantiate(Rare[rare[0]], parent); int ind = rare[0]; rare.RemoveAt(0);
                    return new Tuple<int, int, GameObject>(1, ind, cnt);
                }
            }
            else if (legd.Count != 0)
            {
                GameObject cnt = Instantiate(Legend[legd[0]], parent); int ind = legd[0]; legd.RemoveAt(0);
                return new Tuple<int, int, GameObject>(2, ind, cnt);
            }
        }
        return null;
    }

    [SerializeField] Transform InventTrans;
    [SerializeField] GameObject InventItem;
    int InventNum = 0;

    public void RemoveItem(int Rarity, int ind)
    {
        if (ind >= 3)
        {
            if (Rarity == 0) Normal.RemoveAt(ind);
            else if (Rarity == 1) Rare.RemoveAt(ind);
            else Legend.RemoveAt(ind);
        }
        else if (ind != 2)   // Apply Weapon
        {
            int cnt = ind * 3 + Rarity;
            switch (Weapon[cnt].LV)
            {
                case 0:
                    var tmp = Instantiate(InventItem, InventTrans); tmp.transform.localPosition = new Vector2(-457.5f + 130 * (InventNum % 8), (1 - Mathf.FloorToInt(InventNum / 8)) * 130f);
                    tmp.GetComponent<ItemView>().Init(Rarity, cnt, true, Weapon[cnt].Im); InventNum++;
                    GameManager.instance.PlayerScript.WeaponAdd(cnt);
                    break;
                case 1:
                    Weapon[cnt].MaxMag *= 2; break;
                case 2:
                    Weapon[cnt].bound *= 0.9f; break;
                case 3:     // Damage Increase
                    break;
                case 4:
                    Weapon[cnt].spread *= 0.9f; break;
                case 5:
                    Weapon[cnt].power *= 1.25f; break;
                case 6:
                    Weapon[cnt].bnum *= 2; break;

            }
            Weapon[cnt].LV++;
        }
        else
        {
            GameManager.instance.PlayerScript.DroneAdd();
        }
    }
}
