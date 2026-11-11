using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.Core;
using Unity.Cinemachine;
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
}

[System.Serializable]
public struct AttackClips
{
    public List<int> DamageRatio; // 추후 스탯별 분리 예정
    public List<AnimationClip> Clips;
    public AttackClips( List<int> dm, List<AnimationClip> cls) { DamageRatio = dm; Clips = cls; }
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
    [SerializeField] Transform WeaponHolder;
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
        //rigid = GetComponent<Rigidbody>();
        //nim = GetComponent<Animator>();
        //GameManager.instance.CV.Follow = transform;
        //GameManager.instance.CV.LookAt = Aim;

        GameManager.instance.Player = mTrans;
        GameManager.instance.PlayerScript = this;
        GameManager.instance.PlayerHealFunc = HealFunction;
    }

    private void Start()
    {
        //CurWeaponInfo = GameManager.instance.Data.Weapon[CurWeaponInd];
        //transform.position = new Vector3(GameManager.instance.bx * 80, 0, GameManager.instance.by * 80);
        Invoke("StartBug", 0.1f);
    }

    void StartBug()
    {
        MoveAble = true;
        OnCurve = false;
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
            if (onLock && (!OnRunning)) LockRot();
            else MoveRot();
            float moveDist = Time.fixedDeltaTime * MoveSpeed * BuffAmount[2] * (OnRunning ? 2.5f : 1);
            var ray = Physics.Raycast(origin: mTrans.position + Vector3.up, direction: NextDir, maxDistance: moveDist + 0.3f, layerMask: MoveObstacleMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
            if (!ray) rigid.MovePosition(rigid.position + NextDir * moveDist);
        }
    }

    Quaternion rot;
    void LockRot()
    {
        if (CurLock == null) return;
        if (drLock.sqrMagnitude > 0.001f) rigid.MoveRotation(Quaternion.LookRotation(drLock));
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
            curves[2].clip = CurAttack.Clips[CurAttackLevel];
            anim.SetInteger("AttackLevel", CurAttackLevel);
        }

        var cur = curves[(int)state - 1];

        anim.SetTrigger(cur.trigger);
        float t = cur.clip.length / cur.speed, ctime = 0;
        float rt = 1f / t;
        Vector3 _nd, _f,_r;
        if (onLock)
        {
            mTrans.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            if (Dir == Vector3.zero) _nd = mTrans.forward * cur.dist;
            else 
            {
                _f = Camera.main.transform.forward; _f.y = 0; _r = Camera.main.transform.right; _r.y = 0; _nd = (_f * Dir.y + _r * Dir.x).normalized;
                _nd *= cur.dist; 
            }
        }
        else
        {
            if (Dir != Vector3.zero)
            {
                _f = Camera.main.transform.forward; _f.y = 0; _r = Camera.main.transform.right; _r.y = 0; _nd = (_f * Dir.y + _r * Dir.x); _nd.Normalize();  
                mTrans.rotation = Quaternion.LookRotation(_nd);
            }
            _nd = mTrans.forward * cur.dist;
        }

        float c,l = cur.ac.Evaluate(0f), de = 0;
        while(ctime < t)
        {
            yield return new WaitForFixedUpdate();
            ctime += Time.fixedDeltaTime; if (ctime > t) ctime = t;
            c = cur.ac.Evaluate(ctime * rt); de = c - l;
            rigid.MovePosition(rigid.position + de * _nd);
            l = c;
        }
        yield return GameManager.DotOne;

        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        OnCurve = false;
        animCurve = null;

        if (PInp != CurveState.none) { SetAnimCurve(PInp); PInp = CurveState.none; }
        else curCurv = CurveState.none;
    }

    PriorityQueue<Transform, float> LockPQ = new((a, b) => a.Item2 < b.Item2);
    HashSet<Transform> OnUse = new();
    [SerializeField] Transform CurLock = null;
    [SerializeField] bool onLock = false;
    Collider[] Target = new Collider[32];
    int EMask = 1 << 7, OMask = (1 << 6) | (1 << 12);
    void OnLock(InputValue val)
    {
        var OriginPos = transform.position + Vector3.up;
        int res = Physics.OverlapSphereNonAlloc(OriginPos,15f,Target,EMask);

        Transform tcnt;
        Vector3 pcnt;
        float dist;
        float w = 2; // Max Dist

        LockPQ.Clear(); // Lock On을 한 시점 Dist는 달라질 수 있기에 매번 갱신이 필요하지 않나.
        for(int i = 0; i < res; i++)
        {
            tcnt = Target[i].transform;
            pcnt = Camera.main.WorldToViewportPoint(Target[i].bounds.center);
            if (pcnt.z <= 0 || pcnt.x < 0 || pcnt.x > 1 || pcnt.y < 0 || pcnt.y > 1)
            {
                OnUse.Remove(tcnt);
                continue;
            }
            if (Physics.Linecast(OriginPos, Target[i].bounds.center, OMask)) continue;

            pcnt.x -= 0.5f; pcnt.y -= 0.5f; pcnt.z = 0;
            dist = pcnt.sqrMagnitude;
            if (OnUse.Contains(Target[i].transform)) dist += w;

            LockPQ.push((tcnt,dist));
        }

        if (LockPQ.Count == 0) LockFunc(false);
        else
        {
            var to = LockPQ.pop();
            if (to.Item2 >= 2) LockFunc(false);
            else { CurLock = to.Item1; LockFunc(); }
        }
    }

    // 나중에 CamManager나 GameManager로 분리하셈.

    [SerializeField] List<Transform> LockOrbs;  // Mid, Aim
    Coroutine LockCor = null;
    Vector3 drLock;
    IEnumerator LockUpdater()
    {
        var orb = GameManager.instance.FC[1].GetComponent<CinemachineOrbitalFollow>();
        while (true)
        {
            drLock = CurLock.position - LockOrbs[0].position; drLock.y = 0;
            if (drLock.sqrMagnitude >= 1f) orb.HorizontalAxis.Value = Mathf.Atan2(drLock.x, drLock.z) * Mathf.Rad2Deg;
            LockOrbs[1].position = Vector3.Lerp(LockOrbs[0].position,CurLock.position,0.4f);
            yield return null;
        }
    }

    void LockFunc(bool Lock = true,Transform To = null)
    {
        if (To != null) CurLock = To;
        if (Lock)
        {
            if(LockCor == null) LockCor = StartCoroutine(LockUpdater());
            anim.SetBool("OnLock",true);
            onLock = true; OnUse.Add(CurLock);
            GameManager.instance.FC[1].Priority = 20;
        }
        else
        {
            if (LockCor != null) { StopCoroutine(LockCor); LockCor = null; }
            GameManager.instance.FC[1].Priority = 0;
            anim.SetBool("OnLock", false);
            GameManager.instance.FC[0].transform.position = GameManager.instance.FC[1].transform.position;
            onLock = false; CurLock = null; OnUse.Clear();
        }
    }

    // Moving
    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>();
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero) && Grounded);
        if (!OnCurve) { anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y); }

    }
    void Dodge()
    {
        SetBeatable(1);
        SetAnimCurve(CurveState.roll);
    }

    float LastDodge = 0;
    float DodgeGap = 0.3f;
    bool IsDodge = false;
    bool OnRunning = false;
    void OnDodge(InputValue value)
    {
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
            if (Time.unscaledTime - LastDodge < DodgeGap) Dodge();
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
    [SerializeField] Transform Aim;
    Vector3 MouseSpeed = new Vector3(25, 0.4f, 0);
    Vector3 DampingVar = new Vector3(0, 0, 0);
    WeaponInfo CurWeaponInfo;
    bool CatchScroll = true;
    bool CurRight = true;
    bool _OnChangeWeapon = false;

    
    [SerializeField] AttackClips CurAttack;
    [SerializeField] int CurAttackLevel = 0;
    float LastAttackTime = 0;
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
                if (Time.time - LastAttackTime >= AttackLeftTime) CurAttackLevel = CurAttack.Clips.Count - 1;
                CurAttackLevel = (CurAttackLevel + 1) % CurAttack.Clips.Count;
            }
        }
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


    // 이 밑 부분은 추후 상위 Class로 뺄 예정. 현재 Test에는 사용하지 않음
    bool HitAble = true;
    float MaxHP = 100;
    float CurHP = 100;

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

    public void GetDamage(int amount)
    {
        if (Beatable) return;
        HitAble = false; Invoke("HitGap", 0.5f);
        CurHP -= amount;
        GameManager.instance.UI.HPChange(CurHP / MaxHP);
        if (CurHP <= 0)
        {
            anim.SetTrigger("Lose"); StopAllCoroutines();
            GameManager.instance.UI.EndGame();
            Destroy(this);

        }
    }

    
    [SerializeField] List<GameObject> BuffObjects;
    public float[] BuffAmount = { 1, 1, 1, 1, 1, 1 };  // 공, 체, 속, 범위, 재장속, 돈 + 경치
    public void HealFunction(int Count)
    {
        BuffObjects[0].SetActive(true); GameManager.instance.Audio.PlayClip(2, 1, audioclips[4]);
        CurHP = Mathf.Min(MaxHP, CurHP + Count);
        GameManager.instance.UI.HPChange(CurHP / MaxHP);
    }

    [SerializeField] Transform GainArea;
    public void SetBuffer(int type)
    {
        BuffAmount[type] += type == 2 ? 0.05f : 0.1f;
        if (type == 1) { MaxHP += 10; CurHP += 10; GameManager.instance.UI.HPChange(CurHP / MaxHP); }
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

    void HitGap()
    {
        HitAble = true;
    }

    // Setting -> 추후 다른 Script로 이관 예정
    public void SettingCameraFollowOffset(Vector3 offset)
    {
        if (offset.x != 0) FolllowOffset.x = (offset.x - 3) * 0.1f;
        if (offset.y != 0) FolllowOffset.y = offset.y * 0.1f;
        if (offset.z != 0) FolllowOffset.z = (offset.z - 20) * 0.1f;
    }
    public void SettingCameraFollowOffsetZoom(Vector3 offset)
    {
        if (offset.x != 0) FollowOffset_Z.x = (offset.x - 4) * 0.1f;
        if (offset.y != 0) FollowOffset_Z.y = offset.y * 0.1f;
        if (offset.z != 0) FollowOffset_Z.z = (offset.z - 4) * 0.1f;
    }

    public void SettingCameraSpeed(Vector3 offset)
    {
        if (offset.x != 0) MouseSpeed.x = offset.x * 5;
        if (offset.y != 0) MouseSpeed.y = offset.y * 0.1f;
    }

    //public void SettingCameraDamping(Vector3 offset)
    //{
    //    DampingVar = offset;
    //    GameManager.instance.Cv.m_XDamping = DampingVar.x;
    //    GameManager.instance.CVtr.m_YDamping = DampingVar.y;
    //    GameManager.instance.CVtr.m_ZDamping = DampingVar.z;
    //}
}
