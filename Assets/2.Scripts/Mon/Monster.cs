using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    [SerializeField] List<SphereCollider> AttackCol;
    Rigidbody rigid;
    Animator anim;
    NavMeshAgent agent;
    BoxCollider col;
    int HP = 10;

    public enum State
    {
        IDLE,
        TRACE,
        ATTACK,
        OnHit,
        DIE
    }
    State CurState = State.IDLE;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        BloodEffects.Add(BloodPref.GetComponent<ParticleSystem>()); for (int i = 0; i < 4; i++) BloodEffects.Add(Instantiate(BloodPref, transform).GetComponent<ParticleSystem>()); 
    }

    private void Start()
    {
        GameManager.instance.Enemy.GameEnd.Add(OnGameEnd);
    }

    bool MoveAble = true;

    bool ChangeOccur = false;
    IEnumerator CheckStat()
    {
        WaitForSeconds wfs = new WaitForSeconds(0.3f);
        ChangeOccur = true;
        while (gameObject.activeSelf)
        {
            yield return wfs; ChangeOccur = false;
            if (!MoveAble) continue;
            Vector3 dir = GameManager.instance.Player.position - transform.position; dir.y = 0;
            float dist = dir.magnitude;

            if (dist > 10 && CurState != State.IDLE) { CurState = State.IDLE; ChangeOccur = true; }
            else if (dist > 1.5f && dist < 10f) 
            { 
                agent.SetDestination(GameManager.instance.Player.position); 
                if(CurState != State.TRACE)ChangeOccur = true;
                CurState = State.TRACE;
            }
            else if (dist <= 1.5f && CurState != State.ATTACK) { CurState = State.ATTACK; ChangeOccur = true; }
        }
    }

    IEnumerator UpdateStat()
    {
        WaitForSeconds wfs = new WaitForSeconds(0.3f);

        while (gameObject.activeSelf)
        {
            yield return wfs;
            if (!ChangeOccur) continue;
            switch (CurState)
            {
                case State.IDLE:
                    agent.isStopped = true;
                    anim.SetBool("OnWalk", false);
                    break;
                case State.TRACE:
                    agent.isStopped = false;
                    anim.SetBool("OnWalk", true);
                    break;
                case State.ATTACK:
                    foreach (var j in AttackCol) j.enabled = true;
                    agent.isStopped = true; anim.SetBool("OnWalk", false);
                    MoveAble = false; anim.SetBool("OnAttack", true);
                    break;
            }
            
        }
    }


    void SetMoveAble()
    {
        Vector3 dir = GameManager.instance.Player.position - transform.position; dir.y = 0;
        float dist = dir.magnitude;
        if (dist > 1.5f) { MoveAble = true; anim.SetBool("OnAttack", false); foreach (var j in AttackCol) j.enabled = false; }
    }

    void Dead()
    {
        GameManager.instance.Enemy.Pool.Add(gameObject); gameObject.SetActive(false);
        GameManager.instance.UI.ScoreUp();
        HP = 10;
    }

    [SerializeField] GameObject BloodPref;
    List<ParticleSystem> BloodEffects = new List<ParticleSystem>();
    int LastBlood = 0;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8 && HP > 0) 
        {
            HP--;
            MoveAble = false; agent.isStopped = true;
            Vector3 pos = collision.GetContact(0).point;
            Quaternion rot = Quaternion.LookRotation(-collision.GetContact(0).normal);
            BloodEffects[LastBlood].transform.position = pos; BloodEffects[LastBlood].transform.rotation = rot;
            BloodEffects[LastBlood].Play(); LastBlood = (LastBlood + 1) % BloodEffects.Count;
            rigid.velocity = Vector3.zero;
            if (HP <= 0) { StopAllCoroutines(); anim.SetTrigger("Die"); }
            else 
            {
                CurState = State.OnHit;
                anim.SetTrigger("OnHit"); 
            }
        }
    }

    private void OnEnable()
    {
        agent.isStopped = true;
        anim.SetBool("OnWalk", false);
        StartCoroutine(CheckStat());
        StartCoroutine(UpdateStat());
    }

    public void OnGameEnd()
    {
        if (!gameObject.activeSelf) return;
        foreach (var j in AttackCol) j.enabled = false; 
        col.enabled = false; rigid.isKinematic = false;
        StopAllCoroutines();
        agent.isStopped = true;
        anim.SetTrigger("End");
    }
}
