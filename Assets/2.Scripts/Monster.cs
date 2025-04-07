using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    Rigidbody rigid;
    Animator anim;
    NavMeshAgent agent;
    BoxCollider col;
    int HP = 10;
    

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        BloodEffects.Add(BloodPref.GetComponent<ParticleSystem>()); for (int i = 0; i < 4; i++) BloodEffects.Add(Instantiate(BloodPref, transform).GetComponent<ParticleSystem>()); 
    }

    bool MoveAble = true;
    float AttackRange = 1.5f;

    private void FixedUpdate()
    {
        if (!MoveAble) return;   
        Vector3 dir = GameManager.instance.Player.position - transform.position; dir.y = 0;
        float dist = dir.magnitude;


        if (dist > 10) { anim.SetBool("OnWalk", false);  }
        else if (dist > AttackRange)
        {
            anim.SetBool("OnWalk", true);
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, dir);
            rigid.MovePosition(transform.position + dir.normalized * Time.deltaTime * 2);
        }
        else
        {
            MoveAble = false; anim.SetBool("OnAttack", true);
        }
    }

    void SetMoveAble()
    {
        MoveAble = true; anim.SetBool("OnAttack", false);
    }

    void Dead()
    {
        gameObject.SetActive(false);
    }

    [SerializeField] GameObject BloodPref;
    List<ParticleSystem> BloodEffects = new List<ParticleSystem>();
    int LastBlood = 0;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8 && HP > 0) 
        {
            HP--;
            MoveAble = false; 
            Vector3 pos = collision.GetContact(0).point;
            Quaternion rot = Quaternion.LookRotation(-collision.GetContact(0).normal);
            BloodEffects[LastBlood].transform.position = pos; BloodEffects[LastBlood].transform.rotation = rot;
            BloodEffects[LastBlood].Play(); LastBlood = (LastBlood + 1) % BloodEffects.Count;
            if (HP <= 0) anim.SetTrigger("Die");
            else anim.SetTrigger("OnHit");
        }
    }
}
