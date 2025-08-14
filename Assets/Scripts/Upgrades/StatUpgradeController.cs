using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Skills;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Handles applying stat upgrades using resources.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class StatUpgradeController : MonoBehaviour
    {
        public static StatUpgradeController Instance { get; private set; }
        [SerializeField] private List<StatUpgrade> upgrades = new();

        private readonly Dictionary<StatUpgrade, int> levels = new();

        /// <summary>
        ///     Exposes the list of upgrades managed by this controller.
        /// </summary>
        public IEnumerable<StatUpgrade> AllUpgrades => upgrades;

        private void Awake()
        {
            Instance = this;
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        public int GetLevel(StatUpgrade upgrade)
        {
            if (UpgradeFeatureToggle.DisableStatUpgrades)
                return 0;
            return upgrade != null && levels.TryGetValue(upgrade, out var lvl) ? lvl : 0;
        }

        /// <summary>
        ///     Returns the total additive increase for the given upgrade.
        /// </summary>
        public float GetIncrease(StatUpgrade upgrade)
        {
            if (upgrade == null) return 0f;
            if (UpgradeFeatureToggle.DisableStatUpgrades)
                return 0f;
            var lvl = GetLevel(upgrade);
            return lvl * upgrade.statIncreasePerLevel;
        }

        /// <summary>
        ///     Returns the base value for the given upgrade.
        /// </summary>
        public float GetBaseValue(StatUpgrade upgrade)
        {
            return upgrade != null ? upgrade.baseValue : 0f;
        }

        /// <summary>
        ///     Returns a multiplier for legacy usages of stat upgrades.
        /// </summary>
        public float GetMultiplier(StatUpgrade upgrade)
        {
            return 1f + GetIncrease(upgrade);
        }

        /// <summary>
        ///     Calculates the total value for a stat including flat and percent bonuses.
        /// </summary>
        public float GetTotalValue(StatUpgrade upgrade)
        {
            if (upgrade == null) return 0f;

            int lvl = UpgradeFeatureToggle.DisableStatUpgrades ? 0 : GetLevel(upgrade);
            float baseVal = GetBaseValue(upgrade);
            float levelIncrease = lvl * upgrade.statIncreasePerLevel;

            var skillCtrl = SkillController.Instance;
            float flat = skillCtrl ? skillCtrl.GetFlatStatBonus(upgrade) : 0f;
            float percent = skillCtrl ? skillCtrl.GetPercentStatBonus(upgrade) : 0f;

            float totalBeforePercent = baseVal + levelIncrease + flat;
            return totalBeforePercent * (1f + percent);
        }

        public bool CanUpgrade(StatUpgrade upgrade)
        {
            if (UpgradeFeatureToggle.DisableStatUpgrades)
                return false;
            var threshold = GetThreshold(upgrade);
            if (threshold == null) return false;
            foreach (var req in threshold.requirements)
            {
                var lvl = GetLevel(upgrade);
                var cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;
                var manager = ResourceManager.Instance;
                if (manager != null && manager.GetAmount(req.resource) < cost)
                    return false;
            }

            return true;
        }

        public bool ApplyUpgrade(StatUpgrade upgrade)
        {
            if (UpgradeFeatureToggle.DisableStatUpgrades)
                return false;
            var threshold = GetThreshold(upgrade);
            if (threshold == null || !CanUpgrade(upgrade))
                return false;

            foreach (var req in threshold.requirements)
            {
                var lvl = GetLevel(upgrade);
                var cost = req.amount + Mathf.Max(0, lvl - threshold.minLevel) * req.amountIncreasePerLevel;
                ResourceManager.Instance?.Spend(req.resource, cost);
            }

            levels[upgrade] = GetLevel(upgrade) + 1;
            Log($"Upgraded {upgrade.name} to level {levels[upgrade]}", TELogCategory.Upgrade, this);
            return true;
        }

        private StatUpgrade.Threshold GetThreshold(StatUpgrade upgrade)
        {
            if (upgrade == null) return null;
            var lvl = GetLevel(upgrade);
            foreach (var t in upgrade.thresholds)
                if (lvl >= t.minLevel && lvl < t.maxLevel)
                    return t;
            return null;
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new Dictionary<string, int>();
            foreach (var pair in levels)
                if (pair.Key != null)
                    dict[pair.Key.name] = pair.Value;
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