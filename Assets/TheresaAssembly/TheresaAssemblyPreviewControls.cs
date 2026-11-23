using UnityEngine;

// Only attached in the dedicated preview scene.
[DefaultExecutionOrder(-50)]
public sealed class TheresaAssemblyPreviewControls : MonoBehaviour {
 public TheresaFragmentAssembly effect;
 Renderer[] parts;
 void Awake(){parts=effect.GetComponentsInChildren<Renderer>();}
 //void LateUpdate(){
 //  if(parts.Length==0)return;var b=parts[0].bounds;foreach(var r in parts)b.Encapsulate(r.bounds);
 //  var cam=effect.effectCamera;cam.transform.position=b.center+new Vector3(0,0,5);cam.transform.LookAt(b.center);cam.orthographicSize=b.size.y*.72f;
 //}
 void OnGUI(){
   GUILayout.BeginArea(new Rect(16,16,230,180),GUI.skin.box);
   GUILayout.Label("THERESA / FRAGMENT ASSEMBLY");
   effect.rightToLeft=GUILayout.Toggle(effect.rightToLeft,effect.assemblyMode==TheresaFragmentAssembly.AssemblyMode.Direction3D?"Reverse 3D direction":"Right to left");
   if(GUILayout.Button("Assemble"))effect.Assemble();
   if(GUILayout.Button("Disassemble"))effect.Disassemble();
   GUILayout.Label("Progress: "+effect.Progress.ToString("0.00"));
   GUILayout.EndArea();
 }
}
