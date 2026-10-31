#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using TimelessEchoes.Characters;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.Editor
{
    internal static class CharacterPortraitIosAssets
    {
        // Decode original source PNGs at build time. Runtime atlas readability,
        // tight sprite meshes and gameplay cameras never enter this export path.
        internal static void Export(string destination)
        {
            var catalog = CharacterCatalog.Load();
            if (!catalog) throw new InvalidOperationException("Character portrait catalog is missing.");
            var frame = catalog.Frame("Idle_Down", 0);
            Texture2D texture = null;
            string texturePath = null;
            var names = new HashSet<string>();
            try
            {
                foreach (var part in catalog.parts)
                {
                    if (part == null) continue;
                    var name = CharacterActivityPortrait.AssetName(part);
                    var sprite = part.At(frame);
                    // Tools intentionally have no idle artwork. Do not invent a pose.
                    if (!sprite)
                    {
                        if (part.slot == CharacterSlot.Body)
                            throw new InvalidOperationException("The body idle portrait frame is missing.");
                        continue;
                    }
                    if (name == null || !names.Add(name))
                        throw new InvalidOperationException("Invalid or duplicate character portrait asset: " + part.id);
                    var path = AssetDatabase.GetAssetPath(sprite.texture);
                    if (texturePath != path)
                    {
                        if (texture) UnityEngine.Object.DestroyImmediate(texture);
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        texturePath = path;
                        if (!texture.LoadImage(File.ReadAllBytes(path)))
                            throw new InvalidOperationException("Could not decode character portrait sheet: " + path);
                    }
                    var rect = sprite.rect;
                    var width = Mathf.RoundToInt(rect.width);
                    var height = Mathf.RoundToInt(rect.height);
                    if (width != 64 || height != 64)
                        throw new InvalidOperationException("Expected a 64px character canvas: " + part.id);
                    var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    try
                    {
                        image.SetPixels(texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), width, height));
                        image.Apply(false, false);
                        var folder = Path.Combine(destination, name + ".imageset");
                        Directory.CreateDirectory(folder);
                        File.WriteAllBytes(Path.Combine(folder, "idle.png"), image.EncodeToPNG());
                        File.WriteAllText(Path.Combine(folder, "Contents.json"),
                            "{\"images\":[{\"filename\":\"idle.png\",\"idiom\":\"universal\",\"scale\":\"2x\"}],\"info\":{\"author\":\"xcode\",\"version\":1}}");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(image); }
                }
            }
            finally
            {
                if (texture) UnityEngine.Object.DestroyImmediate(texture);
            }
            Debug.Log($"[EOVLiveActivity] Exported {names.Count} character idle layers.");
        }

        // Watch owns its extra authored idle frames; the Live Activity's base catalog
        // and existing portrait IDs remain unchanged. Missing optional pose frames
        // fall back to that part's own idle0, never another character's artwork.
        internal static void ExportWatchAnimation(string destination)
        {
            var catalog = CharacterCatalog.Load();
            if (!catalog) throw new InvalidOperationException("Character portrait catalog is missing.");
            Texture2D texture = null;
            string texturePath = null;
            var names = new HashSet<string>();
            try
            {
                foreach (var part in catalog.parts)
                {
                    if (part == null) continue;
                    var first = part.At(catalog.Frame("Idle_Down", 0));
                    if (!first) continue;
                    var name = CharacterActivityPortrait.AssetName(part);
                    if (name == null || !names.Add(name))
                        throw new InvalidOperationException("Invalid or duplicate Watch portrait asset: " + part.id);
                    for (var index = 0; index < 6; index++)
                    {
                        var sprite = part.At(catalog.Frame("Idle_Down", index));
                        if (!sprite) sprite = first;
                        var path = AssetDatabase.GetAssetPath(sprite.texture);
                        if (texturePath != path)
                        {
                            if (texture) UnityEngine.Object.DestroyImmediate(texture);
                            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            texturePath = path;
                            if (!texture.LoadImage(File.ReadAllBytes(path)))
                                throw new InvalidOperationException("Could not decode Watch portrait sheet: " + path);
                        }
                        var rect = sprite.rect;
                        if (Mathf.RoundToInt(rect.width) != 64 || Mathf.RoundToInt(rect.height) != 64)
                            throw new InvalidOperationException("Expected a 64px Watch character canvas: " + part.id);
                        var image = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                        try
                        {
                            image.SetPixels(texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), 64, 64));
                            image.Apply(false, false);
                            var folder = Path.Combine(destination, name + "_WatchIdle" + index + ".imageset");
                            Directory.CreateDirectory(folder);
                            File.WriteAllBytes(Path.Combine(folder, "idle.png"), image.EncodeToPNG());
                            File.WriteAllText(Path.Combine(folder, "Contents.json"),
                                "{\"images\":[{\"filename\":\"idle.png\",\"idiom\":\"universal\",\"scale\":\"2x\"}],\"info\":{\"author\":\"xcode\",\"version\":1}}");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(image); }
                    }
                }
            }
            finally { if (texture) UnityEngine.Object.DestroyImmediate(texture); }
            Debug.Log($"[EOVWatch] Exported {names.Count} personalized idle layers with six frames each.");
        }
    }
}
#endif
