using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] Transform WeaponHolder;
    [SerializeField] Transform FirePos;
    [SerializeField] CinemachineVirtualCamera CV;
    [SerializeField] AudioClip[] audioclips;
    [SerializeField] MeshRenderer muzzleFlash;

    Rigidbody rigid;
    Animator anim;

    CinemachineTransposer transposer;
    AudioSource audiodio;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        transposer = CV.GetCinemachineComponent<CinemachineTransposer>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        audiodio = GetComponent<AudioSource>();

        AttackGap[0] = 0.1667f;
    }

    private void Start()
    {
        GameManager.instance.Player = transform;
        GameManager.instance.PlayerScript = this;
        GameManager.instance.PlayerHealFunc = HealFunction;
        transform.position = new Vector3(GameManager.instance.bx * 80, 0, GameManager.instance.by * 80);
        Invoke("StartBug", 1f);
    }

    void StartBug()
    {
        MoveAble = true;
    }

    float MoveSpeed = 5f;
    bool MoveAble = false;
    private void FixedUpdate()
    {
        if (!MoveAble) return;
        if (!Dir.Equals(Vector2.zero))
        {
            Vector3 NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;
            rigid.MovePosition(rigid.position + NextDir * Time.deltaTime * (MoveSpeed + BuffAmount[2]));
        }
    }

    Vector3 Dir;
    IEnumerator Shoot_Sub()
    {
        if (CurWeaponInd != 0) { anim.SetBool("OnFire",false); yield return new WaitForSeconds(AttackGap[CurWeaponInd] - AttackGap[0]); }
        if (_onFire) anim.SetBool("OnFire", true);
        else { anim.SetBool("OnFire", false); FirstFire = true; }
        ShootCor = null;
    }

    Coroutine ShootCor = null;
    void EndShoot()
    {
        if(ShootCor == null) ShootCor = StartCoroutine(Shoot_Sub());
    }

    public void Shoot()
    {
        if (ShootCor != null) return;
        if (LeftBul[CurWeaponInd] <= 0) return;
        float Theta = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
        if (Aim.localPosition.y < 3)
        {
            Aim.Translate(0, Rebound[CurWeaponInd], 0);
            if (Weapons[CurWeaponInd] != null) { Vector3 WDir = Weapons[CurWeaponInd].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[CurWeaponInd].eulerAngles = WDir; }
        }
        audiodio.PlayOneShot(audioclips[0],0.8f);
        if(!muzzleFlash.enabled)StartCoroutine(ShowMuzzleFlash());
        if (WeaponType[CurWeaponInd]) GameManager.instance.bullet.ShootBullet(FirePos.position, Weapons[CurWeaponInd].forward);
        else
        {
            for(int _ = 0; _ < 9; _++)
            {
                Quaternion randomRot = Quaternion.Euler(
                   Random.Range(-5f, 5f), // 상하
                   Random.Range(-5f, 5f), // 좌우
                   0
               );

                Vector3 spreadDirection = randomRot * Weapons[CurWeaponInd].forward;

                GameManager.instance.bullet.ShootBullet(FirePos.position, spreadDirection);
            }
        }
        LeftBul[CurWeaponInd]--; BulletText.text = $"{LeftBul[CurWeaponInd]}/{MaxBul[CurWeaponInd]}"; BulletGage.fillAmount = LeftBul[CurWeaponInd] / MaxBul[CurWeaponInd];
    }

    public int CurWeaponInd = 0;
    [SerializeField] List<Transform> Weapons;
    [SerializeField] List<float> Rebound;
    [SerializeField] List<bool> WeaponType;
    [SerializeField] List<float> AttackGap;
    [SerializeField] List<float> LeftBul;
    [SerializeField] List<float> MaxBul;

    #region InputSystems

    Coroutine WalkSoundCor = null;
    // WASD, Shaft
    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>();
        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero));
        if (!Dir.Equals(Vector2.zero) && WalkSoundCor == null) WalkSoundCor = StartCoroutine(WalkSound());
    }
    bool walkType = false;
    IEnumerator WalkSound() { while (!Dir.Equals(Vector2.zero)) { audiodio.PlayOneShot(walkType ? audioclips[2] : audioclips[3], 0.8f); walkType =walkType == false; yield return GameManager.DotThree; } WalkSoundCor = null; }

    // Space
    bool JumpAble = false;
    void OnJump(InputValue value)
    {
        if (JumpAble && MoveAble) { rigid.AddForce(Vector3.up * (5 + BuffAmount[2]), ForceMode.Impulse); JumpAble = false; }
    }

    // Mouse
    [SerializeField] Transform Aim;
    Vector2 MouseDir;
    void OnLook(InputValue value)
    {
        if (!MoveAble) return;
        MouseDir = value.Get<Vector2>();
        float Ny = Mathf.Clamp(Aim.localPosition.y + MouseDir.y * Time.deltaTime * 0.4f, 0f, 3f);
        Aim.localPosition = new Vector3(0.5f, Ny, Aim.localPosition.z);
        if (Weapons[CurWeaponInd] != null) { Vector3 WDir = Weapons[CurWeaponInd].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[CurWeaponInd].eulerAngles = WDir;  }
        transform.Rotate(Vector3.up * 25f * Time.deltaTime * MouseDir.x);
    }

    bool _onFire = false; bool FirstFire = true;
    void OnFire(InputValue value)
    {
        if (!MoveAble) return;
        _onFire = value.isPressed;
        if (_onFire && FirstFire) { anim.SetBool("OnFire", true); FirstFire = false; }

    }

    bool _onFocus = false;
    void OnFocus(InputValue value)
    {
        if (!MoveAble) return;
        _onFocus = _onFocus == false;
        if (_onFocus) transposer.m_FollowOffset = new Vector3(0.5f, 1.7f, 0.2f);
        else
        {
            transposer.m_FollowOffset = new Vector3(0.5f, 2, -1.5f);
            Aim.localPosition = new Vector3(0.5f, 1.5f, Aim.localPosition.z);
        }
    }

    [SerializeField] Image BulletGage;
    [SerializeField] TMP_Text BulletText;
    void OnReload(InputValue value)
    {
        if (!MoveAble) return;
        MoveAble = false; Dir = Vector3.zero;
        audiodio.PlayOneShot(audioclips[1],1);
        anim.SetTrigger("OnReload");
    }
    public void ReloadEnd()
    {
        MoveAble = true && ControllFromExtern; LeftBul[CurWeaponInd] = MaxBul[CurWeaponInd];
        BulletText.text = $"{LeftBul[CurWeaponInd]}/{MaxBul[CurWeaponInd]}"; BulletGage.fillAmount = 1f;
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
    }

    void OnInteract(InputValue value)
    {
        GameManager.instance.UI.InteractSomething();
    }


    bool CatchScroll = true;
    bool CurRight = true;

    public void ChangeWeapon(int dr)
    {
        Weapons[CurWeaponInd].gameObject.SetActive(false);
        CurWeaponInd = (CurWeaponInd + dr + Weapons.Count) % Weapons.Count;
        Weapons[CurWeaponInd].gameObject.SetActive(true);
        FirePos = Weapons[CurWeaponInd].GetChild(0); muzzleFlash = FirePos.GetComponent<MeshRenderer>();
        Vector3 WDir = Weapons[CurWeaponInd].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[CurWeaponInd].eulerAngles = WDir;
        BulletText.text = $"{LeftBul[CurWeaponInd]}/{MaxBul[CurWeaponInd]}"; BulletGage.fillAmount = LeftBul[CurWeaponInd] / MaxBul[CurWeaponInd];
    }
    void OnScroll(InputValue value)
    {
        if (CatchScroll && !anim.GetBool("OnFire") && MoveAble)
        {
            anim.SetBool("OnChangeWeapon", true);
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
        anim.SetBool("OnChangeWeapon", false);
    }

    void ChangeScroll()
    {
        CatchScroll = true;
    }
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) JumpAble = true;
    }

    bool HitAble = true;
    float MaxHP = 100;
    float CurHP = 100;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyAttack") && HitAble)
        {
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

            GameManager.instance.Enemy.OnGameEnd();
        }
    }

    [SerializeField] List<GameObject> BuffObjects;
    float[] BuffAmount = { 0, 0, 0 ,1, 0};  // 공, 방, 속, 범위, 재장속
    public void HealFunction(int Count)
    {
        BuffObjects[0].SetActive(true);
        CurHP = Mathf.Min(MaxHP, CurHP + Count);
        GameManager.instance.UI.HPChange(CurHP / MaxHP);
    }

    [SerializeField] Transform GainArea;
    public void SetBuffer(int type)
    {
        BuffAmount[type] += 0.1f;
        if (type == 3) { GainArea.localScale = new Vector3(BuffAmount[3]*1.25f, BuffAmount[3]*1.25f, BuffAmount[3]*1.25f); BuffObjects[4].SetActive(true); }
        if (type == 4) anim.SetFloat("ReloadTime", 1f + BuffAmount[4]);
    }
    
    public void BuffOn(int type,int Last,int Amount)
    {
        if (Amount > BuffAmount[type - 1])
        {
            BuffAmount[type - 1] = Amount;
            BuffObjects[type].SetActive(true);
            GameManager.instance.UI.SetBuff(type, Last);
        }
    }

    public void BuffOff(int type)
    {
        BuffAmount[type-1] = 0;
        BuffObjects[type].SetActive(false);
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
}
