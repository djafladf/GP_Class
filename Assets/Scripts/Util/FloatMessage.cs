using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FloatMessage : MonoBehaviour
{
    Image Im;
    TMP_Text MainText;
    RectTransform ImRect;
    RectTransform ParentRect;
    RectTransform InfRect;
    private void Awake()
    {
        if (GameManager.instance.FloatM == null)
        {
            GameManager.instance.FloatM = this;
            ParentRect = transform.parent.GetComponent<RectTransform>();
            InfRect = GetComponent<RectTransform>();
            MainText = transform.GetChild(1).GetComponent<TMP_Text>();
            Im = transform.GetChild(0).GetComponent<Image>();
            ImRect = Im.GetComponent<RectTransform>();
            gameObject.SetActive(false);
        }
        else Destroy(gameObject);
    }

    Vector2 MousePos;
    Vector2 MySize;
    Vector2 MaxSize;
    private void Update()
    {
        MousePos = Input.mousePosition; 
        MousePos.x = Mathf.Clamp(MousePos.x + MySize.x, MySize.x, MaxSize.x); MousePos.y = Mathf.Clamp(MousePos.y + MySize.y, MySize.y, MaxSize.y);
        transform.position = MousePos;
    }

    int LastInd = 0;

    public int Register()
    {
        return LastInd++;
    }

    int CurUseInd;

    IEnumerator AutoClosing(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        Close(-1,true);
        ClosingCor = null;
    }

    Coroutine ClosingCor = null;
    public void Init(string Message,int ind = 0, float closingTime = -1,Sprite spr = null)
    {
        MainText.text = Message;
        if (spr != null) 
        { 
            Im.gameObject.SetActive(true); ImRect.sizeDelta = spr.rect.size;
            Im.sprite = spr; 
        } 
        else Im.gameObject.SetActive(false);
        gameObject.SetActive(true); CurUseInd = ind;
        LayoutRebuilder.ForceRebuildLayoutImmediate(InfRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ParentRect);
        MySize = InfRect.rect.size * 0.5f; MaxSize = ParentRect.rect.size - MySize;
        if(ClosingCor != null) StopCoroutine(ClosingCor);
        if (closingTime != -1) ClosingCor = StartCoroutine(AutoClosing(closingTime));
        else ClosingCor = null;
    }

    public void Close(int ind, bool MasterKey = false)
    {
        if (CurUseInd == ind | MasterKey)
        {
            if (ClosingCor != null) StopCoroutine(ClosingCor);
            ClosingCor = null;
            gameObject.SetActive(false);
            MainText.text = "";
        }
    }
}
