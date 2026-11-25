using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    // Master, Background, Effet
    AudioSource[] Sources;
    List<AudioMixerGroup> Volumes = new List<AudioMixerGroup>();
    private void Awake()
    {
        if (GameManager.instance.Audio == null)
        {
            GameManager.instance.Audio = this;
            Sources = GetComponents<AudioSource>();
            Volumes.AddRange(mixer.FindMatchingGroups("Master"));
        }
        else Destroy(gameObject);
    }

    public void SetMasterVolume(Vector3 var)
    {
        float vol = Mathf.Log10(Mathf.Max(var.x * 0.01f, 0.0001f)) * 20f;
        mixer.SetFloat("Master", vol);
    }
    public void SetBGMVolume(Vector3 var)
    {
        float vol = Mathf.Log10(Mathf.Max(var.x * 0.01f, 0.0001f)) * 20f;
        mixer.SetFloat("BGM", vol);
    }
    public void SetSFXVolume(Vector3 var)
    {
        float vol = Mathf.Log10(Mathf.Max(var.x * 0.01f, 0.0001f)) * 20f;
        mixer.SetFloat("SFX", vol);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">0:?, 1:BGM, 2: SFX</param>
    /// <param name="vol"> volume </param>
    /// <param name="clip"> clip </param>
    public void PlayClip(int type,float vol,AudioClip clip)
    {
        Sources[type].PlayOneShot(clip, vol);
    }

    public void ChangePitch()
    {
        mixer.SetFloat("SFX_Pitch", Mathf.Log(Mathf.Clamp(Time.timeScale, 0.01f, 1f),2f) * 12f);
    }

    public void StopBGM()
    {
        Sources[1].Stop();
    }
}
