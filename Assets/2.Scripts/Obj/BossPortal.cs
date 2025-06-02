using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPortal : MonoBehaviour
{
    [SerializeField] GameObject Gundam;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(InterAct, true, "Press<sprite name=\"e\"> To Interact");
        }
    }

    public void InterAct()
    {
        GameManager.instance.Player.transform.position = new Vector3(-200, 0, -200);
        Instantiate(Gundam, GameManager.instance.Enemy.transform); Gundam.transform.position = new Vector3(-200, 0, -230);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(null, false, null);
        }
    }
}
