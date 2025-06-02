using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour
{
    [SerializeField] GameObject Bullet;
    [SerializeField] LayerMask Targetmask;
    List<Bullet> MyBull = new List<Bullet>();

    private void Start()
    {
        MyBull.Add(Instantiate(Bullet, GameManager.instance.bullet.transform).GetComponent<Bullet>()); MyBull.Add(Instantiate(Bullet, GameManager.instance.bullet.transform).GetComponent<Bullet>()); MyBull.Add(Instantiate(Bullet, GameManager.instance.bullet.transform).GetComponent<Bullet>());
        StartCoroutine(Fire());
    }

    int LastBull = 0;
    IEnumerator Fire()
    {
        WaitForSeconds wfs = new WaitForSeconds(1f);
        while (true)
        {
            if(Physics.SphereCast(transform.position, 10, transform.forward, out RaycastHit hit, 10,Targetmask))
            {
                MyBull[LastBull++].Init(transform.position, (hit.transform.position - transform.position).normalized, 3); LastBull = LastBull % 3;
                yield return wfs;
            }
            yield return GameManager.DotOne;
        }
    }
}
