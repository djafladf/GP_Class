using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class FireLit : InteractThing
{
    [SerializeField] Transform RPlace;
    [SerializeField] Material mat;
    [SerializeField] VisualEffect vfx;
    [SerializeField] GameObject HitObj;
    float lifeTime = 0f;

    float deg1 = Mathf.PI / 3f;
    float deg2 = Mathf.PI * 2 / 3f;
    Transform pattern;
    Vector2 sub = default;
    bool OnFire = false;

    protected override void Awake()
    {
        base.Awake();
        mat = new Material(mat); pattern = transform.GetChild(0);
        pattern.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    IEnumerator tt()
    {
        lifeTime = 0;

        vfx.SetFloat("Rad", 3 - lifeTime);
        mat.SetFloat("_LifeTime", lifeTime);
        vfx.Play(); HitObj.SetActive(true);
        while (lifeTime <= 3)
        {
            yield return null;
            lifeTime += Time.deltaTime;

            vfx.SetFloat("Rad", 3 - lifeTime);
            mat.SetFloat("_LifeTime", lifeTime);
        }
        vfx.Stop();
        
        yield return GameManager.OneSec;
        Destroy(pattern.gameObject);
        Destroy(mat);
        transform.GetChild(1).gameObject.SetActive(true);
        OnFire = true;
    }

    
    public void LitEnd()
    {
        StartCoroutine(tt());
    }

    // 나중에 MapManager같은거 나오면 거따가 넣어야될거같은디?
    public override void IFunc(System.Object var)
    {
        if(!OnFire) GameManager.instance.PlayerScript.LitStart(this,RPlace);
        else // SetRest
        {

        }
    }

    private void OnDestroy()
    {
        if(mat != null) Destroy(mat);
    }
}
