using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Altar : MonoBehaviour
{
    [SerializeField] MapController MyMap;
    [SerializeField] int AltarType;
    [SerializeField] SpawnType ST;

    [SerializeField] GameObject Walls;
    [SerializeField] ParticleSystem WaveEffect;
    [SerializeField] ParticleSystem Unlock;
    [SerializeField] ParticleSystem ClearEffect;
    [SerializeField] Light SphereLight;

    BoxCollider InteractField;

    private void Awake()
    {
        InteractField = GetComponent<BoxCollider>();
        Wall1 = Walls.transform.GetChild(0); Wall2 = Walls.transform.GetChild(1);
        
    }

    private void Start()
    {
        if (AltarType == 0)
        {
            GameManager.instance.Enemy.RegisterMonsterType(0, 150);
        }
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
            GameManager.instance.UI.ToggleInteract(InterAct, true, "Press<sprite name=\"e\"> To Interact");
        }
    }

    public void UnLockStart()
    {
        if (AltarType == 0)
        {
            foreach (var j in ST.SpawnPos) j.gameObject.SetActive(true);
            GameManager.instance.Enemy.StartMaking(ref ST);
            GameManager.instance.UI.SetTimer(ST.LastTime, WaveEnd);
        }
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
            if (i == 30) ClearEffect.Play();
            SphereLight.intensity = i * 5;
            yield return GameManager.DotOne;
        }
        GameManager.instance.UI.ShowAscending("Mission Clear!",2);
        if (AltarType == 0) { GameManager.instance.Enemy.KillAll(); foreach (var j in ST.SpawnPos) j.gameObject.SetActive(false); }
        SphereLight.intensity = 0;
        MyMap.ToggleAllDoor(true);
        Walls.SetActive(false);
    }

    public void InterAct()
    {
        MyMap.ToggleAllDoor(false);
        OnWave = true; WaveEffect.Play();
        GameManager.instance.UI.ToggleInteract(null, false, null);
        InteractField.enabled = false;
        StartCoroutine(CamShake(3, 8));
    }

    CinemachineTransposer transposer;
    IEnumerator CamShake(float time, float intensity = 1)    
    {
        Camera.main.GetComponent<CinemachineBrain>().enabled = false;
        WaitForSeconds LittleSec = new WaitForSeconds(0.05f);
        var MainCam = Camera.main;
        float Cx = MainCam.transform.position.x;
        float Cy = MainCam.transform.position.y;
        float Cz = MainCam.transform.position.z;
        int count = (int)(time * 6);
        float intSub = intensity / count;
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
            MainCam.transform.position = new Vector3(Cx, Cy, Cz);
            intensity -= intSub;
        }
        Camera.main.GetComponent<CinemachineBrain>().enabled = true;
        if(AltarType == 0)GameManager.instance.UI.ShowAscending($"Survive <color=red>{ST.LastTime}</color> Seconds",2,UnLockStart);
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(null, false,null);
        }
    }

}
