using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tabs : MonoBehaviour
{
    [SerializeField] List<Image> TabImages;     // By Two
    [SerializeField] List<TMP_Text> TabText;
    [SerializeField] List<GameObject> TargetObjects;
    [SerializeField] Transform Selector;
    [SerializeField] UnityEvent<GameObject> ToggleOnEvent;
    [SerializeField] UnityEvent<GameObject> ToggleOffEvent;
    [SerializeField] int MaxTab;

    int CurOpen = -1;

    Color One = new Color(0.5f, 0.4f, 0.4f);
    Color Two = new Color(0.2f, 0.1f, 0.1f);

    private void Awake()
    {
        if(TabImages.Count == 0)
        {
            for(int i = 0; i <MaxTab; i++)
            {
                var t = transform.GetChild(i);
                TabImages.Add(t.GetComponent<Image>());TabImages.Add(t.GetChild(0).GetComponent<Image>());
                TabText.Add(t.GetChild(1).GetComponent<TMP_Text>());
                if(!t.TryGetComponent<EventTrigger>(out var et)) { et = t.AddComponent<EventTrigger>(); }
                EventUtil<int>.ButtonInit(et, PointOn, PointOff, Toggle,i);
            }
        }
        //gameObject.SetActive(false);
    }

    public void Toggle(PointerEventData dt,int ind)
    {
        if (CurOpen != -1)
        {
            if (TargetObjects[CurOpen] != null) ToggleOffEvent?.Invoke(TargetObjects[CurOpen]);
            TabImages[2 * CurOpen].raycastTarget = true; TabImages[2 * CurOpen].color = Color.gray; TabImages[2 * CurOpen + 1].color = Two; 
            TabText[CurOpen].color = Two;
        }
        CurOpen = ind;
        if (TargetObjects[ind] != null)
        {
            //TargetObjects[ind].SetActive(true);
            ToggleOnEvent?.Invoke(TargetObjects[ind]);
        }
    }
    int Ons = -1;
    public void PointOn(PointerEventData dt,int ind)
    {
        if (ind == CurOpen) return;
        Ons = ind;
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
        int t = CurOpen; CurOpen = -1; Ons = -1;
        if (t != -1) PointOff(null, t);
        if (Ons != -1) PointOff(null, Ons);
        
        //GameManager.instance.FloatM.Close(-1, true);
    }
}
