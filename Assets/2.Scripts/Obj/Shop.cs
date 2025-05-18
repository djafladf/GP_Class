using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public int[] ItemCost;

    public bool CanBuyAble(int i)
    {
        return ItemCost[i] < GameManager.instance.UI.CurScore;
    }

    public void SetItems()
    {

    }
}
