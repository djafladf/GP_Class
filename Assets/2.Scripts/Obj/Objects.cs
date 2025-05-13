using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Objects : MonoBehaviour
{
    bool OnMove = false;
    private void FixedUpdate()
    {
        if (OnMove)
        {
            Vector3 Dir = (GameManager.instance.Player.position - transform.position).normalized;
            transform.Translate(Dir * Time.deltaTime * 15);
        }
    }

    private void OnEnable()
    {
        OnMove = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false); GameManager.instance.UI.ExpChange(1);
        }
        else if (other.CompareTag("GainArea"))
        {
            OnMove = true;
        }
    }
}
