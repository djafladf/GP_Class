using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.Core;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;


[System.Serializable]
public class AnimCurve
{
    public string name;
    public AnimationCurve ac;
    public string trigger;
    public AnimationClip clip;
    public float speed;
    public float dist;
    public float StUsement;
}

public enum CurveState
{
    none,
    roll,
    jump,
    attack,
    teleport
}


public class Player : MonoBehaviour
{
    Transform mTrans;
    [SerializeField] Transform[] WeaponHolder; // R, H, Back
    [SerializeField] Transform FirePos;
    [SerializeField] AudioClip[] audioclips;

    [SerializeField] Vector3 FolllowOffset;
    [SerializeField] Vector3 FollowOffset_Z;

    [SerializeField] Rigidbody rigid;
    [SerializeField] Animator anim;

    private void Awake()
    {
        mTrans = transform.parent;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        GameManager.instance.Player = mTrans;
        GameManager.instance.PlayerScript = this;
        GameManager.instance.PlayerHealFunc = HealFunction;
        MoveAble = true;
        OnCurve = false;
        RPos = RWeapon.localPosition; RSRot = RWeapon.localRotation;
        aoc = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = aoc;
    }

    private void Start()
    {
        CurWeapon = GameManager.instance.Data.Weapon[0];
        Invoke("StartBug", 0.1f);
    }

    float MoveSpeed = 2f;
    [SerializeField] bool OnCurve = true;
    [SerializeField] bool MoveAble = false;
    [SerializeField] bool Grounded = false;
    [SerializeField] LayerMask MoveObstacleMask;

    private void Update()
    {
        Grounded = Physics.CheckSphere(mTrans.position, 0.1f, GroundMask);
        anim.SetBool("Grounded",Grounded);
        if (Grounded && rigid.linearVelocity.y < -0.01f) 
        { 
            if(Jumping && animCurve != null)
            {
                StopCoroutine(animCurve); animCurve = null; OnCurve = false;
            }
            Jumping = false; anim.SetBool("OnMove", !Dir.Equals(Vector2.zero));
        }
    }


