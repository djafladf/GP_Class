using System.Collections.Generic;
using Unity.AppUI.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

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
        aDeg = 0;
    }
    Vector3 drLock;
    // Lock Update;

    [SerializeField] Transform Nav;
    [SerializeField] List<GameObject> Pins; // E N W S , Boss;
    [SerializeField] List<GameObject> EPins;
    int[] ddp = { 0, 90, 180, 270 };
    [SerializeField] Transform BossObj;
    List<Transform> DetectObjs = new();
    float aDeg;
    private void FixedUpdate()
    {
        aDeg = Mathf.Repeat(90f - MainCam.transform.eulerAngles.y, 360f);
        float delta;
        for (int i = 0; i < 4; i++)
        {
            delta = Mathf.DeltaAngle(aDeg, ddp[i]);
            if (Mathf.Abs(delta) <= 100) { Pins[i].SetActive(true); Pins[i].transform.localPosition = new Vector3((delta + 50) * 10, 0, 0); }
            else Pins[i].SetActive(false);
        }
        int l = 0;
        Vector3 pPos = LockOrbs[0].position;
        Vector3 cnt = BossObj.position - pPos;
        float deg = Mathf.Atan2(cnt.z, cnt.x) * Mathf.Rad2Deg;
        delta = Mathf.DeltaAngle(aDeg, deg);
        if (Mathf.Abs(delta) <= 100) { Pins[4].SetActive(true); Pins[4].transform.localPosition = new Vector3((delta + 50) * 10, 0, 0); }
        else Pins[4].SetActive(false);

        foreach (var j in DetectObjs)
        {
            cnt = j.position - pPos;
            deg = Mathf.Atan2(cnt.z, cnt.x) * Mathf.Rad2Deg;
            delta = Mathf.DeltaAngle(aDeg, deg);
            if (Mathf.Abs(delta) <= 100) { EPins[l].SetActive(true); EPins[l].transform.localPosition = new Vector3((delta + 50) * 10, 0, 0); }
            l++;
            // 일단 l이 Pool 개수 넘어가는건 가정하지 않음
        }
        for (int i = l; i < EPins.Count; i++) EPins[i].SetActive(false);
    }
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
        if (LockPQ.Count == 0) UnLock();
        else
        {
            var to = LockPQ.pop();
            if (to.Item2 >= 2) UnLock();
            else { CurLock = to.Item1; ToLock(); }
        }
    }

}
