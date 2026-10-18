using Blindsided;
using TimelessEchoes.Gear;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.Characters
{
    // A shared cached result keeps inventory, portraits, the hero and pooled Echoes in sync.
    public static class CharacterEquipmentAppearance
    {
        private static CharacterProfile lastBase, resolved;
        private static EquipmentController lastEquipment;
        private static GearItem lastHelmet, lastChest, lastBoots, lastWeapon;
        private static int lastKey;

        public static EquipmentVisibility Visibility => Oracle.oracle?.saveData == null ? new EquipmentVisibility() :
            Oracle.oracle.saveData.EquipmentVisibility ??= new EquipmentVisibility();

        public static CharacterProfile Resolve(CharacterProfile character)
        {
            if (character == null || !character.Created) return character;
            var equipment = EquipmentController.Instance;
            var helmet = equipment ? equipment.GetEquipped("Helmet") : null;
            var chest = equipment ? equipment.GetEquipped("Chest") : null;
            var boots = equipment ? equipment.GetEquipped("Boots") : null;
            var weapon = equipment ? equipment.GetEquipped("Weapon") : null;
            if (!character.Modular && helmet == null && chest == null && boots == null && weapon == null) return character;
            var visibility = Visibility;
            int Tier(GearItem item) => item?.rarity ? item.rarity.tierIndex + 1 : 0;
            var key = visibility.Key | (Tier(helmet) << 4) | (Tier(chest) << 8) | (Tier(boots) << 12) | (Tier(weapon) << 16);
            if (lastBase == character && lastEquipment == equipment && lastHelmet == helmet && lastChest == chest &&
                lastBoots == boots && lastWeapon == weapon && lastKey == key && resolved != null) return resolved;
            var catalog = CharacterCatalog.Load(); if (!catalog) return character;
            lastBase = character; lastEquipment = equipment; lastHelmet = helmet; lastChest = chest;
            lastBoots = boots; lastWeapon = weapon; lastKey = key;
            resolved = character.Modular ? character.Copy() : catalog.defaults.Copy();
            resolved.Name = character.Name; resolved.Created = resolved.Modular = true;
            CharacterCatalog.GearVisual Art(GearItem item) => item?.rarity && catalog.gear != null &&
                item.rarity.tierIndex >= 0 && item.rarity.tierIndex < catalog.gear.Length ? catalog.gear[item.rarity.tierIndex] : null;
            var headArt = visibility.Helmet ? Art(helmet) : null;
            var chestArt = visibility.Chest ? Art(chest) : null;
            var bootsArt = visibility.Boots ? Art(boots) : null;
            if (!string.IsNullOrEmpty(headArt?.helmet)) resolved.Head = headArt.helmet;
            if (!string.IsNullOrEmpty(chestArt?.chest)) { resolved.Chest = chestArt.chest; resolved.Legs = chestArt.legs; }
            if (!string.IsNullOrEmpty(bootsArt?.boots)) resolved.Feet = bootsArt.boots;
            if (visibility.Boots && boots != null && QuestEquipmentReward.GetIcon(boots)) resolved.Feet = "Feet/Shoes_1_Brown";
            resolved.Tool = Art(weapon) is { } weaponArt ? weaponArt.tool : "Tool/Unarmed";
            return resolved;
        }
    }
}
