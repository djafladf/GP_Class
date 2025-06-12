using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class Event : UnityEvent<BaseEventData> { }

[System.Serializable]
public class WeaponInfo
{
    public string name;
    public int Rarity;
    public float power;
    public float bound;
    public float rpm;
    public float spread;
    public int bnum;
    public float MaxMag;
    public float CurMag;
    public int LV;
    public GameObject Obj;
    public Sprite Im;

    public string DescribeSelf()
    {
        string cnt = "";

        cnt = $@"<color=black><size=150%>{name}</size></color> <color=red><size=75%>Lv.{LV}</size></color>
Åº¼Ó : {power * 25}
¹Ýµ¿ : {bound * 0.02f}
¿¬»ç °£°Ý : {rpm * 0.1667f}
ÅºÆÛÁü : {spread}%
ÅºÃ¢ : {MaxMag}
1È¸ ¹ß»ç ¼ö : {bnum}";
        return cnt;
    }
}

[System.Serializable]
public struct SpawnType
{
    public string name;
    public float SpawnGap;
    public int EnemyID;
    public int MinLevel;
    public int MaxLevel;
}

[System.Serializable]
public class ItemType
{

}
[System.Serializable]
public class ForTestV4
{
    public List<Vector4> List;
}

[System.Serializable]
public class ForTestInt
{
    public List<int> List; 
}

[System.Serializable]
public class ForTestImage
{
    public List<Image> List;
}

// Map
[System.Serializable]
public class Cell
{
    public int col, row;
    public int Group = 0;
    public bool Right;
    public bool Left;
    public bool Up;
    public bool Down;
};