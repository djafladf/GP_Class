using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(TheresaFragmentAssembly))]
public class TheresaFragmentAssemblyInspector : Editor {
 public override void OnInspectorGUI(){DrawDefaultInspector();var fx=(TheresaFragmentAssembly)target;EditorGUILayout.HelpBox("Direction 3D: Assembly Direction controls both reveal order and particle travel, independently of the camera. Direction Space selects character-local or world axes. Cell Size 3D uses character-local units. Reverse Direction flips the sweep. Screen Horizontal retains the previous camera-based mode.",MessageType.Info);using(new EditorGUI.DisabledScope(!Application.isPlaying)){if(GUILayout.Button("Assemble"))fx.Assemble();if(GUILayout.Button("Disassemble"))fx.Disassemble();}}
}
