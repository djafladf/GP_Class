using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRTEffect : MonoBehaviour
{
    public Shader Shader;
    public Material _material;

    private void Start()
    {
        GameManager.instance.shad = this;
        this.enabled = false;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, _material);
    }

    /// <summary>
    /// Toggle Shader
    /// </summary>
    /// <param name="Toggletype">0:Toggle,1:On,2:Off</param>
    /// <returns></returns>
    public bool ToggleShader(int Toggletype = 0)
    {
        if (Toggletype == 1) this.enabled = true;
        else if (Toggletype == 2) this.enabled = false;
        else this.enabled = this.enabled == false;
        return this.enabled;
    }

    public void ChangeShader(ref Shader Changer)
    {
        Destroy(_material);
        Shader = Changer;
        _material = new Material(Shader);
    }

    public void SendMessageToShader(Dictionary<string,float> Messages)
    {
        foreach (var j in Messages) _material.SetFloat(j.Key, j.Value);
    }
}
