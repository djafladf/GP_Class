using Cinemachine;
using System.Collections;
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
    Transform CurWeapon = null;

    Rigidbody rigid;
    Animation anim;

    CinemachineTransposer transposer;
    AudioSource audio;

    private void Awake()
    {
        transposer = CV.GetCinemachineComponent<CinemachineTransposer>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animation>();
        audio = GetComponent<AudioSource>();
        anim.Play("Idle");
        

        CurWeapon = WeaponHolder.GetChild(0);
        AttackGap = anim["IdleFireSMG"].length;
        CrossTime = AttackGap * 0.8f;
    }

    private void Start()
    {
        GameManager.instance.Player = transform;
    }

    float MoveSpeed = 5f;
    float CrossTime;
    bool MoveAble = true;
    private void FixedUpdate()
    {
        Cursor.visible = false;
        if (!MoveAble) return;
        // Animation
        if (!Dir.Equals(Vector2.zero))
        {
            Vector3 NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;

            rigid.MovePosition(rigid.position + NextDir * Time.deltaTime * MoveSpeed);
            if (_onFire)
            {
                anim.CrossFade("RunFireSMG",CrossTime);
            }
            else if (Dir.x > 0) anim.CrossFade("RunR", CrossTime);
            else if (Dir.x < 0) anim.CrossFade("RunL", CrossTime);
            else if (Dir.y > 0) anim.CrossFade("RunF", CrossTime);
            else anim.CrossFade("RunB", CrossTime);
        }
        else
        {
            if (_onFire)
            {
                anim.CrossFade("IdleFireSMG", 0.25f);
            }
            else anim.CrossFade("Idle", 0.25f);
        }
    }

    Vector3 Dir;

    // Idle <-> Attack 전환 도중 발생하는 연사 방지
    // 0 : Idle, 1 : Cool, 2 : Call While Shoot (키 씹힘 방지)
    int ShootCall = 0;
    float AttackGap;
    void Shoot_Sub()
    {
        if (ShootCall == 2) { ShootCall = 0; Shoot(); }
        else ShootCall = 0;
    }

    float LastTime = 0;
    public void Shoot()
    {
        if (LeftBul <= 0) return;
        if (ShootCall > 0) { ShootCall = 2; return; }
        float Theta = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
        if (Aim.localPosition.y < 3)
        {
            Aim.Translate(0, 0.02f, 0);
            if (CurWeapon != null) { Vector3 WDir = CurWeapon.eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; CurWeapon.eulerAngles = WDir; }
        }
        audio.PlayOneShot(audioclips[0],1.0f);
        StartCoroutine(ShowMuzzleFlash());
        GameManager.instance.bullet.ShootBullet(FirePos.position, CurWeapon.forward);
        LeftBul--; BulletText.text = $"{LeftBul}/60"; BulletGage.fillAmount = LeftBul / 60f;
        ShootCall = 1; Invoke("Shoot_Sub", AttackGap);
    }

    int LeftBul = 60;

#region InputSystems
    // WASD, Shaft
    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>();
    }

    // Space
    bool JumpAble = false;
    void OnJump(InputValue value)
    {
        if (JumpAble && MoveAble) { rigid.AddForce(Vector3.up * 5, ForceMode.Impulse); JumpAble = false; }
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
        if (CurWeapon != null) { Vector3 WDir = CurWeapon.eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; CurWeapon.eulerAngles = WDir;  }
        transform.Rotate(Vector3.up * 25f * Time.deltaTime * MouseDir.x);
    }

    bool _onFire = false;
    void OnFire(InputValue value)
    {
        _onFire = _onFire == false;
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
        audio.clip = audioclips[1]; audio.Play();
        anim.CrossFade("IdleReloadSMG", CrossTime);
    }
    public void ReloadEnd()
    {
        MoveAble = true; LeftBul = 60;
        BulletText.text = $"{LeftBul}/60"; BulletGage.fillAmount = 1f;
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
        yield return new WaitForSeconds(CrossTime);
        muzzleFlash.enabled = false;
    }
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) JumpAble = true;
    }

}
