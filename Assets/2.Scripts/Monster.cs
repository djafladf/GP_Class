using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    Rigidbody rigid;
    Animator anim;
    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    bool MoveAble = true;
    private void FixedUpdate()
    {
        if (!MoveAble) return;
        Vector3 dir = GameManager.instance.Player.position - transform.position; dir.y = 0;
        float dist = dir.magnitude;

        if (dist > 10) { anim.SetBool("OnWalk", false); anim.SetBool("OnAttack", false); }
        else if (dist > 1.5f)
        {
            anim.SetBool("OnWalk", true); anim.SetBool("OnAttack", false);
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, dir);
            rigid.MovePosition(transform.position + dir.normalized * Time.deltaTime * 2);
        }
        else
        {
            MoveAble = false; anim.SetBool("OnAttack", true);
        }
    }

    void ToggleMove()
    {
        MoveAble = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8) { MoveAble = false; anim.SetTrigger("OnHit"); }
    }
}
