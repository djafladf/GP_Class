using UnityEngine;

public class InteractThing : MonoBehaviour
{
    [SerializeField] string TargetTag;
    Rigidbody rigid;
    Collider coll;

    protected virtual void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out rigid)) rigid = gameObject.AddComponent<Rigidbody>();
        if (!TryGetComponent<Collider>(out coll)) coll = gameObject.AddComponent<BoxCollider>(); // 기본적으로 BoxCollider를 넣는거승로 함.
    }

    int ind = -1;
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(TargetTag)) ind = GameManager.instance.UI.RegisterInteract(IFunc,text.Equals("") ? null : text);
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(TargetTag) && ind != -1) { GameManager.instance.UI.CancleInteract(ind); ind = -1; }
    }

    [Header("0 : Remove , 1 : Reuse(First), 2 : Reuse(Last)")]
    [SerializeField] int RemoveType;
    [SerializeField] string text;
    public virtual void IFunc(System.Object Parameter = null)
    {
        
    }
}
