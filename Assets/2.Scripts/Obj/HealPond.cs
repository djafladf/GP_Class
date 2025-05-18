using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealPond : MonoBehaviour
{
    [SerializeField] int Cost = 100;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(Heal, true, string.Concat(
                "Press<sprite name=\"e\"> To Interact\n",
                $"<size=60><sprite name=\"Coin\"><color=#FEA040>{Cost}</color></size>"
                ));
        }
    }

    public void Heal()
    {
        if (GameManager.instance.UI.CurScore < Cost) GameManager.instance.UI.ShowAscending("No Enough <sprite name=\"Coin\">!", 1);
        else
        {
            GameManager.instance.UI.CurScore -= Cost;
            GameManager.instance.UI.ShowAscending("All <color=red>HP</color> has been regained..", 1);
            GameManager.instance.PlayerScript.BuffOn(Random.Range(1, 4), 120, 1);
            GameManager.instance.PlayerHealFunc.Invoke(10000);
            GameManager.instance.UI.ToggleInteract(null, false, null);
            gameObject.SetActive(false);
        }
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
