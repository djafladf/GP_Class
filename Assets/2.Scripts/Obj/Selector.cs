using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class Selector : Buttons
{
    [SerializeField] Vector3 TargetSize = Vector3.one;
    [SerializeField] float TargetTime;
    [SerializeField] List<Event> ClickEvent;
    [SerializeField] List<Image> Other;

    Vector3 InitSize;
    Image im;

    float Process = 0;
    float Trigger;

    protected override void Awake()
    {
        base.Awake();
        InitSize = transform.localScale;
        Trigger = -0.05f / TargetTime;
        im = GetComponent<Image>();
    }

    private void OnEnable()
    {
        im.raycastTarget = true;
    }

    protected override void Click(PointerEventData Data)
    {
        foreach (var j in Other) j.raycastTarget = false;
        StartCoroutine(SelectCor());
    }

    IEnumerator SelectCor()
    {
        for(float i = 1; i > 0; i-=0.1f)
        {
            yield return wfs;
            im.material.SetColor("_Color",new Color(0.2f, 0.3f, 0.5f, i));
        }
        EndAct();
    }
    void EndAct()
    {
        Process = 0; Trigger = -0.05f / TargetTime;
        transform.localScale = InitSize;
        transform.parent.gameObject.SetActive(false);
        foreach (var j in ClickEvent) j.Invoke(null);
        GameManager.instance.UI.SetStop(-1);
    }

    protected override void OnPointer(PointerEventData data)
    {
        
        if (SizeCor == null) StartCoroutine(SizeChanger());
        Trigger *= -1;
    }

    protected override void OutPointer(PointerEventData data)
    {
        if (!im.raycastTarget) return;
        if (SizeCor == null) StartCoroutine(SizeChanger());
        Trigger *= -1;
    }

    Coroutine SizeCor = null;
    WaitForSecondsRealtime wfs = new WaitForSecondsRealtime(0.05f);
    IEnumerator SizeChanger()
    {
        while(Process >= 0 && Process <= 1)
        {
            yield return wfs;
            Process += Trigger;
            transform.localScale = Vector3.Lerp(InitSize, TargetSize, Process);
        }
        Process = Mathf.Clamp(Process, 0, 1);
        SizeCor = null;
    }
}
