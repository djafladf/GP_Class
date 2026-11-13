using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [SerializeField] GameObject PlayUI;
    [SerializeField] RawImage PauseUI;

    [HideInInspector] public int CurScore = 0;
    private void Awake()
    {
        GameManager.instance.UI = this;
        return;
    }

    private void Start()
    {
        GameManager.instance.Player.gameObject.SetActive(true);
    }

    #region Weapon
    [Header("Weapon Settings")]
    public List<Sprite> WeaponIm;
    [SerializeField] RectTransform WeaponSet;
    [SerializeField] List<RectTransform> WeaponBacks;
    [SerializeField] List<Image> WeaponIms;
    [SerializeField] int SlideCount = 0;
    bool IsRightNow = false;
    WaitForSeconds SlideWait = new WaitForSeconds(0.001f);

    [SerializeField] List<int> CurOrder = new List<int>(new int[]{ 0, 1, 2, 3, 4 });


    public void AddWeaponImage(Sprite sp)
    {
        WeaponIm.Add(sp); if (WeaponIm.Count == 1) WeaponIms[CurOrder[2]].sprite = WeaponIm[0];
        //WeaponIms[CurOrder[1]].sprite = WeaponIm[(GameManager.instance.PlayerScript.CurWeaponInd - 1 + WeaponIm.Count) % WeaponIm.Count];
        //WeaponIms[CurOrder[3]].sprite = WeaponIm[(GameManager.instance.PlayerScript.CurWeaponInd + 1) % WeaponIm.Count];
    }
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
                //WeaponIms[CurOrder[0]].sprite = WeaponIm[(GameManager.instance.PlayerScript.CurWeaponInd - 2 + WeaponIm.Count*2) % WeaponIm.Count];
                cnt = CurOrder[4]; CurOrder.RemoveAt(4);
                for (int i = 0; i < 20; i++)
                {
                    if (i == 10) WeaponBacks[CurOrder[1]].SetAsLastSibling();
                    WeaponBacks[CurOrder[1]].sizeDelta = new Vector2(100 + i, 100 + i);
                    WeaponBacks[CurOrder[2]].sizeDelta = new Vector2(120 - i, 120 - i);
                    for (int l = 0; l < 4; l++) WeaponBacks[CurOrder[l]].anchoredPosition += new Vector2(5, 0);
                    yield return SlideWait;
                }
                CurOrder.Insert(0, cnt); WeaponBacks[cnt].localPosition = new Vector2(-200, 0);
                GameManager.instance.PlayerScript.ChangeWeapon(-1);
            }
            else
            {
                //WeaponIms[CurOrder[4]].sprite = WeaponIm[(GameManager.instance.PlayerScript.CurWeaponInd + 2) % WeaponIm.Count];
                cnt = CurOrder[0]; CurOrder.RemoveAt(0);
                for (int i = 0; i < 20; i++)
                {
                    if (i == 10) WeaponBacks[CurOrder[2]].SetAsLastSibling();
                    WeaponBacks[CurOrder[2]].sizeDelta = new Vector2(100 + i, 100 + i);
                    WeaponBacks[CurOrder[1]].sizeDelta = new Vector2(120 - i, 120 - i);
                    for (int l = 0; l < 4; l++) WeaponBacks[CurOrder[l]].anchoredPosition += new Vector2(-5, 0);

                    yield return SlideWait;
                }
                CurOrder.Add(cnt); WeaponBacks[cnt].localPosition = new Vector2(200, 0);
                GameManager.instance.PlayerScript.ChangeWeapon(1);
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
        Timer.gameObject.SetActive(false);
    }

    public void InteractSomething()
    {
        if (Interact != null)
        {
            Interact.Invoke();
        }
    }


    // 일단은 Stat 자체 증감은 없다고 가정함.
    [SerializeField] List<Image> Bars; // 0,1 : HP.  2,3 : Sta. 4,5 : Mana
    Coroutine[] BarCols = { null,null,null };
    float[] BarRefresh = { 0, 0, 0 };
    IEnumerator BarSet(int type)
    {
        while (BarRefresh[type] > 0)
        {
            BarRefresh[type] -= 0.1f; yield return GameManager.DotOne;
        }
        BarRefresh[type] = 0;

        int i = type * 2, ii = type * 2 + 1;
        while (Bars[i].fillAmount < Bars[ii].fillAmount)
        {
            Bars[ii].fillAmount -= Time.deltaTime;
            yield return null;
        }
        Bars[ii].fillAmount = Bars[i].fillAmount;
        BarCols[type] = null;
        if(type == 1)
        {
            if(StaFill != null) StopCoroutine(StaFill);
            StaFill = StartCoroutine(StaminaFill());
        }
    }
    Coroutine StaFill = null;
    IEnumerator StaminaFill()
    {
        float cf = 0;
        yield return GameManager.OneSec;
        while (Bars[2].fillAmount < 1)
        {
            yield return null;
            cf = Time.fixedDeltaTime * 0.5f;
            Bars[2].fillAmount += cf;
            Bars[3].fillAmount += cf;
            GameManager.instance.PlayerScript.HealSta(cf);
        }
        Bars[2].fillAmount = Bars[3].fillAmount = 1;
        GameManager.instance.PlayerScript.HealSta(1);
        StaFill = null;
    }


    public void CharPoint(int type, float amount)
    {
        Bars[type * 2].fillAmount -= amount;
        BarRefresh[type] = 1.5f;
        if (BarCols[type] == null) BarCols[type] = StartCoroutine(BarSet(type));
        if (type == 1 && StaFill != null) { StopCoroutine(StaFill); StaFill = null; }
    }

    [SerializeField] Image BossHP;
    [SerializeField] TMP_Text BossName;
    public void ToggleBoss(string name)
    {
        BossHP.fillAmount = 1; BossName.text = name;
        BossHP.transform.parent.gameObject.SetActive(true);
    }

    public void BossHpChange(float amount)
    {
        BossHP.fillAmount = amount;
    }
    #endregion
