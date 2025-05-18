using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] List<Door> Doors;
    public bool IsUnknown = true;
    public int Indx, Indy;

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
