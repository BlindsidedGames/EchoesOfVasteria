using UnityEngine;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    /// Provides occasional special names for enemies.
    /// </summary>
    public static class EnemyNameProvider
    {
        private static readonly string[] SpecialNames =
        {
            "Phraox",
            "MrVastayan",
            "tanty.",
            "The Wize",
            "MatHeadGetz",
            "Latimer Cross",
        };

        /// <summary>
        /// Chance (0-1) that an enemy will use a special name.
        /// Defaults to 1 in 100.
        /// </summary>
        public static float SpecialNameChance = 0.01f;

        /// <summary>
        /// Returns either the provided default name or a special name.
        /// </summary>
        public static string GetName(string defaultName)
        {
            if (SpecialNames.Length > 0 && Random.value < SpecialNameChance)
                return SpecialNames[Random.Range(0, SpecialNames.Length)];
            return defaultName;
        }
    }
}
