using System.Collections.Generic;
using Unity.AppUI.Core;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] List<CinemachineCamera> FC; // Normal, Lock
    [SerializeField] List<CinemachineOrbitalFollow> orbs;
    [SerializeField] List<Transform> LockOrbs; // Mid, Aim;
    Camera MainCam;
    private void Awake()
    {
        GameManager.instance.Camera = this;
        MainCam = GetComponent<Camera>();
    }

    Vector3 drLock;
    // Lock Update;
    private void LateUpdate()
    {
        if (!LockCheck) return;
        drLock = CurLock.position - LockOrbs[0].position; drLock.y = 0;
        if (drLock.sqrMagnitude >= 1f) orbs[1].HorizontalAxis.Value = Mathf.Atan2(drLock.x, drLock.z) * Mathf.Rad2Deg;
        LockOrbs[1].position = Vector3.Lerp(LockOrbs[0].position, CurLock.position, 0.4f);
    }

    Vector3  _f, _r;
    public void CamFront(ref Vector3 Dir, ref Vector3 _nd)
    {
        _f = MainCam.transform.forward; _f.y = 0; _f.Normalize();
        _r = MainCam.transform.right; _r.y = 0; _r.Normalize();
        _nd = (_f * Dir.y + _r * Dir.x);
    }
    [HideInInspector] public Transform CurLock;
    public bool LockCheck = false;
    public void ToLock(Transform To = null)
    {
        if(To != null) CurLock = To;
        OnUse.Add(CurLock);
        LockCheck = true;
        FC[1].Priority = 20;
    }
    public void UnLock()
    {
        orbs[0].HorizontalAxis.Value = orbs[1].HorizontalAxis.Value;
        orbs[0].VerticalAxis.Value = orbs[1].VerticalAxis.Value;
        orbs[0].RadialAxis.Value = orbs[1].RadialAxis.Value;
        FC[1].Priority = 0;
        CurLock = null; LockCheck = false; OnUse.Clear();
    }

    Vector3 fixedDir = default;
    public void LookRot(Rigidbody rig)
    {
        if (!LockCheck || CurLock == null) return;
        fixedDir = CurLock.position - rig.position; fixedDir.y = 0;
        if (fixedDir.sqrMagnitude < 0.001f) return;
        rig.MoveRotation(Quaternion.LookRotation(fixedDir));
    }

    PriorityQueue<Transform, float> LockPQ = new((a, b) => a.Item2 < b.Item2);
    HashSet<Transform> OnUse = new();
    Collider[] Target = new Collider[32];
    [SerializeField] LayerMask EMask = 1 << 7, OMask = (1 << 6) | (1 << 12);
    public void OnLock()
    {
        var OriginPos = LockOrbs[0].position;
        int res = Physics.OverlapSphereNonAlloc(OriginPos, 15f, Target, EMask);

        Transform tcnt;
        Vector3 pcnt;
        float dist;
        float w = 2; // Max Dist

        LockPQ.Clear(); // Lock On을 한 시점 Dist는 달라질 수 있기에 매번 갱신이 필요하지 않나.
        for (int i = 0; i < res; i++)
        {
            tcnt = Target[i].transform;
            pcnt = Camera.main.WorldToViewportPoint(Target[i].bounds.center);
            if (pcnt.z <= 0 || pcnt.x < 0 || pcnt.x > 1 || pcnt.y < 0 || pcnt.y > 1)
            {
                OnUse.Remove(tcnt);
                continue;
            }
            if (Physics.Linecast(OriginPos, Target[i].bounds.center, OMask)) continue;

            pcnt.x -= 0.5f; pcnt.y -= 0.5f; pcnt.z = 0;
            dist = pcnt.sqrMagnitude;
            if (OnUse.Contains(Target[i].transform)) dist += w;

            LockPQ.push((tcnt, dist));
        }
        print(OnUse.Count);
        print(LockPQ.Count);
        if (LockPQ.Count == 0) UnLock();
        else
        {
            var to = LockPQ.pop();
            if (to.Item2 >= 2) UnLock();
            else { CurLock = to.Item1; ToLock(); }
        }
    }

}
