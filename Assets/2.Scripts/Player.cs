using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody rigid;
    Animation anim;
    Animator _anim;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animation>();
        _anim = GetComponent<Animator>();
        anim.Play("Idle");
        Cursor.visible = false;
    }

    float MoveSpeed = 5f;
    private void FixedUpdate()
    {
        // Animation
        if (!Dir.Equals(Vector2.zero))
        {
            Vector3 NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;

            rigid.MovePosition(rigid.position + NextDir * Time.deltaTime * MoveSpeed);
            if (_onFire) anim.CrossFade("RunFireSMG",0.25f);
            else if (Dir.x > 0) anim.CrossFade("RunR",0.25f);
            else if (Dir.x < 0) anim.CrossFade("RunL",0.25f);
            else if (Dir.y > 0) anim.CrossFade("RunF",0.25f);
            else anim.CrossFade("RunB",0.25f);
        }
        else
        {
            if (_onFire) anim.CrossFade("IdleFireSMG",0.25f);
            else anim.CrossFade("Idle",0.25f);
        }

        // Animator
        /*if (!Dir.Equals(Vector2.zero))
        {
            Vector3 NextDir = (transform.forward * Dir.y + transform.right * Dir.x).normalized;

            rigid.MovePosition(rigid.position + NextDir * Time.deltaTime * MoveSpeed);
            _anim.SetBool("OnMove", true);
            _anim.SetFloat("dx", Dir.x);
            _anim.SetFloat("dy", Dir.y);
        }
        else
        {
            _anim.SetBool("OnMove", false);
        }*/
    }

    Vector3 Dir;
    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>();
    }

    bool JumpAble = false;
    void OnJump(InputValue value)
    {
        if (JumpAble) { rigid.AddForce(Vector3.up * 5,ForceMode.Impulse); JumpAble = false; }
    }

    // Mouse Controll
    [SerializeField] Transform Aim;
    Vector2 MouseDir;
    void OnLook(InputValue value)
    {
        MouseDir = value.Get<Vector2>();
        float Ny = Mathf.Clamp(Aim.localPosition.y + MouseDir.y * Time.deltaTime * 0.2f, 1f, 2f);
        Aim.localPosition = new Vector3(0, Ny, 0);
        transform.Rotate(Vector3.up * 10f * Time.deltaTime * MouseDir.x);
    }

    bool _onFire = false;
    void OnFire(InputValue value)
    {
        _onFire = _onFire == false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) JumpAble = true;
    }
}
