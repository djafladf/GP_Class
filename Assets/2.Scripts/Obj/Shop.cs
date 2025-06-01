using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Shop : MonoBehaviour
{
    public int[] ItemCost = new int[3];
    int[] RarityCost = { 100, 300, 750 };
    private void Start()
    {
        GameManager.instance.Data.ResetPool();
        for(int i = 0; i < 3; i++)
        {
            var cnt = GameManager.instance.Data.ReturnItem(transform.GetChild(i).GetChild(0));
            ItemCost[i] = RarityCost[cnt.Item1];
        }
    }


    public bool CanBuyAble(int i)
    {
        return ItemCost[i] < GameManager.instance.UI.CurScore;
    }
}