#region Map
    [Header("Map")]
    [SerializeField] RectTransform MapObj;
    [SerializeField] RectTransform MaskerObj, PlayerPin;
    [SerializeField] List<Sprite> PinType; // 0 : Unknown, 1 : Base, 2 : Monster, 3 : Puzzle, 4 : Heal, 5 : Shop, 6: Fog
    int[,] RoomTypes;
    Image[,] PinSprites;
    [SerializeField] RectTransform Pins;
    [SerializeField] GameObject MapPin, PassPin,FogPin;
    GameObject[,] Fogs;
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

    [SerializeField] TMP_Text[] MenuTexts;

    public void ApplyOnUI(int ind, float val)
    {
        MenuTexts[ind].text = $"{Mathf.FloorToInt(val * 100)}%";
    }

#region Menu
    RenderTexture captured;
    int CurStopCall = 0;
    bool OnMenu = false;
    [Header("Menu Setting")]
    [SerializeField] GameObject MenuObj;
    public void ShowMenu()
    {
        OnMenu = OnMenu == false;
        //if (OnMenu)
        //{
        //    MenuObj.SetActive(true);
        //    MapObj.transform.SetParent(MenuObj.transform.GetChild(2));
        //    MapObj.localPosition = new Vector2(125, 0);
        //    MapObj.sizeDelta = new Vector2(850, 850);
        //    MaskerObj.sizeDelta = new Vector2(800, 800);
        //    Pins.localPosition = Vector2.zero;
        //    PlayerPin.localPosition = new Vector2(Lastx * 75f, Lasty * 75f);
        //}
        //else
        //{
        //    MenuObj.SetActive(false);
        //    MapObj.transform.SetParent(PlayUI.transform);
        //    MapObj.localPosition = new Vector2(750, 400);
        //    MapObj.sizeDelta = new Vector2(220, 220);
        //    MaskerObj.sizeDelta = new Vector2(200, 200);
        //    Pins.localPosition = new Vector2(-Lastx * 75f, -Lasty * 75f);
        //    PlayerPin.localPosition = Vector2.zero;
        //}
        //SetStop(OnMenu ? 1 : -1);
    }

    /// <summary>
    /// On/Off Stop
    /// </summary>
    /// <param name="var">1 : On, -1 : Off</param>
    public void SetStop(int var)
    {
        if(var == 1 && CurStopCall == 0)
        {
            Camera.main.targetTexture = captured; Camera.main.Render(); Camera.main.targetTexture = null;
            PauseUI.texture = captured;  GameManager.instance.SetTime(0);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            PlayUI.SetActive(false); PauseUI.gameObject.SetActive(true);
        }

        CurStopCall += var;

        if (CurStopCall == 0)
        {
            PauseUI.gameObject.SetActive(false); PlayUI.SetActive(true);
            Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
            GameManager.instance.SetTime(0, true);
        }
    }
    #endregion
}
