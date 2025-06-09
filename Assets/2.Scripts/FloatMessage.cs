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
    RectTransform ParentRect;
    RectTransform InfRect;
    private void Awake()
    {
        if (GameManager.instance.FloatM == null)
        {
            GameManager.instance.FloatM = this;
            ParentRect = transform.parent.GetComponent<RectTransform>();
            InfRect = GetComponent<RectTransform>();
            MainText = transform.GetChild(0).GetComponent<TMP_Text>();
            Im = GetComponent<Image>();
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

    public void Init(string Message, Color backColor,int ind = 0,float font = 50)
    {
        MainText.text = Message; MainText.fontSize = font; Im.color = backColor;
        gameObject.SetActive(true); CurUseInd = ind;
        LayoutRebuilder.ForceRebuildLayoutImmediate(InfRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(InfRect);
        MySize = InfRect.rect.size * 0.5f; MaxSize = ParentRect.rect.size - MySize;
    }

    public void Close(int ind, bool MasterKey = false)
    {
        if (CurUseInd == ind | MasterKey)
        {
            gameObject.SetActive(false);
            MainText.text = "";
        }
    }
}
