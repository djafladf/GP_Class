using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Normal Setting")]
    [SerializeField] TMP_Text InteractText;
    [SerializeField] TMP_Text Ascending;
    [SerializeField] Blurring AscendingOption;

    [SerializeField] GameObject PlayUI;

    [HideInInspector] public int CurScore = 0;
    private void Awake()
    {
        GameManager.instance.UI = this;
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
    #region ETC
    [Header("ETC")]
    [SerializeField] GameObject Fader;
    [SerializeField] Material FaderMat;
    public void FadeInOut(int isin = 0,int t = 2)
    {
        FaderMat.SetFloat("_Start",Time.time);
        FaderMat.SetFloat("_In",isin);
        FaderMat.SetFloat("_To",t);
        if (isin == 0) Fader.gameObject.SetActive(true);
        else Invoke("OffFader", t);
    }

    void OffFader()
    {
        Fader.gameObject.SetActive(false);
    }

    public void ToggleInUI(bool OnOff)
    {
        PlayUI.SetActive(OnOff);
    }
    [SerializeField] List<Texture2D> BlackTexs;
    [SerializeField] Material BlackMat;
    [SerializeField] GameObject BlackImage;
    public void ShowBlackImage(int i,float AutoEnd = 7)
    {
        BlackImage.gameObject.SetActive(true);
        BlackMat.SetFloat("_Start", Time.time);
        BlackMat.SetFloat("_Fadeout", AutoEnd);
        BlackMat.SetTexture("_Tex", BlackTexs[i]);
    }
    public void OffBlackImage()
    {
        BlackImage.gameObject.SetActive(false);
    }

    #region Combo
    [SerializeField] TMP_Text[] ComboTex;
    [SerializeField] GameObject[] ComboObjs;
    int ComboACount = 1;
    bool AllowSameAct = true;
    public LinkedList<CurveState> ComboStats = new();
    Coroutine ComboCor = null;
    public CurveState GetNextCombo() { if (ComboStats.Count == 0) return CurveState.none; else return ComboStats.First.Value; }
    public void SetSame(int boo) { AllowSameAct = boo == 0; GameManager.GStat.SameNext = AllowSameAct; }
    public void SetCount(Vector3 count) { ComboACount = (int)count.x ; GameManager.GStat.NextAble = ComboACount; }
    public void ClearCombo()
    {
        if(ComboCor != null) StopCoroutine(ComboCor);
        foreach (var j in ComboObjs) j.SetActive(false);
        ComboStats.Clear();
    }
    public int ComboAdd(ref CurveState curCS, CurveState add)
    {
        if (ComboStats.Count >= ComboACount) return 0;
        if (ComboStats.Count == 0) { if (curCS == add && !AllowSameAct) return 0; }
        else if (ComboStats.Last.Value == add && !AllowSameAct) return 0;
        ComboStats.AddFirst(add);
        ComboObjs[ComboStats.Count - 1].SetActive(true);
        ComboTex[ComboStats.Count - 1].text = add.ToString();
        return 2;
    }
    public CurveState ComboConsume()
    {
        if(ComboStats.Count == 0) return CurveState.none;
        ComboObjs[ComboStats.Count-1].SetActive(false);
        var t = ComboStats.First.Value; ComboStats.RemoveFirst();
        if (ComboCor == null) ComboCor = StartCoroutine(ComboCoroutine());
        return t;
    }
    IEnumerator ComboCoroutine()
    {
        int i, l;
        for(i = 0; i < 10; i++)
        {
            for(l = 0; l < ComboStats.Count; l++)
            {
                ComboObjs[l].transform.localPosition = new Vector3(17.5f + l * 87.5f - i * 8.75f, 0, 0);
            }
            yield return GameManager.MinSeconds;
        }           
        ComboCor = null;
    }
    #endregion

    #region Interact
    ReserveSystem<(Action<System.Object>, String)> Interact = new();

    public int RegisterInteract(Action<System.Object> InteractAct,string Text = null)
    {
        int t = Interact.Register((InteractAct, Text));
        UpdateInteractText();
        return t;
    }
    public bool CancleInteract(int _ticket)
    {
        if(!Interact.CancleInteract(_ticket)) return false;
        UpdateInteractText();
        return true;
    }

    public void InteractFunc(System.Object T = null, int RemoveType = 0)
    {
        if (!Interact.TryUse(out var act,RemoveType)) return;
        UpdateInteractText();
        act.Item1?.Invoke(T);
    }
    void UpdateInteractText()
    {
        if (!Interact.First(out var data) || data.Item2 == null)
        {
            InteractText.gameObject.SetActive(false);
            return;
        }

        InteractText.gameObject.SetActive(true);
        InteractText.text = data.Item2;
    }
    #endregion
    public void ShowAscending(string text, float time, Action act = null)
    {
        AscendingOption.Setting(time,text: text, AfterAction: act);
        Ascending.gameObject.SetActive(true);
    }

    #region BarStat
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
    #endregion
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
        yield return null;
        //while (LeftTime[0] > 0 || LeftTime[1] > 0 || LeftTime[2] > 0)
        //{
        //    for (int i = 0; i < 3; i++) if (LeftTime[i] > 0) 
        //        {
        //            LeftTime[i]--; BuffImages[i].fillAmount = LeftTime[i] * MaxTime[i];
        //            if (LeftTime[i] == 0) { BuffImages[i].gameObject.SetActive(false); GameManager.instance.PlayerScript.BuffOff(i+1); }
        //        }
        //    yield return GameManager.OneSec;
        //}
        //BuffCor = null;
    }


    #endregion
}
