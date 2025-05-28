using UnityEngine;
using UnityEditor;

public class ConvertToMecanim
{
    [MenuItem("Tools/Convert to Mecanim")]
    static void ConvertSelectedClips()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is AnimationClip clip)
            {
                clip.legacy = false;
                EditorUtility.SetDirty(clip);
                Debug.Log($"Converted {clip.name} to Mecanim");
            }
        }
        AssetDatabase.SaveAssets();
    }
}
