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
    private const float MIGRATION_FACTOR = 0.70f; // apply 70% of previous upgrade value
    private void Awake()
    {
        OnLoadData += OnLoadDataHandler;
    }

    private void OnDestroy()
    {
        OnLoadData -= OnLoadDataHandler;
    }

    private void Start()
    {
        // Fallback: if we somehow missed the OnLoadData event due to timing, attempt once on Start
        var data = oracle?.saveData;
        if (data != null && !data.StatUpgradesMigratedToGear)
            OnLoadDataHandler();
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

        // Prefer the controller's serialized list so we don't rely on Resources placement
        StatUpgrade[] allUpgrades;
        var ctrlForList = TimelessEchoes.Upgrades.StatUpgradeController.Instance;
        if (ctrlForList != null && ctrlForList.AllUpgrades != null)
            allUpgrades = ctrlForList.AllUpgrades.Where(u => u != null).ToArray();
        else
            allUpgrades = AssetCache.GetAll<StatUpgrade>("");
        var allStats = AssetCache.GetAll<StatDefSO>("");
        var keys = saveData.UpgradeLevels.Keys.ToList();
        bool hadAnyLevels = false;
        foreach (var key in keys)
        {
            var level = saveData.UpgradeLevels[key];
            if (level <= 0) continue;
            hadAnyLevels = true;
            var upgrade = allUpgrades?.FirstOrDefault(u => u != null && u.name == key);
            if (upgrade == null) continue;

            // Prefer explicit association; fall back to id/name match if missing.
            var stat = upgrade.associatedStat
                       ?? allStats?.FirstOrDefault(s => s != null && (s.id == key || s.name == key));
            if (stat == null) continue;

            float bonus = level * upgrade.statIncreasePerLevel * MIGRATION_FACTOR;
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

            // Ensure equipped items are reloaded immediately in-memory so bonuses apply now
            var equip = EquipmentController.Instance;
            if (equip != null)
            {
                var me = typeof(EquipmentController).GetMethod("LoadState", BindingFlags.Instance | BindingFlags.NonPublic);
                me?.Invoke(equip, null);
            }
            // Mark migrated when the helmet was actually created
            saveData.StatUpgradesMigratedToGear = true;
        }
        else if (!hadAnyLevels)
        {
            // Nothing to migrate; mark as done so we don't run again
            saveData.StatUpgradesMigratedToGear = true;
        }
        else
        {
            // There were levels but we couldn't map any to stats; mark migrated to avoid re-running.
            // Log a hint for debugging (likely missing associatedStat or assets not in Resources).
            Debug.LogWarning("StatUpgradeGearMigrator: Found upgrade levels but could not create any affixes. Verify associatedStat is set for all upgrades or stat IDs match.");
            saveData.StatUpgradesMigratedToGear = true;
        }
    }
}
