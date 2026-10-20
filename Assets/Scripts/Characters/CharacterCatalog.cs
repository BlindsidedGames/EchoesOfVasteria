using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Characters
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Character Catalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public string id, label;
            public CharacterSlot slot;
            public string resourcePath;
            private static readonly string[] Colours = { "White_and_Brown", "Dark_Brown", "Light_Brown", "Auburn", "Black", "Blonde", "Brown", "Ginger", "Grey", "Blue", "Green", "Orange", "Pink", "Purple", "Red", "White", "Bronze", "Gold", "Iron" };
            private string FileName => id.Substring(id.LastIndexOf('/') + 1);
            public string Colour => Array.Find(Colours, c => FileName.EndsWith("_" + c, StringComparison.Ordinal)) ?? "";
            public string Style
            {
                get
                {
                    var colour = Colour;
                    var name = colour.Length == 0 ? FileName : FileName.Substring(0, FileName.Length - colour.Length - 1);
                    // The source royal red trousers have a spelling error; they are the same style.
                    return name.Replace("Pantst", "Pants");
                }
            }
            public string StyleLabel => (Style.StartsWith("Hair_", StringComparison.Ordinal) ? Style :
                Style.EndsWith("_1", StringComparison.Ordinal) ? Style.Substring(0, Style.Length - 2) : Style).Replace('_', ' ');
            public bool IsArmour => Style.StartsWith("Plate_", StringComparison.Ordinal) || Style.StartsWith("Heavy_Plate_", StringComparison.Ordinal);
            [NonSerialized] private CharacterPartFrames loaded;
            internal bool IsLoaded => loaded;
            public Sprite At(int index) {
                if (!loaded) loaded = Resources.Load<CharacterPartFrames>(resourcePath);
                return loaded && loaded.frames != null && index >= 0 && index < loaded.frames.Length ? loaded.frames[index] : null;
            }
            public void Release() => loaded = null;
        }
        [Serializable] public struct Pose { public string name; public int start, length; }
        [Serializable] public struct SourceFrame { public Sprite sprite; public int frame; }
        [Serializable] public sealed class GearVisual { public string helmet, chest, legs, boots, tool; }
        public GearVisual[] gear = Array.Empty<GearVisual>();
        public Sprite[] skillToolIcons = Array.Empty<Sprite>(); // Brown, then metals; axe, pick, watering can, rod.
        public Sprite[] skillSupportIcons = Array.Empty<Sprite>(); // Brown, then metals; looting crest, combat weapon.
        public Part[] parts = Array.Empty<Part>();
        public Pose[] poses = Array.Empty<Pose>();
        public SourceFrame[] sourceFrames = Array.Empty<SourceFrame>();
        public bool[] toolBehind = Array.Empty<bool>();
        public CharacterProfile defaults = new();
        private Dictionary<string, Part> byId;
        private Dictionary<Sprite, int> frames;
        private Dictionary<string, Pose> byPose;
        private static CharacterCatalog cached;
        [NonSerialized] private AsyncOperation unusedAssetUnload;
        public static CharacterCatalog Load() => cached ? cached : cached = Resources.Load<CharacterCatalog>("Characters/Catalog");
        private void OnEnable() { byId = null; frames = null; byPose = null; }
        private void Prepare()
        {
            if (byId != null) return;
            byId = new(StringComparer.Ordinal); frames = new(); byPose = new(StringComparer.Ordinal);
            foreach (var part in parts) if (part != null) byId[part.id] = part;
            foreach (var binding in sourceFrames) if (binding.sprite) frames[binding.sprite] = binding.frame;
            foreach (var pose in poses) byPose[pose.name] = pose;
        }
        public Part Find(string id) { Prepare(); return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out var part) ? part : null; }
        public int Frame(string pose, int offset)
        {
            Prepare(); return byPose.TryGetValue(pose, out var row) ? row.start + Mathf.Abs(offset % Mathf.Max(1, row.length)) : 0;
        }
        public bool TrySource(Sprite sprite, out int frame) { Prepare(); frame = 0; return sprite && frames.TryGetValue(sprite, out frame); }
        public Part PartFor(CharacterProfile profile, CharacterSlot slot)
        {
            var id = slot is CharacterSlot.Body or CharacterSlot.Hands ? slot.ToString() :
                slot == CharacterSlot.Tool ? (string.IsNullOrEmpty(profile?.Tool) ? "Tool" : profile.Tool) : profile?.Get(slot);
            var part = Find(id);
            if (part != null && part.slot == slot) return part;
            // Missing optional hats/headwear remain unequipped; required clothing has a safe fallback.
            if (slot is CharacterSlot.Head or CharacterSlot.Accessory) return null;
            part = Find(defaults.Get(slot));
            return part != null && part.slot == slot ? part : null;
        }
        public Sprite SpriteFor(CharacterProfile profile, CharacterSlot slot, int frame)
        {
            var part = PartFor(profile, slot);
            return part?.At(frame);
        }
        public CharacterProfile NewProfile()
        {
            var profile = defaults.Copy(); profile.Created = false; profile.Modular = true; profile.Name = ""; return profile;
        }
        public void TrimAppearanceCache(CharacterProfile preview, CharacterProfile character)
        {
            // Browsing styles must not retain an entire wardrobe until the creator closes.
            // Keep room for the visible outfit, the world outfit and recently viewed parts.
            var count = 0;
            foreach (var part in parts) if (part.IsLoaded) count++;
            if (count > 20) ReleaseUnused(preview, character);
        }

        public void ReleaseUnused(params CharacterProfile[] profiles)
        {
            var selected = new HashSet<Part>();
            foreach (var profile in profiles)
            {
                if (profile == null) continue;
                for (var i = 0; i < 8; i++) { var part = PartFor(profile, (CharacterSlot)i); if (part != null) selected.Add(part); }
            }
            foreach (var part in parts) if (!selected.Contains(part)) part.Release();
            if (unusedAssetUnload == null || unusedAssetUnload.isDone)
                unusedAssetUnload = Resources.UnloadUnusedAssets();
        }
    }
}
