using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    [SerializeField] List<Collider> AttackCol;
    protected Rigidbody rigid;
    protected Animator anim;
    protected NavMeshAgent agent;
    protected BoxCollider col;
    [SerializeField] protected int InitHP = 5;
    protected int HP;
    [SerializeField] protected float AttackRagne;

    public enum State
    {
        IDLE,
        TRACE,
        ATTACK,
        OnHit,
        DIE
    }
    protected State CurState = State.IDLE;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        if (BloodPref != null)
        {
            BloodEffects.Add(BloodPref.GetComponent<ParticleSystem>()); for (int i = 0; i < 4; i++) BloodEffects.Add(Instantiate(BloodPref, transform).GetComponent<ParticleSystem>());
        }
    }

    protected virtual void Start()
    {
        GameManager.instance.Enemy.GameEnd.Add(OnGameEnd);
        GameManager.instance.Enemy.DeadAct.Add(ExtraDead);
    }

    protected virtual void FixedUpdate()
    {
        if(!rigid.isKinematic) rigid.velocity = Vector3.zero;
        Vector3 dir = (GameManager.instance.Player.position - transform.position).normalized; dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    protected bool MoveAble = true;

    protected bool ChangeOccur = false;
    protected virtual IEnumerator CheckStat()
    {
        WaitForSeconds wfs = new WaitForSeconds(0.3f);
        ChangeOccur = true;
        while (gameObject.activeSelf)
        {
            yield return wfs; ChangeOccur = false;
            if (!MoveAble) continue;
            Vector3 dir = GameManager.instance.Player.position - transform.position; dir.y = 0;
            float dist = dir.magnitude;

            if (dist > AttackRagne) 
            { 
                agent.SetDestination(GameManager.instance.Player.position); 
                if(CurState != State.TRACE)ChangeOccur = true;
                CurState = State.TRACE;
            }
            else if (dist <= AttackRagne && CurState != State.ATTACK) { CurState = State.ATTACK; ChangeOccur = true; }
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
        gameObject.SetActive(false);
        if (!IsExtraDead) 
        { 
            GameManager.instance.UI.ScoreUp();
            if (Random.Range(0f, 1f) < 0.6f) { var Obj = Instantiate(GameManager.instance.Data.Exp[0], GameManager.instance.Enemy.transform); Obj.transform.position = new Vector3(transform.position.x,1, transform.position.z); }
        }
    }

    bool IsExtraDead;
    public void ExtraDead()
    {
        if (!gameObject.activeSelf) return;
        HP = 0; StopAllCoroutines(); anim.SetTrigger("Die"); foreach (var j in AttackCol) j.enabled = false; IsExtraDead = true;
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
            if (BloodPref != null)
            {
                BloodEffects[LastBlood].transform.position = pos; BloodEffects[LastBlood].transform.rotation = rot;
                BloodEffects[LastBlood].Play(); LastBlood = (LastBlood + 1) % BloodEffects.Count;
            }
            rigid.velocity = Vector3.zero;
            foreach (var j in AttackCol) j.enabled = false;
            if (HP <= 0) { StopAllCoroutines(); rigid.isKinematic = true; col.enabled = false; anim.SetTrigger("Die"); }
            else 
            {
                CurState = State.OnHit;
                anim.SetTrigger("OnHit"); 
            } 
        }
    }

    private void OnEnable()
    {
        HP = InitHP;
        IsExtraDead = false; MoveAble = true; CurState = State.IDLE; rigid.isKinematic = false; col.enabled = true;
        anim.SetBool("OnAttack", false);
        StartCoroutine(CheckStat());
        StartCoroutine(UpdateStat());
    }

    public void OnGameEnd()
    {
        if (!gameObject.activeSelf) return;
        foreach (var j in AttackCol) j.enabled = false; 
        col.enabled = false; rigid.isKinematic = true;
        StopAllCoroutines();
        agent.isStopped = true;
        anim.SetTrigger("End");
    }
}
