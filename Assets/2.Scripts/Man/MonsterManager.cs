using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterManager : MonoBehaviour
{
    [HideInInspector] public List<Action> GameEnd;
    [SerializeField] GameObject Pref;

    [HideInInspector] public List<GameObject> Pool = new List<GameObject>();
    int LastPool = 1;

    int PoolSize = 10;
    private void Awake()
    {
        GameEnd = new List<Action>();
        for(int i = 0; i < PoolSize; i++)
        {
            Pool.Add(Instantiate(Pref, transform));
            Pool[i].gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        GameManager.instance.Enemy = this;
        StartCoroutine(Make());
    }

    IEnumerator Make()
    {
        WaitForSeconds wfs = new WaitForSeconds(4f);

        while (true)
        {
            yield return wfs;
            if (Pool.Count < 1) continue;
            GameObject top = Pool[0]; Pool.RemoveAt(0); top.SetActive(true);
            switch (Random.Range(0, 4))
            {
                case 0:
                    top.transform.position = new Vector3(20f, 0, Random.Range(-20f, 20f));
                    break;
                case 1:
                    top.transform.position = new Vector3(-20f, 0, Random.Range(-20f, 20f));
                    break;
                case 2:
                    top.transform.position = new Vector3(Random.Range(-20f, 20f), 0, 20f);
                    break;
                case 3:
                    top.transform.position = new Vector3(Random.Range(-20f, 20f), 0, -20f);
                    break;
            }
        }
    }

    public void OnGameEnd()
    {
        StopAllCoroutines();
        foreach (var j in GameEnd) j.Invoke();
    }

}
