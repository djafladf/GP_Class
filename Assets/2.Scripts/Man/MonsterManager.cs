using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] bool Test = true;
    [HideInInspector] public List<Action> GameEnd;
    [HideInInspector] public List<Action> DeadAct;
    [SerializeField] List<GameObject> Pref;
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

    /// <summary>
    /// Make Mop
    /// </summary>
    /// <param name="tp"> Spawn Type </param>
    /// <param name="StartPos"> Stand Pos </param>
    /// <param name="Size"> Size Of Space </param>
    /// <param name="ExclusiveField"> Obstalce Pos </param>
    public void StartMaking(ref SpawnType tp)
    {
        for (int i = 0; i < tp.SpawnGap.Count; i++) StartCoroutine(Make(tp.EnemyID[i], tp.StartTime[i], tp.SpawnGap[i],tp.LastTime,tp.SpawnPos));
    }

    IEnumerator Make(int ind, float StartTime, float SpawnGap, int Last,List<Transform> SpawnPos)
    {
        if(StartTime != 0) yield return new WaitForSeconds(StartTime);
        int l = Mathf.FloorToInt(Last / SpawnGap);

        var wfs = new WaitForSeconds(SpawnGap);
        Vector3 PosCnt = Vector3.zero;
        int MaxFind = Pool[ind].Count;
        for(int i = 0; i < l; i++)
        {
            PosCnt = SpawnPos[Random.Range(0, SpawnPos.Count)].position;
            for(int _ = 0; _ < MaxFind; _++)
            {
                LastUse[ind]++; if (LastUse[ind] == MaxFind) LastUse[ind] = 0;
                if (!Pool[ind][LastUse[ind]].activeSelf)
                {
                    Pool[ind][LastUse[ind]].transform.position = PosCnt;
                    Pool[ind][LastUse[ind]].SetActive(true);
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
