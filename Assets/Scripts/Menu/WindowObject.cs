using UnityEngine;

public class WindowObject : MonoBehaviour
{

    private void Start()
    {
        gameObject.SetActive(false);
    }
    bool IFirst = true;
    private void OnEnable()
    {
        if (IFirst) { IFirst = false; return; }
        GameManager.instance.Setting.AddWindow(gameObject);   
    }
}
