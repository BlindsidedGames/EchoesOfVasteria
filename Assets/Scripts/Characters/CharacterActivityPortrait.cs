using System;
using System.Collections.Generic;
using System.Linq;

namespace TimelessEchoes.Characters
{
    // Named idle layers for native presentation. Equipment and fallback choices
    // remain owned by the same catalog and resolver as the in-game character.
    public static class CharacterActivityPortrait
    {
        public static string[] Capture(CharacterProfile character)
        {
            var profile = CharacterEquipmentAppearance.Resolve(character);
            if (profile is not { Created: true, Modular: true }) return null;
            var catalog = CharacterCatalog.Load();
            if (!catalog) return null;
            var frame = catalog.Frame("Idle_Down", 0);
            var toolBehind = frame < catalog.toolBehind.Length && catalog.toolBehind[frame];
            var layers = new List<string>(8);
            void Add(CharacterSlot slot)
            {
                var part = catalog.PartFor(profile, slot);
                if (part == null || !part.At(frame)) return;
                var name = AssetName(part);
                if (name != null) layers.Add(name);
            }
            if (toolBehind) Add(CharacterSlot.Tool);
            Add(CharacterSlot.Body);
            Add(CharacterSlot.Head);
            Add(CharacterSlot.Legs);
            Add(CharacterSlot.Feet);
            Add(CharacterSlot.Chest);
            Add(CharacterSlot.Hands);
            if (!toolBehind) Add(CharacterSlot.Tool);
            Add(CharacterSlot.Accessory);
            return layers.Count == 0 ? null : layers.ToArray();
        }

        public static string AssetName(CharacterCatalog.Part part)
        {
            var path = part?.resourcePath;
            if (string.IsNullOrEmpty(path)) return null;
            var id = path.Substring(path.LastIndexOf('/') + 1);
            return id.Length == 32 && id.All(Uri.IsHexDigit) ? "EOVPortrait_" + id : null;
        }
    }
}
