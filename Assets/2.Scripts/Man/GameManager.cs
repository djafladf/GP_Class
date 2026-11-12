using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static WaitForSeconds MinSeconds = new WaitForSeconds(0.05f);
    public static WaitForSeconds OneSec = new WaitForSeconds(1f);
    public static WaitForSeconds TwoSec = new WaitForSeconds(2f);
    public static WaitForSeconds DotOne = new WaitForSeconds(0.1f);
    public static WaitForSeconds DotTwo = new WaitForSeconds(0.2f);
    public static WaitForSeconds DotThree = new WaitForSeconds(0.3f);
    public static WaitForSeconds DotFive = new WaitForSeconds(0.5f);
    [HideInInspector] public Transform Player;
    [HideInInspector] public Player PlayerScript;
    [HideInInspector] public BulletManager bullet;
    [HideInInspector] public UIManager UI;
    [HideInInspector] public DataManager Data;
    [HideInInspector] public AudioManager Audio;
    [HideInInspector] public CameraManager Camera;
    [HideInInspector] public Transform ParticleSet;
    [HideInInspector] public FloatMessage FloatM;
    
    public Action<int> PlayerHealFunc;

    public static System.Random rng = new System.Random();

    public static void SetSeed(int seed) => rng = new System.Random(seed);
    public static void ResetSeed() => rng = new System.Random();

    public static void RandomIndex<T>(List<T> list)
    {
        for(int i = list.Count-1; i > 0; i--)
        {
            int swapInd = rng.Next(i + 1);
            (list[i], list[swapInd]) = (list[swapInd], list[i]);
        }
    }
    public static List<T> RandomIndex_Return<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapInd = rng.Next(i + 1);
            (list[i], list[swapInd]) = (list[swapInd], list[i]);
        }
        return list;
    }

    private void Awake()
    {
        if (instance == null) { instance = this; }
        else Destroy(gameObject);
    }

    [SerializeField] float TimeVar = 1;
    [SerializeField] float Trig = -1;
    private void FixedUpdate()
    {
        TimeVar += Time.deltaTime * Trig;
        if (TimeVar >= 1 || TimeVar <= 0.5) Trig *= -1;
    }



    private readonly PriorityQueue<float> TimeSet = new((a,b) => a < b);
    public void SetTime(float var, bool IsRemove = false)
    {
        if (IsRemove) TimeSet.Remove(var);
        else TimeSet.push(var);
        Time.timeScale = TimeSet.Count > 0 ? TimeSet.top() : 1f;
    }
}
