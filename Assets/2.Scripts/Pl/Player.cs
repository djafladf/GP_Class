using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    [SerializeField] Transform Tuto;
    [SerializeField] Transform WeaponHolder;
    [SerializeField] Transform FirePos;
    [SerializeField] AudioClip[] audioclips;
    MeshRenderer muzzleFlash;

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

        ViewTarget = new Tuple<Transform, Transform>(transform,Aim);
        GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;

        GameManager.instance.Player = transform;
        GameManager.instance.PlayerScript = this;
        GameManager.instance.PlayerHealFunc = HealFunction;

        Weapons = new List<Transform>(GameManager.instance.Data.Weapon.Count);// Init Start;
        CurUnlocked = new List<int>(GameManager.instance.Data.Weapon.Count);
        Muzzles = new List<MeshRenderer>(GameManager.instance.Data.Weapon.Count);
        NextShoottime = new WaitForSeconds(0.1667f);
        

        if (anim.avatar != null) Spine = anim.GetBoneTransform(HumanBodyBones.Spine);

        gameObject.SetActive(false);
    }

    private void Start()
    {
        CurWeaponInfo = GameManager.instance.Data.Weapon[CurWeaponInd]; ReloadEnd(); 
        transform.position = new Vector3(GameManager.instance.bx * 80, 0, GameManager.instance.by * 80);
        Tuto.transform.SetParent(GameManager.instance.transform);

        Invoke("StartBug", 0.1f);
        //BuffOn(3, 60, 0.3f);

        //DroneAdd(); DroneAdd(); DroneAdd();

        //GameManager.instance.UI.ScoreUp(1000);
    }

    void StartBug()
    {
        MoveAble = true;
        //Win();
        //GetDamage(100);
    }

    float MoveSpeed = 5f;
    bool MoveAble = false;
    private void FixedUpdate()
    {
        if (!MoveAble) return;
        if (!Dir.Equals(Vector2.zero))
        {
            Vector3 NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;
            rigid.MovePosition(rigid.position + NextDir * Time.deltaTime * MoveSpeed * BuffAmount[2]);
        }
    }

    Transform Spine;
    float Spineangle, Subangle;
    private void LateUpdate()
    {
        if (Spine == null) return;
        Spineangle = Mathf.Atan2(Aim.localPosition.y - 1.2f, 1) * Mathf.Rad2Deg;
        Subangle = Mathf.Atan2(Aim.localPosition.x, Aim.localPosition.z) * Mathf.Rad2Deg;
        Spine.localRotation = Quaternion.Euler(-Subangle, 0, Spineangle);
    }


    #region Gun
    List<Transform> Weapons;
    List<int> CurUnlocked;
    List<MeshRenderer> Muzzles;
    public int CurWeaponInd = 0;
    
    Vector3 Dir;

    [SerializeField] GameObject RedDot;
    bool _onFire = false;
    void OnFire(InputValue value)
    {
        if (!MoveAble | _OnChangeWeapon | Time.timeScale == 0 | _onReload) return;
        if (value.isPressed)
        {
            RedDot.gameObject.SetActive(true);
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
            RedDot.gameObject.SetActive(false);
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

    /*IEnumerator Shoot_Sub()
    {
        if (CurUnlocked[CurWeaponInd] >= 3) { anim.SetBool("OnFire",false); yield return new WaitForSeconds(GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].rpm * 0.17f - 0.165f); }
        if (_onFire) anim.SetBool("OnFire", true);
        else { anim.SetBool("OnFire", false); FirstFire = true; }
        ShootCor = null;
    }*/

    Coroutine ShootCor = null;


    public void ShootFunction()
    {
        if (CurWeaponInfo.CurMag <= 0) return;
        float Theta = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
        float Ny = Mathf.Clamp(Aim.localPosition.y + CurWeaponInfo.bound * 0.02f, 0f, 2.4f);
        Aim.localPosition = new Vector3(Aim.localPosition.x, Ny, Aim.localPosition.z);
        GameManager.instance.Audio.PlayClip(2, 0.8f, audioclips[0]);
        if(!muzzleFlash.enabled) StartCoroutine(ShowMuzzleFlash());

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
        BulletText.text = $"{CurWeaponInfo.CurMag}/{CurWeaponInfo.MaxMag}"; 
        BulletGage.fillAmount = CurWeaponInfo.CurMag / CurWeaponInfo.MaxMag;
    }

    public void WeaponAdd(int ind)
    {
        var cnt = Instantiate(GameManager.instance.Data.Weapon[ind].Obj, WeaponHolder); Weapons.Add(cnt.transform); CurUnlocked.Add(ind); Muzzles.Add(cnt.transform.GetChild(0).GetComponent<MeshRenderer>()); cnt.SetActive(false);
        GameManager.instance.UI.AddWeaponImage(GameManager.instance.Data.Weapon[ind].Im);
        if(Weapons.Count == 1)
        {
            muzzleFlash = Muzzles[0]; FirePos = Weapons[0].GetChild(0); CurWeaponInfo = GameManager.instance.Data.Weapon[0]; Weapons[0].gameObject.SetActive(true);
        }
    }

    int DroneCount = 0;
    public void DroneAdd()
    {
        var cnt = Instantiate(GameManager.instance.Data.Drone,transform); float deg; if (DroneCount % 2 == 0) deg = DroneCount * 15f * Mathf.Deg2Rad; else deg = (195 - DroneCount * 15) * Mathf.Deg2Rad;
        cnt.transform.localPosition = new Vector3(Mathf.Cos(deg),1.2f + Mathf.Sin(deg),Random.Range(-0.3f,0.3f)); DroneCount++;
    }

    
    #endregion


    #region InputSystems

    Coroutine WalkSoundCor = null;

    bool[] SlowCall = { false,false,false}; // Move, Shoot, Reload;
    void SlowSetting(bool IsSlow,int ind)
    {
        SlowCall[ind] = IsSlow;
        if (SlowCall[0] | SlowCall[1] | SlowCall[2]) MoveSpeed = 3.5f;
        else MoveSpeed = 5f;
    }

    void OnMove(InputValue value)
    {
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

        Dir = value.Get<Vector2>();
        if (Dir.y < 0) SlowSetting(true,0);
        else SlowSetting(false,0);
        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero));

        if (!Dir.Equals(Vector2.zero) && WalkSoundCor == null) WalkSoundCor = StartCoroutine(WalkSound());
    }

    bool walkType = false;
    IEnumerator WalkSound() { while (!Dir.Equals(Vector2.zero)) { GameManager.instance.Audio.PlayClip(2, 0.7f,  walkType ? audioclips[2] : audioclips[3]); walkType =walkType == false; if (MoveSpeed == 5) yield return GameManager.DotFive; else yield return GameManager.OneSec; } WalkSoundCor = null; }

    // Space
    bool JumpAble = true;
    [SerializeField] LayerMask GroundMask;
    void OnJump(InputValue value)
    {
        if (MoveAble && JumpAble) 
        {
            Vector3 leftRayOrigin = transform.position + new Vector3(-0.05f, 0.05f, 0);
            Vector3 rightRayOrigin = transform.position + new Vector3(0.05f, 0.05f, 0);
            if (Physics.Raycast(rightRayOrigin, Vector3.down, 0.15f, GroundMask) || Physics.Raycast(leftRayOrigin, Vector3.down, 0.1f, GroundMask))
            {
                
                anim.SetTrigger("Jump"); MoveAble = false;
            }
        }
    }

    void Jump()
    {
        GameManager.instance.Audio.PlayClip(2, 1f, audioclips[5]);
        rigid.AddForce(Vector3.up * (5 + BuffAmount[2]), ForceMode.Impulse); MoveAble = true;
    }

    // Mouse
    public void SettingCameraFollowOffset(Vector3 offset)
    {
        
        if (offset.x != 0) FolllowOffset.x = (offset.x-3) * 0.1f;
        if (offset.y != 0) FolllowOffset.y = offset.y * 0.1f;
        if (offset.z != 0) FolllowOffset.z = (offset.z-20) * 0.1f;

        if (!_onFocus)
        {
            GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;
            Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z);
        }
    }
    public void SettingCameraFollowOffsetZoom(Vector3 offset)
    {
        if (offset.x != 0) FollowOffset_Z.x = (offset.x -4) * 0.1f;
        if (offset.y != 0) FollowOffset_Z.y = offset.y * 0.1f;
        if (offset.z != 0) FollowOffset_Z.z = (offset.z-4) * 0.1f;
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
    Vector3 MouseSpeed = new Vector3(25,0.4f,0);
    Vector2 MouseDir;
    Vector3 DampingVar = new Vector3(0,0,0);
    Vector3 DampingSubVar;
    void OnLook(InputValue value)
    {
        if (!MoveAble) return;
        MouseDir = value.Get<Vector2>();
        float Ny = Mathf.Clamp(Aim.localPosition.y + MouseDir.y * Time.deltaTime * MouseSpeed.y, 0f, 2.4f);
        Aim.localPosition = new Vector3(Aim.localPosition.x, Ny, Aim.localPosition.z);
        transform.Rotate(Vector3.up * MouseSpeed.x * Time.deltaTime * MouseDir.x);
    }


    [SerializeField] LayerMask OnZoom;
    [SerializeField] LayerMask OnDefault;
    [SerializeField] Vector3 AimPos;

    bool _onFocus = false; bool ControlFocusFromOther = false;
    Tuple<Transform, Transform> ViewTarget;
    Tuple<Transform, Transform> ExtViewTarget;
    public void ControllFocus(bool On, Tuple<Transform,Transform> ExtSetting)
    {
        if (On)
        {
            ControlFocusFromOther = true;
            JumpAble = false; ExtViewTarget = ExtSetting;
        }
        else
        {
            ControlFocusFromOther = false;
            Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z); Camera.main.cullingMask = OnDefault; JumpAble = true;
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
            GameManager.instance.CV.Follow =ViewTarget.Item1; GameManager.instance.CV.LookAt =ViewTarget.Item2;
            Aim.localPosition = new Vector3(FolllowOffset.x, 1.5f, AimPos.z);
        }
    }

    [SerializeField] Image BulletGage;
    [SerializeField] TMP_Text BulletText;
    bool _onReload = false;
    void OnReload(InputValue value)
    {
        if (!MoveAble | _onReload | _OnChangeWeapon) return;
        _onFire = false; SlowSetting(true,2); _onReload = true;
        GameManager.instance.Audio.PlayClip(2, 1, audioclips[1]);
        anim.SetBool("OnReload",true);
    }
    public void ReloadEnd()
    {
        CurWeaponInfo.CurMag = CurWeaponInfo.MaxMag; anim.SetBool("OnReload", false);  _onReload = false;
        BulletText.text = $"{CurWeaponInfo.CurMag}/{CurWeaponInfo.MaxMag}"; BulletGage.fillAmount = 1f;
        SlowSetting(false,2); anim.SetBool("OnFire", false); SlowSetting(false, 1);
    }

    IEnumerator ShowMuzzleFlash()
    {
        Vector2 offset = new Vector2(Random.Range(0, 2), Random.Range(0, 2)) * 0.5f;
        muzzleFlash.material.mainTextureOffset = offset;
        float angle = Random.Range(0, 360);
        muzzleFlash.transform.localRotation = Quaternion.Euler(0, 0, angle);
        float scale = Random.Range(0.04f, 0.06f);
        muzzleFlash.transform.localScale = Vector3.one * scale;
        muzzleFlash.enabled = true;
        yield return GameManager.DotOne;
        muzzleFlash.enabled = false;
    }

    void OnMenu(InputValue value)
    {
        GameManager.instance.UI.ShowMenu();
        _onFire = false; anim.SetBool("OnFire", false);
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
        Weapons[CurWeaponInd].gameObject.SetActive(false);
        CurWeaponInd = (CurWeaponInd + dr + Weapons.Count) % Weapons.Count;
        Weapons[CurWeaponInd].gameObject.SetActive(true);
        FirePos = Weapons[CurWeaponInd].GetChild(0); muzzleFlash = Muzzles[CurWeaponInd];
        CurWeaponInfo = GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]];

        NextShoottime = new WaitForSeconds(CurWeaponInfo.rpm * 0.1667f);
        //Vector3 WDir = Weapons[CurWeaponInd].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[CurWeaponInd].eulerAngles = WDir;
        BulletText.text = $"{CurWeaponInfo.CurMag}/{CurWeaponInfo.MaxMag}";
        BulletGage.fillAmount = CurWeaponInfo.CurMag / CurWeaponInfo.MaxMag;
    }
    void OnScroll(InputValue value)
    {
        if (CatchScroll && !_onFire && MoveAble && !_onReload)
        {
            _OnChangeWeapon = true;
            anim.SetBool("OnFire", false);
            var scrollV = value.Get<float>();
            if (scrollV > 0) CurRight = true;
            else if(scrollV < 0) CurRight = false;
            GameManager.instance.UI.SlideWeapon(CurRight);
            CatchScroll = false;
            Invoke("ChangeScroll",0.1f);
        }
    }
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
    public float[] BuffAmount = { 1, 1, 1 ,1, 1,1};  // 공, 체, 속, 범위, 재장속, 돈 + 경치
    public void HealFunction(int Count)
    {
        BuffObjects[0].SetActive(true); GameManager.instance.Audio.PlayClip(2,1,audioclips[4]);
        CurHP = Mathf.Min(MaxHP, CurHP + Count);
        GameManager.instance.UI.HPChange(CurHP / MaxHP);
    }

    [SerializeField] Transform GainArea;
    public void SetBuffer(int type)
    {
        BuffAmount[type] += type == 2 ? 0.05f : 0.1f;
        if (type == 1) { MaxHP += 10; CurHP += 10; GameManager.instance.UI.HPChange(CurHP / MaxHP); }
        else if (type == 3) { GainArea.localScale = new Vector3(BuffAmount[3]*1.25f, BuffAmount[3]*1.25f, BuffAmount[3]*1.25f); BuffObjects[4].SetActive(true); }
        else if (type == 4) anim.SetFloat("ReloadTime", BuffAmount[4]);
        GameManager.instance.UI.ApplyOnUI(type, BuffAmount[type] -1);
    }
    float[] LastBuffAmount = { 0, 0, 0 ,0,0,0};
    public void BuffOn(int type,int Last,float Amount)
    {
        if (Amount > LastBuffAmount[type - 1])
        {
            BuffAmount[type - 1] += Amount; LastBuffAmount[type - 1] = Amount;
            BuffObjects[type].SetActive(true);
            GameManager.instance.UI.SetBuff(type, Last);
            GameManager.instance.UI.ApplyOnUI(type-1, BuffAmount[type-1] - 1);
        }
    }

    public void BuffOff(int type)
    {
        BuffAmount[type-1] -= LastBuffAmount[type-1]; LastBuffAmount[type - 1] = 0;
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
        GameManager.instance.CVtr.m_FollowOffset = new Vector3(0,1.5f,1.5f);
        GameManager.instance.UI.GameClear(); GameManager.instance.Audio.StopBGM(); GameManager.instance.Audio.PlayClip(1, 1, audioclips[7]);
        Destroy(this);
    }
}
