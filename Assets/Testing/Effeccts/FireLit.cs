using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class FireLit : MonoBehaviour
{
    [SerializeField] Material mat;
    [SerializeField] VisualEffect vfx;
    public float lifeTime = 0f;

    float deg1 = Mathf.PI / 3f;
    float deg2 = Mathf.PI * 2 / 3f;
    Transform pattern;
    Vector2 sub = default;

    private void Awake()
    {
        mat = new Material(mat); pattern = transform.GetChild(0);
        pattern.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    IEnumerator tt()
    {
        lifeTime = 0;

        vfx.SetFloat("Rad", 3 - lifeTime);
        mat.SetFloat("_LifeTime", lifeTime);
        vfx.Play();
        while (lifeTime <= 3)
        {
            yield return null;
            lifeTime += Time.deltaTime;

            vfx.SetFloat("Rad", 3 - lifeTime);
            mat.SetFloat("_LifeTime", lifeTime);
        }
        vfx.Stop();
        Destroy(mat);
        yield return GameManager.OneSec;
        Destroy(pattern.gameObject);
    }

    void Interact()
    {
        StartCoroutine(tt());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) GameManager.instance.UI.ToggleInteract(Interact,true,"Lit");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) GameManager.instance.UI.ToggleInteract(null, false, "");
    }

    private void OnDestroy()
    {
        if(mat != null) Destroy(mat);
    }
}
