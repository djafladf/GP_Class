// Unity 6.3 LTS. Editor-only; no Animation Rigging dependency.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace THWeaponProjection
{
    public sealed class THWeaponProjectionBaker : EditorWindow
    {
        [SerializeField] Animator character;
        [SerializeField] AnimationClip referenceIdle, bodyClip, referenceBody;
        [SerializeField] Transform spine, rightGrip, leftGrip, weapon, handleCenter;
        [SerializeField] Vector3 rotationOffset;
        [SerializeField] bool averageReference = true, closeWeaponLoop;
        [SerializeField] float referenceTime, referenceBodyTime, minGripDistance = .005f;
        [SerializeField] int fps = 30;
        [SerializeField] float FarFromSpine = 1.25f;
        string status;

        [MenuItem("Tools/Two Hand Longsword/Projection Clip Baker")]
        static void Open() => GetWindow<THWeaponProjectionBaker>("TH Weapon Baker");
        T Field<T>(string label, T value) where T : Object =>
            (T)EditorGUILayout.ObjectField(label, value, typeof(T), true);
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Bake weapon curves from sampled BODY grips. Scene and source clips stay untouched. No arm IK is baked.", MessageType.Info);
            character = Field("Character Animator", character);
            referenceIdle = Field("TH_Idle reference", referenceIdle);
            bodyClip = Field("Source body clip", bodyClip);
            referenceBody = Field("Reference body (optional)", referenceBody);
            referenceBodyTime = EditorGUILayout.FloatField("Reference body time", referenceBodyTime);
            spine = Field("Spine / abdomen", spine);
            rightGrip = Field("Right Hand child grip", rightGrip);
            leftGrip = Field("Left Hand child grip", leftGrip);
            weapon = Field("Animated Weapon pivot", weapon);
            handleCenter = Field("Handle center (weapon child)", handleCenter);
            if (GUILayout.Button("Find Spine + Hand Child(0)")) AutoFill();
            FarFromSpine = EditorGUILayout.FloatField("Weapon Forward Distance", FarFromSpine);
            rotationOffset = EditorGUILayout.Vector3Field("Weapon local rotation offset", rotationOffset);
            averageReference = EditorGUILayout.Toggle("Average TH_Idle frames", averageReference);
            referenceTime = EditorGUILayout.FloatField("Reference time (single mode)", referenceTime);
            fps = EditorGUILayout.IntSlider("Bake samples / second", fps, 1, 240);
            minGripDistance = EditorGUILayout.FloatField("Degenerate grip distance", minGripDistance);
            closeWeaponLoop = EditorGUILayout.Toggle("Force weapon loop endpoints", closeWeaponLoop);
            EditorGUILayout.HelpBox("Default projection is exact (no position offset). Reference body supplies the hand axis if TH_Idle only animates Weapon; otherwise source clip at time 0 is used. Loop endpoint forcing may alter the final projected position.", MessageType.None);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Bake NEW TH_<source>.anim"))
                    try { Bake(); } catch (Exception e) { status = e.Message; Debug.LogException(e); }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        }

        void AutoFill()
        {
            if (!character || !character.isHuman) return;
            spine = character.GetBoneTransform(HumanBodyBones.Spine);
            var r = character.GetBoneTransform(HumanBodyBones.RightHand);
            var l = character.GetBoneTransform(HumanBodyBones.LeftHand);
            if (r && r.childCount > 0) rightGrip = r.GetChild(0);
            if (l && l.childCount > 0) leftGrip = l.GetChild(0);
        }
        string PathOf(Transform t)
        {
            if (!t || t == character.transform || !t.IsChildOf(character.transform))
                throw new InvalidOperationException("All selected transforms must be descendants of the selected Animator.");
            // Find(path) cannot disambiguate duplicate sibling names or slashes.
            for (var n = t; n != character.transform; n = n.parent)
                if (n.name.Contains("/") || n.parent.Cast<Transform>().Count(x => x.name == n.name) != 1)
                    throw new InvalidOperationException("Ambiguous animation path at " + n.name + ". Use unique sibling names without '/'.");
            return AnimationUtility.CalculateTransformPath(t, character.transform);
        }
        void Validate()
        {
            if (!character || !character.avatar || !character.avatar.isValid || !character.isHuman)
                throw new InvalidOperationException("Assign a valid Humanoid Animator with its Avatar.");
            if (!referenceIdle || !bodyClip || bodyClip.length <= 0 || bodyClip.legacy)
                throw new InvalidOperationException("Assign TH_Idle and a non-Legacy body clip with positive duration.");
            if (!bodyClip.humanMotion) throw new InvalidOperationException("Source body clip must contain Humanoid motion.");
            if (referenceIdle.legacy || (referenceBody && (referenceBody.legacy || !referenceBody.humanMotion)))
                throw new InvalidOperationException("Reference clips must be non-Legacy; reference body must be Humanoid.");
            if (!spine || !rightGrip || !leftGrip || !weapon || !handleCenter)
                throw new InvalidOperationException("Assign every Transform field.");
            PathOf(spine); PathOf(rightGrip); PathOf(leftGrip); var wp = PathOf(weapon); PathOf(handleCenter);
            var rh = character.GetBoneTransform(HumanBodyBones.RightHand);
            var lh = character.GetBoneTransform(HumanBodyBones.LeftHand);
            if (!rightGrip.IsChildOf(rh) || !leftGrip.IsChildOf(lh) || rightGrip == leftGrip)
                throw new InvalidOperationException("Use actual grip markers below the respective Hand bones.");
            if (handleCenter != weapon && !handleCenter.IsChildOf(weapon))
                throw new InvalidOperationException("Handle center must be Weapon or a child of Weapon.");
            if (spine.IsChildOf(weapon) || rightGrip.IsChildOf(weapon) || leftGrip.IsChildOf(weapon))
                throw new InvalidOperationException("Weapon cannot be an ancestor of the body/grip inputs.");
            if (!AnimationUtility.GetCurveBindings(referenceIdle).Any(b => b.type == typeof(Transform) && b.path == wp))
                throw new InvalidOperationException("TH_Idle does not animate selected Weapon path: " + wp);
            // Uniform positive scales make quaternion orientation and pivot compensation well defined.
            var relevant = new System.Collections.Generic.HashSet<Transform>();
            foreach (var input in new[] { spine, rightGrip, leftGrip, weapon, handleCenter })
                for (var n = input; n; n = n.parent) relevant.Add(n);
            foreach (var t in relevant)
            {
                var s = t.localScale;
                if (s.x <= 0 || Mathf.Abs(s.x-s.y) > .0001f || Mathf.Abs(s.y-s.z) > .0001f)
                    throw new InvalidOperationException("Positive uniform scale required: " + t.name);
            }
            if (character.transform.parent)
            {
                var s = character.transform.parent.lossyScale;
                if (s.x <= 0 || Mathf.Abs(s.x-s.y) > .0001f || Mathf.Abs(s.y-s.z) > .0001f)
                    throw new InvalidOperationException("Character parent must have positive uniform scale.");
            }
        }

        void Bake()
        {
            Validate();
            string path = EditorUtility.SaveFilePanelInProject("Save baked clip", "TH_" + bodyClip.name, "anim", "Create a NEW clip; existing assets will never be overwritten.");
            if (string.IsNullOrEmpty(path)) return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            BakeToPath(path);
        }

        void BakeToPath(string path)
        {
            Validate();
            if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("Output already exists.");
            AnimationClip result = null;
            try
            {
                using (var sample = new Sampler(character))
                {
                    Transform s = sample.Find(PathOf(spine)), r = sample.Find(PathOf(rightGrip)), l = sample.Find(PathOf(leftGrip));
                    Transform w = sample.Find(PathOf(weapon)), h = sample.Find(PathOf(handleCenter));
                    Quaternion refRotation = Quaternion.identity, firstQ = Quaternion.identity;
                    Vector3 refAxis = Vector3.zero;
                    Vector4 sum = Vector4.zero;
                    int count = averageReference ? Mathf.Max(1, Mathf.CeilToInt(referenceIdle.length * fps)) : 1;
                    // Exclude duplicate loop endpoint from the mean.
                    for (int i = 0; i < count; i++)
                    {
                        sample.Pose(referenceBody ? referenceBody : bodyClip, Mathf.Clamp(referenceBodyTime, 0, (referenceBody ? referenceBody : bodyClip).length));
                        float t = averageReference ? referenceIdle.length * i / count : Mathf.Clamp(referenceTime, 0, referenceIdle.length);
                        if (referenceIdle.humanMotion) sample.OverlayHuman(referenceIdle, t);
                        else referenceIdle.SampleAnimation(sample.Root, t);
                        var q = Quaternion.Inverse(s.rotation) * w.rotation;
                        if (i == 0) firstQ = q;
                        if (Quaternion.Dot(firstQ, q) < 0) q = Negate(q);
                        sum += new Vector4(q.x, q.y, q.z, q.w);
                        Vector3 delta = r.position - l.position;
                        if (delta.magnitude < minGripDistance) throw new InvalidOperationException("Reference grips coincide; assign a suitable Reference Body pose.");
                        refAxis += Quaternion.Inverse(s.rotation) * delta.normalized;
                    }
                    if (sum.sqrMagnitude < 1e-8f || refAxis.sqrMagnitude < 1e-6f)
                        throw new InvalidOperationException("Reference average is ambiguous. Use a single reference time.");
                    refRotation = new Quaternion(sum.x, sum.y, sum.z, sum.w).normalized;
                    refAxis.Normalize();
                    int steps = Mathf.Max(1, Mathf.CeilToInt(bodyClip.length * fps));
                    var curves = Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray();
                    Vector3 firstPos = default;
                    Quaternion prevLocal = Quaternion.identity, firstLocal = Quaternion.identity, previousSwing = Quaternion.identity;
                    float maxError = 0;
                    for (int i = 0; i <= steps; i++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("TH weapon bake", bodyClip.name, (float)i/steps))
                            throw new OperationCanceledException("Bake cancelled; no asset created.");
                        float time = bodyClip.length * i / steps;
                        sample.Pose(bodyClip, time);
                        Vector3 localForward = spine.InverseTransformDirection(character.transform.forward);
                        Vector3 f = s.TransformDirection(localForward.normalized).normalized;
                        Vector3 target = s.position + f * FarFromSpine;
                        Quaternion worldRotation = s.rotation * refRotation * Quaternion.Euler(rotationOffset);
                        Vector3 handleOffset = w.InverseTransformPoint(h.position);
                        w.rotation = worldRotation;
                        w.position += target - w.TransformPoint(handleOffset);
                        Vector3 pos = w.localPosition;
                        Quaternion rot = w.localRotation.normalized;
                        if (i == 0) { firstPos = pos; firstLocal = rot; }
                        if (closeWeaponLoop && i == steps) { pos = firstPos; rot = firstLocal; }
                        if (i > 0 && Quaternion.Dot(prevLocal, rot) < 0) rot = Negate(rot);
                        prevLocal = rot;
                        float[] values = { pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w };
                        for (int j = 0; j < 7; j++) curves[j].AddKey(time, values[j]);
                        maxError = Mathf.Max(maxError, Vector3.Distance(h.position, target));
                    }
                    result = Object.Instantiate(bodyClip);
                    result.name = System.IO.Path.GetFileNameWithoutExtension(path);
                    result.hideFlags = HideFlags.None;
                    result.frameRate = fps;
                    string wp = PathOf(weapon);
                    foreach (var b in AnimationUtility.GetCurveBindings(result))
                        if (b.path == wp && b.type == typeof(Transform) && IsPositionOrRotation(b.propertyName))
                            AnimationUtility.SetEditorCurve(result, b, null);
                    string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
                    for (int j = 0; j < 7; j++)
                    {
                        for (int k = 0; k < curves[j].length; k++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(curves[j], k, AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(curves[j], k, AnimationUtility.TangentMode.Linear);
                        }
                        AnimationUtility.SetEditorCurve(result, EditorCurveBinding.FloatCurve(wp, typeof(Transform), properties[j]), curves[j]);
                    }
                    result.EnsureQuaternionContinuity();
                    // Explicitly retain Root Motion / loop / additive settings and all original events/curves.
                    AnimationUtility.SetAnimationClipSettings(result, AnimationUtility.GetAnimationClipSettings(bodyClip));
                    AssetDatabase.CreateAsset(result, path);
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = result;
                    EditorGUIUtility.PingObject(result);
                    status = $"Saved {path}\nWeapon binding: {wp}\n{steps+1} keys/channel; sampled handle residual: {maxError:G4} world units.";
                    result = null; // Asset is owned by AssetDatabase now.
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (result && !EditorUtility.IsPersistent(result)) Object.DestroyImmediate(result);
            }
        }

        static bool IsPositionOrRotation(string p) => p.StartsWith("m_LocalPosition.", StringComparison.Ordinal) ||
            p.StartsWith("m_LocalRotation.", StringComparison.Ordinal) || p.StartsWith("localEulerAngles", StringComparison.Ordinal);
        static Quaternion Negate(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, -q.w);
        static Quaternion StableSwing(Vector3 from, Vector3 to, Vector3 preferred)
        {
            // A deterministic reference roll resolves the antiparallel singularity.
            if (Vector3.Dot(from, to) < -.9999f)
            {
                Vector3 axis = Vector3.ProjectOnPlane(preferred, from);
                if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(from, Mathf.Abs(from.y) < .9f ? Vector3.up : Vector3.right);
                var half = Quaternion.AngleAxis(180, axis.normalized);
                return Quaternion.FromToRotation(half * from, to) * half;
            }
            return Quaternion.FromToRotation(from, to);
        }

        sealed class Sampler : IDisposable
        {
            readonly Scene scene;
            public GameObject Root { get; private set; }
            readonly Animator animator;
            readonly Transform[] transforms;
            readonly Vector3[] positions, scales;
            readonly Quaternion[] rotations;
            PlayableGraph graph;
            AnimationClipPlayable playable;
            AnimationClip current;
            AnimationClip samplingClip;
            public Sampler(Animator source)
            {
                scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    // Inactive staging prevents ExecuteAlways components from being enabled on clone.
                    var staging = new GameObject("TH_Bake_Staging");
                    SceneManager.MoveGameObjectToScene(staging, scene);
                    staging.SetActive(false);
                    Root = Object.Instantiate(source.gameObject, staging.transform, false);
                    foreach (var b in Root.GetComponentsInChildren<Behaviour>(true)) b.enabled = false;
                    // Constraints and third party IK remain disabled; sample only the original body motion.
                    Root.transform.SetParent(null, false);
                    Root.SetActive(true);
                    Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    animator = Root.GetComponent<Animator>();
                    animator.enabled = true;
                    animator.runtimeAnimatorController = null;
                    animator.fireEvents = false;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind();
                    transforms = Root.GetComponentsInChildren<Transform>(true);
                    positions = transforms.Select(t => t.localPosition).ToArray();
                    rotations = transforms.Select(t => t.localRotation).ToArray();
                    scales = transforms.Select(t => t.localScale).ToArray();
                }
                catch { EditorSceneManager.ClosePreviewScene(scene); throw; }
            }
            public Transform Find(string path) => Root.transform.Find(path) ?? throw new InvalidOperationException("Clone binding missing: " + path);
            void Evaluate(AnimationClip clip, float time)
            {
                if (current != clip)
                {
                    if (graph.IsValid()) graph.Destroy();
                    if (samplingClip) Object.DestroyImmediate(samplingClip);
                    samplingClip = Object.Instantiate(clip);
                    var settings = AnimationUtility.GetAnimationClipSettings(samplingClip);
                    settings.loopTime = false;
                    AnimationUtility.SetAnimationClipSettings(samplingClip, settings);
                    graph = PlayableGraph.Create("TH isolated body sampler");
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    playable = AnimationClipPlayable.Create(graph, samplingClip);
                    playable.SetApplyFootIK(false);
                    playable.SetApplyPlayableIK(false);
                    var output = AnimationPlayableOutput.Create(graph, "Body", animator);
                    output.SetSourcePlayable(playable);
                    graph.Play(); current = clip;
                }
                playable.SetTime(time);
                graph.Evaluate(0);
                // Remove root displacement; body sampling and weapon binding share Animator space.
                Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            public void Pose(AnimationClip clip, float time)
            {
                for (int i = 0; i < transforms.Length; i++)
                { transforms[i].SetLocalPositionAndRotation(positions[i], rotations[i]); transforms[i].localScale = scales[i]; }
                Evaluate(clip, time);
            }
            public void OverlayHuman(AnimationClip clip, float time) => Evaluate(clip, time);
            public void Dispose()
            {
                if (graph.IsValid()) graph.Destroy();
                if (samplingClip) Object.DestroyImmediate(samplingClip);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
