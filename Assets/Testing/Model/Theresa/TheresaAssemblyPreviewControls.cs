using UnityEditor;
using UnityEngine;

// Only attached in the dedicated preview scene.
[CustomEditor(typeof(Theresia))]
public sealed class TheresaAssemblyPreviewControls : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 
        var fx = (Theresia)target;

        if (GUILayout.Button("Appear")) fx.Appear();
        if (GUILayout.Button("Disappear")) fx.DisApeear();
    }
}
