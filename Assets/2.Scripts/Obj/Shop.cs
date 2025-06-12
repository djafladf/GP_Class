using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Shop : MonoBehaviour
{
    public int[] ItemCost = new int[3];
    int[] RarityCost = { 500, 1000, 1500 };

    Tuple<int, int>[] CurItem = { null, null, null }; 
    private void Start()
    {
        GameManager.instance.Data.ResetPool();
        for(int i = 0; i < 3; i++)
        {
            var cnt = GameManager.instance.Data.ReturnItem(transform.GetChild(i).GetChild(0),GameManager.instance.CurDepth * 0.1f);
            CurItem[i] = new Tuple<int, int>(cnt.Item1,cnt.Item2);
            ItemCost[i] = RarityCost[cnt.Item1];
        }
    }

    public void Purchase(int ind)
    {
        GameManager.instance.Data.RemoveItem(CurItem[ind].Item1, CurItem[ind].Item2);
    }

    public bool CanBuyAble(int i)
    {
        return ItemCost[i] < GameManager.instance.UI.CurScore;
    }
}
