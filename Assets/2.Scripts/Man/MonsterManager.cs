using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;
using Random = UnityEngine.Random;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] bool Test = true;
    [HideInInspector] public List<Action> GameEnd;
    [HideInInspector] public List<Action> DeadAct;
    [SerializeField] List<GameObject> Pref;
    [SerializeField] List<SpawnType> tp;
    List<int> LastUse;

    [HideInInspector] public List<List<GameObject>> Pool;
    private void Awake()
    {
        GameManager.instance.Enemy = this;
        GameEnd = new List<Action>(); DeadAct = new List<Action>();
        Pool = new List<List<GameObject>>();
        MakeNumCnt = new List<int>();
        LastUse = new List<int>();
        for (int i = 0; i < Pref.Count; i++) { Pool.Add(new List<GameObject>()); MakeNumCnt.Add(0); LastUse.Add(0); }


        RegisterMonsterType(0, 60);
        RegisterMonsterType(1, 60);
        RegisterMonsterType(2, 30);
    }

    
    public void LoadEnd()
    {
        // Fiil Objects
        for (int i = 0; i < Pref.Count; i++) for (int x = Pool[i].Count; x < MakeNumCnt[i]; x++) { GameObject cnt = Instantiate(Pref[i],transform); cnt.SetActive(false); Pool[i].Add(cnt); }
        // Del UnUse Objects
        for (int i = 0; i < Pref.Count; i++) for (int x = MakeNumCnt[i]; x < Pool[i].Count; x++) { Destroy(Pool[i][x]); Pool[i].RemoveAt(MakeNumCnt[i]); }
    }

    List<int> MakeNumCnt;
    public void RegisterMonsterType(int type, int Num)
    {
        int MakeNum = Pool[type].Count - Num; MakeNumCnt[type] = Mathf.Max(MakeNumCnt[type], Num);
        if (Test) for (int i = 0; i < Pref.Count; i++) for (int x = Pool[i].Count; x < MakeNumCnt[i]; x++) { GameObject cnt = Instantiate(Pref[i], transform); cnt.SetActive(false); Pool[i].Add(cnt); }
    }



    public void StartMaking(int dif, int time, ref List<Transform> SpawnPos)
    {
        foreach(var j in tp)
        {
            if (j.MinLevel <= dif) StartCoroutine(Make(j.EnemyID,time,SpawnPos));
        }
    }

    IEnumerator Make(int id,int time, List<Transform> SpawnPos)
    {
        int Count = Mathf.FloorToInt(time / tp[id].SpawnGap);
        var wfs = new WaitForSeconds(tp[id].SpawnGap);
        int MaxFind = Pool[id].Count; int l = SpawnPos.Count;
        for (int _ = 0; _ < Count; _++)
        {
            for (int i = 0; i < MaxFind; i++)
            {
                LastUse[id]++; if (LastUse[id] == MaxFind) LastUse[id] = 0;
                if (!Pool[id][LastUse[id]].activeSelf)
                {
                    Pool[id][LastUse[id]].transform.position = SpawnPos[GameManager.rng.Next(l)].position;
                    Pool[id][LastUse[id]].SetActive(true);
                    break;
                }
            }
            yield return wfs;
        }

    }

    public void KillAll()
    {
        StopAllCoroutines();
        foreach (var j in DeadAct) j.Invoke();
    }

    public void OnGameEnd()
    {
        StopAllCoroutines();
        foreach (var j in GameEnd) j.Invoke();
    }

}
