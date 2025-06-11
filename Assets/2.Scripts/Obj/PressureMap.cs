using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class PressureMap : MonoBehaviour
{
    [SerializeField] GameObject PressurePlat;
    [SerializeField] GameObject CubePlat;
    [SerializeField] GameObject WallSet;
    [SerializeField] Transform pr;
    [SerializeField] AudioClip clip;

    Collider coll;

    MapController MyMap;
    GameObject Wall;
    Transform LookPos;

    int n = 11;
    int mn; 

    int[,] visitCount;

    int min_Fill = 30;
    int Cur_Fill = 0;

    List<Action> RestFunc = new List<Action>();
    private void Start()
    {
        coll = GetComponent<BoxCollider>();
        MyMap = transform.parent.parent.GetChild(0).GetComponent<MapController>();
        Wall = Instantiate(WallSet, pr); LookPos = Wall.transform.GetChild(1);
        visitCount = new int[n, n];
        mn = (n - 1) / 2;
        Vector2Int StartPos = new Vector2Int(mn, mn);
        Vector2Int next;

        List<Vector2Int> MustVisit = new List<Vector2Int>();

        int l = 100;
        while (min_Fill > Cur_Fill || MustVisit.Count != 0)
        {
            if (MustVisit.Count != 0)
            {
                next = MustVisit[0]; MustVisit.RemoveAt(0);
            }
            else
            {
                var cand = new List<Vector2Int>();
                for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) { if (!(x == StartPos.x && y == StartPos.y) && Heuristic(StartPos, new Vector2Int(x, y)) > 3) cand.Add(new Vector2Int(x, y)); }
                if (cand.Count == 0) break;
                cand = cand.OrderBy(_ => Guid.NewGuid()).ToList();
                next = cand[0];
            }
            var Path = FindPathAStar(StartPos, next);
            for (int i = 1; i < Path.Count; i++)
            {
                var pt = Path[i];
                if (pt.x == mn && pt.y == mn) continue;
                if (visitCount[pt.x, pt.y] == 0)
                {
                    var plate = Instantiate(PressurePlat, pr); var script = plate.GetComponentInChildren<PressurePlat>();script.PressureAct = OnOffTrigger; RestFunc.Add(script.ResetFunc);
                    plate.transform.localPosition = new Vector3((pt.x - mn) * 2, 0, (pt.y - mn) * 2);
                    Cur_Fill++;
                }
                visitCount[pt.x, pt.y]++; if (visitCount[pt.x, pt.y] % 2 == 0) MustVisit.Add(new Vector2Int(pt.x, pt.y));
            }
            StartPos = next;
            if (--l < 0) break; // 데드락 바잊
        }
        visitCount[mn, mn] = 1;
        for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) if (visitCount[x, y] == 0) Instantiate(CubePlat, pr).transform.localPosition = new Vector3((x - mn) * 2, 0, (y - mn) * 2);
        min_Fill = Cur_Fill;
        pr.SetParent(transform.parent.parent); pr.transform.localPosition = new Vector3(0, 0, 0);
    }

    public void OnOffTrigger(bool OnOff)
    {
        if (OnOff)
        {
            Cur_Fill--;
            if (Cur_Fill <= 0)
            {
                StopAllCoroutines();
                GameManager.instance.PlayerScript.ControllMoveAble(false);
                Wall.transform.GetChild(0).gameObject.SetActive(true);
                Invoke("EndTask",2);
                GameManager.instance.Audio.PlayClip(2, 1, clip);
            }
        }
        else Cur_Fill++;
    }

    void EndTask()
    {
        MyMap.UnlockNearDoor();
        GameManager.instance.PlayerScript.ControllFocus(false, null); GameManager.instance.PlayerScript.ControllMoveAble(true);
        Wall.transform.GetChild(0).gameObject.SetActive(false);
        GameManager.instance.Data.ResetPool();
        var cnt = GameManager.instance.Data.ReturnItem(GameManager.instance.ParticleSet); cnt.Item3.AddComponent<DropItem>(); cnt.Item3.transform.localScale = Vector3.one * 2;
        cnt.Item3.GetComponent<DropItem>().Init(cnt.Item1, cnt.Item2); cnt.Item3.transform.position = transform.position + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
        for (int i = 0; i < Random.Range(5, 10); i++) { var tmp = Instantiate(GameManager.instance.Data.Exp[0], GameManager.instance.ParticleSet); tmp.transform.position = transform.position + new Vector3(Random.Range(-1f, 1f), 0.1f, Random.Range(-1f, 1f)); }
        Destroy(pr.gameObject); Destroy(transform.parent.gameObject);
    }

    IEnumerator FollowCam()
    {
        while (true)
        {
            LookPos.position = new Vector3(GameManager.instance.Player.position.x, 15, GameManager.instance.Player.position.z);
            yield return GameManager.DotOne;
        }
    }
    Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    List<Vector2Int> FindPathAStar(Vector2Int start,Vector2Int goal)
    {
        var openSet = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var closedSet = new HashSet<Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(pos => fScore.ContainsKey(pos) ? fScore[pos] : int.MaxValue).First(); // PQ
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            var rdCount = Enumerable.Range(0,4).OrderBy(x => Guid.NewGuid()).ToList();
            foreach (var rd in rdCount)
            {
                var neighbor = current + Dirs[rd];
                // 범위 체크
                if (neighbor.x < 0 || neighbor.x >= n || neighbor.y < 0 || neighbor.y >= n)
                    continue;
                // 통과 불가 체크
                if (closedSet.Contains(neighbor))
                    continue;

                int tentativeG = gScore[current] + visitCount[neighbor.x, neighbor.y] %2 != 0 ? 300 * visitCount[neighbor.x,neighbor.y] : 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int cnt = tentativeG + Heuristic(neighbor, goal);
                    fScore[neighbor] = cnt;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // 경로를 찾지 못함
        return null;
    }

    int Heuristic(Vector2Int a, Vector2Int b)
    {
        // 맨해튼 거리
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    public void Init()
    {
        pr.gameObject.SetActive(true);
        OnGame = true;
        //StartCoroutine(FollowCam());
        GameManager.instance.UI.ShowAscending("Trigger All", 2);
        GameManager.instance.PlayerScript.ControllFocus(true, new Tuple<Transform, Transform>(LookPos, GameManager.instance.Player));
        coll.enabled = false; Invoke("Testt", 1);
    }

    void Testt()
    {
        coll.enabled = true;
    }

    public void Reset()
    {
        Cur_Fill = min_Fill;
        foreach (var j in RestFunc) j.Invoke();
    }

    bool OnGame = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(!OnGame) GameManager.instance.UI.ToggleInteract(Init, true, "Press<sprite name=\"e\"> To Interact");
            else GameManager.instance.UI.ToggleInteract(Reset, true, "Press<sprite name=\"e\"> To Reset");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(null, false, null);
        }
    }
}
