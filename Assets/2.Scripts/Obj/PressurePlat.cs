using System;
using UnityEngine;

public class PressurePlat : MonoBehaviour
{
    [SerializeField] GameObject ActiveObject;
    [SerializeField] AudioClip InterActSound;
    public Action<bool> PressureAct;

    bool OnActive = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnActive = OnActive == false;
            GameManager.instance.Audio.PlayClip(2, 2, InterActSound);
            if(PressureAct != null) PressureAct.Invoke(OnActive);
            ActiveObject.SetActive(OnActive);
        }
    }

    public void ResetFunc()
    {
        OnActive = false; ActiveObject.SetActive(false);
    }
}
