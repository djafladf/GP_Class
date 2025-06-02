using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropItem : MonoBehaviour
{
    int ind, rarity;
    public void Init(int rarity, int ind)
    {
        this.ind = ind; this.rarity = rarity;
    }

    public void InterAct()
    {
        GameManager.instance.UI.ToggleInteract(null, false, null);
        GameManager.instance.Data.RemoveItem(rarity, ind);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(InterAct, true, "Press<sprite name=\"e\"> To Get");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(null, false, null);
        }
    }
}
