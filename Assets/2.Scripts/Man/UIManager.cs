using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] TMP_Text Score;

    int CurScore = 0;
    private void Start()
    {
        GameManager.instance.UI = this;
    }

    public void ScoreUp()
    {
        CurScore += 10;
        Score.text = $"{CurScore}".PadLeft(5, '0');
    }
}
