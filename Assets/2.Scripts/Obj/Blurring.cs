using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Blurring : MonoBehaviour
{
    [SerializeField] float time;
    Image im;
    TMP_Text tx;

    float imnit, txnit;
    private void Awake()
    {
        if(TryGetComponent<Image>(out im)) imnit = im.color.a;
        if(TryGetComponent<TMP_Text>(out tx)) txnit = tx.color.a;
        gameObject.SetActive(false);
    }

    Action After;
    public void Setting(float tm, string text = null, Sprite image = null, Action AfterAction = null)
    {
        time = tm;
        if (text != null && tx != null) tx.text = text;
        if (im != null && image != null) { im.sprite = image; imnit = im.color.a; }
        After = AfterAction;
        if (gameObject.activeSelf) { StopCoroutine(Col); Col = StartCoroutine(Blur()); }
        //gameObject.SetActive(true);
    }

    Coroutine Col = null;

    private void OnEnable()
    {
        Col =  StartCoroutine(Blur());
    }
    private void OnDisable()
    {
        if (Col != null) { StopCoroutine(Col); Col = null; }
    }

    IEnumerator Blur()
    {
        WaitForSeconds wfs = new WaitForSeconds(0.1f);
        float imAlpha = 0, txAlpha = 0;


        if (im != null) { imAlpha = 0.1f / time * im.color.a; im.color = new Color(im.color.r, im.color.g, im.color.b, imnit); }
        if (tx != null) { txAlpha = 0.1f / time * tx.color.a; tx.color = new Color(tx.color.r, tx.color.g, tx.color.b, txnit); }
        for(float _ = 0; _ < time; _ += 0.1f)
        {
            yield return wfs;
            if(im != null) im.color = new Color(im.color.r, im.color.g, im.color.b, im.color.a - imAlpha);
            if (tx != null) tx.color = new Color(tx.color.r, tx.color.g, tx.color.b, tx.color.a - txAlpha);
        }
        if (After != null) After.Invoke();
        gameObject.SetActive(false);
    }
}
