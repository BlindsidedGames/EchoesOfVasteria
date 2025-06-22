using System;

namespace TimelessEchoes.Gear
{
    public enum GearSlot
    {
        Ring,
        Necklace,
        Brooch,
        Pocket
    }

    public enum GearRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Mythical
    }

    [Serializable]
    public class GearItem
    {
        public string name;
        public GearSlot slot;
        public GearRarity rarity;
        public int damage;
        public float attackSpeed;
        public int health;
        public int defense;
        public float moveSpeed;
    }
}