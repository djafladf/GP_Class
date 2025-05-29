using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    List<Door> Doors = new List<Door>();
    bool IsUnknown = true;
    int Indx, Indy;

    public void Init(int x, int y)
    {
        Indx = x; Indy = y;
    }

    public void MakeWall(ref Vector4 tp)
    {
        Transform pr = transform.parent;
        for(int i = 0; i < 4; i++)
        {
            GameObject normal = pr.GetChild(2 * i + 1).gameObject, gate = pr.GetChild(2 * i + 2).gameObject;
            if (tp[i] == 1)
            {
                Doors.Add(gate.transform.GetChild(0).GetComponent<Door>());
                Destroy(normal);
            }
            else Destroy(gate);
        }
        ToggleAllDoor(true);
    }

    public void ToggleAllDoor(bool Type)
    {
        foreach (var j in Doors) j.LockToggle(Type);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.MapSetting(Indx,Indy,IsUnknown);
        }
    }
}
