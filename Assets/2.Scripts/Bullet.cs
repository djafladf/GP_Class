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

    private void Awake()
    {
        Particles[0] = Particle.GetComponent<ParticleSystem>();
        Particles[1] = Particle.GetChild(0).GetComponent<ParticleSystem>();
        Particles[2] = Particle.GetChild(1).GetComponent<ParticleSystem>();
        TR = GetComponent<TrailRenderer>();
        rigid = GetComponent<Rigidbody>();
        mesh = GetComponent<MeshRenderer>();
        coll = GetComponent<BoxCollider>();
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

    private void OnCollisionEnter(Collision col)
    {
        var cp = col.GetContact(0);
        var rot = Quaternion.LookRotation(-cp.normal);
        Particle.transform.position = cp.point; Particle.transform.rotation = rot;
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
