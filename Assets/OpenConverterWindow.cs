using UnityEngine;
using UnityEditor;

public class MaterialConverter
{
    [MenuItem("Tools/Convert All Materials to URP Lit")]
    public static void OpenConverterWindow()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat.shader.name == "Standard")
            {
                Debug.Log($"Converting: {path}");
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                count++;
            }
        }

        Debug.Log($"Converted {count} materials to URP Lit shader.");
    }
}