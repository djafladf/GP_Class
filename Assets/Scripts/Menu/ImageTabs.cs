using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageTabs : MonoBehaviour
{
    [SerializeField] List<Image> TabImages;
    [SerializeField] List<Sprite> Changes;
    [SerializeField] List<GameObject> TargetObjects;

    int CurOpen = -1;
    public void Toggle(int ind)
    {
        if (CurOpen != -1)
        {
            TargetObjects[CurOpen].SetActive(false); TabImages[CurOpen].raycastTarget = true; TabImages[CurOpen].sprite = Changes[CurOpen * 2+1];
        }
        CurOpen = ind;
        if (TargetObjects[CurOpen] != null) { TargetObjects[CurOpen].SetActive(true); TabImages[CurOpen].raycastTarget = false; }
    }

    public void PointOn(int ind)
    {
        if (ind == CurOpen) return;
        TabImages[ind].sprite = Changes[ind * 2];
    }

    public void PointOff(int ind)
    {
        if (ind == CurOpen) return;
        TabImages[ind].sprite = Changes[ind * 2+1];
    }

    private void OnEnable()
    {

    }
    private void OnDisable()
    {
        CurOpen = -1;
        //GameManager.instance.FloatM.Close(-1, true);
    }
}
