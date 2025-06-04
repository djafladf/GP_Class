using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorSub : MonoBehaviour
{
    private void Update()
    {
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
}
