using System.Collections.Generic;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Handles applying stat upgrades using resources.
    /// </summary>
    public class StatUpgradeController : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private List<StatUpgrade> upgrades = new();

        private Dictionary<StatUpgrade, int> levels = new();

        private void Awake()
        {
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        public int GetLevel(StatUpgrade upgrade)
        {
            return upgrade != null && levels.TryGetValue(upgrade, out var lvl) ? lvl : 0;
        }

        public bool CanUpgrade(StatUpgrade upgrade)
        {
            var threshold = GetThreshold(upgrade);
            if (threshold == null) return false;
            foreach (var req in threshold.requirements)
            {
                int lvl = GetLevel(upgrade);
                int cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;
                if (resourceManager != null && resourceManager.GetAmount(req.resource) < cost)
                    return false;
            }
            return true;
        }

        public bool ApplyUpgrade(StatUpgrade upgrade)
        {
            var threshold = GetThreshold(upgrade);
            if (threshold == null || !CanUpgrade(upgrade))
                return false;

            foreach (var req in threshold.requirements)
            {
                int lvl = GetLevel(upgrade);
                int cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;
                resourceManager?.Spend(req.resource, cost);
            }

            levels[upgrade] = GetLevel(upgrade) + 1;
            return true;
        }

        private StatUpgrade.Threshold GetThreshold(StatUpgrade upgrade)
        {
            if (upgrade == null) return null;
            int lvl = GetLevel(upgrade);
            foreach (var t in upgrade.thresholds)
            {
                if (lvl >= t.minLevel && lvl < t.maxLevel)
                    return t;
            }
            return null;
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new Dictionary<string, int>();
            foreach (var pair in levels)
            {
                if (pair.Key != null)
                    dict[pair.Key.name] = pair.Value;
            }
            oracle.saveData.UpgradeLevels = dict;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.UpgradeLevels ??= new Dictionary<string, int>();
            levels.Clear();
            foreach (var upgrade in upgrades)
            {
                if (upgrade == null) continue;
                oracle.saveData.UpgradeLevels.TryGetValue(upgrade.name, out var lvl);
                levels[upgrade] = lvl;
            }
        }
    }
}
