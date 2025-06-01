using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAttack : MonoBehaviour
{
    [SerializeField] int Damage;
    private void OnParticleCollision(GameObject other)
    {
        GameManager.instance.PlayerScript.GetDamage(Damage);
    }
}
