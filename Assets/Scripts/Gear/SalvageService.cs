using UnityEngine;
using TimelessEchoes.Upgrades;

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

        public int Salvage(GearItem item)
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
            foreach (var res in results)
            {
                rm.Add(res.resource, res.count, trackStats: false);
                totalAwardedEntries++;
            }

            // Persist awarded salvage to in-memory save (defer disk write) when anything was given
            if (totalAwardedEntries > 0)
            {
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


