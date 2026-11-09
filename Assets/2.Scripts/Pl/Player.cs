using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;


[System.Serializable]
public struct AnimCurve
{
    public string name;
    public AnimationCurve ac;
    public string trigger;
    public AnimationClip clip;
    public float speed;
    public float dist;
}

public enum CurveState
{
    none,
    roll,
    jump,
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
        CanControll = true;
    }

    float MoveSpeed = 2f;
    [SerializeField] bool CanControll = true;
    [SerializeField] bool MoveAble = false;
    [SerializeField] bool Grounded = false;
    [SerializeField] LayerMask MoveObstacleMask;

    private void Update()
    {
        Grounded = Physics.CheckSphere(mTrans.position, 0.1f, GroundMask);
        if (Grounded && rigid.linearVelocity.y < -0.01f) Jumping = false;
    }


    Vector3 NextDir = Vector3.zero;
    Vector3 camF = default, camR = default;
    private void FixedUpdate()
    {
        if (!MoveAble || !CanControll) return;
        if (!Dir.Equals(Vector2.zero))
        {
            camF = Camera.main.transform.forward; camF.y = 0; camR = Camera.main.transform.right; camR.y = 0;
            NextDir = (camF * Dir.y + camR * Dir.x); Dir.Normalize();

            if(NextDir.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(NextDir);
                mTrans.rotation = Quaternion.RotateTowards(mTrans.rotation,rot,540f * Time.deltaTime);
            }
            float moveDist = Time.fixedDeltaTime * MoveSpeed * BuffAmount[2] * (OnRunning ? 2.5f : 1);
            var ray = Physics.Raycast(origin: mTrans.position + Vector3.up, direction: NextDir, maxDistance: moveDist + 0.3f, layerMask: MoveObstacleMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
            if (!ray) rigid.MovePosition(rigid.position + NextDir * moveDist);
        }
    }

    [SerializeField] List<AnimCurve> curves;
    // Attack, Dodge
    CurveState PInp = CurveState.none;

    #region InputSystems
    #region Keyboard
    Coroutine animCurve = null;

    void SetAnimCurve(CurveState state)
    {
        if (animCurve != null) { if (PInp == CurveState.none) PInp = state; }
        else animCurve = StartCoroutine(CurveCor(state));
    }
    IEnumerator CurveCor(CurveState state)
    {
        CanControll = false;
        var cur = curves[(int)state - 1];

        anim.SetTrigger(cur.trigger);
        float t = cur.clip.length / cur.speed, ctime = 0;
        float rt = 1f / t;
        Vector3 _nd, _f,_r;
        if (onFocus)
        {
            if (Dir == Vector3.zero) _nd = mTrans.forward * cur.dist;
            else { _f = mTrans.forward; _f.y = 0; _r = mTrans.right; _r.y = 0; _nd = (_f * Dir.y + _r * Dir.x).normalized * cur.dist; }
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

        float c, l = 0, de = 0;
        while(ctime < t)
        {
            yield return new WaitForFixedUpdate();
            ctime += Time.fixedDeltaTime; if (ctime > t) ctime = t;
            c = cur.ac.Evaluate(ctime * rt); de = c - l;
            rigid.MovePosition(rigid.position + de * _nd);
            l = c;
        }
        yield return GameManager.DotOne;
        CanControll = true;
        animCurve = null;

        if (PInp != CurveState.none) { SetAnimCurve(PInp); PInp = CurveState.none; }
    }
    bool onFocus = false;
    void OnFocus(InputValue val)
    {

    }

    // Moving
    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>();
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero) && Grounded);
        //Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        //anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        //if (!Dir.Equals(Vector2.zero) && WalkSoundCor == null) WalkSoundCor = StartCoroutine(WalkSound());
    }
    Coroutine DJudge = null;
    bool OnRunning = false;
    bool IsDodge = false;
    IEnumerator DodgeJudge()
    {
        IsDodge = true;
        yield return GameManager.DotThree;
        IsDodge = false;
        DJudge = null;
    }

    void Dodge()
    {
        SetBeatable(1);
        SetAnimCurve(CurveState.roll);
    }

    void OnDodge(InputValue value)
    {
        if (value.isPressed)
        {
            OnRunning = true;
            if (DJudge != null) StopCoroutine(DJudge);
            DJudge = StartCoroutine(DodgeJudge());
        }
        else
        {
            if (IsDodge) Dodge();
            OnRunning = false;
        }
    }

    Vector3 Dir;
    bool walkType = false;

    // Space
    bool Jumping = false;
    Vector3 JumpDir;
    [SerializeField] LayerMask GroundMask;
    void OnJump(InputValue value)
    {
        if (!MoveAble || !Grounded || Jumping) return;
        Vector3 leftRayOrigin = mTrans.position + new Vector3(-0.05f, 0.05f, 0);
        Vector3 rightRayOrigin = mTrans.position + new Vector3(0.05f, 0.05f, 0);
        if (Physics.Raycast(rightRayOrigin, Vector3.down, 0.15f, GroundMask) || Physics.Raycast(leftRayOrigin, Vector3.down, 0.1f, GroundMask))
        {
            JumpDir = Dir; anim.SetTrigger("Jump"); CanControll = false; Jumping = true;
        }
    }

    void Jump()
    {
        GameManager.instance.Audio.PlayClip(2, 1f, audioclips[5]); MoveAble = true;
        rigid.AddForce((Vector3.up + NextDir * 0.5f) * (5 + BuffAmount[2]), ForceMode.Impulse);
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
    Vector2 MouseDir;
    Vector3 DampingVar = new Vector3(0, 0, 0);
    void OnLook(InputValue value)
    {
        MouseDir = value.Get<Vector2>();
        float Ny = Mathf.Clamp(Aim.localPosition.y + MouseDir.y * Time.deltaTime * MouseSpeed.y, 0f, 2.4f);
        Aim.localPosition = new Vector3(Aim.localPosition.x, Ny, Aim.localPosition.z);
        //transform.Rotate(Vector3.up * MouseSpeed.x * Time.deltaTime * MouseDir.x);  // 나중에 빼야됨
    }
    WeaponInfo CurWeaponInfo;
    bool CatchScroll = true;
    bool CurRight = true;
    bool _OnChangeWeapon = false;

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
            GameManager.instance.Enemy.OnGameEnd();
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
