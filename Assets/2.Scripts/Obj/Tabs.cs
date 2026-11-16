using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tabs : MonoBehaviour
{
    [SerializeField] List<Image> TabImages;     // By Two
    [SerializeField] List<TMP_Text> TabText;
    [SerializeField] List<GameObject> TargetObjects;

    int CurOpen = 0;

    Color One = new Color(0.4f, 0.3f, 0.3f);
    Color Two = new Color(0.2f, 0.1f, 0.1f);

    public void Toggle(int ind)
    {
        TargetObjects[CurOpen].SetActive(false); TabImages[2 * CurOpen].raycastTarget = true; TabImages[2 * CurOpen].color = Color.gray; TabImages[2 * CurOpen + 1].color = Two; TabText[CurOpen].color = Two;  CurOpen = ind;
        TargetObjects[CurOpen].SetActive(true);
    }

    public void PointOn(int ind)
    {
        if (ind == CurOpen) return;
        TabImages[2 * ind].color = Color.white; TabImages[2 * ind + 1].color = One; TabText[ind].color = One;
    }

    public void PointOff(int ind)
    {
       if (ind == CurOpen) return;
       TabImages[2 * ind].color = Color.gray; TabImages[2 * ind + 1].color = Two; TabText[ind].color = Two;
    }

    private void OnEnable()
    {
        if (CurOpen != 0) { PointOn(0); Toggle(0); }
    }
    private void OnDisable()
    {
        GameManager.instance.FloatM.Close(-1, true);
    }
}
