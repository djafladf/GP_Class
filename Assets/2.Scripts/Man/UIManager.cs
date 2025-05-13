using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UIManager : MonoBehaviour
{
    [SerializeField] TMP_Text Score;
    [SerializeField] TMP_Text HP_Text;
    [SerializeField] TMP_Text InteractText;
    [SerializeField] TMP_Text Level;
    [SerializeField] TMP_Text Timer;
    [SerializeField] TMP_Text Ascending;
    [SerializeField] Blurring AscendingOption;

    [SerializeField] Image EXPBar;
    [SerializeField] Image HPBar;

    [SerializeField] GameObject PlayUI;
    [SerializeField] GameObject PauseUI;

    int CurScore = 0;
    private void Start()
    {
        GameManager.instance.UI = this;
    }



    public void HPChange(float rf)
    {
        HPBar.fillAmount = rf;
    }

    float ExpSub = 0.1f;
    float CurExpVar = 0;
    int CurLevel = 1;
    public void ExpChange(float amount)
    {
        CurExpVar += ExpSub * amount;
        if(CurExpVar >= 1)
        {
            CurExpVar--;
            Level.text = $"LV.{++CurLevel}";
            ExpSub *= 0.92f;
        }
        EXPBar.fillAmount = Mathf.Min(CurExpVar, 1);
    }

    Action Interact = null;
    public void ToggleInteract(Action InteractAct,bool On,string Text)
    {
        InteractText.gameObject.SetActive(On); if(Text != null) InteractText.text = Text;
        Interact = InteractAct;
    }

    public void ShowAscending(string text, float time, Action act = null)
    {
        AscendingOption.Setting(time,text: text, AfterAction: act);
        Ascending.gameObject.SetActive(true);
    }

    public void SetTimer(float Time, Action act = null)
    {
        Timer.gameObject.SetActive(true);
        StartCoroutine(TimerWork(Time,act));
    }

    IEnumerator TimerWork(float Time, Action act)
    {
        while(Time > 0)
        {
            Timer.text = Time.ToString("F1");
            yield return GameManager.DotOne;
            Time -= 0.1f;
        }
        if (act != null) act.Invoke();
    }

    public void InteractSomething()
    {
        if (Interact != null)
        {
            Interact.Invoke();
        }
    }
    public void ScoreUp()
    {
        CurScore += 10;
        Score.text = $"{CurScore}".PadLeft(5, '0');
    }

    bool CurUIState = true;
    public void UIToggle()
    {
        if (CurUIState) { 
            PlayUI.SetActive(false); PauseUI.SetActive(true); GameManager.instance.SetTime(0);
            GameManager.instance.shad.SendMessageToShader(new Dictionary<string, float>{
            { "BlurRadius",3 },
            { "AlphaWeight",0.3f},
            { "Power",10 }
        }
        );
           GameManager.instance.shad.ToggleShader(1); CurUIState = false;
        }
        else { PlayUI.SetActive(true); PauseUI.SetActive(false); GameManager.instance.SetTime(0,true);
            GameManager.instance.shad.ToggleShader(2); CurUIState = true;
        }
    }
}
