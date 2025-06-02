using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Altar : MonoBehaviour
{
    [SerializeField] MapController MyMap;
    [SerializeField] int AltarType;
    [SerializeField] List<SpawnType> st;
    [SerializeField] List<Transform> SpawnPos;

    [SerializeField] GameObject Walls;
    [SerializeField] ParticleSystem WaveEffect;
    [SerializeField] ParticleSystem Unlock;
    [SerializeField] ParticleSystem ClearEffect;
    [SerializeField] Light SphereLight;

    AudioSource ad;
    BoxCollider InteractField;

    SpawnType ST;
    private void Awake()
    {
        MyMap = transform.parent.parent.GetChild(0).GetComponent<MapController>();
        InteractField = GetComponent<BoxCollider>();
        Wall1 = Walls.transform.GetChild(0); Wall2 = Walls.transform.GetChild(1);
        ad = GetComponent<AudioSource>();
        ST = st[Random.Range(0, st.Count)]; ST.SpawnPos = SpawnPos;
    }

    private void Start()
    {
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
            foreach (var j in SpawnPos) j.gameObject.SetActive(true);
            GameManager.instance.Enemy.StartMaking(ST);
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
        ad.time = 69; ad.Play();
        for (int i = 0; i < 35; i++)
        {
            if (i == 30) ClearEffect.Play();
            SphereLight.intensity = i * 5;
            yield return GameManager.DotOne;
        }
        GameManager.instance.UI.ShowAscending("Mission Clear!",2);
        if (AltarType == 0) { GameManager.instance.Enemy.KillAll(); foreach (var j in ST.SpawnPos) j.gameObject.SetActive(false); }
        SphereLight.intensity = 0;
        MyMap.UnlockNearDoor();
        Walls.SetActive(false);

        var cnt = GameManager.instance.Data.ReturnItem(GameManager.instance.ParticleSet); cnt.Item3.AddComponent<DropItem>(); cnt.Item3.transform.localScale = Vector3.one * 2;
        cnt.Item3.GetComponent<DropItem>().Init(cnt.Item1, cnt.Item2); cnt.Item3.transform.position = transform.position + new Vector3(10 + Random.Range(-1f,1f), 0.5f, Random.Range(-1f, 1f));
        for (int i = 0; i < Random.Range(5, 10); i++) { var tmp = Instantiate(GameManager.instance.Data.Exp[0], GameManager.instance.ParticleSet); tmp.transform.position = transform.position + new Vector3(10 + Random.Range(-1f, 1f), 0.1f, Random.Range(-1f, 1f)); }
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
        ad.time = 4;  ad.Play();
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
        ad.Stop();
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
