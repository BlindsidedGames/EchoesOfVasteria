using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Blindsided;

namespace TimelessEchoes.Characters
{
    public enum CharacterSlot { Body, Legs, Feet, Chest, Hands, Head, Accessory, Tool }

    [Serializable]
    public sealed class CharacterProfile
    {
        public bool Created;
        public bool Modular;
        public string Name = "";
        public string Head = "", Chest = "", Legs = "", Feet = "", Accessory = "", Tool = "";
        public CharacterProfile Copy() => (CharacterProfile)MemberwiseClone();
        // Existing saves retain their original art until the player chooses a new appearance.
        public static CharacterProfile Legacy() => new() { Created = true, Name = "Adventurer" };
        public static CharacterProfile Fresh() => new() { Modular = true };
        public string Get(CharacterSlot slot) => slot switch {
            CharacterSlot.Head => Head, CharacterSlot.Chest => Chest, CharacterSlot.Legs => Legs,
            CharacterSlot.Feet => Feet, CharacterSlot.Accessory => Accessory, CharacterSlot.Tool => Tool, _ => "" };
        public void Set(CharacterSlot slot, string id)
        {
            switch (slot) {
                case CharacterSlot.Head: Head = id; break; case CharacterSlot.Chest: Chest = id; break;
                case CharacterSlot.Legs: Legs = id; break; case CharacterSlot.Feet: Feet = id; break;
                case CharacterSlot.Accessory: Accessory = id; break;
                case CharacterSlot.Tool: Tool = id; break;
            }
        }
        public static bool TryName(string value, out string name, out string error)
        {
            name = (value ?? "").Trim().Normalize(NormalizationForm.FormC);
            error = name.Length == 0 ? "Give your character a name." :
                name.Any(char.IsControl) ? "Use a name without line breaks or control characters." :
                new StringInfo(name).LengthInTextElements > 24 ? "Use a name of up to 24 characters." : null;
            return error == null;
        }
    }

    [Serializable]
    public sealed class EquipmentVisibility
    {
        public bool Helmet;
        public bool Chest = true, Boots = true;
        public bool Get(string slot) => slot switch { "Helmet" => Helmet, "Chest" => Chest, "Boots" => Boots, _ => false };
        public void Set(string slot, bool visible) { switch (slot) { case "Helmet": Helmet = visible; break; case "Chest": Chest = visible; break; case "Boots": Boots = visible; break; } }
        public int Key => (Helmet ? 1 : 0) | (Chest ? 2 : 0) | (Boots ? 4 : 0);
    }

    public static class CharacterCreation
    {
        public static CharacterProfile Current => Oracle.oracle?.saveData?.Character;
        public static bool Required => Oracle.oracle && Oracle.oracle.HasLoadedCurrentSlotData && Current is { Created: false };
    }
}
