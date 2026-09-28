using System;
using TimelessEchoes.Gear;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Forge")]
    public sealed class ToolkitForgeDefinition : ScriptableObject
    {
        [Serializable] public sealed class GearArt { public string slot; public Sprite frame, unknown; public Sprite[] rarities; }
        public ForgeCatalog catalog;
        public ToolkitCauldronDefinition.SpriteAnimation portraitAnimation = new();
        public GearArt[] gear = Array.Empty<GearArt>();
        public Sprite[] coreIcons = Array.Empty<Sprite>();
        public Sprite panel, inset, slot, selector, button, arrowValid, arrowInvalid, xpTrack, xpFill, portrait, migratedHelmet;
        public Sprite toggleOn, toggleOff;
        public Sprite GearSprite(GearItem item, string slotName)
        {
            if (item != null && item.rarity == null && slotName == "Helmet" && migratedHelmet) return migratedHelmet;
            var entry = Array.Find(gear, x => x.slot == slotName);
            if (entry == null) return null;
            if (item?.rarity == null || entry.rarities.Length == 0) return entry.unknown;
            return entry.rarities[Mathf.Clamp(item.rarity.tierIndex, 0, entry.rarities.Length - 1)] ?? entry.unknown;
        }
    }
}
