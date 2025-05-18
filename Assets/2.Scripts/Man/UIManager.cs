using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UIManager : MonoBehaviour
{
    [Header("Normal Setting")]
    [SerializeField] TMP_Text Score;
    [SerializeField] TMP_Text InteractText;
    [SerializeField] TMP_Text Level;
    [SerializeField] TMP_Text Timer;
    [SerializeField] TMP_Text Ascending;
    [SerializeField] Blurring AscendingOption;

    [SerializeField] Image EXPBar;
    [SerializeField] Image HPBar;

    [SerializeField] GameObject PlayUI;
    [SerializeField] GameObject PauseUI;

    [HideInInspector] public int CurScore = 0;
    private void Awake()
    {
        GameManager.instance.UI = this;
        WeaponIms[2].sprite = WeaponIm[0]; WeaponIms[3].sprite = WeaponIm[1 % WeaponIm.Count];WeaponIms[1].sprite = WeaponIm[(WeaponIm.Count + WeaponIm.Count - 1)%WeaponIm.Count];
    }

#region Weapon
    [Header("Weapon Settings")]
    [SerializeField] List<Sprite> WeaponIm;
    [SerializeField] RectTransform WeaponSet;
    [SerializeField] List<RectTransform> WeaponBacks;
    [SerializeField] List<Image> WeaponIms;
    [SerializeField] int SlideCount = 0;
    bool IsRightNow = false;
    WaitForSeconds SlideWait = new WaitForSeconds(0.001f);

    [SerializeField] List<int> CurOrder = new List<int>(new int[]{ 0, 1, 2, 3, 4 });
    public void SlideWeapon(bool IsUp)
    {
        if (SlideCount != 0 && IsUp != IsRightNow) { SlideCount = 0; }
        SlideCount++; IsRightNow = IsUp;
        if (SliderCor == null) SliderCor = StartCoroutine(Slider());
    }

    Coroutine SliderCor;
    IEnumerator Slider()
    {
        int cnt;
        while (SlideCount>0)
        {
            if (IsRightNow)
            {
                WeaponIms[CurOrder[0]].sprite = WeaponIm[(GameManager.instance.PlayerScript.CurWeaponInd - 2 + WeaponIm.Count*2) % WeaponIm.Count];
                cnt = CurOrder[4]; CurOrder.RemoveAt(4);
                for (int i = 0; i < 20; i++)
                {
                    if (i == 10) WeaponBacks[CurOrder[1]].SetAsLastSibling();
                    WeaponBacks[CurOrder[1]].sizeDelta = new Vector2(100 + i, 100 + i);
                    WeaponBacks[CurOrder[2]].sizeDelta = new Vector2(120 - i, 120 - i);
                    for (int l = 0; l < 4; l++) WeaponBacks[CurOrder[l]].Translate(5, 0, 0);
                    yield return SlideWait;
                }
                CurOrder.Insert(0, cnt); WeaponBacks[cnt].localPosition = new Vector2(-200, 0);
                GameManager.instance.PlayerScript.ChangeWeapon(1);
            }
            else
            {
                WeaponIms[CurOrder[4]].sprite = WeaponIm[(GameManager.instance.PlayerScript.CurWeaponInd + 2) % WeaponIm.Count];
                cnt = CurOrder[0]; CurOrder.RemoveAt(0);
                for (int i = 0; i < 20; i++)
                {
                    if (i == 10) WeaponBacks[CurOrder[2]].SetAsLastSibling();
                    WeaponBacks[CurOrder[2]].sizeDelta = new Vector2(100 + i, 100 + i);
                    WeaponBacks[CurOrder[1]].sizeDelta = new Vector2(120 - i, 120 - i);
                    for (int l = 0; l < 4; l++) WeaponBacks[CurOrder[l]].Translate(-5, 0, 0);
                    yield return SlideWait;
                }
                CurOrder.Add(cnt); WeaponBacks[cnt].localPosition = new Vector2(200, 0);
                GameManager.instance.PlayerScript.ChangeWeapon(-1);
            }

            SlideCount--;
        }
        GameManager.instance.PlayerScript.ChangeWeaponEnd();
        SliderCor = null;
    }
    #endregion
#region External Interact
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
    public void ScoreUp(int amount = 10)
    {
        CurScore += amount;
        Score.text = $"{CurScore}".PadLeft(5, '0');
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
        if (CurExpVar >= 1)
        {
            CurExpVar--;
            Level.text = $"LV.{++CurLevel}";
            ExpSub *= 0.92f;
        }
        EXPBar.fillAmount = Mathf.Min(CurExpVar, 1);
    }
    #endregion
#region Map
    [Header("Map")]
    [SerializeField] List<Sprite> PinType; // 0 : Unknown, 1 : Base, 2 : Monster, 3 : Puzzle, 4 : Heal, 5 : Shop
    [SerializeField] List<ForTestInt> RoomTypes;
    [SerializeField] List<ForTestImage> PinSprites;
    [SerializeField] Transform Pins;
    int Lastx = 1, Lasty = 0;
    public void MapSetting(int x, int y,bool IsUnknown = false)
    {
        if (IsUnknown) PinSprites[y].List[x].sprite = PinType[RoomTypes[y].List[x]];
        Pins.transform.Translate(new Vector2((Lastx - x) * 75, (Lasty - y) * 75));
        Lastx = x; Lasty = y;
    }
    #endregion
#region Buff
    [Header("Buff")]
    [SerializeField] List<Image> BuffImages;
    [SerializeField] RectTransform BuffSet;
    float[] MaxTime = { 0, 0, 0 };
    float[] LeftTime = { 0, 0, 0 };
    Coroutine BuffCor;

    public void SetBuff(int type, int Last)
    {
        BuffImages[--type].gameObject.SetActive(true); MaxTime[type] = 1f / Last; LeftTime[type] = Last;
        LayoutRebuilder.ForceRebuildLayoutImmediate(BuffSet);
        if (BuffCor == null) BuffCor = StartCoroutine(BuffTimer());
    }

    IEnumerator BuffTimer()
    {
        while (LeftTime[0] > 0 || LeftTime[1] > 0 || LeftTime[2] > 0)
        {
            for (int i = 0; i < 3; i++) if (LeftTime[i] > 0) 
                {
                    LeftTime[i]--; BuffImages[i].fillAmount = LeftTime[i] * MaxTime[i];
                    if (LeftTime[i] == 0) { BuffImages[i].gameObject.SetActive(false); GameManager.instance.PlayerScript.BuffOff(i+1); }
                }
            yield return GameManager.OneSec;
        }
        BuffCor = null;
    }


    #endregion


    // UnUse
    bool CurUIState = true;
    public bool UIToggle()
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
        return CurUIState;
    }
    
}
