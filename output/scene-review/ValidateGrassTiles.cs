using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

// Read-only imported asset validation; never edits assets or cells.
public static class ValidateGrassTiles
{
    [Serializable] public class Catalogue
    {
        public string texture_guid, original_texture_path, texture_path;
        public Entry[] tiles;
    }
    [Serializable] public class Entry
    {
        public int original_index;
        public string original_sprite_name, new_name, sprite_local_id, sprite_id;
        public string asset_guid, original_asset_path, asset_path;
        public RectEntry rect_pixels;
    }
    [Serializable] public class RectEntry { public int x, y, width, height; }
    private static Catalogue Read()
    {
        // Ephemeral CLI assemblies are not registered with Unity's native serializer.
        var jsonType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", true);
        var deserialize = jsonType.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
        return (Catalogue)deserialize.Invoke(null, new object[] {
            File.ReadAllText("docs/TerrainTiles/GrassDirtJagged.tiles.json"), typeof(Catalogue) });
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Tile Load(Entry e) => AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(e.asset_guid));

    public static object Validate()
    {
        var c = Read();
        Check(AssetDatabase.GUIDToAssetPath(c.texture_guid) == c.texture_path, "Texture GUID/path mismatch");
        var sprites = AssetDatabase.LoadAllAssetsAtPath(c.texture_path).OfType<Sprite>().ToArray();
        Check(sprites.Length == 49, "Expected all 49 imported sprites");
        foreach (var e in c.tiles)
        {
            var t = Load(e);
            Check(t != null && t.name == e.new_name, "Tile missing or wrong name: " + e.original_index);
            Check(AssetDatabase.GUIDToAssetPath(e.asset_guid) == e.asset_path, "Tile GUID/path mismatch");
            Check(t.sprite != null && t.sprite.name == e.new_name, "Tile sprite missing/wrong name");
            string guid; long localId;
            Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(t.sprite, out guid, out localId), "Unresolved sprite");
            Check(guid == c.texture_guid && localId.ToString() == e.sprite_local_id, "Sprite local ID changed: " + e.original_index);
            Check(t.sprite.rect == new Rect(e.rect_pixels.x, e.rect_pixels.y, e.rect_pixels.width, e.rect_pixels.height), "Sprite rectangle changed");
            Check(sprites.Count(s => s.name == e.new_name) == 1, "Duplicate/missing sprite name");
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(c.texture_path);
        Check(importer.spritePixelsPerUnit == 16 && importer.filterMode == FilterMode.Point, "Native pixel settings changed");
        return new { tiles = c.tiles.Length, sprites = sprites.Length, textureGuidPreserved = true,
                     tileGuidsPreserved = true, spriteLocalIdsPreserved = true, rectanglesPreserved = true,
                     playing = EditorApplication.isPlaying,
                     activeSceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
    }
}
