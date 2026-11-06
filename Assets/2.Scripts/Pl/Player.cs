using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] Transform WeaponHolder;
    [SerializeField] Transform FirePos;
    [SerializeField] AudioClip[] audioclips;

    [SerializeField] Vector3 FolllowOffset;
    [SerializeField] Vector3 FollowOffset_Z;

    Rigidbody rigid;
    Animator anim;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        GameManager.instance.CV.Follow = transform;
        GameManager.instance.CV.LookAt = Aim;

        ViewTarget = new Tuple<Transform, Transform>(transform, Aim);
        GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;

        GameManager.instance.Player = transform;
        GameManager.instance.PlayerScript = this;
        GameManager.instance.PlayerHealFunc = HealFunction;

        //Weapons = new List<Transform>(GameManager.instance.Data.Weapon.Count);// Init Start;
        //NextShoottime = new WaitForSeconds(0.1667f);


        //if (anim.avatar != null) Spine = anim.GetBoneTransform(HumanBodyBones.Spine);

        //gameObject.SetActive(false);
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
        //Win();
        //GetDamage(100);
    }

    float MoveSpeed = 5f;
    [SerializeField] bool MoveAble = false;
    [SerializeField] bool Grounded = false;
    [SerializeField] LayerMask MoveObstacleMask;
    [SerializeField] Vector3 CurVelocity;

    private void Update()
    {
        CurVelocity = rigid.linearVelocity;
        Grounded = Physics.CheckSphere(transform.position, 0.1f, GroundMask);
        if (Grounded && rigid.linearVelocity.y < -0.01f) Jumping = false;
    }


    Vector3 NextDir = Vector3.zero;
    private void FixedUpdate()
    {
        if (!MoveAble) return;
        if (!Dir.Equals(Vector2.zero))
        {
            NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;

            float moveDist = Time.fixedDeltaTime * MoveSpeed * BuffAmount[2];
            var ray = Physics.Raycast(origin: transform.position + Vector3.up, direction: NextDir, maxDistance: moveDist + 0.3f, layerMask: MoveObstacleMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
            if (!ray) rigid.MovePosition(rigid.position + NextDir * moveDist * (Jumping ? 0.2f : 1f));
        }
    }

    // Spine 회전

    Transform Spine;
    float Spineangle, Subangle;
    private void LateUpdate()
    {
        if (Spine == null) return;
        Spineangle = Mathf.Atan2(Aim.localPosition.y - 1.2f, 1) * Mathf.Rad2Deg;
        Subangle = Mathf.Atan2(Aim.localPosition.x, Aim.localPosition.z) * Mathf.Rad2Deg;
        Spine.localRotation = Quaternion.Euler(-Subangle, 0, Spineangle);
    }

    /*
        #region Gun
        List<Transform> Weapons;
        public int CurWeaponInd = 0;

        [SerializeField] GameObject RedDot;
        bool _onFire = false;
        void OnFire(InputValue value)
        {
            if (!MoveAble | _OnChangeWeapon | Time.timeScale == 0) return;
            if (value.isPressed)
            {
                _onFire = true;
                anim.SetBool("OnFire", true);
                SlowSetting(true,1);
                if (ShootCor == null) ShootCor = StartCoroutine(ShootTrigger());
            }
            else
            {
                SlowSetting(false,1);
                _onFire = false;
                anim.SetBool("OnFire", false);
            }
        }
        WaitForSeconds NextShoottime;
        IEnumerator ShootTrigger()
        {
            while (_onFire)
            {
                ShootFunction();
                yield return NextShoottime;
            }

            ShootCor = null;
        }

        IEnumerator Shoot_Sub()
        {
            if (CurUnlocked[CurWeaponInd] >= 3) { anim.SetBool("OnFire",false); yield return new WaitForSeconds(GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].rpm * 0.17f - 0.165f); }
            if (_onFire) anim.SetBool("OnFire", true);
            else { anim.SetBool("OnFire", false); FirstFire = true; }
            ShootCor = null;
        }

        Coroutine ShootCor = null;


        public void ShootFunction()
        {
            if (CurWeaponInfo.CurMag <= 0) return;
            float Theta = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
            float Ny = Mathf.Clamp(Aim.localPosition.y + CurWeaponInfo.bound * 0.02f, 0f, 2.4f);
            Aim.localPosition = new Vector3(Aim.localPosition.x, Ny, Aim.localPosition.z);
            GameManager.instance.Audio.PlayClip(2, 0.8f, audioclips[0]);

            for (int _ = 0; _ < CurWeaponInfo.bnum; _++)
            {
                Quaternion randomRot = Quaternion.Euler(
                       Random.Range(-CurWeaponInfo.spread, CurWeaponInfo.spread), // 상하
                       Random.Range(-CurWeaponInfo.spread, CurWeaponInfo.spread), // 좌우
                       0
                   );
                Vector3 spreadDirection = randomRot * Weapons[CurWeaponInd].forward;
                GameManager.instance.bullet.ShootBullet(FirePos.position, spreadDirection, CurWeaponInfo.power * Random.Range(0.5f,1.5f));
            }
            CurWeaponInfo.CurMag -= 1;
        }

        public void WeaponAdd(int ind)
        {
            //var cnt = Instantiate(GameManager.instance.Data.Weapon[ind].Obj, WeaponHolder); Weapons.Add(cnt.transform);cnt.SetActive(false);
            GameManager.instance.UI.AddWeaponImage(GameManager.instance.Data.Weapon[ind].Im);
            if(Weapons.Count == 1)
            {
                CurWeaponInfo = GameManager.instance.Data.Weapon[0]; Weapons[0].gameObject.SetActive(true);
            }
        }


        #endregion
    */

    #region InputSystems

    Coroutine WalkSoundCor = null;

    bool[] SlowCall = { false, false, false }; // Move, Shoot, Reload;
    void SlowSetting(bool IsSlow, int ind)
    {
        SlowCall[ind] = IsSlow;
        if (SlowCall[0] | SlowCall[1] | SlowCall[2]) MoveSpeed = 3.5f;
        else MoveSpeed = 5f;
    }

    void OnMove(InputValue value)
    {
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

        Dir = value.Get<Vector2>();
        if (Dir.y < 0) SlowSetting(true, 0);
        else SlowSetting(false, 0);
        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero) && Grounded);

        if (!Dir.Equals(Vector2.zero) && WalkSoundCor == null) WalkSoundCor = StartCoroutine(WalkSound());
    }

    Vector3 Dir;
    bool walkType = false;
    IEnumerator WalkSound() { while (!Dir.Equals(Vector2.zero)) { GameManager.instance.Audio.PlayClip(2, 0.7f, walkType ? audioclips[2] : audioclips[3]); walkType = walkType == false; if (MoveSpeed == 5) yield return GameManager.DotFive; else yield return GameManager.OneSec; } WalkSoundCor = null; }

    // Space
    bool Jumping = false;
    Vector3 JumpDir;
    [SerializeField] LayerMask GroundMask;
    void OnJump(InputValue value)
    {
        if (!MoveAble || !Grounded || Jumping) return;
        Vector3 leftRayOrigin = transform.position + new Vector3(-0.05f, 0.05f, 0);
        Vector3 rightRayOrigin = transform.position + new Vector3(0.05f, 0.05f, 0);
        if (Physics.Raycast(rightRayOrigin, Vector3.down, 0.15f, GroundMask) || Physics.Raycast(leftRayOrigin, Vector3.down, 0.1f, GroundMask))
        {
            JumpDir = Dir; anim.SetTrigger("Jump"); MoveAble = false; Jumping = true;
        }
    }

    void Jump()
    {
        GameManager.instance.Audio.PlayClip(2, 1f, audioclips[5]); MoveAble = true;
        rigid.AddForce((Vector3.up + NextDir * 0.5f) * (5 + BuffAmount[2]), ForceMode.Impulse);
    }

    // Mouse
    public void SettingCameraFollowOffset(Vector3 offset)
    {

        if (offset.x != 0) FolllowOffset.x = (offset.x - 3) * 0.1f;
        if (offset.y != 0) FolllowOffset.y = offset.y * 0.1f;
        if (offset.z != 0) FolllowOffset.z = (offset.z - 20) * 0.1f;

        if (!_onFocus)
        {
            GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;
            Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z);
        }
    }
    public void SettingCameraFollowOffsetZoom(Vector3 offset)
    {
        if (offset.x != 0) FollowOffset_Z.x = (offset.x - 4) * 0.1f;
        if (offset.y != 0) FollowOffset_Z.y = offset.y * 0.1f;
        if (offset.z != 0) FollowOffset_Z.z = (offset.z - 4) * 0.1f;
        AimPos = new Vector3(FollowOffset_Z.x, FollowOffset_Z.y, 3);
        if (_onFocus)
        {
            GameManager.instance.CVtr.m_FollowOffset = FollowOffset_Z;
            Aim.localPosition = AimPos;
        }
    }

    public void SettingCameraSpeed(Vector3 offset)
    {
        if (offset.x != 0) MouseSpeed.x = offset.x * 5;
        if (offset.y != 0) MouseSpeed.y = offset.y * 0.1f;
    }

    public void SettingCameraDamping(Vector3 offset)
    {
        DampingVar = offset;
        GameManager.instance.CVtr.m_XDamping = DampingVar.x;
        GameManager.instance.CVtr.m_YDamping = DampingVar.y;
        GameManager.instance.CVtr.m_ZDamping = DampingVar.z;
    }

    [SerializeField] Transform Aim;
    Vector3 MouseSpeed = new Vector3(25, 0.4f, 0);
    Vector2 MouseDir;
    Vector3 DampingVar = new Vector3(0, 0, 0);
    Vector3 DampingSubVar;
    void OnLook(InputValue value)
    {
        if (!MoveAble) return;
        MouseDir = value.Get<Vector2>();
        float Ny = Mathf.Clamp(Aim.localPosition.y + MouseDir.y * Time.deltaTime * MouseSpeed.y, 0f, 2.4f);
        Aim.localPosition = new Vector3(Aim.localPosition.x, Ny, Aim.localPosition.z);
        transform.Rotate(Vector3.up * MouseSpeed.x * Time.deltaTime * MouseDir.x);  // 나중에 빼야됨
    }


    [SerializeField] LayerMask OnZoom;
    [SerializeField] LayerMask OnDefault;
    [SerializeField] Vector3 AimPos;

    bool _onFocus = false; bool ControlFocusFromOther = false;
    Tuple<Transform, Transform> ViewTarget;
    Tuple<Transform, Transform> ExtViewTarget;
    public void ControllFocus(bool On, Tuple<Transform, Transform> ExtSetting)
    {
        if (On)
        {
            ControlFocusFromOther = true;
            ExtViewTarget = ExtSetting;
        }
        else
        {
            ControlFocusFromOther = false;
            Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z); Camera.main.cullingMask = OnDefault;
        }
        _onFocus = false;
        GameManager.instance.CV.PreviousStateIsValid = false;
        Camera.main.cullingMask = OnDefault;
        GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;
        GameManager.instance.CV.Follow = ViewTarget.Item1; GameManager.instance.CV.LookAt = ViewTarget.Item2;
        Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z);
    }

    void OnFocus(InputValue value)
    {
        if (!MoveAble) return;
        _onFocus = _onFocus == false;
        if (_onFocus)
        {
            DampingSubVar = DampingVar; SettingCameraDamping(Vector3.zero);
            GameManager.instance.CV.PreviousStateIsValid = false;
            Camera.main.cullingMask = ControlFocusFromOther ? OnDefault : OnZoom; GameManager.instance.CVtr.m_FollowOffset = FollowOffset_Z;
            GameManager.instance.CV.Follow = ControlFocusFromOther ? ExtViewTarget.Item1 : ViewTarget.Item1; GameManager.instance.CV.LookAt = ControlFocusFromOther ? ExtViewTarget.Item2 : ViewTarget.Item2;
            Aim.localPosition = AimPos;
        }
        else
        {
            SettingCameraDamping(DampingSubVar);
            GameManager.instance.CV.PreviousStateIsValid = false;
            Camera.main.cullingMask = OnDefault;
            GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;
            GameManager.instance.CV.Follow = ViewTarget.Item1; GameManager.instance.CV.LookAt = ViewTarget.Item2;
            Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z);
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

    public void GetDamage(int amount)
    {
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

    bool ControllFromExtern = true;
    public void ControllMoveAble(bool Toggle)
    {
        MoveAble = Toggle;
        ControllFromExtern = Toggle;
    }

    void HitGap()
    {
        HitAble = true;
    }

    public void Win()
    {
        anim.Rebind(); anim.Update(0); anim.SetTrigger("Win");
        var cnt = new GameObject(); cnt.transform.parent = transform; cnt.transform.localPosition = new Vector3(0, 1.5f, 0);
        GameManager.instance.CV.Follow = transform;
        GameManager.instance.CV.LookAt = cnt.transform;
        GameManager.instance.CVtr.m_FollowOffset = new Vector3(0, 1.5f, 1.5f);
        GameManager.instance.UI.GameClear(); GameManager.instance.Audio.StopBGM(); GameManager.instance.Audio.PlayClip(1, 1, audioclips[7]);
        Destroy(this);
    }
}
