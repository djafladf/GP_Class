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

    public bool ToggleShader(bool MustOn = false)
    {
        if (MustOn) this.enabled = true;
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
