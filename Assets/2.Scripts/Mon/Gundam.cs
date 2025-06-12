using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.U2D;

public class Gundam : MonoBehaviour
{
    [SerializeField] int MaxHP;
    [SerializeField] GameObject Bullet;
    [SerializeField] Transform ShootPos;
    [SerializeField] GameObject Pattern1, Pattern2;
    [SerializeField] AudioClip clip;

    float HP;
    Animator anim;
    Rigidbody rigid;

    private void Awake()
    {
        HP = MaxHP;
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    IEnumerator Pattern(GameObject cnt, float time)
    {
        WaitForSeconds wfs = new WaitForSeconds(time);

        while (GameManager.instance.PlayerScript != null)
        {
            yield return wfs;
            for (int _ = 0; _ < PatternCount; _++)
            {
                GameObject tmp = Instantiate(cnt, GameManager.instance.ParticleSet); tmp.transform.localScale = tmp.transform.localScale * SpeedSub;
                Vector3 pos = GameManager.instance.Player.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)); pos.y = 0.1f; tmp.transform.position = pos;
                yield return GameManager.DotOne;
            }

        }
    }

    private void Start()
    {
        GameManager.instance.UI.ToggleBoss("Gundam");
        StartCoroutine(Pattern(Pattern1, 10)); StartCoroutine(Pattern(Pattern2, 25));
    }

    int PatternCount = 1;
    float SpeedSub = 1;

    bool CantMove = false;
    public float Test = 20;

    Vector3 Dir;
    float dist;
    private void FixedUpdate()
    {
        Dir = (GameManager.instance.Player.position - transform.position); dist = Dir.magnitude; Dir = Dir.normalized;
        transform.rotation = Quaternion.LookRotation(Dir); 

        if (dist < 8) { Dir *= -3f * SpeedSub; anim.SetBool("OnWalk", true); anim.SetFloat("dy", -1); }
        else if (dist < 16) { anim.SetBool("OnWalk", false); Dir = Vector3.zero; }
        else { Dir *= 4f * SpeedSub; anim.SetBool("OnWalk", true); anim.SetFloat("dy", 1); }

        if (CantMove) return;

        rigid.MovePosition(rigid.position + Dir * Time.deltaTime);
        
    }

    public void Shoot()
    {
        GameManager.instance.Audio.PlayClip(2, 1, clip);
        var cnt = Instantiate(Bullet, GameManager.instance.bullet.transform); cnt.transform.position = ShootPos.position;
        Vector3 dir = (GameManager.instance.Player.position + Vector3.up - cnt.transform.position).normalized; cnt.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        cnt.GetComponent<Rigidbody>().AddForce(dir * 40 * SpeedSub, ForceMode.Impulse);
    }



    public void SetMoveAble()
    {
        CantMove = false;
    }

    public void WeakPoint()
    {
        HP -= 2 * GameManager.instance.PlayerScript.BuffAmount[0]; rigid.velocity = Vector3.zero;
        CantMove = true;
        GameManager.instance.UI.BossHpChange(Mathf.Max(0, HP / MaxHP));
        if (HP <= 0) { StopAllCoroutines(); anim.SetTrigger("Die"); GameManager.instance.PlayerScript.Win(); }
        else
        {
            anim.SetTrigger("OnHit");
            if (HP < MaxHP * 0.65f && PatternCount == 1) { SpeedSub = 1.25f; PatternCount = 2; anim.SetFloat("Multi", SpeedSub); }
            else if (HP < MaxHP * 0.3f && PatternCount == 2) { SpeedSub = 1.5f; PatternCount = 3; anim.SetFloat("Multi", SpeedSub); }
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8 && HP > 0)
        {
            HP-= GameManager.instance.PlayerScript.BuffAmount[0];
            GameManager.instance.UI.BossHpChange(Mathf.Max(0,HP / MaxHP));
            rigid.velocity = Vector3.zero;
            if (HP <= 0) { StopAllCoroutines(); anim.SetTrigger("Die"); GameManager.instance.PlayerScript.Win(); }
            else
            {
                
                if (HP < MaxHP * 0.65f && PatternCount == 1) { SpeedSub = 1.25f; PatternCount = 2; anim.SetFloat("Multi", SpeedSub); }
                else if (HP < MaxHP * 0.3f && PatternCount == 2) { SpeedSub = 1.5f; PatternCount = 3; anim.SetFloat("Multi", SpeedSub); }
            }
        }
    }


    public void Die()
    {
        Destroy(gameObject);
    }
}
