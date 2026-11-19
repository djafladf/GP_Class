using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhosMaker : MonoBehaviour
{
    public struct Ghost
    {
        public GameObject ghost;
        public Transform[] trans;
        public Mesh[] mesh;
        public float Baked;
        public Ghost(GameObject g,Transform[] t, Mesh[] m)
        {
            ghost = g; trans = t;
            mesh = m; Baked = 0;
        }
    }


    [SerializeField] Transform mTrans;
    [SerializeField] SkinnedMeshRenderer[] skins;
    [SerializeField] Material GhostMat;
    Queue<Ghost> unused = new();
    Queue<Ghost> onused = new();

    private void Awake()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject root = new GameObject("Ghost");

            Transform[] trans = new Transform[skins.Length];
            Mesh[] meshes = new Mesh[skins.Length];

            for (int j = 0; j < skins.Length; j++)
            {
                GameObject obj = new GameObject("GhostPart");
                obj.transform.SetParent(root.transform, false);

                meshes[j] = new Mesh();
                obj.AddComponent<MeshFilter>().sharedMesh = meshes[j];
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();

                int count = Mathf.Max(1, skins[j].sharedMesh.subMeshCount);
                Material[] mats = new Material[count];

                for (int k = 0; k < count; k++)
                    mats[k] = GhostMat;

                mr.sharedMaterials = mats;


                trans[j] = obj.transform;
            }

            root.SetActive(false);
            unused.Enqueue(new Ghost(root, trans, meshes));
        }
    }


    void CreateGhost()
    {
        if (!unused.TryDequeue(out var res)) return;

        for (int i = 0; i < skins.Length; i++)
        {
            var skin = skins[i];
            bool visible = skin.enabled && skin.gameObject.activeInHierarchy;

            res.trans[i].gameObject.SetActive(visible);
            if (!visible) continue;

            skin.BakeMesh(res.mesh[i]);

            res.trans[i].SetPositionAndRotation(
                skin.transform.position,
                skin.transform.rotation
            );

            //res.trans[i].localScale = skin.transform.lossyScale;
        }

        res.Baked = Time.time;
        res.ghost.SetActive(true);
        onused.Enqueue(res);

        if (GhostClearing == null)
            GhostClearing = StartCoroutine(ClearGhost());
    }


    public void On()
    {
        if (MakingCor == null) MakingCor = StartCoroutine(GhostCall());
    }
    public void Off()
    {
        if (MakingCor != null) { StopCoroutine(MakingCor); MakingCor = null; }
    }
    private void Start()
    {
        On();
    }
    Coroutine MakingCor = null;
    IEnumerator GhostCall()
    {
        while (true)
        {
            CreateGhost();
            yield return GameManager.DotThree;
        }
    }

    [SerializeField] float bakeTime = 0.1f;
    Coroutine GhostClearing = null;
    IEnumerator ClearGhost()
    {
        while (onused.Count > 0)
        {
            int l = 0;
            yield return GameManager.DotThree; // 0.05f;
            foreach (var j in onused) { if (Time.time - j.Baked > bakeTime) l++; else break; }
            for(;l > 0; l--) { var t = onused.Peek(); t.ghost.SetActive(false); unused.Enqueue(t); onused.Dequeue(); } 
        }
        GhostClearing = null;
    }
}
