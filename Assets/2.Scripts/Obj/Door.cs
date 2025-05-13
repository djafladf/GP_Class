using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Transform DoorLeft, DoorRight;


    [SerializeField] float Process = 0;  // 0 : Close , 1 : Open
    [SerializeField] float Trigger = 1;  //-1 : Close , 1 : Open
    WaitForSeconds wfs = new WaitForSeconds(0.05f);
    IEnumerator DoorChange()
    {
        while (Process >= 0 && Process <= 1)
        {
            yield return wfs;
            DoorLeft.transform.localPosition = new Vector3(0, 0, DoorLeft.transform.localPosition.z + 0.0475f * Trigger);
            DoorRight.transform.localPosition = new Vector3(0,-0.15f,DoorRight.transform.localPosition.z - 0.0475f * Trigger);
            Process += 0.025f * Trigger;
        }
        if(Trigger == 1)
        {
            DoorLeft.transform.localPosition = new Vector3(0, 0, 2.8f); DoorRight.transform.localPosition = new Vector3(0,-0.15f, -2.4f); Process = 1;
        }
        else
        {
            DoorLeft.transform.localPosition = new Vector3(0, 0, 0.9f); DoorRight.transform.localPosition = new Vector3(0, -0.15f, -0.5f); Process = 0;
        }
        DoorToggle = null;
    }

    public bool IsLock = false;
    Coroutine DoorToggle = null;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !IsLock)
        {
            if (DoorToggle == null) DoorToggle = StartCoroutine(DoorChange());
            Trigger = 1;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !IsLock)
        {
            if (DoorToggle == null) DoorToggle = StartCoroutine(DoorChange());
            Trigger = -1;
        }
    }
}
