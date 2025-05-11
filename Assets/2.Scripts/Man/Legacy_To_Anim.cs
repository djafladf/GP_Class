using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Legacy_To_Anim : MonoBehaviour
{
    [SerializeField] AnimationClip[] Clips;

    private void Awake()
    {
        ChangeToNormal();
    }

    public void ChangeToNormal()
    {
        foreach(var i in Clips)
        {
            if (i.legacy) i.legacy = false;
        }
    }
}
