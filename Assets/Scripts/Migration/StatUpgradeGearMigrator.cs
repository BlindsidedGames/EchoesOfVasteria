using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Buffs;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

[DefaultExecutionOrder(-2)]
public class StatUpgradeGearMigrator : MonoBehaviour
{
    private const float MIGRATION_FACTOR = 0.70f; // apply 70% of previous upgrade value
    [Header("Retro Quest Rewards")]
    [SerializeField]
    private List<QuestData> RetroRewardQuests = new();

    [SerializeField]
    private List<string> RetroRewardQuestIds = new();
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
        if (data != null)
            TryApplyRetroQuestRewards(data);
        if (data != null && !data.StatUpgradesMigratedToGear)
            OnLoadDataHandler();
    }

    private void OnLoadDataHandler()
    {
        var saveData = oracle?.saveData;
        if (saveData == null)
            return;

        // Always attempt retro quest reward application once per save
        TryApplyRetroQuestRewards(saveData);

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

    private void TryApplyRetroQuestRewards(GameData saveData)
    {
        if (saveData == null)
            return;

        saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
        saveData.RetroQuestRewardsApplied ??= new HashSet<string>();

        if (saveData.RetroQuestRewardsPassRan)
            return;

        // Build the set of quest IDs to check
        var targetIds = new HashSet<string>();
        if (RetroRewardQuests != null)
            foreach (var q in RetroRewardQuests)
                if (q != null && !string.IsNullOrEmpty(q.questId))
                    targetIds.Add(q.questId);
        if (RetroRewardQuestIds != null)
            foreach (var id in RetroRewardQuestIds)
                if (!string.IsNullOrEmpty(id))
                    targetIds.Add(id);

        if (targetIds.Count == 0)
            return;

        List<QuestData> allQuestAssets = null; // lazy cache

        foreach (var id in targetIds)
        {
            if (!saveData.Quests.TryGetValue(id, out var rec) || rec == null || !rec.Completed)
                continue;
            if (saveData.RetroQuestRewardsApplied.Contains(id))
                continue;

            // Prefer directly referenced asset; otherwise look it up by id
            var quest = RetroRewardQuests != null ? RetroRewardQuests.Find(q => q != null && q.questId == id) : null;
            if (quest == null)
                quest = FindQuestById(id, ref allQuestAssets);
            if (quest == null)
                continue;

            // Apply resource rewards
            if (quest.rewards != null)
            {
                foreach (var reward in quest.rewards)
                {
                    if (reward.resource == null || reward.amount <= 0)
                        continue;
                    var rm = ResourceManager.Instance;
                    if (rm != null)
                    {
                        rm.Add(reward.resource, reward.amount, trackStats: false);
                    }
                    else
                    {
                        saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
                        var name = reward.resource.name;
                        if (!saveData.Resources.TryGetValue(name, out var entry))
                        {
                            entry = new GameData.ResourceEntry { Earned = true, Amount = 0, BestPerMinute = 0 };
                            saveData.Resources[name] = entry;
                        }
                        entry.Earned = true;
                        entry.Amount += reward.amount;

                        // Update cumulative resource stats so UI remains consistent
                        saveData.ResourceStats ??= new Dictionary<string, GameData.ResourceRecord>();
                        if (!saveData.ResourceStats.TryGetValue(name, out var stat))
                        {
                            stat = new GameData.ResourceRecord();
                            saveData.ResourceStats[name] = stat;
                        }
                        stat.TotalReceived += Mathf.RoundToInt((float)reward.amount);
                    }
                }
            }

            // Apply unlocks and other non-resource effects
            if (quest.unlockBuffSlots > 0)
            {
                var bm = BuffManager.Instance;
                if (bm != null)
                    bm.UnlockSlots(quest.unlockBuffSlots);
                else
                {
                    var newCount = Mathf.Clamp(saveData.UnlockedBuffSlots + quest.unlockBuffSlots, 1, 5);
                    saveData.UnlockedBuffSlots = newCount;
                }
            }

            if (quest.unlockAutoBuffSlots > 0)
            {
                var bm = BuffManager.Instance;
                if (bm != null)
                    bm.UnlockAutoSlots(quest.unlockAutoBuffSlots);
                else
                {
                    var oldCount = Mathf.Clamp(saveData.UnlockedAutoBuffSlots, 0, 5);
                    var newCount = Mathf.Clamp(oldCount + quest.unlockAutoBuffSlots, 0, 5);
                    saveData.UnlockedAutoBuffSlots = newCount;
                    saveData.AutoBuffSlots ??= new List<bool> { false, false, false, false, false };
                    while (saveData.AutoBuffSlots.Count < 5)
                        saveData.AutoBuffSlots.Add(false);
                    for (var i = oldCount; i < newCount; i++)
                        saveData.AutoBuffSlots[i] = true;
                }
            }

            if (quest.maxDistanceIncrease > 0f)
            {
                var tracker = GameplayStatTracker.Instance;
                if (tracker != null)
                    tracker.IncreaseMaxRunDistance(quest.maxDistanceIncrease);
                else
                {
                    saveData.General ??= new GameData.GeneralStats();
                    saveData.General.MaxRunDistance = (saveData.General.MaxRunDistance > 0f)
                        ? saveData.General.MaxRunDistance + quest.maxDistanceIncrease
                        : Mathf.Max(50f, quest.maxDistanceIncrease);
                }
            }

            if (quest.disciplePercentReward > 0f)
            {
                saveData.DisciplePercent += quest.disciplePercentReward;
                DiscipleGenerationManager.Instance?.RefreshRates();
            }

            saveData.RetroQuestRewardsApplied.Add(id);
        }

        saveData.RetroQuestRewardsPassRan = true;
        try { SaveData(); }
        catch (System.Exception ex) { Debug.LogError($"Retro quest rewards save failed: {ex}"); }
    }

    private QuestData FindQuestById(string id, ref List<QuestData> cache)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        if (cache == null)
            cache = new List<QuestData>(AssetCache.GetAll<QuestData>("Quests"));
        foreach (var q in cache)
            if (q != null && q.questId == id)
                return q;
        return null;
    }
}
