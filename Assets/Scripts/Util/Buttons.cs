using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Buttons: MonoBehaviour
{

    protected EventTrigger ET;


    protected virtual void Awake()
    {
        if (GetComponent<EventTrigger>() == null) gameObject.AddComponent<EventTrigger>();
        ET = GetComponent<EventTrigger>();
        EventUtil.ButtonInit(ET, OnPointer, OutPointer, Click);
    }


    protected virtual void OnPointer(PointerEventData data) { }

    protected virtual void OutPointer(PointerEventData data) { }

    protected virtual void Click(PointerEventData Data) { }
}
