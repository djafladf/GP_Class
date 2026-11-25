using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-900)]
public class SettingManager : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] List<SliderSub> Sounds;
    [Header("Camera")]
    [SerializeField] List<SliderSub> Cams;
    [Header("System")]
    [SerializeField] List<SliderSub> Systems;
    

    void Awake()
    {
        if (GameManager.instance.Setting == null)
        {
            GameManager.instance.Setting = this;
            Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
        }
        else Destroy(gameObject);
    }

    #region Something Windows
    [SerializeField] GameObject MinMenu;
    Stack<GameObject> WindowStack = new();
    public void TogleWindow()
    {
        // On Menu
        if (WindowStack.TryPop(out var topW))
        {
            topW.SetActive(false);
            if (WindowStack.TryPeek(out topW)) topW.gameObject.SetActive(true);
            else 
            { 
                Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
                GameManager.instance.PlayerScript.ToggleAction(false);
                GameManager.instance.Camera.ResetSetting();
            }
        }
        else { AddWindow(MinMenu); }
    }

    public void AddWindow(GameObject window)
    {
        // Normal -> First Menu
        if (!WindowStack.TryPeek(out var topW))
        {
            GameManager.instance.PlayerScript.ToggleAction();
            GameManager.instance.Camera.CamSetting(AllowFollow: false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        else topW.SetActive(false);
        WindowStack.Push(window); window.gameObject.SetActive(true);
    }

    

    #endregion
}