    Vector3 NextDir = Vector3.zero;
    Vector3 camF = default, camR = default;
    private void FixedUpdate()
    {
        if (!MoveAble || OnCurve) return;
        if (!Dir.Equals(Vector2.zero))
        {
            camF = Camera.main.transform.forward; camF.y = 0; camR = Camera.main.transform.right; camR.y = 0;
            NextDir = (camF * Dir.y + camR * Dir.x); NextDir.Normalize();
            if (GameManager.instance.Camera.LockCheck && (!OnRunning)) LockRot();
            else MoveRot();

            if(OnRunning) 
            {
                if (CurST <= 0) { OnRunning = false; anim.SetBool("OnRun", false); IsDodge = false; }
                else { GameManager.instance.UI.CharPoint(1, Time.deltaTime * 0.2f); CurST -= Time.deltaTime * 0.2f; }
            }
            float moveDist = Time.fixedDeltaTime * MoveSpeed * BuffAmount[2] * (OnRunning ? 2.5f : 1);
            var ray = Physics.Raycast(origin: mTrans.position + Vector3.up, direction: NextDir, maxDistance: moveDist + 0.3f, layerMask: MoveObstacleMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
            if (!ray) rigid.MovePosition(rigid.position + NextDir * moveDist);
        }
    }

    Quaternion rot;
    void LockRot()
    {
        GameManager.instance.Camera.LookRot(rigid);
    }
    void MoveRot()
    {
        if (NextDir.sqrMagnitude < 0.01f) return;
        rot = Quaternion.LookRotation(NextDir);
        rigid.MoveRotation(Quaternion.RotateTowards(
            rigid.rotation,
            rot,
            540f * Time.fixedDeltaTime
        ));
    }

    [SerializeField] List<AnimCurve> curves;
    // Attack, Dodge
    CurveState curCurv = CurveState.none;
    CurveState PInp = CurveState.none;

    #region InputSystems
    #region Keyboard
    Coroutine animCurve = null;
    

    /// <summary>
    /// 0 : Fail, 1 : Success, 2 : Reserve
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    int SetAnimCurve(CurveState state)
    {
        int succ = 0;
        if (animCurve != null) { if (PInp == CurveState.none) { PInp = state; succ = 2; } }
        else { animCurve = StartCoroutine(CurveCor(state)); succ = 1; }

        return succ;
    }
    IEnumerator CurveCor(CurveState state)
    {
        OnCurve = true;
        curCurv = state;
        yield return null;
        if(state == CurveState.attack)
        {
            CurAttack = onDouble ? CurAttack = CurWeapon.Two[CurAttackLevel] : CurWeapon.One[CurAttackLevel];
            curves[2].clip = CurAttack.Clip;
            curves[2].StUsement = CurAttack.StUsement;
            anim.SetInteger("AttackLevel", CurAttackLevel);
        }
        if (CurST >= curves[(int)state - 1].StUsement)
        {
            CurST -= curves[(int)state - 1].StUsement;
            GameManager.instance.UI.CharPoint(1, curves[(int)state - 1].StUsement);
            var cur = curves[(int)state - 1];
            float t = cur.clip.length / cur.speed, ctime = 0;
            float rt = 1f / t;
            Vector3 _nd = default;
            if (GameManager.instance.Camera.LockCheck)
            {
                if (Dir == Vector3.zero) _nd = mTrans.forward * cur.dist;
                else
                {
                    GameManager.instance.Camera.CamFront(ref Dir, ref _nd);
                    _nd *= cur.dist;
                }
                mTrans.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
            else
            {
                if (Dir != Vector3.zero)
                {
                    GameManager.instance.Camera.CamFront(ref Dir, ref _nd);
                    mTrans.rotation = Quaternion.LookRotation(_nd);
                }
                _nd = mTrans.forward * cur.dist;
            }

            anim.SetTrigger(cur.trigger);

            float c, l = cur.ac.Evaluate(0f), de = 0;
            while (ctime < t)
            {
                yield return new WaitForFixedUpdate();
                ctime += Time.fixedDeltaTime; if (ctime > t) ctime = t;
                c = cur.ac.Evaluate(ctime * rt); de = c - l;
                rigid.MovePosition(rigid.position + de * _nd);
                l = c;
            }
            yield return GameManager.DotOne;
        }
        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        OnCurve = false;
        animCurve = null;

        if (PInp != CurveState.none) { SetAnimCurve(PInp); PInp = CurveState.none; }
        else curCurv = CurveState.none;
    }
    void OnLock(InputValue val)
    {
        GameManager.instance.Camera.OnLock();
        anim.SetBool("OnLock",GameManager.instance.Camera.LockCheck);
    }

    // Moving
    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>();
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero) && Grounded);
        if (!OnCurve) { anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y); }

    }

    float LastDodge = 0;
    float DodgeGap = 0.3f;
    bool IsDodge = false;
    bool OnRunning = false;
    // Dodge + Run
    void OnDodge(InputValue value)
    {
        if (CurST < Time.deltaTime) return;
        if (value.isPressed)
        {
            if (IsDodge) return;
            IsDodge = true;
            LastDodge = Time.unscaledTime;
            OnRunning = true;
            anim.SetBool("OnRun", true);
        }
        else
        {
            if (!IsDodge) return;
            IsDodge = false;
            if (Time.unscaledTime - LastDodge < DodgeGap) SetAnimCurve(CurveState.roll);
            anim.SetBool("OnRun", false);
            OnRunning = false;
        }
    }

    Vector3 Dir;

    // Space
    bool Jumping = false;
    Vector3 JumpDir;
    [SerializeField] LayerMask GroundMask;
    void OnJump(InputValue value)
    {
        if (!MoveAble || !Grounded || OnCurve) return;
        Vector3 leftRayOrigin = mTrans.position + new Vector3(-0.05f, 0.05f, 0);
        Vector3 rightRayOrigin = mTrans.position + new Vector3(0.05f, 0.05f, 0);
        if (Physics.Raycast(rightRayOrigin, Vector3.down, 0.15f, GroundMask) || Physics.Raycast(leftRayOrigin, Vector3.down, 0.1f, GroundMask))
        {
            Jumping = true; SetAnimCurve(CurveState.jump);
        }
    }
    void JumpForce()
    {
        rigid.AddForce(Vector3.up * 4f + mTrans.forward * MoveSpeed * BuffAmount[2] * (OnRunning ? 2.5f : 1), ForceMode.Impulse);
    }

    Vector3 RPos, LPos;
    Quaternion RSRot, LSRot;
    [SerializeField] Transform RWeapon, LWeapon;


    bool onDouble = false;
    void OnDouble(InputValue value)
    {
        if (!value.isPressed) return;
        if (curCurv != CurveState.none) return;
        CurAttackLevel = 0; LastAttackTime = -1000;
        onDouble = onDouble == false;
        anim.SetInteger("SwapMod",onDouble ? 1 : 0);
    }
    void SwapNature()
    {
        WeaponHolder[2].transform.position = WeaponHolder[0].transform.position;
    }
    void SwapEnd()
    {
        if (!onDouble)
        {
            for(int i = 0; i < CurWeapon.One.Count; i++) aoc[$"One{i+1}"] = CurWeapon.One[i].Clip;
            RWeapon.SetParent(WeaponHolder[0], false);
            RWeapon.localPosition = RPos; RWeapon.localRotation = RSRot;
        }
        else
        {
            for (int i = 0; i < CurWeapon.Two.Count; i++) aoc[$"One{i + 1}"] = CurWeapon.Two[i].Clip;
            RWeapon.SetParent(WeaponHolder[2], false);
            RWeapon.localPosition = Vector3.zero; RWeapon.localRotation = default;
        }
        anim.SetInteger("SwapMod", -1);
    }

    bool onCons = false;
    void OnDoubleCheck()
    {
        if (onDouble)
        {
            anim.SetTrigger("THCons");
            if (onCons)
            {
                RWeapon.SetParent(WeaponHolder[2],false);
                RWeapon.localPosition = Vector3.zero; RWeapon.localRotation = Quaternion.identity;
            }
            else
            {
                RWeapon.SetParent(WeaponHolder[0],false);
                RWeapon.localPosition = RPos; RWeapon.localRotation = RSRot;
            }
            onCons = onCons == false;
        }
    }

    void OnMenu(InputValue value)
    {
        GameManager.instance.UI.ShowMenu();
    }

    void OnInteract(InputValue value)
    {
        GameManager.instance.UI.InteractSomething();
    }

    #endregion

    #region Mouse
    bool CatchScroll = true;
    bool CurRight = true;
    bool _OnChangeWeapon = false;

    WeaponInfo CurWeapon;
    AttackClip CurAttack;
    
    [SerializeField] int CurAttackLevel = 0;
    float LastAttackTime = -1000;
    float AttackLeftTime = 2f;

    void AttackExceed()
    {
        LastAttackTime = Time.time;
    }

    void OnLAttack(InputValue value)
    {
        if (value.isPressed)
        {
            var suc = SetAnimCurve(CurveState.attack);
            if(suc != 0)    // 예약 성공 or 즉시 발동
            {
                if (Time.time - LastAttackTime >= AttackLeftTime) CurAttackLevel = CurWeapon.returnCount(onDouble) - 1;
                CurAttackLevel = (CurAttackLevel + 1) % CurWeapon.returnCount(onDouble);
            }
        }
    }
    AnimatorOverrideController aoc;
    void clipChange()
    {

    }

    public void ChangeWeapon(int dr)
    {
        //Weapons[CurWeaponInd].gameObject.SetActive(false);
        //CurWeaponInd = (CurWeaponInd + dr + Weapons.Count) % Weapons.Count;
        //Weapons[CurWeaponInd].gameObject.SetActive(true);

        //NextShoottime = new WaitForSeconds(CurWeaponInfo.rpm * 0.1667f);
    }
    //void OnScroll(InputValue value)
    //{
    //    if (CatchScroll && !_onFire && MoveAble)
    //    {
    //        _OnChangeWeapon = true;
    //        anim.SetBool("OnFire", false);
    //        var scrollV = value.Get<float>();
    //        if (scrollV > 0) CurRight = true;
    //        else if(scrollV < 0) CurRight = false;
    //        GameManager.instance.UI.SlideWeapon(CurRight);
    //        CatchScroll = false;
    //        Invoke("ChangeScroll",0.1f);
    //    }
    //}
    public void ChangeWeaponEnd()
    {
        _OnChangeWeapon = false;
    }

    void ChangeScroll()
    {
        CatchScroll = true;
    }
    #endregion
    #endregion


    // 이 밑 부분은 추후 상위 Class(Organic)로 뺄 예정. 현재 Test에는 사용하지 않음
    bool HitAble = true;
    float MaxHP = 10, CurHP = 10;
    [SerializeField]float MaxST = 1, CurST = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyAttack") && HitAble)
        {
            GameManager.instance.Audio.PlayClip(2, 0.5f, audioclips[6]);
            GetDamage(10);
        }
    }

    bool Beatable = false;
    void SetBeatable(int type)
    {
        Beatable = type == 1;
    }

    public void HealSta(float amount)
    {
        CurST = Mathf.Min(MaxST, CurST + amount);
    }

    public void GetDamage(int amount)
    {
        if (Beatable) return;
        HitAble = false; Invoke("HitGap", 0.5f);
        CurHP -= amount;
        //GameManager.instance.UI.HPChange(CurHP / MaxHP);
    }

    
    [SerializeField] List<GameObject> BuffObjects;
    public float[] BuffAmount = { 1, 1, 1, 1, 1, 1 };  // 공, 체, 속, 범위, 재장속, 돈 + 경치
    public void HealFunction(int Count)
    {
        BuffObjects[0].SetActive(true); GameManager.instance.Audio.PlayClip(2, 1, audioclips[4]);
        CurHP = Mathf.Min(MaxHP, CurHP + Count);
        //GameManager.instance.UI.HPChange(CurHP / MaxHP);
    }

    [SerializeField] Transform GainArea;
    public void SetBuffer(int type)
    {
        BuffAmount[type] += type == 2 ? 0.05f : 0.1f;
        if (type == 1) { MaxHP += 10; CurHP += 10; }
        else if (type == 3) { GainArea.localScale = new Vector3(BuffAmount[3] * 1.25f, BuffAmount[3] * 1.25f, BuffAmount[3] * 1.25f); BuffObjects[4].SetActive(true); }
        else if (type == 4) anim.SetFloat("ReloadTime", BuffAmount[4]);
        GameManager.instance.UI.ApplyOnUI(type, BuffAmount[type] - 1);
    }
    float[] LastBuffAmount = { 0, 0, 0, 0, 0, 0 };
    public void BuffOn(int type, int Last, float Amount)
    {
        if (Amount > LastBuffAmount[type - 1])
        {
            BuffAmount[type - 1] += Amount; LastBuffAmount[type - 1] = Amount;
            BuffObjects[type].SetActive(true);
            GameManager.instance.UI.SetBuff(type, Last);
            GameManager.instance.UI.ApplyOnUI(type - 1, BuffAmount[type - 1] - 1);
        }
    }

    public void BuffOff(int type)
    {
        BuffAmount[type - 1] -= LastBuffAmount[type - 1]; LastBuffAmount[type - 1] = 0;
        BuffObjects[type].SetActive(false); GameManager.instance.UI.ApplyOnUI(type - 1, BuffAmount[type - 1] - 1);
    }
}
