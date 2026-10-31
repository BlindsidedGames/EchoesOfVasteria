using UnityEngine;

namespace TimelessEchoes
{
    [System.Serializable]
    public struct DefenseTuning
    {
        [Tooltip("Simple armor scalar 'n' used in damage formula: D * (1 - x/(x+n))")]
        public float N;
    }

    public static class Combat
    {
        public const float DefaultArmorScalarN = 25f;

        public static float ExpectedDps(Hero.HeroStatsSnapshot hero, float enemyDefense, float damageMultiplier = 1f)
        {
            var crit = 1f + Mathf.Clamp01(hero.critChancePercent / 100f) * Mathf.Max(0f, hero.critDamagePercent) / 100f;
            return ApplyDefense(hero.damage * damageMultiplier * crit, enemyDefense) * Mathf.Max(0f, hero.attacksPerSecond);
        }

        // Expected exposure across encounters, not a simulation of projectile paths or positioning.
        // Echoes add attacks and share the target load; only their share transfers half damage.
        public static (double damage, bool defeated) ExpectedEncounter(Hero.HeroStatsSnapshot hero, Enemies.EnemyData enemy,
            int level, double combatEchoes, float killMultiplier = 1f)
        {
            var echoes = System.Math.Max(0, combatEchoes);
            var defense = enemy.GetDefenseForLevel(level);
            var dps = (1 + echoes) * ExpectedDps(hero, defense, killMultiplier);
            var exposure = enemy.GetMaxHealthForLevel(level) / System.Math.Max(.0001, dps);
            var transferred = 1 - .5 * echoes / (1 + echoes);
            var damage = ApplyDefense(enemy.GetDamageForLevel(level), hero.defense) * transferred;
            var fuse = enemy.prefab ? enemy.prefab.GetComponent<Enemies.BombschroomFuse>() : null;
            if (fuse) return exposure >= fuse.FuseDuration ? (damage * fuse.BlastDamageMultiplier, false) : (0, true);
            return (exposure * System.Math.Max(0, enemy.attackSpeed) * damage, dps > 0);
        }

        public static float ApplyDefense(float incomingDamage, float defense)
        {
            return ApplyDefense(incomingDamage, defense, new DefenseTuning { N = DefaultArmorScalarN });
        }

        public static float ApplyDefense(float incomingDamage, float defense, DefenseTuning tuning)
        {
            if (incomingDamage <= 0f)
                return 0f;

            float armor = Mathf.Max(0f, defense);
            float n = tuning.N > 0f ? tuning.N : DefaultArmorScalarN;
            return incomingDamage * (1f - (armor / (armor + n)));
        }

        // Kept for call-site compatibility; enemyLevel is ignored in the simplified model
        public static float ApplyDefense(float incomingDamage, float defense, int enemyLevel, DefenseTuning tuning)
        {
            return ApplyDefense(incomingDamage, defense, tuning);
        }
    }
}


