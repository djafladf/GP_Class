using UnityEngine;
using UnityEngine.EventSystems;

public class FloatListener : Buttons
{
    [SerializeField] string text;
    [SerializeField] Sprite spr;
    int ind;
    private void Start()
    {
        ind = GameManager.instance.FloatM.Register();
    }

    protected override void OnPointer(PointerEventData data)
    {
        GameManager.instance.FloatM.Init(text,ind:ind,spr:spr);
    }
    protected override void OutPointer(PointerEventData data)
    {
        GameManager.instance.FloatM.Close(ind);
    }
}
