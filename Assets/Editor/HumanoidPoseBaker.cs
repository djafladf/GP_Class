
using UnityEngine;
using UnityEditor;

public class HumanoidPoseBaker : EditorWindow
{
    GameObject character;
    AnimationClip sourceClip;

    [MenuItem("Tools/Animation/Bake Humanoid Pose")]
    static void Open()
    {
        GetWindow<HumanoidPoseBaker>("Pose Baker");
    }

    void OnGUI()
    {
        character = (GameObject)EditorGUILayout.ObjectField(
            "Character", character, typeof(GameObject), true);

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip", sourceClip, typeof(AnimationClip), false);

        if (GUILayout.Button("Bake Pose"))
            Bake();
    }

    void Bake()
    {
        if (!character || !sourceClip)
        {
            Debug.LogError("Character와 Animation Clip을 지정해야 합니다.");
            return;
        }

        Animator sourceAnimator = character.GetComponent<Animator>();

        if (!sourceAnimator || !sourceAnimator.avatar || !sourceAnimator.isHuman)
        {
            Debug.LogError("유효한 Humanoid Animator가 필요합니다.");
            return;
        }

        if (AnimationMode.InAnimationMode())
        {
            Debug.LogError("Unity Animation Preview를 종료한 뒤 실행하세요.");
            return;
        }

        GameObject copy = Instantiate(character);
        copy.name = character.name + "_PoseExport";

        Transform[] transforms = copy.GetComponentsInChildren<Transform>(true);

        Vector3[] positions = new Vector3[transforms.Length];
        Quaternion[] rotations = new Quaternion[transforms.Length];
        Vector3[] scales = new Vector3[transforms.Length];

        AnimationMode.StartAnimationMode();

        try
        {
            AnimationMode.BeginSampling();
            try
            {
                AnimationMode.SampleAnimationClip(copy, sourceClip, 0f);
            }
            finally
            {
                AnimationMode.EndSampling();
            }

            for (int i = 0; i < transforms.Length; i++)
            {
                positions[i] = transforms[i].localPosition;
                rotations[i] = transforms[i].localRotation;
                scales[i] = transforms[i].localScale;
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
        }

        AnimationClip baked = new AnimationClip
        {
            name = sourceClip.name + "_Baked",
            frameRate = 30f,
            legacy = true
        };

        float end = 1f / 30f;

        for (int i = 0; i < transforms.Length; i++)
        {
            string path = AnimationUtility.CalculateTransformPath(
                transforms[i], copy.transform);

            Vector3 p = positions[i];
            Quaternion r = rotations[i];
            Vector3 s = scales[i];

            Set(baked, path, "localPosition.x", p.x, end);
            Set(baked, path, "localPosition.y", p.y, end);
            Set(baked, path, "localPosition.z", p.z, end);

            Set(baked, path, "localRotation.x", r.x, end);
            Set(baked, path, "localRotation.y", r.y, end);
            Set(baked, path, "localRotation.z", r.z, end);
            Set(baked, path, "localRotation.w", r.w, end);

            Set(baked, path, "localScale.x", s.x, end);
            Set(baked, path, "localScale.y", s.y, end);
            Set(baked, path, "localScale.z", s.z, end);
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/" + sourceClip.name + "_Baked.anim");

        AssetDatabase.CreateAsset(baked, assetPath);
        AssetDatabase.SaveAssets();

        foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true))
            DestroyImmediate(animator);

        Animation animation = copy.GetComponent<Animation>();

        if (!animation)
            animation = copy.AddComponent<Animation>();

        animation.AddClip(baked, baked.name);
        animation.clip = baked;
        animation.playAutomatically = false;

        baked.SampleAnimation(copy, 0f);

        Selection.activeGameObject = copy;

        Debug.Log("Pose Bake 완료: " + assetPath);
    }

    static void Set(AnimationClip clip, string path, string property,
        float value, float end)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, value),
            new Keyframe(end, value));

        clip.SetCurve(path, typeof(Transform), property, curve);
    }
}
