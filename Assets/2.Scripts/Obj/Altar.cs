using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Altar : MonoBehaviour
{
    [SerializeField] GameObject Walls;
    [SerializeField] ParticleSystem WaveEffect;
    [SerializeField] ParticleSystem Unlock;
    [SerializeField] Light SphereLight;

    BoxCollider InteractField;

    private void Awake()
    {
        InteractField = GetComponent<BoxCollider>();
        Wall1 = Walls.transform.GetChild(0); Wall2 = Walls.transform.GetChild(1);
    }

    bool OnWave = false;
    Transform Wall1, Wall2;
    private void FixedUpdate()
    {
        if (OnWave)
        {
            Wall1.Rotate(new Vector3(0, Time.deltaTime * 10));
            Wall2.Rotate(new Vector3(0, Time.deltaTime * 10));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            print("Altar Interact Active!");
            GameManager.instance.UI.ToggleInteract(InterAct, true, "Press <color=red>E</color> To Interact");
        }
    }

    public void WaveStart()
    {
        
        GameManager.instance.UI.SetTimer(10, WaveEnd);
    }

    public void WaveEnd()
    {
        WaveEffect.Stop();
        Unlock.Play();
        StartCoroutine(UnLockAct());
    }

    IEnumerator UnLockAct()
    {
        for(int i = 0; i < 35; i++)
        {
            SphereLight.intensity = i * 5;
            yield return GameManager.DotOne;
        }
        SphereLight.intensity = 0;
        Walls.SetActive(false);
    }

    public void InterAct()
    {
        OnWave = true; WaveEffect.Play();
        GameManager.instance.UI.ToggleInteract(null, false, null);
        InteractField.enabled = false;
        StartCoroutine(CamShake(3, 5));
    }

    CinemachineTransposer transposer;
    IEnumerator CamShake(float time, float intensity = 1)    
    {
        Camera.main.GetComponent<CinemachineBrain>().enabled = false; ;
        WaitForSeconds LittleSec = new WaitForSeconds(0.05f);
        var MainCam = Camera.main;
        float Cx = MainCam.transform.position.x;
        float Cy = MainCam.transform.position.y;
        float Cz = MainCam.transform.position.z;
        int count = (int)(time * 6);
        for (int i = 0; i < count; i++)
        {
            MainCam.transform.position = new Vector3(Cx - 0.2f * intensity, Cy, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx + 0.2f * intensity, Cy, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy + 0.2f * intensity, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy - 0.2f * intensity, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy, Cz + 0.2f * intensity);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy, Cz - 0.2f * intensity);
            yield return LittleSec;
        }
        Camera.main.GetComponent<CinemachineBrain>().enabled = true;
        GameManager.instance.UI.ShowAscending("Survive <color=red>150</color> Seconds",2,WaveStart);
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(null, false,null);
            print("Altar Interact UnActive!");
        }
    }

}
