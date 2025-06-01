using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GundamBul : MonoBehaviour
{
    [SerializeField] LayerMask Mask;
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer != Mask)
        {
            Invoke("LateDeath", 1f);
        }
    }

    void LateDeath()
    {
        Destroy(gameObject);
    }
}
