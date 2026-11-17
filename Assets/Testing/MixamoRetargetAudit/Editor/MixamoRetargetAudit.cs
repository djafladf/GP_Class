using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MixamoRetargetAudit
{
    const string Dir = "Assets/Testing/MixamoRetargetAudit";
    const string Out = "C:/Users/m/Documents/Codex/2026-11-17/referenced-chatgpt-conversation-this-is-an-2/outputs";
    const string Source = "Assets/Testing/Ch36_nonPBR@Standing Up.fbx";
    const string Chan = "Assets/unity-chan!/Unity-chan! Model/Art/Models/unitychan.fbx";
    const string Am = "Assets/Testing/Model/amiya/am3_RightArm.fbx";
    static StringBuilder report;
    static void Note(string s) { report.AppendLine(s); File.WriteAllText(Out + "/unity-audit.txt", report.ToString()); }
    static Avatar AvatarAt(string p) { return AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault(); }
    static string Copy(string src, string name) { string p = Dir + "/" + name + ".fbx"; if (!File.Exists(p) && !AssetDatabase.CopyAsset(src, p)) throw new Exception("Copy failed " + src); AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport); return p; }
    static void Rig(string p) {
        var mi=(ModelImporter)AssetImporter.GetAtPath(p); var a=AvatarAt(p);
        Note("\nRIG " + p + " type="+mi.animationType+" definition="+mi.avatarSetup+" source="+(mi.sourceAvatar?AssetDatabase.GetAssetPath(mi.sourceAvatar):"none")+" avatar="+(a?a.name:"none")+" valid="+(a&&a.isValid)+" human="+(a&&a.isHuman));
        var go=AssetDatabase.LoadAssetAtPath<GameObject>(p); var names=go.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray();
        Note("Hierarchy: " + string.Join(",",names));
        if(a && a.isHuman) foreach(var h in a.humanDescription.human) Note("MAP "+h.humanName+"="+h.boneName+" exists="+names.Contains(h.boneName));
        foreach(var c in mi.clipAnimations.Length>0?mi.clipAnimations:mi.defaultClipAnimations) Note("CLIP "+c.name+" frames="+c.firstFrame+".."+c.lastFrame+" loop="+c.loopTime+" loopPose="+c.loopPose+" bakeRotation="+c.lockRootRotation+" originalRotation="+c.keepOriginalOrientation+" bakeY="+c.lockRootHeightY+" originalY="+c.keepOriginalPositionY+" feetY="+c.heightFromFeet+" bakeXZ="+c.lockRootPositionXZ+" originalXZ="+c.keepOriginalPositionXZ);
    }
    [MenuItem("Tools/Mixamo Retarget Audit/Run isolated verification")]
    public static void Run() {
        if(DateTime.UtcNow >= new DateTime(2026,11,17,9,58,30,DateTimeKind.Utc)) return;
        Directory.CreateDirectory(Out); report=new StringBuilder();
        Note("Unity "+Application.unityVersion+" UTC "+DateTime.UtcNow.ToString("O")+"\nOriginal assets are read-only. Tests use copied models and a separate scene.");
        try {
            Rig(Source); Rig(Chan); Rig(Am);
            foreach(var an in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Note("OPEN SCENE ANIMATOR "+an.name+" avatar="+(an.avatar?AssetDatabase.GetAssetPath(an.avatar):"none")+" valid="+(an.avatar&&an.avatar.isValid)+" human="+an.isHuman+" rootMotion="+an.applyRootMotion+" controller="+(an.runtimeAnimatorController?AssetDatabase.GetAssetPath(an.runtimeAnimatorController):"none"));
            string chan=Copy(Chan,"UnityChan_Test"), fixedPath=Copy(Source,"StandingUp_Fixed");
            // Reproduce the incompatible-copy configuration, capture the error, then leave the reproduction disabled.
            string bad=Copy(Source,"StandingUp_Mismatch_Repro"); var bi=(ModelImporter)AssetImporter.GetAtPath(bad);
            string errors=""; Application.LogCallback callback=(msg,stack,type)=>{ if(type==LogType.Error || type==LogType.Warning) errors+=type+": "+msg+"\n"; };
            Application.logMessageReceived+=callback;
            try {bi.animationType=ModelImporterAnimationType.Human;bi.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;bi.sourceAvatar=AvatarAt(Am);bi.SaveAndReimport();} finally {Application.logMessageReceived-=callback;}
            Note("\nREPRO ERRORS\n"+errors); File.WriteAllText(Out+"/mismatch-reproduction.txt",errors);
            bi=(ModelImporter)AssetImporter.GetAtPath(bad);bi.animationType=ModelImporterAnimationType.Generic;bi.avatarSetup=ModelImporterAvatarSetup.NoAvatar;bi.sourceAvatar=null;bi.SaveAndReimport();
            var fi=(ModelImporter)AssetImporter.GetAtPath(fixedPath);fi.animationType=ModelImporterAnimationType.Human;fi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;fi.sourceAvatar=null;fi.autoGenerateAvatarMappingIfUnspecified=true;fi.SaveAndReimport();
            // Preserve the authored vertical origin; Feet plus baked Y shifts this source's toes below ground.
            fi=(ModelImporter)AssetImporter.GetAtPath(fixedPath);var clips=fi.clipAnimations.Length>0?fi.clipAnimations:fi.defaultClipAnimations;
            foreach(var c in clips){c.name="Standing Up";c.loopTime=false;c.loopPose=false;c.lockRootRotation=true;c.keepOriginalOrientation=true;c.lockRootHeightY=true;c.keepOriginalPositionY=true;c.heightFromFeet=false;c.lockRootPositionXZ=true;}
            fi.clipAnimations=clips;fi.SaveAndReimport();Rig(fixedPath);
            var clip=AssetDatabase.LoadAllAssetsAtPath(fixedPath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            Note("FIXED CLIP humanMotion="+clip.humanMotion+" length="+clip.length+" fps="+clip.frameRate);
            string ctrlPath=Dir+"/StandingUp_Test.controller";var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if(!ctrl){ctrl=AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);var state=ctrl.layers[0].stateMachine.AddState("Standing Up (one shot)");state.motion=clip;ctrl.layers[0].stateMachine.defaultState=state;}
            ctrl.layers[0].stateMachine.defaultState.motion=clip;
            var old=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Ground y=0";floor.transform.localScale=Vector3.one;
                string[] paths={fixedPath,chan};
                for(int k=0;k<paths.Length;k++){
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(paths[k]),scene);go.name=new[]{"Mixamo_Source","UnityChan_Retarget","am3_Retarget"}[k];go.transform.position=new Vector3((k-1)*2.2f,0,0);
                    if(k==2)go.transform.localScale=Vector3.one*.01f;
                    var animator=go.GetComponent<Animator>(); if(!animator)animator=go.AddComponent<Animator>();animator.avatar=AvatarAt(paths[k]);animator.runtimeAnimatorController=ctrl;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))if(script)script.enabled=false;
                    Sample(go,clip,k);
                    PrefabUtility.SaveAsPrefabAsset(go,Dir+"/"+go.name+".prefab");
                }
                var camera=new GameObject("Test Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2.2f,-7);camera.transform.LookAt(new Vector3(0,0.9f,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.15f,.19f);camera.gameObject.tag="MainCamera";
                var light=new GameObject("Test Light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-25,0);
                EditorSceneManager.SaveScene(scene,Dir+"/StandingUp_Retarget_Test.unity");
            } finally {SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Note("\nAUDIT COMPLETE UTC "+DateTime.UtcNow.ToString("O"));
        }catch(Exception e){Note("FAILED "+e);Debug.LogException(e);}
    }
    static void Sample(GameObject go,AnimationClip clip,int index){
        var a=go.GetComponent<Animator>();Note("\nSAMPLE "+go.name+" ownAvatar="+AssetDatabase.GetAssetPath(a.avatar));
        if(!a.avatar || !a.avatar.isValid || !a.avatar.isHuman) throw new Exception("Invalid target avatar "+go.name);
        var graph=PlayableGraph.Create("Mixamo Audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(playable);graph.Play();
        var csv=new StringBuilder("time,bone,x,y,z,qx,qy,qz,qw\n");
        try{
            for(int n=0;n<=20;n++){
                float t=clip.length*n/20f;playable.SetTime(t);graph.Evaluate(0);
                foreach(HumanBodyBones b in Enum.GetValues(typeof(HumanBodyBones))){if(b==HumanBodyBones.LastBone)continue;var tr=a.GetBoneTransform(b);if(!tr)continue;var v=go.transform.InverseTransformPoint(tr.position);var q=tr.localRotation;csv.AppendLine(FormattableString.Invariant($"{t},{b},{v.x},{v.y},{v.z},{q.x},{q.y},{q.z},{q.w}"));}
                var hip=a.GetBoneTransform(HumanBodyBones.Hips);var lf=a.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=a.GetBoneTransform(HumanBodyBones.RightFoot);
                Note("t="+t.ToString("F3")+" hips="+go.transform.InverseTransformPoint(hip.position).ToString("F4")+" feetY="+(lf.position.y-go.transform.position.y).ToString("F4")+","+(rf.position.y-go.transform.position.y).ToString("F4"));
            }
            playable.SetTime(0);graph.Evaluate(0);
        }finally{graph.Destroy();}
        File.WriteAllText(Out+"/pose-"+index+".csv",csv.ToString());
    }
}
