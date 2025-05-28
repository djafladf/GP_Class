using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class Event : UnityEvent<BaseEventData> { }

[System.Serializable]
public struct SpawnType
{
    public int LastTime;
    public List<float> StartTime;
    public List<float> SpawnGap;
    public List<int> EnemyID;
    public List<Transform> SpawnPos;
}

[System.Serializable]
public class ItemType
{

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