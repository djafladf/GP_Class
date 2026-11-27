#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Creates two separate TMP Sprite Assets from original variant designs,
/// not by recoloring one mixed atlas. Requires the 2D Sprite package.
/// </summary>
public static class InputIconsSelectedTMPImporter
{
    const string Root = "Assets/InputIconsSelectedTMP";
    const int Size = 2048;
    const int Count = 114;

    [Serializable] class Entry
    {
        public string name;
        public string path;
        public int x;
        public int y;
        public int w;
        public int h;
    }
    [Serializable] class Map
    {
        public int count;
        public int atlas_width;
        public int atlas_height;
        public Entry[] sprites;
    }
    static string Atlas(bool white) => Root + (white ? "/White/input_icons_white.png" : "/Black/input_icons_black.png");

    [MenuItem("Tools/Input Icons Selected/1. Slice White")]
    public static void SliceWhite() => Slice(true);

    [MenuItem("Tools/Input Icons Selected/2. Slice Black")]
    public static void SliceBlack() => Slice(false);

    static bool Slice(bool white)
    {
        string path = Atlas(white);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var mapAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + (white ? "/White/input_icons_white_map.json" : "/Black/input_icons_black_map.json"));
        if (importer == null || mapAsset == null) { Debug.LogError("Missing atlas or map: " + path); return false; }
        var map = JsonUtility.FromJson<Map>(mapAsset.text);
        if (map == null || map.sprites == null || map.count != Count || map.sprites.Length != Count ||
            map.atlas_width != Size || map.atlas_height != Size)
        { Debug.LogError("Incorrect icon map: expected 114 entries, atlas 2048x2048."); return false; }
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in map.sprites)
            if (e == null || string.IsNullOrEmpty(e.name) || !unique.Add(e.name) ||
                e.x < 0 || e.y < 0 || e.w <= 0 || e.h <= 0 || e.x+e.w > Size || e.y+e.h > Size)
            { Debug.LogError("Invalid or duplicate sprite name/rectangle."); return false; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 128;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = Size;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(texture);
        if (provider == null) { Debug.LogError("Install the 2D Sprite package."); return false; }
        provider.InitSpriteEditorDataProvider();
        var oldIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
        foreach (var r in provider.GetSpriteRects()) if (!oldIds.ContainsKey(r.name)) oldIds.Add(r.name,r.spriteID);
        var rects = new SpriteRect[Count];
        var pairs = new List<SpriteNameFileIdPair>(Count);
        for (int i=0; i<Count; i++)
        {
            var e = map.sprites[i];
            if (!oldIds.TryGetValue(e.name, out GUID id)) id = GUID.Generate();
            rects[i] = new SpriteRect { name=e.name, spriteID=id, rect=new Rect(e.x,e.y,e.w,e.h),
                alignment=SpriteAlignment.Center, pivot=new Vector2(0.5f,0.5f) };
            pairs.Add(new SpriteNameFileIdPair(e.name,id));
        }
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) names.SetNameFileIdPairs(pairs);
        provider.Apply();
        importer.SaveAndReimport();
        int imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count();
        if (imported != Count) { Debug.LogError("Imported only " + imported + "/114 sprites for " + path); return false; }
        Debug.Log("Sliced " + imported + " sprites: " + path);
        return true;
    }

    static void CreateTMP(bool white)
    {
        string path = Atlas(white);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) { Debug.LogError("Missing texture: " + path); return; }
        if (AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count() != Count)
        { Debug.LogError("Slice before creating TMP asset: " + path); return; }
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_SpriteAsset", new[] {Root}))
        {
            var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (spriteAsset != null && spriteAsset.spriteSheet == tex)
            { Debug.Log("TMP Sprite Asset already exists: " + AssetDatabase.GetAssetPath(spriteAsset)); return; }
        }
        Selection.activeObject = tex;
        // The official TMP Editor menu creates a separate TMP Sprite Asset next to the selected texture.
        bool menuUsed = EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Sprite Asset");
        if (!menuUsed) menuUsed = EditorApplication.ExecuteMenuItem("Assets/Create/TextMesh Pro/Sprite Asset");
        if (!menuUsed)
            Debug.LogWarning("TMP creation menu not found. Select " + path +
                " and choose Assets > Create > TextMesh Pro > Sprite Asset manually.");
        else
            Debug.Log("Requested TMP Sprite Asset creation for " + (white ? "White" : "Black") +
                ". Check its Sprite Character Table (114 entries).");
    }

    [MenuItem("Tools/Input Icons Selected/3. Create White TMP")]
    public static void CreateWhite() => CreateTMP(true);
    [MenuItem("Tools/Input Icons Selected/4. Create Black TMP")]
    public static void CreateBlack() => CreateTMP(false);
    [MenuItem("Tools/Input Icons Selected/5. Import Both")]
    public static void ImportBoth()
    {
        if (Slice(true)) CreateTMP(true);
        if (Slice(false)) CreateTMP(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
