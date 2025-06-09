using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Text.RegularExpressions;

// 이 클래스는 Editor 모드에서만 동작합니다.
public static class BlendShapeAutoRemap
{
    // 정규표현식: "blendShape." 뒤에 숫자(인덱스)가 붙어 있을 때 잡아냄
    private static readonly Regex BlendShapeIndexRegex =
        new Regex(@"\bblend Shape\s*\d+\.(.+)$", RegexOptions.Compiled);

    /// <summary>
    /// 하나의 AnimationClip에서 "blendShape.blend Shape <숫자>.<Name>" 커브를
    /// "blendShape.<Name>" 으로 일괄 치환합니다.
    /// </summary>
    public static void RemapAllBlendShapesInClip(AnimationClip clip, string meshPath, SkinnedMeshRenderer smr)
    {
        if (clip == null || smr == null || smr.sharedMesh == null)
        {
            Debug.LogWarning($"[BlendShapeAutoRemap] 대상이 null: clip={clip}, smr={smr}");
            return;
        }

        // 이 클립 안에 바인딩된 모든 커브(채널) 목록을 가져옵니다.
        var bindings = AnimationUtility.GetCurveBindings(clip);
        bool changed = false;

        foreach (var bind in bindings)
        {
            // SkinnedMeshRenderer 타입 & bind.path가 우리가 지정한 meshPath와 동일해야 처리
            if (bind.type == typeof(SkinnedMeshRenderer) && bind.path == meshPath)
            {
                // propertyName 예시: "blendShape.blend Shape 4.BLW_ANG1"
                // 또는 "blendShape.blend Shape 2.EYE_ANG2"
                string prop = bind.propertyName;
                if (!prop.StartsWith("blendShape.", System.StringComparison.Ordinal))
                    continue;

                // "blendShape." 이후 부분
                string afterBlendShape = prop.Substring("blendShape.".Length);
                // ex) afterBlendShape = "blend Shape 4.BLW_ANG1"

                string[] parts = afterBlendShape.Split('.');
                // match.Groups[1]에는 "BLW_ANG1" (또는 "EYE_ANG2" 등)만 들어 있습니다.
                string actualBlendName = parts[parts.Length - 1];
                // ex) actualBlendName = "BLW_ANG1"

                // 여기서 실제 메시가 가지고 있는 BlendShape 인덱스를 확인해 볼 수도 있습니다.
                // 만약 인덱스가 달라져 있다면, smr.sharedMesh.GetBlendShapeIndex(actualBlendName)로도 확인 가능.
                int realIdx = smr.sharedMesh.GetBlendShapeIndex(actualBlendName);
                if (realIdx < 0)
                {
                    Debug.LogWarning($"[BlendShapeAutoRemap] '{clip.name}' 클립에서 찾을 수 없는 Blend Shape 이름: {actualBlendName}");
                    continue;
                }

                // 기존 바인딩에서 Curve(keyframes)를 가져옵n니다.
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, bind);
                //Debug.Log(curve);
                if (curve == null)
                    continue;

                // 새로운 바인딩 정보 생성: "blendShape.<actualBlendName>"
                var newBind = bind;
                newBind.propertyName = $"blendShape.{actualBlendName}";

                // 기존 커브 삭제
                AnimationUtility.SetEditorCurve(clip, bind, null);
                // 새 커브 등록
                AnimationUtility.SetEditorCurve(clip, newBind, curve);

                Debug.Log($"[BlendShapeAutoRemap] '{clip.name}' : '{prop}' → 'blendShape.{actualBlendName}' 로 리맵됨");
                changed = true;
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }
    }

    //───────────────────────────────────────────────────────────────────
    // 아래부터는 메뉴에서 편하게 테스트할 수 있는 예시 코드입니다.
    //───────────────────────────────────────────────────────────────────
    [MenuItem("Assets/BlendShape/Remap Selected Clips %#r", priority = 3200)]
    private static void RemapSelectedClips()
    {
        var clips = Selection.GetFiltered<AnimationClip>(SelectionMode.Assets);
        if (clips.Length == 0)
        {
            Debug.LogWarning("[BlendShapeAutoRemap] 아무 애니메이션 클립도 선택되어 있지 않습니다.");
            return;
        }

        // 1) Animator가 붙어 있는 오브젝트(NewChan) 바로 아래부터의 경로
        string[] Paths = {
            "Character1_Reference/Character1_Hips/Character1_Spine/Character1_Spine1/Character1_Spine2/Character1_Neck/Character1_Head/BLW_DEF",
            "Character1_Reference/Character1_Hips/Character1_Spine/Character1_Spine1/Character1_Spine2/Character1_Neck/Character1_Head/EYE_DEF",
            "Character1_Reference/Character1_Hips/Character1_Spine/Character1_Spine1/Character1_Spine2/Character1_Neck/Character1_Head/EYE_DEF/EL_DEF",
            "Character1_Reference/Character1_Hips/Character1_Spine/Character1_Spine1/Character1_Spine2/Character1_Neck/Character1_Head/MTH_DEF",
        };

        // 2) 씬에서 SkinnedMeshRenderer가 붙어 있는 GameObject 찾기
        //    - 씬 상에서 "NewChan" 오브젝트를 먼저 찾은 뒤,
        //    - Transform.Find(위에서 정의한 meshPath) 로 찾아 들어갑니다.
        GameObject root = GameObject.Find("NewChan");
        if (root == null)
        {
            Debug.LogError($"[BlendShapeAutoRemap] \"NewChan\" 오브젝트를 찾을 수 없습니다.");
            return;
        }

        // root.transform.Find(...) 는 meshPath를 "/" 기준으로 분리해서 찾습니다.
        foreach (var meshPath in Paths)
        {
            Transform meshTransform = root.transform.Find(meshPath);
            if (meshTransform == null)
            {
                Debug.LogError($"[BlendShapeAutoRemap] Remap 대상 GameObject를 찾을 수 없음 (경로: {meshPath}).");
                return;
            }

            var smr = meshTransform.GetComponent<SkinnedMeshRenderer>();
            if (smr == null)
            {
                Debug.LogError($"[BlendShapeAutoRemap] SkinnedMeshRenderer 컴포넌트를 찾을 수 없습니다: {meshPath}");
                return;
            }

            // 3) 이제 선택된 클립마다 Remap 호출
            foreach (var clip in clips)
            {
                RemapAllBlendShapesInClip(clip, meshPath, smr);
            }

            Debug.Log($"[BlendShapeAutoRemap] 선택된 {clips.Length}개의 애니메이션 클립에 대해 Blend Shape 리맵이 완료되었습니다.");
        }
    }
}
