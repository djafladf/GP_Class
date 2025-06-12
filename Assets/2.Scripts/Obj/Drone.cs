using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour
{
    [SerializeField] GameObject Bullet;
    [SerializeField] LayerMask Targetmask;
    [SerializeField] AudioClip Laser;
    [SerializeField] Animator anim;
    List<Bullet> MyBull = new List<Bullet>();

    private void Start()
    {
        MyBull.Add(Instantiate(Bullet, GameManager.instance.bullet.transform).GetComponent<Bullet>()); MyBull.Add(Instantiate(Bullet, GameManager.instance.bullet.transform).GetComponent<Bullet>()); MyBull.Add(Instantiate(Bullet, GameManager.instance.bullet.transform).GetComponent<Bullet>());
        StartCoroutine(Fire());
    }

    int LastBull = 0;
    IEnumerator Fire()
    {
        WaitForSeconds wfs = new WaitForSeconds(2f);
        while (true)
        {
            if(Physics.SphereCast(transform.position, 5, transform.forward, out RaycastHit hit, 5,Targetmask))
            {
                GameManager.instance.Audio.PlayClip(2, 1, Laser);
                anim.SetTrigger("Attack");
                MyBull[LastBull++].Init(transform.position, (hit.transform.position - transform.position).normalized, 4); LastBull = LastBull % 3;
                yield return wfs;
            }
            yield return GameManager.DotOne;
        }
    }
}
