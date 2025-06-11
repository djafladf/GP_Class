using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] List<Door> Doors;
    bool IsUnknown = true;
    public int Indx, Indy;

    private void Start()
    {
        if(Indx != GameManager.instance.bx || Indy != GameManager.instance.by)transform.parent.gameObject.SetActive(false);
    }

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
            if (tp[i] == 1) Destroy(normal);
            else Destroy(gate);
        }
        //ToggleAllDoor(true);
    }

    public void ToggleExtDorr(int type, bool Type)
    {
        Doors[type].LockToggle(Type);
    }


    public void ToggleAllDoor(bool Type)
    {
        foreach (var j in Doors) if(j != null) j.LockToggle(Type);
    }

    public void UnlockNearDoor()
    {
        GameManager.instance.OpenNearRoom(Indx, Indy);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.MapSetting(Indx,Indy,IsUnknown);
        }
    }
}
