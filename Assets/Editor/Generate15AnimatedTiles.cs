// Assets/Editor/Generate15AnimatedTiles.cs
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Linq;
using System.IO;

public class Generate15AnimatedTiles : EditorWindow
{
    // your block dims:
    const int BlockWidth  = 3;  // columns per waterfall
    const int BlockHeight = 5;  // rows per waterfall
    // how many waterfalls side-by-side?
    const int BlockCount  = 6;

    [MenuItem("Tools/Generate 15 AnimatedTiles")]
    static void Run()
    {
        // pick your sliced sprite-sheet asset
        var path = EditorUtility.OpenFilePanel("Select Sliced Sprite Asset", "Assets", "png,asset");
        if (string.IsNullOrEmpty(path)) return;
        path = "Assets" + path.Substring(Application.dataPath.Length);

        // load all sub-sprites
        var sprites = AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .ToArray();
        if (sprites.Length == 0)
        {
            Debug.LogError("No sprites found at: " + path);
            return;
        }

        // assume uniform tile size
        float tileW = sprites[0].rect.width;
        float tileH = sprites[0].rect.height;

        // build lookup by (col, row)
        var lookup = sprites.ToDictionary(s => new Vector2Int(
            Mathf.FloorToInt(s.rect.x / tileW),
            Mathf.FloorToInt(s.rect.y / tileH)
        ));

        // prepare output folder
        var sheetName = Path.GetFileNameWithoutExtension(path);
        var baseDir   = Path.GetDirectoryName(path);
        var outDir    = $"{baseDir}/../GeneratedTiles/{sheetName}";
        if (!AssetDatabase.IsValidFolder(outDir))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(outDir), Path.GetFileName(outDir));

        int created = 0;

        // for each local pos in the 3×5 block
        for (int localY = 0; localY < BlockHeight; localY++)
        for (int localX = 0; localX < BlockWidth;  localX++)
        {
            // collect the 6 frames for this tile
            var frames = Enumerable.Range(0, BlockCount)
                .Select(bi => new Vector2Int(bi*BlockWidth + localX, localY))
                .Select(coord => lookup.ContainsKey(coord) ? lookup[coord] : null)
                .Where(s => s != null)
                .ToArray();

            if (frames.Length == 0)
                continue;

            // make the AnimatedTile
            var tile = ScriptableObject.CreateInstance<AnimatedTile>();
            tile.m_AnimatedSprites = frames;
            tile.m_MinSpeed = tile.m_MaxSpeed = 5f;   // FPS; tweak as you like

            var assetName = $"{sheetName}_X{localX}_Y{localY}.asset";
            var assetPath = $"{outDir}/{assetName}";
            AssetDatabase.CreateAsset(tile, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Generated {created} AnimatedTiles in {outDir}");
    }
}