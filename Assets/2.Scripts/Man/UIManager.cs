using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] TMP_Text Score;
    [SerializeField] TMP_Text HP_Text;
    [SerializeField] Image HPBar;

    [SerializeField] GameObject PlayUI;
    [SerializeField] GameObject PauseUI;

    int CurScore = 0;
    private void Start()
    {
        GameManager.instance.UI = this;
    }

    public void HPChange(float rf)
    {
        HPBar.fillAmount = rf;
    }
    public void ExpChange(float amount)
    {

    }

    public void ScoreUp()
    {
        CurScore += 10;
        Score.text = $"{CurScore}".PadLeft(5, '0');
    }

    bool CurUIState = true;
    public void UIToggle()
    {
        if (CurUIState) { 
            PlayUI.SetActive(false); PauseUI.SetActive(true); GameManager.instance.SetTime(0);
            GameManager.instance.shad.SendMessageToShader(new Dictionary<string, float>{
            { "BlurRadius",3 },
            { "AlphaWeight",0.3f},
            { "Power",10 }
        }
        );
           GameManager.instance.shad.ToggleShader(1); CurUIState = false;
        }
        else { PlayUI.SetActive(true); PauseUI.SetActive(false); GameManager.instance.SetTime(0,true);
            GameManager.instance.shad.ToggleShader(2); CurUIState = true;
        }
    }
}
