using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody rigid;
    Animation anim;
    Vector2 Dir;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animation>();
        anim.Play("Idle");
    }

    private void FixedUpdate()
    {
        if (!Dir.Equals(Vector2.zero))
        {
            rigid.MovePosition(new Vector3(rigid.position.x + Dir.x, rigid.position.y, rigid.position.z + Dir.y));
            if (Dir.x > 0) anim.CrossFade("RunF");
            else if (Dir.x < 0) anim.CrossFade("RunB");
            else if (Dir.y > 0) anim.CrossFade("RunR");
            else anim.CrossFade("RunL");
        }
        else anim.CrossFade("Idle");
    }

    void OnMove(InputValue value)
    {
        Dir = value.Get<Vector2>() * 0.1f;
    }

    void OnJump(InputValue value)
    {
        
        rigid.MovePosition(rigid.position + Vector3.up);
    }
}
