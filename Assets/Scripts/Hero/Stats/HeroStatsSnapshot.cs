using System;

namespace TimelessEchoes.Hero
{
    [Serializable]
    public struct HeroStatsSnapshot
    {
        // Offense
        public float damage;
        public float attacksPerSecond;
        public float critChancePercent; // 0..100

        // (Runtime dice multiplier is maintained per HeroController; not centralized.)

        // Defense / survivability
        public float maxHealth;
        public float healthRegenPerSecond;
        public float defense;
        public float movementSpeed;

        // Meta
        public long version;
    }
}


