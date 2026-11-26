using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;


[DefaultExecutionOrder(-900)]
public class SettingManager : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] TogImageSet;
    public Color[] KeyImageSet;

    [SerializeField] InputActionAsset PlayerInput;

    [Header("Sound")]
    [SerializeField] List<SliderSub> Sounds;
    [Header("Camera")]
    [SerializeField] List<SliderSub> Cams;
    [Header("System")]
    [SerializeField] List<SliderSub> Systems1;
    [SerializeField] List<Toggles> Systems2;
    [Header("Key")]
    [SerializeField] List<GameObject> Keys;
    List<TMP_Text> KeyBinds = new();
    List<UnityEngine.UI.Image> KeyImages = new();

    (InputAction action, int bindingIndex)[] KeyActions;
    Dictionary<string, List<int>> Binded;
    void Awake()
    {
        if (GameManager.instance.Setting == null)
        {
            GameManager.instance.Setting = this;
            Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
        }
        else Destroy(gameObject);

        int i = 0;
        foreach(var j in Keys)
        {
            if(!j.TryGetComponent<EventTrigger>(out var et)) et = j.AddComponent<EventTrigger>();
            KeyImages.Add(j.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>());
            KeyBinds.Add(j.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>());
            EventUtil<int>.ButtonInit(et, OnKey, OffKey, OnClick,i);
            i++;
        }

        var p = PlayerInput.FindActionMap("Player");
        var ui = PlayerInput.FindActionMap("UI");

        var move = p.FindAction("Move");

        KeyActions = new (InputAction, int)[]
            {
            (move, 2),                   // 0  Up (W)
            (move, 4),                   // 1  Down (S)
            (move, 6),                   // 2  Left (A)
            (move, 8),                   // 3  Right (D)
            (p.FindAction("Dodge"), 0),  // 4
            (p.FindAction("LHand"), 1),  // 5
            (p.FindAction("RHand"), 1),  // 6
            (p.FindAction("Jump"), 0),   // 7
            (p.FindAction("Lock"), 0),   // 8
            (p.FindAction("Double"), 0), // 9
            (p.FindAction("Interact"), 0),// 10
            (p.FindAction("Special"), 0),// 11
            (p.FindAction("Scroll"), 0), // 12
            (ui.FindAction("Cancel"), 0) // 13
            };
        int l = 0;
        foreach (var j in KeyActions)
        {
            Binded.Add(j.action.bindings[j.bindingIndex].effectivePath,new List<int> { l++ });
        }
    }


    #region KeySetting
    InputActionRebindingExtensions.RebindingOperation rebind;
    int CurKeyChange = -1;
    bool OnKeyChange = false;

    public void OnClick(PointerEventData dt, int ind)
    {
        if (OnKeyChange) return;
        CurKeyChange = ind;
        ChangeKey(KeyActions[ind]);
    }

    public void OnKey(PointerEventData dt, int ind)
    {
        if (OnKeyChange) return;
        CurKeyChange = ind;
        KeyImages[ind].color = KeyImageSet[1];
    }

    public void OffKey(PointerEventData dt, int ind)
    {
        if (OnKeyChange) return;
        KeyImages[ind].color = KeyImageSet[0];
    }

    
    public void ChangeKey((InputAction,int) ActionSet)
    {
        var (action, bindingIndex) = ActionSet;
        action.Disable();

        bool WasSame = false;
        rebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsHavingToMatchPath("<Mouse>")
            .WithExpectedControlType<ButtonControl>()
            .OnApplyBinding((op, path)=>
            {
                if (Binded.ContainsKey(path))
                {
                    string name = InputControlPath.ToHumanReadableString(path,
                        InputControlPath.HumanReadableStringOptions.OmitDevice
                    );
                    KeyBinds[bindingIndex].text = name;
                    KeyBinds[bindingIndex].color = Color.red;
                    WasSame = true;
                    Binded[path].Add(bindingIndex);
                    foreach (var j in Binded[path]) KeyBinds[j].color = Color.red;
                    return;
                }
                action.ApplyBindingOverride(bindingIndex, path);
            })
            .OnComplete(op =>
            {
                if (!WasSame)
                {
                    string key = action.GetBindingDisplayString(bindingIndex);
                    KeyBinds[CurKeyChange].text = key;
                }
                op.Dispose();
                rebind = null;
                action.Enable();
            })
            .Start();
    }
    #endregion



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
