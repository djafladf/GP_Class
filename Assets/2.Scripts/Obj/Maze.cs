using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour
{
    [SerializeField] int Size;

    [SerializeField] GameObject CWall, RWall, StoneEffect;
    MazeMap MyMaze;
    BoxCollider col;
    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        MyMaze = new MazeMap();
        MyMaze.MazeMaking(Size,Size);
        MyMaze.Maze[Size / 2-1, Size / 2].Right = true; MyMaze.Maze[Size / 2, Size / 2-1].Down = true;
        MyMaze.Maze[Size / 2, Size / 2].Right = true; MyMaze.Maze[Size / 2, Size / 2].Down = true;
        pr.SetParent(transform.parent);
        MakeWalls();
    }

    [SerializeField] Transform pr;
    // Len : -19.5 ~ 19.5
    void MakeWalls()
    {
        int x, y, Y;
        float cx = 0,cy = 0;
        for (Y = 0; Y < Size; Y++)
        {
            for (x = 0; x < Size; x++)
            {
                cx = -18.5f + x * 4f; cy = 18.5f - Y * 4f;
                if (!MyMaze.Maze[x, Y].Left && x==0)
                {
                    GameObject cnt = Instantiate(CWall, pr);
                    cnt.transform.localPosition = new Vector3(cx - 2,3f,cy);
                }
                if (!MyMaze.Maze[x, Y].Right)
                {
                    GameObject cnt = Instantiate(CWall, pr);
                    cnt.transform.localPosition = new Vector3(cx + 2, 3f,cy);
                }
                if (!MyMaze.Maze[x, Y].Down)
                {
                    GameObject cnt = Instantiate(RWall, pr);
                    cnt.transform.localPosition = new Vector3(cx, 3f,cy - 2);
                }
                if (!MyMaze.Maze[x, Y].Up&& Y == 0)
                {
                    GameObject cnt = Instantiate(RWall,pr);
                    cnt.transform.localPosition = new Vector3(cx, 3f,cy + 2);
                }
            }
        }
    }

    IEnumerator GoUpMaze(bool IsUp)
    {
        for(int y = -1; y <= 1; y++) for(int x = -1; x <= 1; x++)
        {
                GameObject cnt = Instantiate(StoneEffect, transform);
                cnt.transform.position = GameManager.instance.Player.position + new Vector3(-4 * x, 0, -4 * y);
        }
        //Camera.main.GetComponent<CinemachineBrain>().enabled = false;
        GameManager.instance.PlayerScript.ControllMoveAble(false);
        WaitForSeconds LittleSec = new WaitForSeconds(0.05f);
        Vector3 up = new Vector3(0, IsUp ? 0.1f : -0.1f, 0);
        pr.gameObject.SetActive(true);
        col.enabled = false; GameManager.instance.UI.ToggleInteract(null, false, null);
        for (int i = 0; i < 65; i++)
        {
            pr.Translate(up);
            yield return LittleSec;
        }
        GameManager.instance.PlayerScript.ControllMoveAble(true);
        if (IsUp)
        {
            switch (Random.Range(0, 4))
            {
                case 0: transform.localPosition = new Vector3(-18.5f + 4f * Random.Range(0, Size), 0, 18.5f); break;
                case 1: transform.localPosition = new Vector3(-18.5f + 4f * Random.Range(0, Size), 0, -17.5f); break;
                case 2: transform.localPosition = new Vector3(17.5f, 0, 18.5f - 4f * Random.Range(0, Size)); break;
                case 3: transform.localPosition = new Vector3(-18.5f, 0, 18.5f - 4f * Random.Range(0, Size)); break;
            }
            col.enabled = true;
            GameManager.instance.UI.ShowAscending("Find EXIT", 1);
        }
        else { yield return GameManager.DotFive; Destroy(gameObject); }
    }

    IEnumerator CamShake(float time, float intensity = 2)
    {
        Camera.main.GetComponent<CinemachineBrain>().enabled = false;
        WaitForSeconds LittleSec = new WaitForSeconds(0.05f);
        var MainCam = Camera.main;
        float Cx = MainCam.transform.position.x;
        float Cy = MainCam.transform.position.y;
        float Cz = MainCam.transform.position.z;
        float intSub = intensity / (time/0.3f);
        for (float _ = 0; _ < time; _+=0.3f)
        {
            MainCam.transform.position = new Vector3(Cx - 0.2f * intensity, Cy, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx + 0.2f * intensity, Cy, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy + 0.2f * intensity, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy - 0.2f * intensity, Cz);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy, Cz + 0.2f * intensity);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy, Cz - 0.2f * intensity);
            yield return LittleSec;
            MainCam.transform.position = new Vector3(Cx, Cy, Cz);
            intensity -= intSub;
        }
        Camera.main.GetComponent<CinemachineBrain>().enabled = true;
    }

    public void SetMaze()
    {
        StartCoroutine(GoUpMaze(Flag));
        StartCoroutine(CamShake(3.3f));
        Flag = false;
    }

    bool Flag = true;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.UI.ToggleInteract(SetMaze, true,"Press<sprite name=\"e\"> To Interact");
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
