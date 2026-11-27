using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;


[DefaultExecutionOrder(-900)]
public class SettingManager : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] TogImageSet;
    public Color[] KeyImageSet;
    [SerializeField] TMP_SpriteAsset TSAsset;

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


    TMP_Text[] ActionName;
    Image[] KeyImages;
    TMP_Text[] KeyBinds;

    (InputAction action, int bindingIndex)[] KeyActions;
    Dictionary<string, string> Binded = new();
    void Awake()
    {
        if (GameManager.instance.Setting == null)
        {
            GameManager.instance.Setting = this;
            Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
        }
        else { Destroy(gameObject); return; }

        Root = new Node(gameObject); WindowSet.Add(Root); 

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
        int i = 0, l = Keys.Count;


        KeyImages = new Image[l]; KeyBinds = new TMP_Text[l]; ActionName = new TMP_Text[l];
        for (i = 0; i < l; i++)
        {
            if (!Keys[i].TryGetComponent<EventTrigger>(out var et)) et = Keys[i].AddComponent<EventTrigger>();
            ActionName[i] = Keys[i].transform.GetChild(0).GetComponent<TMP_Text>();
            KeyImages[i] = Keys[i].transform.GetChild(1).GetComponent<Image>();
            KeyBinds[i] = Keys[i].transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>();
            KeyBinds[i].spriteAsset = TSAsset; KeyBinds[i].fontSize = 75; KeyBinds[i].richText = true;
            string nmm = InputControlPath.ToHumanReadableString(
                KeyActions[i].action.bindings[KeyActions[i].bindingIndex].effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice
                    );
            KeyBinds[i].text = $"<sprite name=\"{nmm}\">";

            EventUtil<int>.ButtonInit(et, OnKey, OffKey, OnClick, i);

            Binded.Add(KeyActions[i].action.bindings[KeyActions[i].bindingIndex].effectivePath, ActionName[i].text);
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
        KeyImages[ind].color = KeyImageSet[0];
    }

    string PathToHuman(ref string rt)
    {
        return InputControlPath.ToHumanReadableString(rt,
                        InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void ChangeKey((InputAction,int) ActionSet)
    {
        GameManager.instance.FloatM.Init($"<color=red>{ActionName[CurKeyChange].text}</color> 입력 중");
        OnKeyChange = true;

        bool Changed = true;
        var (action, bindingIndex) = ActionSet;
        action.Disable();
        rebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsHavingToMatchPath("<Mouse>")
            .WithExpectedControlType<ButtonControl>()
            .OnApplyBinding((op, path)=>
            {
                string name = PathToHuman(ref path);
                if (Binded.ContainsKey(path))
                {
                    Changed = false;
                    GameManager.instance.FloatM.Init(
                        $"입력된 키 <color=red>{name}</color>은 현재\n<color=blue>{Binded[path]}</color>에서 사용중 입니다.",
                        closingTime: 2f
                        );
                    return;
                }
                KeyBinds[CurKeyChange].text = $"<sprite name=\"{name}\"";
                Binded.Remove(action.bindings[bindingIndex].effectivePath);
                Binded.Add(path, ActionName[CurKeyChange].text);
                action.ApplyBindingOverride(bindingIndex, path);
            })
            .OnComplete(op =>
            {
                op.Dispose();
                rebind = null;
                action.Enable();
                OnKeyChange = false;
                if (Changed) GameManager.instance.FloatM.Close(-1, true);
            })
            .Start();
    }
    #endregion



    #region Something Windows

    class Node
    {
        public Node parent;
        public GameObject self;
        public LinkedSet<Node> child;

        public Node(GameObject _self, Node _parent = null)
        {
            parent = _parent; self = _self; child = new();
            if(_parent != null) _parent.child.Add(this);
            WindowDict.Add(_self, this);
        }

        public void RemoveNode()
        {
            parent?.Removechild(this); parent = null;
            if(self) self.SetActive(false);
            WindowSet.Remove(this); WindowDict.Remove(self);
            while(child.Last(out var T)) T.RemoveNode();
        }
        public void Removechild(Node node)
        {
            child.Remove(node);
        }
    }

    [SerializeField] GameObject MinMenu;

    Node Root; // Manager

    static LinkedSet<Node> WindowSet = new();  // Root 기본 탑재
    static Dictionary<GameObject, Node> WindowDict = new();
    public void TogleWindow()
    {
        // Cur Root = Manager
        // Rollback
        WindowSet.Last(out var t);
        if (t == Root)
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            GameManager.instance.PlayerScript.ToggleAction(true);
            GameManager.instance.Camera.CamSetting(false);
            AddWindow(MinMenu);
        }
        // End Menu Mode
        else
        {
            t.RemoveNode(); WindowSet.Last(out t);
            if (t == Root)
            {
                Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
                GameManager.instance.PlayerScript.ToggleAction(false);
                GameManager.instance.Camera.CamSetting(true);
            }
            else t.self.SetActive(true);
        }
    }

    // 일단은 계층적으로 켜진다고 생각해( grand -> grandchild 같은건 없다)
    // 일단은 Root 하위에 다 있다고 생각읋..?
    public void AddWindow(GameObject window)
    {
        WindowSet.Last(out var et); var t = et.self;
        if (t.transform != window.transform.parent) t.SetActive(false);
        WindowSet.Add(new Node(window, WindowDict[window.transform.parent.gameObject]));
        window.SetActive(true);
    }

    /// <summary>
    /// Root 직속 자식에서는 사용 금지.
    /// </summary>
    public void JustRemove(GameObject window)
    {
        if(WindowDict.TryGetValue(window,out var t)) t.RemoveNode();
    }
    

    #endregion
}
