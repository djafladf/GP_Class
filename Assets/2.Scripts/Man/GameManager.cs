using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static WaitForSeconds MinSeconds = new WaitForSeconds(0.05f);
    public static WaitForSeconds OneSec = new WaitForSeconds(1f);
    public static WaitForSeconds DotOne = new WaitForSeconds(0.1f);
    public static WaitForSeconds DotTwo = new WaitForSeconds(0.2f);
    public static WaitForSeconds DotThree = new WaitForSeconds(0.3f);
    public static WaitForSeconds DotFive = new WaitForSeconds(0.5f);
    [HideInInspector] public Transform Player;
    [HideInInspector] public Player PlayerScript;
    [HideInInspector] public MonsterManager Enemy;
    [HideInInspector] public BulletManager bullet;
    [HideInInspector] public UIManager UI;
    [HideInInspector] public Transform ParticleSet;

    

    public Action<int> PlayerHealFunc;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    [SerializeField] Volume Volume_Day;
    [SerializeField] Volume Volume_Night;
    [SerializeField] float TimeVar = 1;
    [SerializeField] float Trig = -1;
    private void FixedUpdate()
    {
        TimeVar += Time.deltaTime * Trig;
        if (TimeVar >= 1 || TimeVar <= 0.5) Trig *= -1;
        Volume_Day.weight = TimeVar; Volume_Night.weight = 1 - TimeVar;
    }


    List<float> TimeSet = new List<float>();
    public void SetTime(float var, bool IsRemove = false)
    {
        if (IsRemove)
        {
            TimeSet.Remove(var);
            if (TimeSet.Count == 0) Time.timeScale = 1;
            else Time.timeScale = TimeSet[0];
        }
        else
        {
            if (TimeSet.Count == 0) Time.timeScale = var;
            else if (var < TimeSet[0]) Time.timeScale = var;
            TimeSet.Add(var);
            TimeSet.Sort();
        }
    }
}
