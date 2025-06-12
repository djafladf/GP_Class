using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

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
    [HideInInspector] public DataManager Data;
    [HideInInspector] public AudioManager Audio;
    [HideInInspector] public Transform ParticleSet;
    [HideInInspector] public FloatMessage FloatM;

    public CinemachineVirtualCamera CV;
    public CinemachineTransposer CVtr;
    public Action<int> PlayerHealFunc;

    [HideInInspector] public int CurDepth = 0;

    public readonly static System.Random rng = new System.Random();

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
        if (instance == null) { instance = this; CVtr = CV.GetCinemachineComponent<CinemachineTransposer>(); }
        else Destroy(gameObject);
    }

    int NumToLoad = 4; // Manager + Player;
    private void LoadAct()
    {

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



    private readonly SortedSet<float> TimeSet = new SortedSet<float>();
    public void SetTime(float var, bool IsRemove = false)
    {
        if (IsRemove) TimeSet.Remove(var);
        else TimeSet.Add(var);
        Time.timeScale = TimeSet.Count > 0 ? TimeSet.Min : 1f;
    }

    
    [System.Serializable]
    public class MapStruct
    {
        public int MaxType = 6;
        public int MapSize;
        public int MaxDepth;
        public int MinSpreadDepth;
        public int[,] MapType;
        public int[,] Depth;
        public GameObject[,] RoomPrefs;
        public Vector4[,] GoAble;
        public MapController[,] MapCont;
    }
    public MapStruct Map;

    record MapQue
    {
        public int CurDepth;
        public Vector2 CurPos;
        MapQue() { }
        public MapQue(int a, Vector2 s) { CurDepth = a; CurPos = s; }
    }

    public int bx, by;
    Vector3[] dp = { Vector3.forward, Vector3.back, Vector2.right, Vector2.left };
    int[] dp2 = { 1, 0, 3, 2 };
    public void MapMaking()
    {
        Map.MapType = new int[Map.MapSize, Map.MapSize]; Map.Depth = new int[Map.MapSize, Map.MapSize]; Map.GoAble = new Vector4[Map.MapSize, Map.MapSize]; Map.MapCont = new MapController[Map.MapSize, Map.MapSize]; Map.RoomPrefs = new GameObject[Map.MapSize, Map.MapSize];
        bx = Random.Range(2, Map.MapSize - 2); by = Random.Range(2, Map.MapSize-2);
        Map.MapType[bx,by] = 1;
        var s = new MapQue(1, new Vector2(bx, by));
        Queue <MapQue> que = new Queue<MapQue>(); que.Enqueue(s);

        int lastx = bx, lasty = by;
        int[,] dpz = { { 0,1 }, { 0,-1 }, {1,0 }, {-1,0 } };
        Tuple<int, int, int> sb = new Tuple<int, int, int>(bx, by, 1);
        List<int> rd = Enumerable.Range(0, 4).ToList();
        for (int i = 1; i < Map.MaxDepth; i++)
        {
            GameManager.RandomIndex(rd);
            for(int z = 0; z < 4; z++)
            {
                int nx = lastx + dpz[rd[z],0], ny = lasty + dpz[rd[z],1];
                if (nx < 0 || nx >= Map.MapSize || ny < 0 || ny >= Map.MapSize) continue;
                if (Map.MapType[nx, ny] > 0) continue;
                if (Mathf.Abs(sb.Item1 - bx) + Mathf.Abs(sb.Item2 - by) <= Mathf.Abs(nx - bx) + Mathf.Abs(ny - by))
                {
                    sb = new Tuple<int, int, int>(nx, ny, z); 
                }
            }

            var cpass = Instantiate(PassPref, MapPr); cpass.transform.position = new Vector3(80f * lastx + dpz[rd[sb.Item3],0] * 40f, 0, 80f * lasty + dpz[rd[sb.Item3], 1] * 40f); if (rd[sb.Item3] >= 2) cpass.transform.Rotate(0, 90, 0);
            Map.GoAble[lastx,lasty][rd[sb.Item3]] = 1; Map.GoAble[sb.Item1, sb.Item2][dp2[rd[sb.Item3]]] = 1; Map.Depth[sb.Item1, sb.Item2] = i + 1;
            Map.MapType[sb.Item1, sb.Item2] = Random.Range(0,3) == 2 ? 3 : 2; lastx = sb.Item1; lasty = sb.Item2;

            que.Enqueue(new MapQue(i+1,new Vector2(lastx,lasty)));
        }
        Map.MapType[lastx, lasty] = 6; // Boss
        
        float[] Depth = { 0,0.7f, 0.4f, 0.3f, 0.2f, 0 };
        while(que.Count > 0)
        {
            MapQue cnt = que.Dequeue();
            var cmap = Instantiate(MapPref, MapPr); cmap.transform.position = new Vector3(80 * cnt.CurPos.x, 0, 80 * cnt.CurPos.y);
            int cx = Mathf.FloorToInt(cnt.CurPos.x), cy = Mathf.FloorToInt(cnt.CurPos.y); Map.RoomPrefs[cx, cy] = cmap;
            Map.MapCont[cx, cy] = cmap.transform.GetChild(0).GetComponent<MapController>();
            Map.MapCont[cx, cy].Init(cx, cy);
            if (Map.MapType[cx, cy] != 1) { var tmp = Instantiate(ObjPrefs[Map.MapType[cx, cy]], cmap.transform); if (Map.MapType[cx, cy] == 3) Destroy(tmp.transform.GetChild(Random.Range(1,3)).gameObject); }
            if (cnt.CurDepth == Map.MaxDepth) continue;

            int l = -1;

            if (cnt.CurDepth <= Map.MinSpreadDepth) l = Random.Range(0, 4); // 최소 1개의 연결을 보장
            for (int i = 0; i < 4; i++)
            {
                int nx = Mathf.FloorToInt(cnt.CurPos.x + dp[i].x), ny = Mathf.FloorToInt(cnt.CurPos.y + dp[i].z);
                if (nx < 0 || nx >= Map.MapSize || ny < 0 || ny >= Map.MapSize) continue;   // Out Range
                if (Map.MapType[nx, ny] > 0)
                {
                    if (Map.Depth[nx, ny] >= cnt.CurDepth)   // 이미 만들어진 구역이며 자신 보다 더 깊은 Node일 경우 통로만 생성
                    {
                        var cpass = Instantiate(PassPref, MapPr);
                        cpass.transform.position = cmap.transform.position + dp[i] * 40; if (i >= 2) cpass.transform.Rotate(0, 90, 0);
                        Map.GoAble[cx, cy][i] = 1; Map.GoAble[nx, ny][dp2[i]] = 1;
                    }
                }
                else if ((Random.Range(0f, 1f) < Depth[cnt.CurDepth] || l == i) || (l > 0 && (i > l || i == 3)))
                {
                    int var = Random.Range(2, Map.MaxType + 1);     // Shop or Pond Must Be Unique
                    if (var >= 4) { var = Map.MaxType; Map.MaxType -= 1; }

                    Map.MapType[nx, ny] = var; Map.Depth[nx, ny] = cnt.CurDepth + 1; Map.GoAble[cx, cy][i] = 1; Map.GoAble[nx, ny][dp2[i]] = 1;
                    var cpass = Instantiate(PassPref, MapPr);
                    cpass.transform.position = cmap.transform.position + dp[i] * 40; if (i >= 2) cpass.transform.Rotate(0, 90, 0);
                    que.Enqueue(new MapQue(cnt.CurDepth + 1, new Vector2(nx, ny)));
                    l = -1;
                }
            }
        }

        for(int y = 0; y < Map.MapSize; y++) for(int x = 0; x < Map.MapSize; x++)
            {
                if (Map.MapCont[x, y] == null) continue;
                Map.MapCont[x, y].MakeWall(ref Map.GoAble[x, y]);
            }
        OpenNearRoom(bx, by);
        MapPr.GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    // u, d, r, l (0,1),(1,0),
    public void OpenNearRoom(int x, int y)
    {
        int nx, ny;
        Map.MapCont[x, y].ToggleAllDoor(true);
        for(int i = 0; i < 4; i++)
        {
            nx = Mathf.FloorToInt(x + dp[i].x); ny = Mathf.FloorToInt(y + dp[i].z);
            if (Map.GoAble[x, y][i] == 1) Map.MapCont[nx,ny].ToggleExtDorr(dp2[i],true);
        }
    }


    [SerializeField] Transform MapPr;
    [SerializeField] GameObject MapPref, PassPref;
    [SerializeField] List<GameObject> ObjPrefs;
}
