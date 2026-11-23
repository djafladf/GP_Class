using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class Player : MonoBehaviour
{
    Transform mTrans;
    [SerializeField] Transform[] WeaponHolder; // R, H, Back
    [SerializeField] AudioClip[] audioclips;

    [SerializeField] Rigidbody rigid;
    [SerializeField] Animator anim;
    [SerializeField] PlayerInput Input;

    void OnAnimatorMove()
    {
        Vector3 delta = anim.deltaPosition;
        delta.y = 0;

        rigid.MovePosition(rigid.position + delta);

    }

    private void Awake()
    {
        mTrans = transform.parent;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        GameManager.instance.Player = mTrans;
        GameManager.instance.PlayerScript = this;
        //GameManager.instance.PlayerHealFunc = HealFunction;
        MoveAble = true;
        OnCurve = false;
        RPos = RWeapon.localPosition; RSRot = RWeapon.localRotation;
        aoc = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = aoc;
    }

    private void Start()
    {
        CurWeapon = GameManager.instance.Data.Weapon[0];
    }

    float MoveSpeed = 2f;
    [SerializeField] bool OnCurve = true, Grounded = false, MoveAble = false;
    [SerializeField] LayerMask MoveObstacleMask;
    Vector3 NextDir = Vector3.zero;
    Vector3 camF = default, camR = default;
    private void FixedUpdate()
    {
        UpdateGrounded();
        if (!MoveAble || (OnCurve)) return;
        if (!Dir.Equals(Vector2.zero))
        {
            camF = Camera.main.transform.forward; camF.y = 0; camR = Camera.main.transform.right; camR.y = 0;
            NextDir = (camF * Dir.y + camR * Dir.x); NextDir.Normalize();
            if (GameManager.instance.Camera.LockCheck && (!OnRunning)) LockRot();
            else MoveRot();

            if(OnRunning) 
            {
                if (CurST <= 0) { OnRunning = false; anim.SetBool("OnRun", false); IsDodge = false; }
                else { GameManager.instance.UI.CharPoint(1, Time.deltaTime * 0.2f); CurST -= Time.fixedDeltaTime * 0.2f; }
            }
            float moveDist = Time.fixedDeltaTime * MoveSpeed  * (OnRunning ? 2.5f : 1); //* BuffAmount[2]
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

    
    // Attack, Dodge
    [SerializeField] CurveState curCurv = CurveState.none;
    [SerializeField] CurveState PInp = CurveState.none;
    AnimCurve curStateInfo = null;

    #region InputSystems
    #region Keyboard

    /// <summary>
    /// 0 : Fail, 1 : Success, 2 : Reserve
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    int SetAnimCurve(CurveState state)
    {
        if (!MoveAble) return 0;
        if (OnCurve)
        {
            if (PInp != CurveState.none) return 0;
            PInp = state; return 2;
        }
        
        return StartCurve(state);
    }

    int StartCurve(CurveState state,bool IsCombo = false)
    {
        if (state == CurveState.rattack) if (CurAttackLevel >= CurWeapon.One.Count) return 0;
        if (!CanPayCurve(state)) return 0;
        curStateInfo = GameManager.instance.Data.curves[(int)state - 1];
        OnCurve = true; curCurv = state;
        if (state == CurveState.rattack || state == CurveState.lattack) { trail.On(); VF.Play(); anim.SetFloat("AttackLevel", CurAttackLevel); }
        if (state == CurveState.jump) { JumpForce();Jumping = true; LeftGround = false; }
        GameManager.instance.UI.CharPoint(1, curStateInfo.StUsement);
        CurST -= curStateInfo.StUsement;

        Vector3 _nd = default;
        if (GameManager.instance.Camera.LockCheck)
        {
            if (Dir == Vector3.zero) _nd = mTrans.forward;
            else GameManager.instance.Camera.CamFront(ref Dir, ref _nd);
            Vector3 face = Camera.main.transform.forward; face.y = 0f;
            if (face.sqrMagnitude > 0.0001f) rigid.rotation = Quaternion.LookRotation(face);
        }
        else
        {
            if (Dir != Vector3.zero)
            {
                GameManager.instance.Camera.CamFront(ref Dir, ref _nd);
                if (_nd.sqrMagnitude > 0.0001f) rigid.rotation = Quaternion.LookRotation(_nd);
            }
        }

        AnimatorStateInfo before = anim.GetCurrentAnimatorStateInfo(0);

        String nextTrigger;
        if (state == CurveState.lattack || state == CurveState.rattack) nextTrigger = IsCombo ? "Combo" : curStateInfo.trigger;
        else nextTrigger = curStateInfo.trigger;
        anim.SetTrigger(nextTrigger);
        if (CheckCurState == null && state != CurveState.jump) CheckCurState = StartCoroutine(CheckEndCurve(before.fullPathHash,before.normalizedTime));
        return 1;
    }

    bool CanPayCurve(CurveState state)
    {
        if (state == CurveState.rattack || state == CurveState.lattack)
        {
            var attack = onDouble ? (OnRunning ?CurWeapon.TwoDash :CurWeapon.Two[CurAttackLevel]) : (OnRunning ? CurWeapon.OneDash : CurWeapon.One[CurAttackLevel]);
            GameManager.instance.Data.curves[(int)state - 1].StUsement = attack.StUsement;
        }

        var data = GameManager.instance.Data.curves[(int)state - 1];
        return CurST >= data.StUsement;
    }

    Coroutine CheckCurState = null;
    IEnumerator CheckEndCurve(int oldH, float oldT)
    {
        const int layer = 0;
        AnimatorStateInfo stt = default;
        // 새 실행 확인
        while (true)
        {
            yield return null;
            if (anim.IsInTransition(layer)) continue;
            stt = anim.GetCurrentAnimatorStateInfo(layer);

            if (!stt.IsTag("Curve")) continue;

            // 이전 State와 다르거나, 같은 State를 처음부터 재생한 경우
            if (stt.fullPathHash != oldH ||stt.normalizedTime + 0.05f < oldT) break;
        }

        float t = 0,st,ed;
        // 행동 중 감시
        while (true)
        {
            yield return null;
            stt = anim.GetCurrentAnimatorStateInfo(layer);
            if (!stt.IsTag("Curve")) break;
            if (anim.IsInTransition(layer))
            {
                stt = anim.GetNextAnimatorStateInfo(layer);
                if (!stt.IsTag("Curve"))
                {
                    while (anim.IsInTransition(layer)) yield return null;

                    break;
                }
            }
            else  // 연계 가능 시간 화긴
            {
                t = stt.normalizedTime;
                if (!(PInp != CurveState.none && curStateInfo.ComboAble.Contains(PInp))) continue;
                st = curStateInfo.ComboTime.List[0]; ed = curStateInfo.ComboTime.List[1];
                if (st > t || t > ed) continue;
                if (CanPayCurve(PInp))
                {
                    var nt = PInp;
                    PInp = CurveState.none;
                    CheckCurState = null;
                    EndCurve(false);
                    if(StartCurve(nt,true) == 0) yield break;
                    // Equal return;
                }
                PInp = CurveState.none;
            }
            
        }


        CheckCurState = null;
        EndCurve();
    }

    void EndCurve(bool startPending = true)
    {
        if (curCurv == CurveState.none) return;
        if (curCurv == CurveState.rattack){ trail.Off();VF.Stop(); }

        FinishCurve(curCurv, startPending);
    }


    void FinishCurve(CurveState state, bool startPending = true)
    {
        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        OnCurve = false; curCurv = CurveState.none; 
        if (startPending && PInp != CurveState.none)
        {
            CurveState next = PInp;
            PInp = CurveState.none;
            SetAnimCurve(next);
        }
    }

    void OnLock(InputValue val)
    {
        if (!MoveAble) return;
        GameManager.instance.Camera.OnLock();
        anim.SetFloat("OnLock",GameManager.instance.Camera.LockCheck ? 1 : 0);
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
        if (!MoveAble) return;
        if (value.isPressed)
        {
            if (IsDodge || CurST < Time.deltaTime) return;
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
    [SerializeField] bool Jumping = false, LeftGround = false;
    float GroundRadius = 0.1f;
    [SerializeField] LayerMask GroundMask;
    [SerializeField] Transform GroundCheck;
    void UpdateGrounded()
    {
        Vector3 pos = GroundCheck != null ? GroundCheck.position : mTrans.position;
        bool touchingGround = Physics.CheckSphere(pos, GroundRadius, GroundMask, QueryTriggerInteraction.Ignore);
        if (Jumping)
        {
            if (!touchingGround) LeftGround = true;
            if (LeftGround && touchingGround &&rigid.linearVelocity.y <= 0.05f)
            {
                Jumping = false;LeftGround = false;
                anim.SetBool("OnMove",Dir.sqrMagnitude > 0.0001f);
                if (curCurv == CurveState.jump) FinishCurve(CurveState.jump);
            }
        }

        Grounded = touchingGround;
        anim.SetBool("Grounded", Grounded);
    }
    void OnJump(InputValue value)
    {
        if (!value.isPressed ||!MoveAble ||!Grounded ||OnCurve ||Jumping) return;
        Vector3 leftRayOrigin  = mTrans.position + new Vector3(-0.05f, 0.05f, 0),
                rightRayOrigin = mTrans.position + new Vector3(0.05f, 0.05f, 0);

        if (Physics.Raycast(rightRayOrigin, Vector3.down, 0.15f, GroundMask) ||Physics.Raycast(leftRayOrigin, Vector3.down, 0.1f, GroundMask))
        {
            Jumping = true;
            SetAnimCurve(CurveState.jump);
        }
    }

    void JumpForce()
    {
        rigid.AddForce(Vector2.up * 5,ForceMode.Impulse);
    }

    Vector3 RPos, LPos;
    Quaternion RSRot, LSRot;
    [SerializeField] Transform RWeapon, LWeapon;

    bool onDouble = false;
    void OnDouble(InputValue value)
    {
        if (!value.isPressed || !MoveAble || !Grounded || OnCurve || Jumping) return;

        CurAttackLevel = 0; LastAttackTime = -1000;

        anim.SetLayerWeight(1,onDouble ? 1 : 0);
        anim.SetLayerWeight(2,onDouble ? 0 : 1);

        onDouble = onDouble == false;
    }
    void OnMenu(InputValue value)
    {
        GameManager.instance.UI.ShowMenu();
    }

    void OnInteract(InputValue value)
    {
        if (OnCurve || !MoveAble) return;
        GameManager.instance.UI.InteractFunc();
    }

    public void OnRest(Transform trans)
    {
        GameManager.instance.Camera.SetFollower(AllowFollow: false);
        anim.SetLayerWeight(1, 0); anim.SetLayerWeight(2, 0);
        MoveAble = false;
        GameManager.instance.UI.FadeInOut(t:3);
        anim.SetTrigger("Rest");
    }
    public void RestFader() { cFire.RestSetting(); }

    #endregion

    #region Mouse
    bool CatchScroll = true;
    bool CurRight = true;
    bool _OnChangeWeapon = false;

    WeaponInfo CurWeapon;
    AttackClip CurAttack;

    [SerializeField] TrailContainer trail;
    [SerializeField] VisualEffect VF;
    int CurAttackLevel = 0;
    float LastAttackTime = -1000;
    float AttackLeftTime = 3f;

    void OnLAttack(InputValue value)
    {
        if (value.isPressed)
        {
            var suc = SetAnimCurve(CurveState.rattack);
            if(suc != 0 && OnRunning)    // 예약 성공 or 즉시 발동
            {
                if(curCurv != CurveState.rattack) CurAttackLevel = CurWeapon.returnCount(onDouble) - 1;
                CurAttackLevel = (CurAttackLevel + 1) % CurWeapon.returnCount(onDouble);
                //if (Time.time - LastAttackTime >= AttackLeftTime && curCurv != CurveState.attack) CurAttackLevel = CurWeapon.returnCount(onDouble) - 1;
                //CurAttackLevel = (CurAttackLevel + 1) % CurWeapon.returnCount(onDouble);
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
    #region External

    FireLit cFire = null;
    public void LitStart(FireLit f)
    {
        MoveAble = false; Beatable = true;
        cFire = f;
        Input.DeactivateInput(); 
        anim.SetTrigger("OnLit");
    }

    public void MidLit()
    {
        cFire.LitEnd();
    }

    void StopCut()
    {
        MoveAble = true; Beatable = false;
        Input.ActivateInput();
    }

    #endregion 

    // 이 밑 부분은 추후 상위 Class(Organic)로 뺄 예정. 현재 Test에는 사용하지 않음
    bool HitAble = true;
    float MaxHP = 10, CurHP = 10, MaxST = 1, CurST = 1;

    private void OnTriggerEnter(Collider other)
    {
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


    //[SerializeField] List<GameObject> BuffObjects;
    //public float[] BuffAmount = { 1, 1, 1, 1, 1, 1 };  // 공, 체, 속, 범위, 재장속, 돈 + 경치
    //public void HealFunction(int Count)
    //{
    //    BuffObjects[0].SetActive(true); GameManager.instance.Audio.PlayClip(2, 1, audioclips[4]);
    //    CurHP = Mathf.Min(MaxHP, CurHP + Count);
    //    //GameManager.instance.UI.HPChange(CurHP / MaxHP);
    //}

    //[SerializeField] Transform GainArea;
    //public void SetBuffer(int type)
    //{
    //    BuffAmount[type] += type == 2 ? 0.05f : 0.1f;
    //    if (type == 1) { MaxHP += 10; CurHP += 10; }
    //    else if (type == 3) { GainArea.localScale = new Vector3(BuffAmount[3] * 1.25f, BuffAmount[3] * 1.25f, BuffAmount[3] * 1.25f); BuffObjects[4].SetActive(true); }
    //    else if (type == 4) anim.SetFloat("ReloadTime", BuffAmount[4]);
    //    GameManager.instance.UI.ApplyOnUI(type, BuffAmount[type] - 1);
    //}
    //float[] LastBuffAmount = { 0, 0, 0, 0, 0, 0 };
    //public void BuffOn(int type, int Last, float Amount)
    //{
    //    if (Amount > LastBuffAmount[type - 1])
    //    {
    //        BuffAmount[type - 1] += Amount; LastBuffAmount[type - 1] = Amount;
    //        BuffObjects[type].SetActive(true);
    //        GameManager.instance.UI.SetBuff(type, Last);
    //        GameManager.instance.UI.ApplyOnUI(type - 1, BuffAmount[type - 1] - 1);
    //    }
    //}

    //public void BuffOff(int type)
    //{
    //    BuffAmount[type - 1] -= LastBuffAmount[type - 1]; LastBuffAmount[type - 1] = 0;
    //    BuffObjects[type].SetActive(false); GameManager.instance.UI.ApplyOnUI(type - 1, BuffAmount[type - 1] - 1);
    //}
}
