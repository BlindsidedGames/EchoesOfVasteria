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
        public static bool IsDirty { get; private set; } = true;

        public static void Initialize(HeroController hero)
        {
            _hero = hero != null ? hero : HeroController.Instance;
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
            if (!isCrit)
                return s.damage;

            var critMultiplier = 1f + Mathf.Max(0f, s.critDamagePercent) / 100f;
            return s.damage * critMultiplier;
        }

        private static void ForceRecalculate()
        {
            Recalculate();
        }

                private static void Recalculate()
        {
            if (!_initialized)
            {
                _hero = _hero != null ? _hero : HeroController.Instance;
                _initialized = _hero != null;
                if (!_initialized)
                    return;
            }

            var hero = _hero;
            var buffs = BuffManager.Instance;
            var equip = EquipmentController.Instance;
            var crafting = CraftingService.Instance;

            var damageStat = BaseStatService.GetStat("Damage");
            var attackRateStat = BaseStatService.GetStat("Attack Rate");
            var moveSpeedStat = BaseStatService.GetStat("Move Speed");
            var defenseStat = BaseStatService.GetStat("Defense");
            var healthStat = BaseStatService.GetStat("Health");
            var regenStat = BaseStatService.GetStat("Regeneration");
            var critChanceStat = BaseStatService.GetStat("Crit Chance");
            var critDamageStat = BaseStatService.GetStat("Crit Damage");
            var newSnapshot = _cache; // start from previous and update only dirty fields
            var cauldron = CauldronManager.Instance;

            if ((_dirtyMask & DirtyMask.Damage) != 0)
            {
                var baseDamage = damageStat != null ? BaseStatService.GetTotalValue(damageStat) : 0f;
                var gearDamage = equip != null ? equip.GetTotalForMapping(HeroStatMapping.Damage) : 0f;
                float infDmg = cauldron != null ? cauldron.GetInfinityValueFor(HeroStatMapping.Damage, out var _) : 0f;
                var buffMult = buffs != null ? buffs.DamageMultiplier : 1f;
                newSnapshot.damage = (baseDamage + gearDamage + infDmg) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.AttackRate) != 0)
            {
                var baseAttack = attackRateStat != null ? BaseStatService.GetTotalValue(attackRateStat) : 0f;
                var gearAttack = equip != null ? equip.GetTotalForMapping(HeroStatMapping.AttackRate) : 0f;
                float infAps = cauldron != null ? cauldron.GetInfinityValueFor(HeroStatMapping.AttackRate, out var _) : 0f;
                var buffMult = buffs != null ? buffs.AttackSpeedMultiplier : 1f;
                newSnapshot.attacksPerSecond = (baseAttack + gearAttack + infAps) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.CritChance) != 0)
            {
                var critPercent = critChanceStat != null ? BaseStatService.GetTotalValue(critChanceStat) : 0f;
                if (equip != null && crafting != null)
                {
                    var critDef = crafting.GetStatByMapping(HeroStatMapping.CritChance);
                    if (critDef != null)
                    {
                        var raw = equip.GetCritChance(critDef);
                        critPercent += critDef.isPercent ? raw : raw * 100f;
                    }
                }

                if (buffs != null)
                    critPercent += Mathf.Max(0f, buffs.CritChancePercent);
                if (cauldron != null)
                {
                    float infCrit = cauldron.GetInfinityValueFor(HeroStatMapping.CritChance, out var isPct);
                    if (isPct) critPercent += Mathf.Max(0f, infCrit);
                    else critPercent += Mathf.Max(0f, infCrit * 100f);
                }

                newSnapshot.critChancePercent = Mathf.Clamp(critPercent, 0f, 100f);
            }

            if ((_dirtyMask & DirtyMask.CritDamage) != 0)
            {
                var critDamagePercent = critDamageStat != null ? BaseStatService.GetTotalValue(critDamageStat) : 0f;
                if (equip != null && crafting != null)
                {
                    var critDamageDef = crafting.GetStatByMapping(HeroStatMapping.CritDamage);
                    if (critDamageDef != null)
                    {
                        var raw = equip.GetTotalForMapping(HeroStatMapping.CritDamage);
                        critDamagePercent += critDamageDef.isPercent ? raw : raw * 100f;
                    }
                }

                if (buffs != null)
                    critDamagePercent += Mathf.Max(0f, buffs.CritDamagePercent);

                if (cauldron != null)
                {
                    float infCritDamage = cauldron.GetInfinityValueFor(HeroStatMapping.CritDamage, out var isPct);
                    if (isPct) critDamagePercent += Mathf.Max(0f, infCritDamage);
                    else critDamagePercent += Mathf.Max(0f, infCritDamage * 100f);
                }

                newSnapshot.critDamagePercent = Mathf.Max(0f, critDamagePercent);
            }

            if ((_dirtyMask & DirtyMask.Move) != 0)
            {
                var ratingBase = moveSpeedStat != null ? BaseStatService.GetTotalValue(moveSpeedStat) : 0f;
                var gearRating = equip != null ? equip.GetTotalForMapping(HeroStatMapping.MoveSpeed) : 0f;
                var craftingBonus = 0f;
                var movementRating = ratingBase + gearRating + craftingBonus;

                if (cauldron != null)
                {
                    float infMove = cauldron.GetInfinityValueFor(HeroStatMapping.MoveSpeed, out var isPct);
                    if (isPct) movementRating *= (1f + Mathf.Max(0f, infMove) / 100f);
                    else movementRating += Mathf.Max(0f, infMove);
                }

                const float BaseMoveSpeed = 3f;
                const float MoveBonusRange = 6f;
                const float MovementScalarN = 25f;

                var ratingClamped = Mathf.Max(0f, movementRating);
                var scale01 = ratingClamped / (ratingClamped + MovementScalarN);
                var preBuff = BaseMoveSpeed + MoveBonusRange * scale01;
                var buffMult = buffs != null ? buffs.MoveSpeedMultiplier : 1f;
                var finalSpeed = preBuff * buffMult;
                newSnapshot.movementSpeed = Mathf.Max(BaseMoveSpeed, finalSpeed);
            }

            if ((_dirtyMask & DirtyMask.Defense) != 0)
            {
                var baseDef = defenseStat != null ? BaseStatService.GetTotalValue(defenseStat) : 0f;
                var gearDef = equip != null ? equip.GetTotalForMapping(HeroStatMapping.Defense) : 0f;
                var buffMult = buffs != null ? buffs.DefenseMultiplier : 1f;
                float infDef = cauldron != null ? cauldron.GetInfinityValueFor(HeroStatMapping.Defense, out var _) : 0f;
                newSnapshot.defense = (baseDef + gearDef + Mathf.Max(0f, infDef)) * buffMult;
            }

            if ((_dirtyMask & DirtyMask.MaxHealth) != 0)
            {
                var baseHp = healthStat != null ? BaseStatService.GetTotalValue(healthStat) : 0f;
                var gearHp = equip != null ? equip.GetTotalForMapping(HeroStatMapping.MaxHealth) : 0f;
                float infHp = cauldron != null ? cauldron.GetInfinityValueFor(HeroStatMapping.MaxHealth, out var _) : 0f;
                newSnapshot.maxHealth = baseHp + gearHp + Mathf.Max(0f, infHp);
            }

            if ((_dirtyMask & DirtyMask.Regen) != 0)
            {
                var upgradeRegen = regenStat != null ? BaseStatService.GetTotalValue(regenStat) : 0f;
                var gearRegen = equip != null ? equip.GetTotalForMapping(HeroStatMapping.HealthRegen) : 0f;
                var regenMultiplier = buffs != null ? 1f + Mathf.Max(0f, buffs.HealthRegenPercent) / 100f : 1f;
                var baseRegen = upgradeRegen + gearRegen;
                if (cauldron != null)
                {
                    float infRegen = cauldron.GetInfinityValueFor(HeroStatMapping.HealthRegen, out var isPct);
                    if (isPct) baseRegen *= (1f + Mathf.Max(0f, infRegen) / 100f);
                    else baseRegen += Mathf.Max(0f, infRegen);
                }

                newSnapshot.healthRegenPerSecond = baseRegen * regenMultiplier;
            }

            newSnapshot.version = _version + 1;

            var changed = !_initialized ||
                          !Mathf.Approximately(_cache.damage, newSnapshot.damage) ||
                          !Mathf.Approximately(_cache.attacksPerSecond, newSnapshot.attacksPerSecond) ||
                          !Mathf.Approximately(_cache.critChancePercent, newSnapshot.critChancePercent) ||
                          !Mathf.Approximately(_cache.critDamagePercent, newSnapshot.critDamagePercent) ||
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

