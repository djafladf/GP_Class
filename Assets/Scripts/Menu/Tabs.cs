using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tabs : MonoBehaviour
{
    [SerializeField] List<Image> TabImages;     // By Two
    [SerializeField] List<TMP_Text> TabText;
    [SerializeField] List<GameObject> TargetObjects;
    [SerializeField] Transform Selector;

    int CurOpen = -1;

    Color One = new Color(0.4f, 0.3f, 0.3f);
    Color Two = new Color(0.2f, 0.1f, 0.1f);

    void AddEvent(EventTrigger eventTrigger, EventTriggerType Type, Action<PointerEventData,int> Event,int param)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = Type;
        entry.callback.AddListener((data) => { Event((PointerEventData)data,param); });
        eventTrigger.triggers.Add(entry);
    }

    void ButtonInit(EventTrigger eventTrigger, Action<PointerEventData,int> OnPointer, Action<PointerEventData,int> OutPointer, Action<PointerEventData,int> ClickPointer,int i)
    {
        //data.pointerId : left -> -1, right -> -2, Wheel -> -3;
        if (OnPointer != null) AddEvent(eventTrigger, EventTriggerType.PointerEnter, OnPointer,i);

        if (OutPointer != null) AddEvent(eventTrigger, EventTriggerType.PointerExit, OutPointer,i);

        if (ClickPointer != null) AddEvent(eventTrigger, EventTriggerType.PointerClick, ClickPointer,i);
    }

    private void Awake()
    {
        int l = transform.childCount - (Selector == null ? 0 : 1);
        if(TabImages.Count == 0)
        {
            for(int i = 0; i <l; i++)
            {
                var t = transform.GetChild(i);
                TabImages.Add(t.GetComponent<Image>());TabImages.Add(t.GetChild(0).GetComponent<Image>());
                TabText.Add(t.GetChild(1).GetComponent<TMP_Text>());
                if(!t.TryGetComponent<EventTrigger>(out var et)) { et = t.AddComponent<EventTrigger>(); }
                ButtonInit(et, PointOn, PointOff, Toggle,i);
            }
        }
    }

    public void Toggle(PointerEventData dt,int ind)
    {
        if (CurOpen != -1)
        {
            TargetObjects[CurOpen].SetActive(false); TabImages[2 * CurOpen].raycastTarget = true; TabImages[2 * CurOpen].color = Color.gray; TabImages[2 * CurOpen + 1].color = Two; TabText[CurOpen].color = Two;
        }
        CurOpen = ind;
        if(TargetObjects[CurOpen] != null) TargetObjects[CurOpen].SetActive(true);
    }

    public void PointOn(PointerEventData dt,int ind)
    {
        if (ind == CurOpen) return;
        if (Selector != null) { Selector.position = TabImages[2 * ind].transform.position; Selector.gameObject.SetActive(true); }
        TabImages[2 * ind].color = Color.white; TabImages[2 * ind + 1].color = One; 
        if(TabText[ind] != null) TabText[ind].color = One;
    }

    public void PointOff(PointerEventData dt,int ind)
    {
       if (ind == CurOpen) return;
       TabImages[2 * ind].color = Color.gray; TabImages[2 * ind + 1].color = Two;
       if (TabText[ind] != null) TabText[ind].color = Two;
    }

    private void OnEnable()
    {
       
    }
    private void OnDisable()
    {
        Selector.gameObject.SetActive(false);
        CurOpen = -1;
        //GameManager.instance.FloatM.Close(-1, true);
    }
}
