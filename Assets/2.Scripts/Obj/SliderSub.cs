using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SliderSub : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text text;
    [SerializeField] UnityEngine.UI.Slider slid;
    [SerializeField] UnityEvent<Vector3> Act;
    [SerializeField] Vector3 Multiplier;

    private void Start()
    {
        valueSet();
    }

    public void valueSet()
    {
        text.text = $"{slid.value}";
        Act.Invoke(Multiplier*slid.value);
    }

    public void ChangeFromExtern(float value)
    {
        slid.value += value;
    }
}
