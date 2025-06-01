using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [SerializeField] GameObject cnt;
    [SerializeField] LayerMask Mask;

    private void OnCollisionEnter(Collision collision)
    {
        if(((1 << collision.gameObject.layer) & Mask.value) != 0)
        {
            cnt.SendMessage("WeakPoint", SendMessageOptions.DontRequireReceiver);
        }
    }
}
