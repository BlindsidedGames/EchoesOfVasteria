using System;
using System.Linq;
using TimelessEchoes.Buffs;
using TimelessEchoes.Gear;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Centralized, cached stat calculator for the hero. Computes values on demand
    ///     when marked dirty and raises an event for UI to refresh.
    /// </summary>
    public static class HeroStatSystem
    {
        public static event Action<HeroStatsSnapshot> OnStatsRecalculated;

        private static bool _initialized;
        private static DirtyMask _dirtyMask = DirtyMask.All;
        private static long _version;
        private static HeroController _hero;
        private static HeroStatsSnapshot _cache;
        private const float BaseCritChancePercent = 1f; // Global baseline crit chance

        public static bool IsDirty { get; private set; } = true;

        public static void Initialize(HeroController hero)
        {
            _hero = hero != null ? hero : HeroController.Instance ?? Object.FindFirstObjectByType<HeroController>();
            _initialized = _hero != null;
            MarkDirty(DirtyMask.All, DirtyReason.Load);
            ForceRecalculate();
        }

        public static void ForceRunStartRefresh()
        {
            ForceRecalculate();
        }

        public static void MarkDirty(DirtyMask mask = DirtyMask.All, DirtyReason reason = DirtyReason.Unspecified)
        {
            IsDirty = true;
            _dirtyMask |= mask;
#if UNITY_EDITOR
            _lastDirtyReason = reason;
#endif
        }

        public static HeroStatsSnapshot GetSnapshot()
        {
            if (IsDirty)
                Recalculate();
            return _cache;
        }

        public static float GetDamage(bool isCrit = false)
        {
            var s = GetSnapshot();
            return isCrit ? s.damage * 2f : s.damage;
        }

        private static void ForceRecalculate()
        {
            Recalculate();
        }

        private static void Recalculate()
        {
            if (!_initialized)
            {
                _hero = _hero != null
                    ? _hero
                    : HeroController.Instance ?? Object.FindFirstObjectByType<HeroController>();
                _initialized = _hero != null;
                if (!_initialized)
                    return;
            }

            var hero = _hero;
            var buffs = BuffManager.Instance ?? Object.FindFirstObjectByType<BuffManager>();
            var equip = EquipmentController.Instance ?? Object.FindFirstObjectByType<EquipmentController>();
            var crafting = CraftingService.Instance ?? Object.FindFirstObjectByType<CraftingService>();
            var upgrades = StatUpgradeController.Instance ?? Object.FindFirstObjectByType<StatUpgradeController>();
            var skills = SkillController.Instance ?? Object.FindFirstObjectByType<SkillController>();

            var newSnapshot = _cache; // start from previous and update only dirty fields

            if ((_dirtyMask & DirtyMask.Damage) != 0)
            {
                // Base from upgrades/skills
                var baseDamage = 0f;
                if (upgrades != null && upgrades.AllUpgrades != null)
                {
                    var up = upgrades.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Damage");
                    if (up != null)
                    {
                        var baseVal = upgrades.GetBaseValue(up);
                        var levelIncrease = upgrades.GetIncrease(up);
                        var flat = skills != null ? skills.GetFlatStatBonus(up) : 0f;
                        var percent = skills != null ? skills.GetPercentStatBonus(up) : 0f;
                        baseDamage = (baseVal + levelIncrease + flat) * (1f + percent);
                    }
                }

                // Gear
                var gearDamage = equip != null ? equip.GetTotalForMapping(HeroStatMapping.Damage) : 0f;
                // Buff multiplier
                var buffMult = buffs != null ? buffs.DamageMultiplier : 1f;
                newSnapshot.damage = (baseDamage + gearDamage) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.AttackRate) != 0)
            {
                var baseAttack = 0f;
                if (upgrades != null && upgrades.AllUpgrades != null)
                {
                    var up = upgrades.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Attack Rate");
                    if (up != null)
                    {
                        var baseVal = upgrades.GetBaseValue(up);
                        var levelIncrease = upgrades.GetIncrease(up);
                        var flat = skills != null ? skills.GetFlatStatBonus(up) : 0f;
                        var percent = skills != null ? skills.GetPercentStatBonus(up) : 0f;
                        baseAttack = (baseVal + levelIncrease + flat) * (1f + percent);
                    }
                }

                var gearAttack = equip != null ? equip.GetTotalForMapping(HeroStatMapping.AttackRate) : 0f;
                var buffMult = buffs != null ? buffs.AttackSpeedMultiplier : 1f;
                newSnapshot.attacksPerSecond = (baseAttack + gearAttack) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.CritChance) != 0)
            {
                var critPercent = 0f;
                if (equip != null && crafting != null)
                {
                    var critDef = crafting.GetStatByMapping(HeroStatMapping.CritChance);
                    if (critDef != null)
                    {
                        var raw = equip.GetCritChance(critDef);
                        critPercent = critDef.isPercent ? raw : raw * 100f;
                    }
                }

                if (buffs != null)
                    critPercent += Mathf.Max(0f, buffs.CritChancePercent);
                // Apply global baseline
                critPercent += BaseCritChancePercent;
                newSnapshot.critChancePercent = Mathf.Clamp(critPercent, 0f, 100f);
            }

            if ((_dirtyMask & DirtyMask.Move) != 0)
            {
                var baseMove = 0f;
                if (upgrades != null && upgrades.AllUpgrades != null)
                {
                    var up = upgrades.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Move Speed");
                    if (up != null)
                    {
                        var baseVal = upgrades.GetBaseValue(up);
                        var levelIncrease = upgrades.GetIncrease(up);
                        var flat = skills != null ? skills.GetFlatStatBonus(up) : 0f;
                        var percent = skills != null ? skills.GetPercentStatBonus(up) : 0f;
                        baseMove = (baseVal + levelIncrease + flat) * (1f + percent);
                    }
                }

                var gearMove = equip != null ? equip.GetTotalForMapping(HeroStatMapping.MoveSpeed) : 0f;
                var buffMult = buffs != null ? buffs.MoveSpeedMultiplier : 1f;
                newSnapshot.movementSpeed = (baseMove + gearMove) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.Defense) != 0)
            {
                var baseDef = 0f;
                if (upgrades != null && upgrades.AllUpgrades != null)
                {
                    var up = upgrades.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Defense");
                    if (up != null)
                    {
                        var baseVal = upgrades.GetBaseValue(up);
                        var levelIncrease = upgrades.GetIncrease(up);
                        var flat = skills != null ? skills.GetFlatStatBonus(up) : 0f;
                        var percent = skills != null ? skills.GetPercentStatBonus(up) : 0f;
                        baseDef = (baseVal + levelIncrease + flat) * (1f + percent);
                    }
                }

                var gearDef = equip != null ? equip.GetTotalForMapping(HeroStatMapping.Defense) : 0f;
                var buffMult = buffs != null ? buffs.DefenseMultiplier : 1f;
                newSnapshot.defense = (baseDef + gearDef) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.MaxHealth) != 0)
            {
                var baseHp = 0f;
                if (upgrades != null && upgrades.AllUpgrades != null)
                {
                    var up = upgrades.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Health");
                    if (up != null)
                    {
                        var baseVal = upgrades.GetBaseValue(up);
                        var levelIncrease = upgrades.GetIncrease(up);
                        var flat = skills != null ? skills.GetFlatStatBonus(up) : 0f;
                        var percent = skills != null ? skills.GetPercentStatBonus(up) : 0f;
                        baseHp = (baseVal + levelIncrease + flat) * (1f + percent);
                    }
                }

                var gearHp = equip != null ? equip.GetTotalForMapping(HeroStatMapping.MaxHealth) : 0f;
                newSnapshot.maxHealth = baseHp + gearHp;
            }

            if ((_dirtyMask & DirtyMask.Regen) != 0)
            {
                var regenPerSec = 0f;
                if (upgrades != null)
                {
                    var regenUpgrade = upgrades.AllUpgrades != null
                        ? upgrades.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Regeneration")
                        : null;
                    var upgradeRegen = upgrades != null && regenUpgrade != null
                        ? upgrades.GetTotalValue(regenUpgrade)
                        : 0f;
                    var gearRegen = equip != null ? equip.GetTotalForMapping(HeroStatMapping.HealthRegen) : 0f;
                    var regenMultiplier = buffs != null ? 1f + Mathf.Max(0f, buffs.HealthRegenPercent) / 100f : 1f;
                    regenPerSec = (upgradeRegen + gearRegen) * regenMultiplier;
                }

                newSnapshot.healthRegenPerSecond = regenPerSec;
            }

            newSnapshot.version = _version + 1;

            // Health max/current adjustment is handled where max health changes are applied
            // (e.g., in upgrade/equipment flows) to avoid double-applying deltas.

            // Suppress event if values didn't change
            var changed = !_initialized ||
                          !Mathf.Approximately(_cache.damage, newSnapshot.damage) ||
                          !Mathf.Approximately(_cache.attacksPerSecond, newSnapshot.attacksPerSecond) ||
                          !Mathf.Approximately(_cache.critChancePercent, newSnapshot.critChancePercent) ||
                          !Mathf.Approximately(_cache.maxHealth, newSnapshot.maxHealth) ||
                          !Mathf.Approximately(_cache.healthRegenPerSecond, newSnapshot.healthRegenPerSecond) ||
                          !Mathf.Approximately(_cache.movementSpeed, newSnapshot.movementSpeed) ||
                          !Mathf.Approximately(_cache.defense, newSnapshot.defense);

            _cache = newSnapshot;
            _version = newSnapshot.version;
            IsDirty = false;
            _dirtyMask = DirtyMask.None;

            if (changed && OnStatsRecalculated != null)
                OnStatsRecalculated.Invoke(_cache);
        }

#if UNITY_EDITOR
        private static DirtyReason _lastDirtyReason;
#endif
    }
}