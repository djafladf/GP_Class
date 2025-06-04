using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerTest : MonoBehaviour
{
    [SerializeField] Transform WeaponHolder;
    Rigidbody rigid;
    Animator anim;

    CinemachineTransposer transposer;
    AudioSource audiodio;

    Transform Spine;

    [SerializeField] Vector3 FolllowOffset;
    [SerializeField] Vector3 FollowOffset_Z;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        audiodio = GetComponent<AudioSource>();
        MoveAble = true;
        Spine = anim.GetBoneTransform(HumanBodyBones.Spine);
        GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;
    }

    Quaternion WeaponIdleRot;

    float MoveSpeed = 5f;
    bool MoveAble = false;

    private void FixedUpdate()
    {
        if (!MoveAble) return;
        if (!Dir.Equals(Vector2.zero))
        {
            Vector3 NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;
            rigid.MovePosition(rigid.position + NextDir * Time.deltaTime * (MoveSpeed));
        }
    }
    float angle;
    private void LateUpdate()
    {
        angle = Mathf.Atan2(Aim.localPosition.y - 1.2f, 1) * Mathf.Rad2Deg;
        Spine.localRotation = Quaternion.Euler(-10, 0, angle);
    }

    #region Gun


    Vector3 Dir;

    Coroutine ShootCor = null;

    #endregion


    #region InputSystems

    Coroutine WalkSoundCor = null;
    // WASD, Shaft
    void OnMove(InputValue value)
    {
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        Dir = value.Get<Vector2>();
        if (Dir.y <= 0 || _onFire) MoveSpeed = 2.5f;
        else MoveSpeed = 5f;
        anim.SetFloat("dx", Dir.x); anim.SetFloat("dy", Dir.y);
        anim.SetBool("OnMove", !Dir.Equals(Vector2.zero));
    }
    bool walkType = false;
    // Space
    bool JumpAble = false;
    void OnJump(InputValue value)
    {
        if (JumpAble && MoveAble) { JumpAble = false; anim.SetTrigger("Jump");  MoveAble = false; }
    }

    void Jump()
    {
        rigid.AddForce(Vector3.up * (5), ForceMode.Impulse); MoveAble = true;
    }

    // Mouse
    [SerializeField] Transform Aim;
    Vector2 MouseDir;
    void OnLook(InputValue value)
    {
        if (!MoveAble) return;
        MouseDir = value.Get<Vector2>();
        float Ny = Mathf.Clamp(Aim.localPosition.y + MouseDir.y * Time.deltaTime * 0.4f, 0f, 2.4f);
        Aim.localPosition = new Vector3(Aim.localPosition.x, Ny, Aim.localPosition.z);
        //Vector3 WDir = Weapons[0].eulerAngles; WDir.x = Camera.main.transform.eulerAngles.x; Weapons[0].eulerAngles = WDir;

        transform.Rotate(Vector3.up * 25f * Time.deltaTime * MouseDir.x);
    }

    bool _onFire = false; bool FirstFire = true;
    void OnFire(InputValue value)
    {
        if (!MoveAble) return;
        _onFire = value.isPressed;
        if (_onFire) MoveSpeed = 2.5f;
        else if (Dir.y > 0) MoveSpeed = 5f;
        anim.SetBool("OnFire", _onFire);
    }

    bool _onFocus = false;
    void OnFocus(InputValue value)
    {
        if (!MoveAble) return;
        _onFocus = _onFocus == false;
        if (_onFocus) GameManager.instance.CVtr.m_FollowOffset = FollowOffset_Z;
        else
        {
            GameManager.instance.CVtr.m_FollowOffset = FolllowOffset;
            Aim.localPosition = new Vector3(0.2f, 1.5f, Aim.localPosition.z);
        }
    }

    [SerializeField] Image BulletGage;
    [SerializeField] TMP_Text BulletText;
    void OnReload(InputValue value)
    {
        if (!MoveAble) return;
        _onFire = false;
        MoveAble = false; Dir = Vector3.zero;
        anim.SetTrigger("OnReload");
    }

    public void ReloadEnd()
    {
        MoveAble = true && ControllFromExtern;
       
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
