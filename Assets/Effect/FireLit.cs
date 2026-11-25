using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class FireLit : InteractThing
{
    // Player, NPC, Camera
    [SerializeField] List<Transform> Places;
    [SerializeField] Material mat;
    [SerializeField] VisualEffect vfx;
    [SerializeField] Transform Light;
    float lifeTime = 0f;
    Transform pattern;
    bool OnFire = false;

    Vector3 TheLook = default, PLook = default;
    protected override void Awake()
    {
        base.Awake();
        mat = new Material(mat); pattern = transform.GetChild(0);
        pattern.GetComponent<MeshRenderer>().sharedMaterial = mat;
        TheLook = transform.position - Places[1].position; TheLook.y = 0;  TheLook.Normalize();
        PLook = transform.position - Places[0].position; PLook.y = 0; PLook.Normalize();
    }

    //private void Start()
    //{
    //    GameManager.instance.Data.Theresia.Appear(Places[1], TheLook);
    //}

    WaitForSeconds wfs = new WaitForSeconds(7);
    IEnumerator tt()
    {
        lifeTime = 0;
        TriggerOff();
        vfx.SetFloat("Rad", 3 - lifeTime);
        mat.SetFloat("_LifeTime", lifeTime);
        vfx.Play();
        OnFire = true;
        while (lifeTime <= 3)
        {
            yield return null;
            lifeTime += Time.deltaTime;

            vfx.SetFloat("Rad", 3 - lifeTime);
            mat.SetFloat("_LifeTime", lifeTime);
            Light.transform.localScale = Vector3.one * (3 - lifeTime) * 5;
        }
        vfx.Stop();
        
        yield return GameManager.OneSec;
        Destroy(pattern.gameObject);
        Destroy(mat);
        transform.GetChild(1).gameObject.SetActive(true);
        GameManager.instance.UI.ShowBlackImage(0,7);
        yield return wfs;
        GameManager.instance.UI.OffBlackImage(); TriggerOn();
    }

    
    public void LitEnd()
    {
        StartCoroutine(tt());
    }

    public void RestSetting()
    {
        GameManager.instance.Camera.CamSetting(0, 75);
        GameManager.instance.Camera.CamSetting(0, transform, transform);
        GameManager.instance.Camera.CamSetting(false);
        GameManager.instance.Player.position = Places[0].position;
        GameManager.instance.Player.forward = PLook;
        GameManager.instance.Camera.ForceCameraMove(Places[2].position - transform.position);
        GameManager.instance.UI.FadeInOut(1, 3);
        GameManager.instance.UI.ToggleInUI(false);
        GameManager.instance.Data.Theresia.Appear(Places[1],TheLook);
    }

    // 나중에 MapManager같은거 나오면 거따가 넣어야될거같은디?
    public override void IFunc(System.Object var)
    {
        if (!OnFire) GameManager.instance.PlayerScript.LitStart(this);
        else GameManager.instance.PlayerScript.OnRest(transform);
    }

    private void OnDestroy()
    {
        if(mat != null) Destroy(mat);
    }
}
