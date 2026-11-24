using UnityEngine;

public class Theresia : MonoBehaviour
{
    FragmentAssembly FA;
    Animator anim;
    Transform mTrans;

    private void Awake()
    {
        FA = GetComponent<FragmentAssembly>();
        anim = GetComponent<Animator>();
        mTrans = transform.parent;
    }
    private void Start()
    {
        mTrans.gameObject.SetActive(false);
    }

    public void Appear(Transform Pos = null, Vector3 Rot = default)
    {
        
        if (Pos != null)
        {
            mTrans.position = Pos.position;
            mTrans.forward = Rot;
        }
        if (!mTrans.gameObject.activeSelf) mTrans.gameObject.SetActive(true);
        else FA.Begin(true);

        var state = anim.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Disappear"))
        {
            float p = Mathf.Clamp01(state.normalizedTime);
            anim.Play("Appear", 0, 1f - p);
        }
        else anim.SetTrigger("Appear");
    }

    public void DisApeear()
    {
        if (!mTrans.gameObject.activeSelf) return;
        var state = anim.GetCurrentAnimatorStateInfo(0);
        FA.Begin(false);
        if (state.IsName("Appear"))
        {
            float p = Mathf.Clamp01(state.normalizedTime);
            anim.Play("Disappear", 0, 1f - p);
        }
        else anim.SetTrigger("Disappear");
    }

    public void Disable()
    {
        mTrans.gameObject.SetActive(false);
    }
}
