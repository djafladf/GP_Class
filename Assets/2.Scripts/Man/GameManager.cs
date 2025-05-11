using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static WaitForSeconds DotFive = new WaitForSeconds(0.5f);
    [HideInInspector] public Transform Player;
    [HideInInspector] public MonsterManager Enemy;
    [HideInInspector] public BulletManager bullet;
    [HideInInspector] public UIManager UI;
    [HideInInspector] public Transform ParticleSet;
    public CRTEffect shad;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
}
