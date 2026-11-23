using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class TheresaAssemblyTools {
 const string Dir="Assets/TheresaAssembly";
 [MenuItem("Tools/Theresa Assembly/Open Preview Scene")]
 public static void OpenPreview(){ if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Dir+"/TheresaAssemblyPreview.unity"); }
 static GameObject Target(){return Resources.FindObjectsOfTypeAll<GameObject>().First(g=>g.scene.IsValid() && g.name=="Theresa-Humanoid");}
 [MenuItem("Tools/Theresa Assembly/Apply to Theresa in Current Scene")]
 public static void Apply(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
   var shader=Shader.Find("Toon/Theresa Fragment Assembly");if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Assembly shader has errors.");
   var target=Target();var renderers=target.GetComponentsInChildren<Renderer>(true);
   foreach(var r in renderers){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
       if(!mats[i] || mats[i].shader==shader)continue;
       if(mats[i].shader.name!="Toon/Toon 3D as 2D (URP)")throw new Exception("Unexpected material: "+mats[i].name);
       string path=Dir+"/"+mats[i].name+"_Assembly_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mats[i]))+".mat";
       var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(mats[i]);mat.shader=shader;AssetDatabase.CreateAsset(mat,path);}mats[i]=mat;
   }Undo.RecordObject(r,"Apply fragment assembly material");r.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
   var fx=target.GetComponent<TheresaFragmentAssembly>();if(!fx)fx=Undo.AddComponent<TheresaFragmentAssembly>(target);
   fx.fragmentVfxAsset=AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(Dir+"/Resources/Theresa/NormalParticleAssembly.vfx");
   fx.effectCamera=Camera.main;EditorUtility.SetDirty(fx);PrefabUtility.RecordPrefabInstancePropertyModifications(fx);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(target.scene);EditorSceneManager.SaveScene(target.scene);
   File.WriteAllText("Temp/TheresaApplied.txt",string.Join("\n",renderers.Select(r=>r.name+": "+string.Join(",",r.sharedMaterials.Select(m=>m.shader.name)))));
 }
 [MenuItem("Tools/Theresa Assembly/Create Preview Scene")]
 public static void Preview(){
   if(SceneManager.GetActiveScene().path==Dir+"/TheresaAssemblyPreview.unity")return;
   var source=Target();var clone=UnityEngine.Object.Instantiate(source);clone.name="Theresa-Humanoid";
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.MoveGameObjectToScene(clone,scene);SceneManager.SetActiveScene(scene);clone.transform.position=Vector3.zero;
   foreach(var mb in clone.GetComponentsInChildren<MonoBehaviour>(true))if(!(mb is TheresaFragmentAssembly))mb.enabled=false;
   Bounds b=clone.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in clone.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
   var cg=new GameObject("Assembly Preview Camera");SceneManager.MoveGameObjectToScene(cg,scene);cg.tag="MainCamera";var cam=cg.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.045f,.055f,.08f);cam.orthographic=true;cam.orthographicSize=b.size.y*.72f;cam.nearClipPlane=.01f;cam.farClipPlane=100;
   cg.transform.position=b.center+new Vector3(0,0,5);cg.transform.LookAt(b.center);
   var fx=clone.GetComponent<TheresaFragmentAssembly>();fx.effectCamera=cam;fx.playOnStart=true;fx.duration=3;
   clone.AddComponent<TheresaAssemblyPreviewControls>().effect=fx;
   var lg=new GameObject("Preview Light");SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;lg.transform.rotation=Quaternion.Euler(35,-30,0);
   EditorSceneManager.SaveScene(scene,Dir+"/TheresaAssemblyPreview.unity");EditorSceneManager.OpenScene(Dir+"/TheresaAssemblyPreview.unity");Selection.activeGameObject=Target();
 }
}

[CustomEditor(typeof(TheresaFragmentAssembly))]
public class TheresaFragmentAssemblyInspector : Editor {
 public override void OnInspectorGUI(){DrawDefaultInspector();var fx=(TheresaFragmentAssembly)target;EditorGUILayout.HelpBox("Direction 3D: Assembly Direction controls both reveal order and particle travel, independently of the camera. Direction Space selects character-local or world axes. Cell Size 3D uses character-local units. Reverse Direction flips the sweep. Screen Horizontal retains the previous camera-based mode.",MessageType.Info);using(new EditorGUI.DisabledScope(!Application.isPlaying)){if(GUILayout.Button("Assemble"))fx.Assemble();if(GUILayout.Button("Disassemble"))fx.Disassemble();}}
}
