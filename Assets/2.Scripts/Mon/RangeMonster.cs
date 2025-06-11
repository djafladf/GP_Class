using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeMonster : Monster
{
    [SerializeField] GameObject bul;
    GameObject bulet;
    [SerializeField] Transform ShootPos;

    protected override void FixedUpdate()
    {
        if (!rigid.isKinematic) rigid.velocity = Vector3.zero;
        Vector3 dir = (GameManager.instance.Player.position - ShootPos.position).normalized;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    void AttackMethod()
    {
        bulet = Instantiate(bul, GameManager.instance.bullet.transform);
        bulet.transform.position = ShootPos.position;
        var Dir = (GameManager.instance.Player.position - ShootPos.position).normalized;
        bulet.transform.rotation = Quaternion.FromToRotation(Vector3.up, Dir);
        bulet.GetComponent<Rigidbody>().AddForce(Dir * 40, ForceMode.Impulse);
    }
}
