using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    [SerializeField] GameObject BulletPref;

    List<Rigidbody> BulletRigid = new List<Rigidbody>();
    List<GameObject> BulletPool = new List<GameObject>();
    int front = 0;

    private void Awake()
    {
        for(int i = 0; i < 50; i++)
        {
            GameObject cntPref = Instantiate(BulletPref, transform); cntPref.SetActive(false);
            BulletPool.Add(cntPref); BulletRigid.Add(cntPref.GetComponent<Rigidbody>());
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
                CurBul.transform.position = Start;
                CurBul.transform.rotation = Quaternion.FromToRotation(Vector3.up, Dir);
                BulletRigid[front].velocity = Vector3.zero;
                CurBul.SetActive(true);
                BulletRigid[front].AddForce(Dir * 5, ForceMode.Impulse);
                front = (front + 1) % BulletPool.Count;
                break;
            }
            front = (front + 1) % BulletPool.Count;
        }
    }
}
