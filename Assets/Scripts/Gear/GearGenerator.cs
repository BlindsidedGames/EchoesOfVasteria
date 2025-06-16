using UnityEngine;

public static class GearGenerator
{
    public static GearItem Generate(int enemyLevel)
    {
        var slot = (GearSlot)Random.Range(0, 4);
        var rarity = RollRarity(enemyLevel);
        var item = new GearItem
        {
            slot = slot,
            rarity = rarity,
            name = $"{rarity} {slot}"
        };
        int statCount = GetStatCount(rarity);
        for (int i = 0; i < statCount; i++)
        {
            AddRandomStat(item, enemyLevel, rarity);
        }
        return item;
    }

    private static void AddRandomStat(GearItem item, int level, GearRarity rarity)
    {
        int stat = Random.Range(0, 5);
        float mult = 1f + (int)rarity * 0.5f;
        switch (stat)
        {
            case 0: if (item.damage == 0) item.damage = Mathf.CeilToInt(level * mult); break;
            case 1: if (item.attackSpeed == 0) item.attackSpeed = level * 0.05f * mult; break;
            case 2: if (item.health == 0) item.health = Mathf.CeilToInt(level * 2f * mult); break;
            case 3: if (item.defense == 0) item.defense = Mathf.CeilToInt(level * mult * 0.5f); break;
            case 4: if (item.moveSpeed == 0) item.moveSpeed = level * 0.1f * mult; break;
        }
    }

    private static GearRarity RollRarity(int level)
    {
        float r = Random.value + level * 0.05f;
        if (r > 0.9f) return GearRarity.Mythical;
        if (r > 0.75f) return GearRarity.Epic;
        if (r > 0.55f) return GearRarity.Rare;
        if (r > 0.3f) return GearRarity.Uncommon;
        return GearRarity.Common;
    }

    private static int GetStatCount(GearRarity r)
    {
        return r switch
        {
            GearRarity.Common => 1,
            GearRarity.Uncommon => 2,
            GearRarity.Rare => 3,
            GearRarity.Epic => 4,
            _ => 5,
        };
    }
}
