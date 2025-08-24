using UnityEngine;
using TimelessEchoes.Upgrades;
using Blindsided.SaveData;

namespace TimelessEchoes.Gear
{
    public class SalvageService : MonoBehaviour
    {
        public static SalvageService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public int Salvage(GearItem item, bool isAuto = false)
        {
            // Roll salvage drops using weights with optional extra slots from the core.
            if (item == null) return 0;
            var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
            if (rm == null) return 0;

            var drops = item.core != null ? item.core.salvageDrops : null;
            var extraChances = item.core != null ? item.core.salvageAdditionalLootChances : null;
            if (drops == null || drops.Count == 0)
                return 0;

            var results = DropResolver.RollDrops(drops, extraChances, 0f, ignoreQuest: true);
            int totalAwardedEntries = 0;
            rm.BeginBatch();
            try
            {
                foreach (var res in results)
                {
                    rm.Add(res.resource, res.count, trackStats: false);
                    totalAwardedEntries++;

                    // Record salvage yield per resource and total gained from salvage
                    var o = Blindsided.Oracle.oracle;
                    if (o != null && o.saveData != null && o.saveData.Forge != null && res.resource != null)
                    {
                        var forge = o.saveData.Forge;
                        var rname = res.resource.name;
                        if (!forge.ResourcesGainedFromSalvage.ContainsKey(rname))
                            forge.ResourcesGainedFromSalvage[rname] = 0;
                        forge.ResourcesGainedFromSalvage[rname] += res.count;
                        if (!forge.SalvageYieldPerResource.ContainsKey(rname))
                            forge.SalvageYieldPerResource[rname] =
                                new Blindsided.SaveData.GameData.ForgeStats.ResourceAgg();
                        var agg = forge.SalvageYieldPerResource[rname];
                        agg.count++;
                        agg.sum += res.count;
                    }
                }
            } 
            finally
            {
                rm.EndBatch();
            }

            // Persist awarded salvage to in-memory save (defer disk write) when anything was given
            if (totalAwardedEntries > 0)
            {
                // Update salvage counters
                var o = Blindsided.Oracle.oracle;
                if (o != null && o.saveData != null && o.saveData.Forge != null)
                {
                    var forge = o.saveData.Forge;
                    forge.TotalSalvaged++;
                    forge.SalvageItems++;
                    forge.SalvageEntries += totalAwardedEntries;
                    var rarityKey = item.rarity != null ? item.rarity.name : "(null)";
                    var coreKey = item.core != null ? item.core.name : "(null)";
                    if (!forge.SalvagesByRarity.ContainsKey(rarityKey)) forge.SalvagesByRarity[rarityKey] = 0;
                    forge.SalvagesByRarity[rarityKey]++;
                    if (!forge.SalvagesByCore.ContainsKey(coreKey)) forge.SalvagesByCore[coreKey] = 0;
                    forge.SalvagesByCore[coreKey]++;
                    if (!string.IsNullOrWhiteSpace(item.slot))
                    {
                        if (!forge.SalvagesBySlot.ContainsKey(item.slot)) forge.SalvagesBySlot[item.slot] = 0;
                        forge.SalvagesBySlot[item.slot]++;
                    }

                    // Also update best piece scores using the same scoring as upgrades
                    var crafting = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
                    var equipment = EquipmentController.Instance ?? FindFirstObjectByType<EquipmentController>();
                    if (crafting != null)
                    {
                        var current = equipment != null && !string.IsNullOrWhiteSpace(item.slot) ? equipment.GetEquipped(item.slot) : null;
                        float score = TimelessEchoes.Gear.UI.UpgradeEvaluator.ComputeUpgradeScore(crafting, item, current);
                        score = Mathf.Max(0f, score);
                        var slotKey = string.IsNullOrWhiteSpace(item.slot) ? "(null)" : item.slot;
                        if (!forge.BestPieceScoreBySlot.ContainsKey(slotKey) || score > forge.BestPieceScoreBySlot[slotKey])
                            forge.BestPieceScoreBySlot[slotKey] = score;
                        if (!forge.BestPieceScoreByCore.ContainsKey(coreKey) || score > forge.BestPieceScoreByCore[coreKey])
                            forge.BestPieceScoreByCore[coreKey] = score;
                        if (!forge.BestPieceScoreByRarity.ContainsKey(rarityKey) || score > forge.BestPieceScoreByRarity[rarityKey])
                            forge.BestPieceScoreByRarity[rarityKey] = score;
                    }
                }

                try
                {
                    Blindsided.EventHandler.SaveData();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"SaveData after salvage failed: {ex}");
                }
            }

            return totalAwardedEntries;
        }
    }
}


