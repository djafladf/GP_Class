using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class Event : UnityEvent<BaseEventData> { }

[System.Serializable]
public class AnimCurve : ISerializationCallbackReceiver
{
    public string name;
    public string trigger;
    public float StUsement;
    [System.NonSerialized] public HashSet<CurveState> ComboAble;
    public ForTestFloat ComboTime;
    public List<CurveState> ComboAbleInspec;

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {
        ComboAble = new HashSet<CurveState>(ComboAbleInspec);
    }
}

public enum CurveState
{
    none,
    roll,
    jump,
    rattack,
    lattack,
    teleport
}

public enum DamageType
{
    Slash,
    Strike,
    Pierece
}
public enum Elemental 
{ 
    None,
    Fire,
    Originum
}


[System.Serializable]
public class AttackClip
{
    public float StUsement;
    public DamageType DamageType;
    public Elemental Elemental;
    public float DamageRatio;
    public AnimationClip Clip;
}

[System.Serializable]
public class WeaponInfo
{
    public string name;
    public int Rarity;
    public List<AttackClip> One;
    public List<AttackClip> Two;
    public AttackClip OneDash;
    public AttackClip TwoDash;

    public int returnCount(bool IsTwo){ return IsTwo ? Two.Count: One.Count; } 

    public int LV;
    public GameObject Obj;
    public Sprite Im;
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
public class ForTestFloat
{
    public List<float> List;
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