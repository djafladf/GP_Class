using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    [SerializeField] int ind;
    Shop shopM;
    Animator Anim;

    private void Awake()
    {
        Anim = GetComponent<Animator>();
        shopM = transform.parent.GetComponent<Shop>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract
                (
                Purchase,
                true,
                string.Concat("Press<sprite name=\"e\"> To Buy\n",
                $"<size=60><sprite name=\"Coin\"><color=#FEA040>{shopM.ItemCost[ind]}</color></size>"
                )
                );
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract
                (
                null,
                false,
                null
                );
        }
    }

    public void Purchase()
    {
        if (shopM.ItemCost[ind] <= GameManager.instance.UI.CurScore) { Anim.SetTrigger("Close"); GameManager.instance.UI.ToggleInteract(null, false, null); shopM.Purchase(ind); }
        else { GameManager.instance.UI.ShowAscending("No Enough <sprite name=\"Coin\">!",1); }
    }

    void Close()
    {
        GetComponent<BoxCollider>().enabled = false; Destroy(this);
    }
}
