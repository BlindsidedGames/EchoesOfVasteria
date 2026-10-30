using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>Saved road upgrades and their share of the hero's movement progression.</summary>
    public static class TownPathProgression
    {
        public const string DirtQuest = "Town.Paths.Dirt.v1";
        public const string PavingQuest = "Town.Paths.Paving.v1";
        public const float MaximumUnbuffedSpeed = 9f;
        private const float DirtMultiplier = 1.15f;
        private const float PavingMultiplier = DirtMultiplier * 1.20f;

        public static bool IsPathQuest(string id) => id == DirtQuest || id == PavingQuest;

        public static int Tier(GameData data) => Completed(data, PavingQuest) ? 2 : Completed(data, DirtQuest) ? 1 : 0;

        private static bool Completed(GameData data, string id) =>
            data?.Quests != null && data.Quests.TryGetValue(id, out var quest) && quest != null && quest.Completed;

        public static float UnbuffedSpeed(float movementRating, int tier)
        {
            var rating = float.IsNaN(movementRating) ? 0f : Mathf.Max(0f, movementRating);
            var progress = float.IsPositiveInfinity(rating) ? 1f : rating / (rating + 10f);
            var grassCeiling = MaximumUnbuffedSpeed / PavingMultiplier;
            var grassSpeed = 3f + (grassCeiling - 3f) * progress;
            var multiplier = tier >= 2 ? PavingMultiplier : tier == 1 ? DirtMultiplier : 1f;
            return Mathf.Min(MaximumUnbuffedSpeed, grassSpeed * multiplier);
        }
    }
}
