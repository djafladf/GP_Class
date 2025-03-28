using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    [SerializeField] GameObject BulletPref;

    List<GameObject> BulletPool = new List<GameObject>();
    List<Bullet> BulletScripts = new List<Bullet>();
    int front = 0;

    private void Awake()
    {
        for(int i = 0; i < 50; i++)
        {
            GameObject cntPref = Instantiate(BulletPref, transform); cntPref.SetActive(false);
            BulletPool.Add(cntPref); BulletScripts.Add(cntPref.GetComponent<Bullet>());
        }
    }
    private void Start()
    {
        GameManager.instance.bullet = this;
    }

    public void ShootBullet(Vector3 Start, Vector3 Dir)
    {
        for(int i = 0; i < BulletPool.Count;i++)
        {
            GameObject CurBul = BulletPool[front];
            if (!CurBul.activeSelf)
            {
                BulletScripts[front].Init(Start, Dir);
                front = (front + 1) % BulletPool.Count;
                break;
            }
            front = (front + 1) % BulletPool.Count;
        }
    }
}
