using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Toggles : MonoBehaviour
{
    [SerializeField] UnityEvent<int> toggleEvent;
    [SerializeField] List<Image> TIm;
    int CurSelected = 0;

    public void Toggle(int ind)
    {
        if (ind == CurSelected) return;
        TIm[CurSelected].sprite = GameManager.instance.Setting.TogImageSet[0];
        TIm[ind].sprite = GameManager.instance.Setting.TogImageSet[1];
        CurSelected = ind;
    }
}
