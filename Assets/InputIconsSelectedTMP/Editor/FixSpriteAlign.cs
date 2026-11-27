using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public static class FixSpriteAlign
{
    [MenuItem("Tools/Fix TMP Sprite Align")]
    static void Fix()
    {
        var asset = Selection.activeObject as TMP_SpriteAsset;
        if (asset == null) return;

        foreach (var glyph in asset.spriteGlyphTable)
        {
            var m = glyph.metrics;

            glyph.metrics = new GlyphMetrics(
                m.width,
                m.height,
                0,
                m.height,
                m.width
            );
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}