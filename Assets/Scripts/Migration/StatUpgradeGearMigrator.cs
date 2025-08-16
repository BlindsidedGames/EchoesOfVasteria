using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

[DefaultExecutionOrder(-2)]
public class StatUpgradeGearMigrator : MonoBehaviour
{
    private void Awake()
    {
        OnLoadData += OnLoadDataHandler;
    }

    private void OnDestroy()
    {
        OnLoadData -= OnLoadDataHandler;
    }

    private void OnLoadDataHandler()
    {
        var saveData = oracle?.saveData;
        if (saveData == null)
            return;
        if (saveData.StatUpgradesMigratedToGear)
            return;

        saveData.EquipmentBySlot ??= new Dictionary<string, GearItemRecord>();
        if (saveData.EquipmentBySlot.Count > 0)
        {
            saveData.StatUpgradesMigratedToGear = true;
            return;
        }

        saveData.UpgradeLevels ??= new Dictionary<string, int>();
        var gear = new GearItemRecord { slot = "Helmet", rarity = null, affixes = new List<GearAffixRecord>() };

        var allUpgrades = AssetCache.GetAll<StatUpgrade>("");
        var allStats = AssetCache.GetAll<StatDefSO>("");
        var keys = saveData.UpgradeLevels.Keys.ToList();
        foreach (var key in keys)
        {
            var level = saveData.UpgradeLevels[key];
            if (level <= 0) continue;
            var upgrade = allUpgrades?.FirstOrDefault(u => u != null && u.name == key);
            if (upgrade == null) continue;
            var stat = allStats?.FirstOrDefault(s => s != null && s.name == key);
            if (stat == null) continue;
            float bonus = level * upgrade.statIncreasePerLevel;
            gear.affixes.Add(new GearAffixRecord { statId = stat.id ?? stat.name, value = bonus });
            saveData.UpgradeLevels[key] = 0;
        }

        if (gear.affixes.Count > 0)
        {
            saveData.EquipmentBySlot["Helmet"] = gear;
            var ctrl = StatUpgradeController.Instance;
            if (ctrl != null)
            {
                var m = typeof(StatUpgradeController).GetMethod("LoadState", BindingFlags.Instance | BindingFlags.NonPublic);
                m?.Invoke(ctrl, null);
            }
        }

        saveData.StatUpgradesMigratedToGear = true;
    }
}
