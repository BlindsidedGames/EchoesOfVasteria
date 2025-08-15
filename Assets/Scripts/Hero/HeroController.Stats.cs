#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using UnityEngine;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Hero
{
    public partial class HeroController
    {
        /// <summary>
        ///     Current attack damage after upgrades, buffs and dice multipliers.
        /// </summary>
        public float Damage =>
            (baseDamage + damageBonus + gearDamageBonus) *
            (buffController != null ? buffController.DamageMultiplier : 1f) *
            combatDamageMultiplier;

        /// <summary>
        ///     Base attack damage after permanent upgrades and gear (before buffs and dice multipliers).
        /// </summary>
        public float BaseDamage => baseDamage + damageBonus + gearDamageBonus;

        /// <summary>
        ///     Current attacks per second after upgrades and buffs.
        /// </summary>
        public float AttackRate => CurrentAttackRate;

        /// <summary>
        ///     Current movement speed after upgrades and buffs.
        /// </summary>
        public float MoveSpeed =>
            (baseMoveSpeed + moveSpeedBonus + gearMoveSpeedBonus) *
            (buffController != null ? buffController.MoveSpeedMultiplier : 1f);

        /// <summary>
        ///     Maximum health after upgrades.
        /// </summary>
        public float MaxHealthValue => baseHealth + healthBonus + gearHealthBonus;

        private float CurrentAttackRate =>
            (baseAttackSpeed + attackSpeedBonus + gearAttackSpeedBonus) *
            (buffController != null ? buffController.AttackSpeedMultiplier : 1f);

        public float Defense =>
            (baseDefense + defenseBonus + gearDefenseBonus) *
            (buffController != null ? buffController.DefenseMultiplier : 1f);

        private void OnMilestoneUnlocked(Skill skill, MilestoneBonus milestone)
        {
            if (milestone != null && milestone.type == MilestoneType.StatIncrease)
            {
                var oldMax = health != null ? health.MaxHealth : 0f;
                var oldCurrent = health != null ? health.CurrentHealth : 0f;
                ApplyStatUpgrades();

                if (health != null)
                {
                    var newMax = Mathf.RoundToInt(baseHealth + healthBonus);
                    if (newMax > 0 && Mathf.Abs(newMax - oldMax) > 0.01f)
                    {
                        var newCurrent = Mathf.Min(oldCurrent + (newMax - oldMax), newMax);
                        health?.Init(newMax);
                        if (newCurrent < newMax && health != null)
                            health.TakeDamage(newMax - newCurrent);
                    }
                }
            }
        }

        private void ApplyStatUpgrades()
        {
            var controller = StatUpgradeController.Instance;
            if (controller == null)
                Log("StatUpgradeController missing", TELogCategory.Upgrade, this);
            var skillController = SkillController.Instance;
            if (skillController == null)
                Log("SkillController missing", TELogCategory.Upgrade, this);
            if (controller == null) return;

            foreach (var upgrade in controller.AllUpgrades)
            {
                if (upgrade == null) continue;
                var baseVal = controller.GetBaseValue(upgrade);
                var levelIncrease = UpgradeFeatureToggle.DisableStatUpgrades ? 0f : controller.GetIncrease(upgrade);
                var flatBonus = skillController ? skillController.GetFlatStatBonus(upgrade) : 0f;
                var percentBonus = skillController ? skillController.GetPercentStatBonus(upgrade) : 0f;

                var totalBeforePercent = baseVal + levelIncrease + flatBonus;
                var finalValue = totalBeforePercent * (1f + percentBonus);
                var increase = finalValue - baseVal;
                switch (upgrade.name)
                {
                    case "Health":
                        baseHealth = baseVal;
                        healthBonus = increase;
                        break;
                    case "Damage":
                        baseDamage = baseVal;
                        damageBonus = increase;
                        break;
                    case "Attack Rate":
                        baseAttackSpeed = baseVal;
                        attackSpeedBonus = increase;
                        break;
                    case "Move Speed":
                        baseMoveSpeed = baseVal;
                        moveSpeedBonus = increase;
                        break;
                    case "Defense":
                        baseDefense = baseVal;
                        defenseBonus = increase;
                        break;
                }
            }
        }

        private void RecalculateGearBonuses()
        {
            var equip = TimelessEchoes.Gear.EquipmentController.Instance ??
                        FindFirstObjectByType<TimelessEchoes.Gear.EquipmentController>();
            if (equip == null)
            {
                gearDamageBonus = gearAttackSpeedBonus = gearDefenseBonus = gearHealthBonus = gearMoveSpeedBonus = 0f;
                return;
            }

            gearDamageBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.Damage);
            gearAttackSpeedBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.AttackRate);
            gearDefenseBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.Defense);
            gearHealthBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.MaxHealth);
            gearMoveSpeedBonus = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.MoveSpeed);

            // If MaxHealth changes, re-init health so UI reflects new max
            if (health != null)
            {
                var oldMax = Mathf.RoundToInt(health.MaxHealth);
                var current = Mathf.RoundToInt(health.CurrentHealth);
                var newMax = Mathf.RoundToInt(baseHealth + healthBonus + gearHealthBonus);
                if (Mathf.Abs(newMax - oldMax) > 0.01f && newMax > 0)
                {
                    var newCurrent = Mathf.Min(current + (newMax - oldMax), newMax);
                    health.Init(newMax);
                    if (newCurrent < newMax)
                        health.TakeDamage(newMax - newCurrent);
                }
            }
        }
    }
}
