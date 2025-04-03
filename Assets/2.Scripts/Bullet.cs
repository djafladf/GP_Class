using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Transform Particle;
    [SerializeField] TrailRenderer TR;
    Rigidbody rigid;
    BoxCollider coll;
    MeshRenderer mesh;
    ParticleSystem[] Particles = new ParticleSystem[3];

    Vector3 MeshSize;

    private void Awake()
    {
        Particles[0] = Particle.GetComponent<ParticleSystem>();
        Particles[1] = Particle.GetChild(0).GetComponent<ParticleSystem>();
        Particles[2] = Particle.GetChild(1).GetComponent<ParticleSystem>();
        TR = GetComponent<TrailRenderer>();
        rigid = GetComponent<Rigidbody>();
        mesh = GetComponent<MeshRenderer>();
        coll = GetComponent<BoxCollider>();
        MeshSize = GetComponent<MeshFilter>().mesh.bounds.size * 0.5f;
    }

    private void Start()
    {
        Particle.transform.SetParent(GameManager.instance.ParticleSet);
    }
    

    public void Init(Vector3 Start, Vector3 _Dir)
    {
        transform.position = Start;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, _Dir);
        gameObject.SetActive(true); TR.Clear(); rigid.AddForce(_Dir * 50,ForceMode.Impulse);
    }

    GameObject MyHole = null;
    private void OnCollisionEnter(Collision col)
    {
        var cp = col.GetContact(0);
        if (col.transform.CompareTag("Floor"))
        {
            if (MyHole == null) MyHole = GameManager.instance.bullet.MakeHole();
            MyHole.transform.position = cp.point; MyHole.transform.rotation = Quaternion.FromToRotation(Vector3.up, cp.normal); MyHole.SetActive(true);
        }
        Particle.transform.position = cp.point; Particle.transform.rotation = Quaternion.LookRotation(-cp.normal);
        foreach (var j in Particles) j.Play();

        rigid.isKinematic = false; mesh.enabled = false; rigid.velocity = Vector3.zero; coll.enabled = false;
        Invoke("NaturalTrail", 0.5f) ;
    }

    // Trail 부자연스럽게 사라지는거 방지
    void NaturalTrail()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        rigid.velocity = Vector3.zero;
        rigid.isKinematic = false;
        mesh.enabled = true;
        coll.enabled = true;
    }

    // 맵 경계 밖으로 이동
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MapBorder")) gameObject.SetActive(false);
    }
}
