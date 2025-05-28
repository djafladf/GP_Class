using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProjectorSub : MonoBehaviour
{
    Image mi;
    private void Awake()
    {
        mi = GetComponent<Image>();
    }

    private void OnEnable()
    {
        mi.material.SetColor("_Color", new Color(0.2f, 0.3f, 0.5f, 1));
    }

    private void Update()
    {
        mi.material.SetFloat("_UnscaledTime", Time.unscaledTime);
    }
}
