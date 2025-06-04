using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] Transform Tuto;
    [SerializeField] Transform WeaponHolder;
    [SerializeField] Transform FirePos;
    [SerializeField] AudioClip[] audioclips;
    [SerializeField] MeshRenderer muzzleFlash;

    Rigidbody rigid;
    Animator anim;

    AudioSource audiodio;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        audiodio = GetComponent<AudioSource>();
        GameManager.instance.CV.Follow = transform;
        GameManager.instance.CV.LookAt = Aim;

        GameManager.instance.Player = transform;
        GameManager.instance.PlayerScript = this;
        GameManager.instance.PlayerHealFunc = HealFunction;

        gameObject.SetActive(false);
    }

    private void Start()
    {
        Weapons = new List<Transform>(GameManager.instance.Data.Weapon.Count); for (int i = 0; i < 2; i++) Weapons.Add(WeaponHolder.GetChild(i).transform);     // Init Start;
        CurUnlocked = new List<int>(GameManager.instance.Data.Weapon.Count); CurUnlocked.Add(0); CurUnlocked.Add(3);
        Muzzles = new List<MeshRenderer>(GameManager.instance.Data.Weapon.Count);  Muzzles.Add(Weapons[0].GetChild(0).GetComponent<MeshRenderer>()); Muzzles.Add(Weapons[1].GetChild(0).GetComponent<MeshRenderer>());

        
        transform.position = new Vector3(GameManager.instance.bx * 80, 0, GameManager.instance.by * 80);
        Invoke("StartBug", 0.5f);
        Tuto.transform.SetParent(GameManager.instance.transform);

        /*DroneAdd(); DroneAdd(); DroneAdd();*/

        //GameManager.instance.UI.ScoreUp(1000);
        //WeaponLevelUp(1); WeaponLevelUp(2); WeaponLevelUp(4); WeaponLevelUp(5);
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

    #region Gun
    [SerializeField]List<Transform> Weapons;
    List<int> CurUnlocked;
    [SerializeField] List<MeshRenderer> Muzzles;
    public int CurWeaponInd = 0;
    


    Vector3 Dir;
    IEnumerator Shoot_Sub()
    {
        if (CurUnlocked[CurWeaponInd] >= 3) { anim.SetBool("OnFire",false); yield return new WaitForSeconds(GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].rpm * 0.17f - 0.165f); }
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
        if (GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag <= 0) return;
        float Theta = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
        if (Aim.localPosition.y < 3)
        {
            Aim.Translate(0, GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].bound * 0.02f, 0);
            if (Weapons[CurWeaponInd] != null) { Vector3 WDir = Weapons[CurWeaponInd].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[CurWeaponInd].eulerAngles = WDir; }
        }
        audiodio.PlayOneShot(audioclips[0],0.8f);
        if(!muzzleFlash.enabled)StartCoroutine(ShowMuzzleFlash());

        for (int _ = 0; _ < GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].bnum; _++)
        {
            Quaternion randomRot = Quaternion.Euler(
                   Random.Range(-GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].spread, GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].spread), // 상하
                   Random.Range(-GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].spread, GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].spread), // 좌우
                   0
               );
            Vector3 spreadDirection = randomRot * Weapons[CurWeaponInd].forward;
            GameManager.instance.bullet.ShootBullet(FirePos.position, spreadDirection, GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].power);
        }
        GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag -= 1;
        BulletText.text = $"{GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag}/{GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].MaxMag}"; 
        BulletGage.fillAmount = GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag / GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].MaxMag;
    }

    public bool WeaponLevelUp(int ind)
    {
        if (GameManager.instance.Data.Weapon[ind].LV == 0)
        {
            var cnt = Instantiate(GameManager.instance.Data.Weapon[ind].Obj, WeaponHolder); Weapons.Add(cnt.transform); CurUnlocked.Add(ind); Muzzles.Add(cnt.transform.GetChild(0).GetComponent<MeshRenderer>()); cnt.SetActive(false);
            GameManager.instance.UI.AddWeaponImage(GameManager.instance.Data.Weapon[ind].Im);
        }
        else
        {
            switch (GameManager.instance.Data.Weapon[ind].LV)
            {
                case 1:
                    GameManager.instance.Data.Weapon[ind].MaxMag *= 2; break;
                case 2:
                    GameManager.instance.Data.Weapon[ind].bound *= 0.9f; break;
                case 3:     // Damage Increase
                    break;
                case 4:
                    GameManager.instance.Data.Weapon[ind].spread *= 0.9f; break;
                case 5:
                    GameManager.instance.Data.Weapon[ind].power *= 1.25f; break;
                case 6:
                    GameManager.instance.Data.Weapon[ind].bnum *= 2; return true; break;

            }
        }
        GameManager.instance.Data.Weapon[ind].LV++;
        return false;

    }

    int DroneCount = 0;
    public void DroneAdd()
    {
        var cnt = Instantiate(GameManager.instance.Data.Drone,transform); float deg; if (DroneCount % 2 == 0) deg = DroneCount * 30f * Mathf.Deg2Rad; else deg = (210 - DroneCount * 30) * Mathf.Deg2Rad;
        cnt.transform.localPosition = new Vector3(0.1f + Mathf.Cos(deg),1.5f + Mathf.Sin(deg),Random.Range(-0.3f,0.3f)); DroneCount++;
    }

    
    #endregion


    #region InputSystems

    Coroutine WalkSoundCor = null;
    // WASD, Shaft
    void OnMove(InputValue value)
    {
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
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
        if (_onFocus) GameManager.instance.CVtr.m_FollowOffset = new Vector3(0.5f, 1.7f, 0.2f);
        else
        {
            GameManager.instance.CVtr.m_FollowOffset = new Vector3(0.5f, 2, -1.5f);
            Aim.localPosition = new Vector3(0.5f, 1.5f, Aim.localPosition.z);
        }
    }

    [SerializeField] Image BulletGage;
    [SerializeField] TMP_Text BulletText;
    void OnReload(InputValue value)
    {
        if (!MoveAble) return;
        _onFire = false;
        MoveAble = false; Dir = Vector3.zero;
        audiodio.PlayOneShot(audioclips[1],1);
        anim.SetTrigger("OnReload");
    }
    public void ReloadEnd()
    {
        MoveAble = true && ControllFromExtern; GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag = GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].MaxMag;
        BulletText.text = $"{GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag}/{GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].MaxMag}"; BulletGage.fillAmount = 1f;
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
        FirePos = Weapons[CurWeaponInd].GetChild(0); muzzleFlash = Muzzles[CurWeaponInd];
        Vector3 WDir = Weapons[CurWeaponInd].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[CurWeaponInd].eulerAngles = WDir;
        BulletText.text = $"{GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag}/{GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].MaxMag}";
        BulletGage.fillAmount = GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].CurMag / GameManager.instance.Data.Weapon[CurUnlocked[CurWeaponInd]].MaxMag;
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
    public float[] BuffAmount = { 0, 0, 0 ,1, 0,1,1};  // 공, 방, 속, 범위, 재장속, 돈, 경치
    public void HealFunction(int Count)
    {
        BuffObjects[0].SetActive(true); audiodio.PlayOneShot(audioclips[4]);
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
