using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Transform Particle;
    [SerializeField] TrailRenderer TR;

    ParticleSystem[] Particles = new ParticleSystem[4];

    private void Awake()
    {
        Particles[0] = Particle.GetComponent<ParticleSystem>();
        Particles[1] = Particle.GetChild(0).GetComponent<ParticleSystem>();
        Particles[2] = Particle.GetChild(1).GetComponent<ParticleSystem>();
        Particles[3] = Particle.GetChild(1).GetChild(0).GetComponent<ParticleSystem>();
        TR = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        Particle.transform.SetParent(GameManager.instance.ParticleSet);
    }

    private void OnCollisionEnter(Collision col)
    {
        var cp = col.GetContact(0);
        var rot = Quaternion.LookRotation(-cp.normal);
        Particle.transform.position = transform.position; Particle.transform.rotation = rot;
        foreach (var j in Particles) j.Play();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        TR.Clear();
    }
}
