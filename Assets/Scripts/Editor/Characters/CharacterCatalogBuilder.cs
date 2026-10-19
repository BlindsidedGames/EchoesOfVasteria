using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TimelessEchoes.Characters;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.Editor.Characters
{
    public static class CharacterCatalogBuilder
    {
        private const string Art = "Assets/Art/Character/Modular";
        private const string CatalogPath = "Assets/Resources/Characters/Catalog.asset";
        [MenuItem("Tools/Characters/Rebuild Character Catalog")]
        public static void Rebuild() => Build();
        public static object Build()
        {
            GenerateHairColours();
            GenerateEquipmentColours();
            Directory.CreateDirectory("Assets/Resources/Characters"); AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<CharacterCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.poses = Rows(Art + "/Player_Base/Player_Base_animations_row_ranges.txt").ToArray();
            var count = catalog.poses.Max(p => p.start + p.length);
            var parts = new List<CharacterCatalog.Part>();
            Directory.CreateDirectory("Assets/Resources/Characters/Parts"); AssetDatabase.Refresh();
            CharacterCatalog.Part Part(string id, string label, CharacterSlot slot, Sprite[] frames)
            {
                var resourcePath = "Characters/Parts/" + Hash128.Compute(id);
                var assetPath = "Assets/Resources/" + resourcePath + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<CharacterPartFrames>(assetPath);
                if (!asset) { asset = ScriptableObject.CreateInstance<CharacterPartFrames>(); AssetDatabase.CreateAsset(asset, assetPath); }
                asset.frames = frames; EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
                return new CharacterCatalog.Part { id = id, label = label, slot = slot, resourcePath = resourcePath };
            }
            void Add(string path, CharacterSlot slot, string id = null)
            {
                var sprites = Sprites(path);
                var frames = new Sprite[count]; Array.Copy(sprites, frames, Mathf.Min(count, sprites.Length));
                parts.Add(Part(id ?? path.Substring(Art.Length + 1).Replace(".png", ""),
                    Path.GetFileNameWithoutExtension(path).Replace('_', ' '), slot, frames));
            }
            Add(Art + "/Player_Base/Player_Base_animations.png", CharacterSlot.Body, "Body");
            foreach (var slot in new[] { CharacterSlot.Legs, CharacterSlot.Feet, CharacterSlot.Chest, CharacterSlot.Head, CharacterSlot.Accessory })
            {
                var folder = slot == CharacterSlot.Accessory ? "Accessories" : slot.ToString();
                foreach (var path in Directory.GetFiles(Art + "/" + folder, "*.png", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                    Add(path.Replace('\\', '/'), slot);
            }
            Add(Art + "/Hands/Hands_1_Bare.png", CharacterSlot.Hands, "Hands");
            var tools = new Sprite[count]; catalog.toolBehind = new bool[count];
            foreach (var sheet in new[] { "Tools/Iron/Iron_Tools", "Tools/Iron/Iron_Sword", "Tools/Fishing_Rod/Wooden_Fishing_Rod" })
            {
                var sprites = Sprites(Art + "/" + sheet + ".png");
                foreach (var row in Rows(Art + "/" + sheet + "_row_ranges.txt"))
                {
                    var target = catalog.poses.Single(p => p.name == row.name);
                    for (var i = 0; i < target.length; i++)
                    {
                        // Sword artwork deliberately ends before the final body frame.
                        if (row.name.StartsWith("SwordAttack_", StringComparison.Ordinal) && i >= row.length) continue;
                        var index = row.start + Mathf.RoundToInt(i * (row.length - 1f) / Mathf.Max(1, target.length - 1));
                        tools[target.start + i] = index < sprites.Length ? sprites[index] : null;
                        catalog.toolBehind[target.start + i] = row.name.EndsWith("_Up", StringComparison.Ordinal);
                    }
                }
            }
            parts.Add(Part("Tool", "Tools", CharacterSlot.Tool, tools));
            var unarmed = (Sprite[])tools.Clone();
            foreach (var row in catalog.poses.Where(p => p.name.StartsWith("SwordAttack_", StringComparison.Ordinal)))
                Array.Clear(unarmed, row.start, row.length);
            parts.Add(Part("Tool/Unarmed", "Unarmed", CharacterSlot.Tool, unarmed));
            // Match the existing forge icon rows, including the pink/purple and cyan final tiers.
            var metals = new[] { "Bronze", "Iron", "Gold", "Orange", "Blue", "Green", "Red", "Purple" };
            void AddSkillTools(string metal, string id) {
                var skillTools = new Sprite[count];
                foreach (var sheet in new[] { "Tools/Armour/Tools_" + metal, "Tools/Armour/Rod_" + metal }) {
                    var skillSprites = Sprites(Art + "/" + sheet + ".png");
                    var ranges = sheet.Contains("/Rod_") ? "Tools/Fishing_Rod/Wooden_Fishing_Rod" : "Tools/Iron/Iron_Tools";
                    foreach (var row in Rows(Art + "/" + ranges + "_row_ranges.txt")) {
                        var target = catalog.poses.Single(p => p.name == row.name);
                        for (var i = 0; i < target.length; i++) {
                            var index = row.start + Mathf.RoundToInt(i * (row.length - 1f) / Mathf.Max(1, target.length - 1));
                            skillTools[target.start + i] = skillSprites[index];
                        }
                    }
                }
                parts.Add(Part(id, metal + " skill tools", CharacterSlot.Tool, skillTools));
            }
            AddSkillTools("Brown", "Tool/SkillBrown");
            var iconSheet = Sprites("Assets/Art/Packs/Cute_Fantasy_UI/Tools_Icons-Recovered.png");
            var iconRows = new[] { 8, 0, 1, 2, 3, 4, 5, 6, 7 };
            var iconColumns = new[] { 1, 0, 3, 2 };
            catalog.skillToolIcons = iconRows.SelectMany(row => iconColumns.Select(column => iconSheet[row * 12 + column])).ToArray();
            catalog.gear = new CharacterCatalog.GearVisual[metals.Length];
            for (var tier = 0; tier < metals.Length; tier++) {
                var metal = metals[tier];
                var weapon = (Sprite[])unarmed.Clone();
                var sprites = Sprites(Art + "/Tools/Armour/Sword_" + metal + ".png");
                foreach (var row in Rows(Art + "/Tools/Iron/Iron_Sword_row_ranges.txt")) {
                    var target = catalog.poses.Single(p => p.name == row.name);
                    for (var i = 0; i < Mathf.Min(target.length, row.length); i++) weapon[target.start + i] = sprites[row.start + i];
                }
                var toolId = "Tool/Metal" + tier;
                parts.Add(Part(toolId, metal + " sword", CharacterSlot.Tool, weapon));
                AddSkillTools(metal, "Tool/SkillMetal" + tier);
                catalog.gear[tier] = new CharacterCatalog.GearVisual {
                    helmet = "Head/Plate_Helmet_1/Plate_Helmet_1_" + metal,
                    chest = "Chest/Plate_Chest/Plate_Chest_" + metal,
                    legs = "Legs/Plate_Legs/Plate_Legs_" + metal,
                    boots = "Feet/Plate_Boots_" + metal, tool = toolId
                };
            }
            catalog.parts = parts.ToArray();
            string Default(CharacterSlot slot, string file) => parts.First(p => p.slot == slot && p.id.EndsWith("/" + file, StringComparison.Ordinal)).id;
            catalog.defaults = new CharacterProfile { Modular = true,
                Head = Default(CharacterSlot.Head, "Hair_1_Brown"), Chest = Default(CharacterSlot.Chest, "Farmer_Shirt_1_Green"),
                Legs = Default(CharacterSlot.Legs, "Farmer_Pants_1_Blue"), Feet = Default(CharacterSlot.Feet, "Shoes_1_Brown") };
            var bindings = new Dictionary<Sprite, int>();
            foreach (var path in Directory.GetFiles("Assets/Art/Character/BasicCharacter", "*.anim").OrderBy(p => p, StringComparer.Ordinal))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path.Replace('\\', '/'));
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace("_", "").Replace("riight", "right");
                var direction = name.EndsWith("up") ? "Up" : name.EndsWith("right") ? "Right" : "Down";
                var action = name.StartsWith("idle") ? "Idle" : name.StartsWith("run") ? "Running" :
                    name.StartsWith("attack") ? "SwordAttack_1" : name.StartsWith("chop") ? "Chop" :
                    name.StartsWith("mining") ? "Mine" : name.StartsWith("water") ? "Watering" :
                    name.StartsWith("openchest") ? "Push" : name.StartsWith("fishingcatch") ? "Fishing_Catch" : "Fishing_Cast";
                var row = catalog.poses.Single(p => p.name == action + "_" + direction);
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                for (var i = 0; i < keys.Length; i++)
                    if (keys[i].value is Sprite sprite)
                        bindings[sprite] = row.start + (name.StartsWith("fishingidle") ? row.length - 1 : Mathf.RoundToInt(i * (row.length - 1f) / Mathf.Max(1, keys.Length - 1)));
            }
            catalog.sourceFrames = bindings.Select(p => new CharacterCatalog.SourceFrame { sprite = p.Key, frame = p.Value }).ToArray();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
            InstallCreator();
            CharacterTextureImportPolicy.ApplyExisting();
            return new { parts = parts.Count, frames = count, sourceFrames = bindings.Count };
        }
        private static List<CharacterCatalog.Pose> Rows(string path)
        {
            var rows = new List<CharacterCatalog.Pose>();
            foreach (var line in File.ReadAllLines(path)) {
                var match = Regex.Match(line, @"^Row\s+\d+:\s+(\d+)-(\d+)\s+\(\d+\s+frames?\)\s*([^()]+)");
                if (match.Success) { var start = int.Parse(match.Groups[1].Value); rows.Add(new CharacterCatalog.Pose {
                    name = match.Groups[3].Value.Trim(), start = start, length = int.Parse(match.Groups[2].Value) - start + 1 }); }
            }
            return rows;
        }

        // Palette substitution retains every authored frame, skin pixel, outline and alpha value.
        // Regenerate these variants from the original brown sheets when rebuilding the catalog.
        public static void GenerateHairColours()
        {
            var palettes = new (string name, uint light, uint shade, uint dark)[] {
                ("Purple", 0x93388f, 0x622461, 0x391b45),
                ("Blue", 0x0098dc, 0x0069aa, 0x173f70),
                ("Pink", 0xf389f5, 0xc950bc, 0x7c326e),
                ("Auburn", 0xa14d36, 0x71332b, 0x432329),
                ("Dark_Brown", 0x49302b, 0x352222, 0x261923),
                ("Light_Brown", 0xb58058, 0x8d593e, 0x56372e),
                ("White", 0xf9e6cf, 0xc7b9b4, 0x887c89)
            };
            Color32 Rgb(uint rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            for (var style = 1; style <= 6; style++)
            {
                var source = $"{Art}/Head/Hair_{style}/Hair_{style}_Brown.png";
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try {
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(source))) throw new InvalidOperationException("Cannot decode " + source);
                    var original = texture.GetPixels32();
                    foreach (var palette in palettes) {
                        var light = Rgb(palette.light); var shade = Rgb(palette.shade); var dark = Rgb(palette.dark);
                        var pixels = (Color32[])original.Clone();
                        for (var i = 0; i < pixels.Length; i++) {
                            var pixel = pixels[i]; if (pixel.a != 255) continue;
                            if (pixel.r == 112 && pixel.g == 70 && pixel.b == 67) pixels[i] = light;
                            else if (pixel.r == 93 && pixel.g == 44 && pixel.b == 40) pixels[i] = shade;
                            else if (pixel.r == 57 && pixel.g == 31 && pixel.b == 33) pixels[i] = dark;
                        }
                        texture.SetPixels32(pixels); texture.Apply();
                        var destination = source.Replace("_Brown.png", "_" + palette.name + ".png");
                        var bytes = texture.EncodeToPNG();
                        if (!File.Exists(destination) || !File.ReadAllBytes(destination).SequenceEqual(bytes)) File.WriteAllBytes(destination, bytes);
                        if (!File.Exists(destination + ".meta")) {
                            var metadata = File.ReadAllText(source + ".meta").Replace($"Hair_{style}_Brown", $"Hair_{style}_{palette.name}");
                            metadata = Regex.Replace(metadata, @"(?m)^guid: [0-9a-f]+", "guid: " + GUID.Generate());
                            File.WriteAllText(destination + ".meta", metadata);
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

        public static void GenerateEquipmentColours()
        {
            var palettes = new (string name, uint highlight, uint light, uint shade, uint dark)[] {
                ("Bronze", 0xead4aa, 0xe4a672, 0xb86f50, 0x743f39),
                ("Iron", 0xc0cbdc, 0x8e9ab4, 0x6c7c9d, 0x424c6e),
                ("Gold", 0xfff7d2, 0xfee761, 0xfeae34, 0xdd5a08),
                ("Orange", 0xffeb57, 0xffa214, 0xed7614, 0xc64524),
                ("Purple", 0xfdd2ed, 0xf389f5, 0xdb3ffd, 0x93388f),
                ("Green", 0xd3fc7e, 0x5ac54f, 0x33984b, 0x1e6f50),
                ("Red", 0xf8a0a6, 0xf5555d, 0xc42430, 0x891e2b),
                ("Blue", 0x94fdff, 0x00cdf9, 0x0098dc, 0x0069aa),
                ("Brown", 0xbf6f4a, 0x8a4836, 0x5d2c28, 0x391f21)
            };
            Color32 Rgb(uint rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            void Write(string source, string destination, Func<Color32[], int, int, Color32[]> recolour) {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try {
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(source))) throw new InvalidOperationException(source);
                    texture.SetPixels32(recolour(texture.GetPixels32(), texture.width, texture.height)); texture.Apply();
                    var bytes = texture.EncodeToPNG();
                    if (!File.Exists(destination) || !File.ReadAllBytes(destination).SequenceEqual(bytes)) File.WriteAllBytes(destination, bytes);
                    if (!File.Exists(destination + ".meta")) {
                        var meta = File.ReadAllText(source + ".meta").Replace(Path.GetFileNameWithoutExtension(source), Path.GetFileNameWithoutExtension(destination));
                        File.WriteAllText(destination + ".meta", Regex.Replace(meta, @"(?m)^guid: [0-9a-f]+", "guid: " + GUID.Generate()));
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            foreach (var palette in palettes) {
                if (palette.name != "Brown") {
                Write(Art + "/Feet/Shoes_1_White.png", Art + "/Feet/Plate_Boots_" + palette.name + ".png", (original, width, height) => {
                    var pixels = new Color32[original.Length];
                    for (var i = 0; i < pixels.Length; i++) {
                        if (original[i].a == 0) continue;
                        pixels[i] = Rgb(palette.shade);
                        // A raised metal toe makes these boots distinct from the original leather shoes.
                        if ((i / width) % 64 < 63 && i + width < pixels.Length && original[i + width].a == 0)
                            pixels[i + width] = Rgb(palette.light);
                    }
                    return pixels;
                });
                Write(Art + "/Tools/Iron/Iron_Sword.png", Art + "/Tools/Armour/Sword_" + palette.name + ".png", (pixels, width, height) => {
                    for (var i = 0; i < pixels.Length; i++) {
                        var p = pixels[i]; if (p.a == 0) continue;
                        if (p.r == 255 && p.g == 255 && p.b == 255) pixels[i] = Rgb(palette.highlight);
                        else if (p.r == 192 && p.g == 203 && p.b == 220) pixels[i] = Rgb(palette.light);
                        else if (p.r == 139 && p.g == 155 && p.b == 180) pixels[i] = Rgb(palette.shade);
                        else if (p.r == 90 && p.g == 105 && p.b == 136) pixels[i] = Rgb(palette.dark);
                    }
                    return pixels;
                });
                }
                Write(Art + "/Tools/Iron/Iron_Tools.png", Art + "/Tools/Armour/Tools_" + palette.name + ".png", (pixels, width, height) => {
                    for (var i = 0; i < pixels.Length; i++) {
                        var p = pixels[i]; if (p.a == 0) continue;
                        if (p.r == 255 && p.g == 255 && p.b == 255) pixels[i] = Rgb(palette.highlight);
                        else if (p.r == 192 && p.g == 203 && p.b == 220) pixels[i] = Rgb(palette.light);
                        else if (p.r == 139 && p.g == 155 && p.b == 180) pixels[i] = Rgb(palette.shade);
                        else if (p.r == 90 && p.g == 105 && p.b == 136) pixels[i] = Rgb(palette.dark);
                    }
                    return pixels;
                });
                Write(Art + "/Tools/Fishing_Rod/Wooden_Fishing_Rod.png", Art + "/Tools/Armour/Rod_" + palette.name + ".png", (pixels, width, height) => {
                    for (var i = 0; i < pixels.Length; i++) {
                        var p = pixels[i]; if (p.a == 0) continue;
                        // Reel and fittings only: keep the wood, white line, bobber and water intact.
                        if (p.r == 199 && p.g == 207 && p.b == 221) pixels[i] = Rgb(palette.light);
                        else if (p.r == 146 && p.g == 161 && p.b == 185) pixels[i] = Rgb(palette.shade);
                        else if (p.r == 66 && p.g == 76 && p.b == 110) pixels[i] = Rgb(palette.dark);
                    }
                    return pixels;
                });
            }
        }
        private static Sprite[] Sprites(string path)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length == 0) throw new InvalidOperationException("No imported sprites: " + path);
            var indexed = sprites.Select(s => (sprite: s, index: int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))).ToArray();
            var result = new Sprite[indexed.Max(p => p.index) + 1];
            foreach (var pair in indexed) result[pair.index] = pair.sprite;
            return result;
        }
        private static void InstallCreator()
        {
            const string path = "Assets/UI/Toolkit/Intro.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try {
                var creator = prefab.GetComponentInChildren<ToolkitCharacterCreator>(true);
                if (!creator) {
                    var child = new GameObject("Character Creator"); child.transform.SetParent(prefab.transform, false);
                    creator = child.AddComponent<ToolkitCharacterCreator>();
                }
                if (creator.GetComponent<UIDocument>()) UnityEngine.Object.DestroyImmediate(creator.GetComponent<UIDocument>());
                var source = new SerializedObject(prefab.GetComponent<ToolkitIntroScreen>());
                var target = new SerializedObject(creator);
                foreach (var field in new[] { "theme", "runtimeTheme", "textSettings" })
                    target.FindProperty(field).objectReferenceValue = source.FindProperty(field).objectReferenceValue;
                target.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
    }
}
