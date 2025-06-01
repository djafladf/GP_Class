using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Gundam : MonoBehaviour
{
    [SerializeField] int MaxHP;
    [SerializeField] GameObject Bullet;
    [SerializeField] Transform ShootPos;
    [SerializeField] GameObject Pattern1, Pattern2;

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

        while (true)
        {
            yield return wfs;
            GameObject tmp = Instantiate(cnt, GameManager.instance.ParticleSet); Vector3 pos = GameManager.instance.Player.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));pos.y = 0.1f; tmp.transform.position = pos;
            
        }
    }

    private void Start()
    {
        GameManager.instance.UI.ToggleBoss("Gundam");
        StartCoroutine(Pattern(Pattern1, 10)); StartCoroutine(Pattern(Pattern2, 30));
    }

    bool CantMove = false;
    public float Test = 20;

    Vector3 Dir;
    float dist;
    private void FixedUpdate()
    {
        Dir = (GameManager.instance.Player.position - transform.position); Dir.y = 0; dist = Dir.magnitude; Dir = Dir.normalized;
        transform.rotation = Quaternion.LookRotation(Dir); 

        if (dist < 12) { Dir *= -2f; anim.SetBool("OnWalk", true); anim.SetFloat("dy", -1); }
        else if (dist < 15) { anim.SetBool("OnWalk", false); Dir = Vector3.zero; }
        else { Dir *= 3.5f; anim.SetBool("OnWalk", true); anim.SetFloat("dy", 1); }

        if (CantMove) return;

        rigid.MovePosition(rigid.position + Dir * Time.deltaTime);
        
    }

    public void Shoot()
    {
        var cnt = Instantiate(Bullet, GameManager.instance.bullet.transform); cnt.transform.position = ShootPos.position;
        Vector3 dir = (GameManager.instance.Player.position + Vector3.up - cnt.transform.position).normalized; cnt.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        cnt.GetComponent<Rigidbody>().AddForce(dir * 30, ForceMode.Impulse);
    }



    public void SetMoveAble()
    {
        CantMove = false;
    }

    public void WeakPoint()
    {
        HP -= 2; rigid.velocity = Vector3.zero;
        CantMove = true;
        if (HP <= 0) { StopAllCoroutines(); anim.SetTrigger("Die"); }
        else
        {

            GameManager.instance.UI.BossHpChange(HP / MaxHP); anim.SetTrigger("OnHit");
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8 && HP > 0)
        {
            HP--;
            rigid.velocity = Vector3.zero;
            if (HP <= 0) { StopAllCoroutines(); anim.SetTrigger("Die"); }
            else GameManager.instance.UI.BossHpChange(HP / MaxHP);
        }
    }
}
