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
        private const float DefaultArmorScalarN = 60f;

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


