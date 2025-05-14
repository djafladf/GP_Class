using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealPond : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(Heal, true, "Press <color=red>E</color> To Interact");
        }
    }

    public void Heal()
    {
        GameManager.instance.UI.ShowAscending("All <color=red>HP</color> has been regained..",1);
        GameManager.instance.PlayerHealFunc.Invoke(10000);
        gameObject.SetActive(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(null, false, null);
        }
    }

    public void InterAct()
    {

    }
}
