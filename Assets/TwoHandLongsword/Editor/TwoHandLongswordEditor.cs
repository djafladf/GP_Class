using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace TwoHandLongsword.Editor
{
    [CustomEditor(typeof(TwoHandLongswordSetup))]
    public sealed class TwoHandLongswordEditor : UnityEditor.Editor
    {
        TwoHandLongswordSetup S => (TwoHandLongswordSetup)target;
        GameObject preview;
        RigBuilder previewBuilder;
        Vector3[] positions, scales;
        Quaternion[] rotations;
        Transform[] transforms;
        string message;

        [MenuItem("Tools/Two Hand Longsword/Create Working Copy From Selected Animator")]
        static void CreateCopy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
                throw new InvalidOperationException("Exit Play/Animation mode first.");
            var source = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Animator>() : null;
            if (!source) { EditorUtility.DisplayDialog("Two Hand Longsword", "Select the GameObject with the character Animator.", "OK"); return; }
            var copy = Instantiate(source.gameObject, source.transform.parent);
            copy.name = source.name + "_TwoHandWorkingCopy";
            Undo.RegisterCreatedObjectUndo(copy, "Create two hand working copy");
            // Preserve Animator, constraints and renderer components. Disable game logic on this copy.
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true))
                if (b && !(b is RigBuilder) && !(b is Rig) && !(b is IRigConstraint)) b.enabled = false;
            foreach (var r in copy.GetComponentsInChildren<Rig>(true)) r.weight = 0;
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            var s = copy.GetComponent<TwoHandLongswordSetup>() ?? Undo.AddComponent<TwoHandLongswordSetup>(copy);
            s.enabled = true;
            s.animator = copy.GetComponent<Animator>();
            s.referenceIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Testing/Anim/RigTest/TH_Idle.anim");
            // Exact path is important: TH_Idle animates this Transform.
            s.sword = copy.transform.Find("Weapon");
            Selection.activeGameObject = copy;
            EditorSceneManager.MarkSceneDirty(copy.scene);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Work on the generated copy. TH_Idle is never edited. For a weapon-only TH_Idle, assign the intended body Idle clip or explicitly confirm the current arm pose. Capture once; adjust offsets afterwards.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || preview))
            {
                if (GUILayout.Button("1. Capture TH_Idle and Build Controls")) Run(Capture);
                if (GUILayout.Button("2. Apply Inspector Offsets")) Run(Apply);
                if (GUILayout.Button("3. Start Edit Mode IK Preview (duplicate)")) Run(StartPreview);
            }
            if (preview)
            {
                EditorGUILayout.HelpBox("Orange selected object is a temporary preview. Change offsets on this Inspector, then refresh. Stop preview before saving/applying.", MessageType.Info);
                if (GUILayout.Button("Refresh Preview With Offsets")) Run(RefreshPreview);
                if (GUILayout.Button("Stop Preview")) StopPreview();
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        void Run(Action action)
        {
            try { action(); message = ""; }
            catch (Exception e) { StopPreview(); message = e.Message; Debug.LogException(e, S); }
        }

        void Validate()
        {
            if (EditorUtility.IsPersistent(S) || !S.gameObject.scene.IsValid()) throw new InvalidOperationException("Use a scene working copy, not a prefab asset.");
            if (!S.name.EndsWith("_TwoHandWorkingCopy", StringComparison.Ordinal)) throw new InvalidOperationException("Create a working copy from Tools first; source objects are protected.");
            if (!S.animator || S.animator.gameObject != S.gameObject || !S.referenceIdle || !S.sword)
                throw new InvalidOperationException("Assign the Animator on this object, TH_Idle, and the actual animated Weapon Transform.");
            if (!S.sword.IsChildOf(S.transform) || S.sword == S.transform) throw new InvalidOperationException("Weapon must be below this Animator.");
            if (S.sword.GetComponentsInChildren<Animator>(true).Length != 0) throw new InvalidOperationException("Selected Weapon contains an Animator. Select the sword branch instead of the character root.");
            foreach (var t in S.GetComponentsInChildren<Transform>(true))
            {
                Vector3 a = t.lossyScale;
                if (a.x <= 0 || a.y <= 0 || a.z <= 0 || Mathf.Abs(a.x-a.y) > .001f || Mathf.Abs(a.y-a.z) > .001f)
                    throw new InvalidOperationException("Negative/nonuniform scale is unsupported: " + t.name + ". Normalize model scale first.");
            }
            if (!S.bodyIdle && !S.useCurrentBodyPose)
                throw new InvalidOperationException("Assign the intended body Idle clip. TH_Idle in this project only animates Weapon; it cannot supply the arm pose.");
            if (S.bladeDirectionLocal.sqrMagnitude < .001f)
                throw new InvalidOperationException("Confirm Blade Direction Local against the actual sword mesh (pommel toward blade tip). Pivot axes alone cannot establish upper/lower grip order.");
            string swordPath = AnimationUtility.CalculateTransformPath(S.sword, S.transform);
            if (!AnimationUtility.GetCurveBindings(S.referenceIdle).Any(b => b.path == swordPath && b.type == typeof(Transform)))
                throw new InvalidOperationException("TH_Idle has no Transform curves for " + swordPath + ". Select the animated Weapon pivot before capturing.");
        }

        Transform Bone(HumanBodyBones bone)
        {
            if (!S.animator.isHuman || !S.animator.avatar || !S.animator.avatar.isValid)
                throw new InvalidOperationException("This build requires a valid Humanoid Avatar. Generic rigs need explicit bone mapping.");
            return S.animator.GetBoneTransform(bone) ?? throw new InvalidOperationException("Missing bone: " + bone);
        }

        void Capture()
        {
            Validate();
            if (S.captured) throw new InvalidOperationException("Already captured. Preserve this baseline; create another working copy to recapture.");
            var ru = Bone(HumanBodyBones.RightUpperArm); var rm = Bone(HumanBodyBones.RightLowerArm); var rh = Bone(HumanBodyBones.RightHand);
            var lu = Bone(HumanBodyBones.LeftUpperArm); var lm = Bone(HumanBodyBones.LeftLowerArm); var lh = Bone(HumanBodyBones.LeftHand);
            if (S.sword.IsChildOf(rh) || S.sword.IsChildOf(lh))
                throw new InvalidOperationException("Weapon is hand-parented. Preserve that source; use a character-root Weapon pivot matching TH_Idle on the working copy.");
            // Sampling is restricted to a disposable clone, so source/working pose and clip bindings remain intact.
            var sample = Instantiate(S.gameObject, S.transform.parent);
            sample.name = "TH_ReferenceSample";
            sample.hideFlags = HideFlags.HideAndDontSave;
            Vector3 swordWorld, midpoint, rp, lp, rHint, lHint;
            Quaternion swordWorldRotation, rr, lr;
            try
            {
                foreach (var b in sample.GetComponentsInChildren<MonoBehaviour>(true)) if (b) b.enabled = false;
                var a = sample.GetComponent<Animator>();
                a.runtimeAnimatorController = null;
                if (S.bodyIdle) S.bodyIdle.SampleAnimation(sample, Mathf.Clamp(S.bodyTime, 0, S.bodyIdle.length));
                sample.transform.SetPositionAndRotation(S.transform.position, S.transform.rotation);
                S.referenceIdle.SampleAnimation(sample, Mathf.Clamp(S.referenceTime, 0, S.referenceIdle.length));
                Transform W = sample.transform.Find(AnimationUtility.CalculateTransformPath(S.sword, S.transform));
                Transform R = a.GetBoneTransform(HumanBodyBones.RightHand), L = a.GetBoneTransform(HumanBodyBones.LeftHand);
                swordWorld = W.position; swordWorldRotation = W.rotation;
                midpoint = (R.position + L.position) * .5f;
                rp = W.InverseTransformPoint(R.position); lp = W.InverseTransformPoint(L.position);
                if (Vector3.Dot(rp - lp, S.bladeDirectionLocal.normalized) <= .005f)
                    throw new InvalidOperationException("Right hand is not above left hand along the blade direction in this reference. Check the body Idle and blade axis; the tool will not silently swap your reference hands.");
                rr = Quaternion.Inverse(W.rotation) * R.rotation; lr = Quaternion.Inverse(W.rotation) * L.rotation;
                rHint = Hint(a.GetBoneTransform(HumanBodyBones.RightUpperArm), a.GetBoneTransform(HumanBodyBones.RightLowerArm), R, sample.transform.right, S.hintDistanceInArmLengths);
                lHint = Hint(a.GetBoneTransform(HumanBodyBones.LeftUpperArm), a.GetBoneTransform(HumanBodyBones.LeftLowerArm), L, -sample.transform.right, S.hintDistanceInArmLengths);
                float gap = Vector3.Distance(R.position, L.position);
                if (gap < .015f) throw new InvalidOperationException("Hand centers nearly coincide. Choose the intended two-hand body pose before capturing.");
                if (Vector3.Dot(midpoint - sample.transform.position, sample.transform.forward) <= 0)
                    throw new InvalidOperationException("Reference hand midpoint is behind the character. Check the reference pose and character forward axis.");
            }
            finally { DestroyImmediate(sample); }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Build two hand controls");
            try
            {
                Undo.RecordObject(S, "Capture reference");
                var builder = S.GetComponent<RigBuilder>() ?? Undo.AddComponent<RigBuilder>(S.gameObject);
                Undo.RecordObject(builder, "Add rig layer");
                // Keep old Rig objects as reference, but build only this dedicated layer.
                // Inactive layers may still initialize jobs in RigBuilder.
                builder.layers.Clear();
                S.weaponControl = Child("WeaponControl", S.transform);
                S.weaponControl.SetPositionAndRotation(midpoint, S.transform.rotation);
                // Keep original Weapon (and its TH_Idle binding) as an empty animated pivot.
                // A new visual branch holds copied renderers/mesh and receives IK targets.
                var visual = Instantiate(S.sword.gameObject, S.weaponControl);
                visual.name = "Sword";
                Undo.RegisterCreatedObjectUndo(visual, "Create independent sword");
                foreach (var b in visual.GetComponentsInChildren<MonoBehaviour>(true)) if (b) b.enabled = false;
                foreach (var r in S.sword.GetComponentsInChildren<Renderer>(true)) { Undo.RecordObject(r, "Hide source weapon visual on copy"); r.enabled = false; }
                visual.transform.SetPositionAndRotation(swordWorld, swordWorldRotation);
                S.sword = visual.transform;
                S.rightGrip = Child("RightGripTarget", S.sword); S.leftGrip = Child("LeftGripTarget", S.sword);
                S.rightHint = Child("RightElbowHint", S.transform); S.leftHint = Child("LeftElbowHint", S.transform);
                S.rightHint.position = rHint; S.leftHint.position = lHint;
                var rigTransform = Child("TwoHandLocomotionRig", S.transform);
                S.locomotionRig = Undo.AddComponent<Rig>(rigTransform.gameObject);
                S.locomotionRig.weight = 0; // Runtime opt-in; preview explicitly sets it to one.
                S.rightIK = MakeIK("RightArmIK", rigTransform, ru, rm, rh, S.rightGrip, S.rightHint);
                S.leftIK = MakeIK("LeftArmIK", rigTransform, lu, lm, lh, S.leftGrip, S.leftHint);
                builder.layers.Add(new RigLayer(S.locomotionRig));
                S.midpointLocal = S.weaponControl.localPosition;
                S.swordPositionFromMidpoint = S.sword.localPosition; S.swordRotationLocal = S.sword.localRotation;
                S.rightPosition = rp; S.leftPosition = lp; S.rightRotation = rr; S.leftRotation = lr;
                S.rightHintLocal = S.rightHint.localPosition; S.leftHintLocal = S.leftHint.localPosition;
                S.captured = true;
                S.ApplyOffsets();
                EditorUtility.SetDirty(S); EditorUtility.SetDirty(builder);
                EditorSceneManager.MarkSceneDirty(S.gameObject.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        internal static Vector3 Hint(Transform shoulder, Transform elbow, Transform wrist, Vector3 fallback, float distance)
        {
            Vector3 axis = wrist.position - shoulder.position;
            Vector3 normal = axis.sqrMagnitude > 1e-10f ? axis.normalized : Vector3.forward;
            Vector3 bend = Vector3.ProjectOnPlane(elbow.position - shoulder.position, normal);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.ProjectOnPlane(fallback, normal);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.Cross(normal, Vector3.up);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.Cross(normal, Vector3.right);
            float length = Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, wrist.position);
            return elbow.position + bend.normalized * Mathf.Max(.02f, length * distance);
        }

        static Transform Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static TwoBoneIKConstraint MakeIK(string name, Transform parent, Transform root, Transform mid, Transform tip, Transform target, Transform hint)
        {
            var ik = Undo.AddComponent<TwoBoneIKConstraint>(Child(name, parent).gameObject);
            var d = ik.data;
            d.root = root; d.mid = mid; d.tip = tip; d.target = target; d.hint = hint;
            d.targetPositionWeight = 1; d.targetRotationWeight = 1; d.hintWeight = 1;
            d.maintainTargetPositionOffset = false; d.maintainTargetRotationOffset = false;
            ik.data = d; ik.weight = 1;
            return ik;
        }

        void Apply()
        {
            if (!S.captured) throw new InvalidOperationException("Capture first.");
            Undo.RecordObjects(new Object[] { S.weaponControl, S.sword, S.rightGrip, S.leftGrip, S.rightHint, S.leftHint }, "Adjust two hand offsets");
            S.ApplyOffsets(); EditorSceneManager.MarkSceneDirty(S.gameObject.scene);
        }

        void StartPreview()
        {
            if (!S.captured) throw new InvalidOperationException("Capture first.");
            if (AnimationMode.InAnimationMode()) throw new InvalidOperationException("Stop the Animation window preview first.");
            preview = Instantiate(S.gameObject, S.transform.parent);
            preview.name = "TH_IK_PREVIEW (temporary)";
            preview.hideFlags = HideFlags.HideAndDontSave;
            foreach (var b in preview.GetComponentsInChildren<MonoBehaviour>(true))
                if (b && !(b is Rig) && !(b is IRigConstraint)) b.enabled = false;
            var ps = preview.GetComponent<TwoHandLongswordSetup>();
            var a = preview.GetComponent<Animator>();
            a.runtimeAnimatorController = null;
            AnimationClip body = S.previewBody ? S.previewBody : S.bodyIdle;
            float time = S.previewBody ? S.previewTime : S.bodyTime;
            if (body) body.SampleAnimation(preview, Mathf.Clamp(time, 0, body.length));
            preview.transform.SetPositionAndRotation(S.transform.position, S.transform.rotation);
            // Only sample Weapon curves here: the captured independent Sword must not be overwritten.
            S.referenceIdle.SampleAnimation(preview, Mathf.Clamp(S.referenceTime, 0, S.referenceIdle.length));
            preview.transform.position += S.transform.right * 2;
            foreach (var r in preview.GetComponentsInChildren<Rig>(true)) r.weight = r == ps.locomotionRig ? 1 : 0;
            transforms = preview.GetComponentsInChildren<Transform>(true);
            positions = transforms.Select(t => t.localPosition).ToArray();
            rotations = transforms.Select(t => t.localRotation).ToArray();
            scales = transforms.Select(t => t.localScale).ToArray();
            previewBuilder = preview.GetComponent<RigBuilder>();
            if (!previewBuilder.Build()) throw new InvalidOperationException("RigBuilder.Build failed; inspect Avatar and bone chains.");
            previewBuilder.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            RefreshPreview();
            SceneView.FrameLastActiveSceneView();
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.quitting += StopPreview;
        }

        void RefreshPreview()
        {
            if (!preview) return;
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
                transforms[i].localScale = scales[i];
            }
            var p = preview.GetComponent<TwoHandLongswordSetup>();
            p.weaponPositionOffset = S.weaponPositionOffset; p.weaponRotationOffset = S.weaponRotationOffset;
            p.rightPositionOffset = S.rightPositionOffset; p.rightRotationOffset = S.rightRotationOffset;
            p.leftPositionOffset = S.leftPositionOffset; p.leftRotationOffset = S.leftRotationOffset;
            p.rightHintOffset = S.rightHintOffset; p.leftHintOffset = S.leftHintOffset;
            p.ApplyOffsets();
            previewBuilder.Evaluate(0);
            SceneView.RepaintAll();
        }

        void OnPlayMode(PlayModeStateChange state) => StopPreview();
        void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path) => StopPreview();
        void OnDisable() => StopPreview();
        void StopPreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorApplication.quitting -= StopPreview;
            if (previewBuilder) previewBuilder.Clear();
            if (preview) DestroyImmediate(preview);
            preview = null; previewBuilder = null;
            SceneView.RepaintAll();
        }
    }
}
