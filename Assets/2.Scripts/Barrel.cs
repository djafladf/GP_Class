using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : MonoBehaviour
{
    [SerializeField] Material[] Mats;
    int HP = 3;
    Rigidbody rigid;

    private void Awake()
    {
        transform.GetChild(0).GetComponent<MeshRenderer>().material = Mats[Random.Range(0, Mats.Length)];
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8 && HP > 0) 
        {
            HP--; 
            if (HP <= 0)
            {
                var part = transform.GetChild(0); part.SetParent(GameManager.instance.ParticleSet);
                part.gameObject.SetActive(true);
                Destroy(gameObject, 3.0f);
                Collider[] colls = Physics.OverlapSphere(transform.position, 10, 1<<6);
                foreach(var cnt in colls)
                {
                    var rig = cnt.GetComponent<Rigidbody>();
                    rig.mass = 1.0f;
                    rig.constraints = RigidbodyConstraints.None;
                    rig.AddExplosionForce(1500.0f, transform.position, 10, 1200.0f);
                }
            }
        }
    }
}
