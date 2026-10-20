using System;

namespace TimelessEchoes.Shop
{
    [Serializable] public sealed class ShopProgress { public double GatheringSeconds; }
    public static class ShopRules
    {
        public const double ClaimSeconds = 1800, MaximumSeconds = 7200, GatheringMultiplier = 1.5;
        public static double Remaining(double seconds) => double.IsNaN(seconds) || double.IsInfinity(seconds) ? 0 : Math.Max(0, Math.Min(MaximumSeconds, seconds));
        public static bool CanClaim(double seconds) => Remaining(seconds) <= MaximumSeconds - ClaimSeconds;
        public static bool IntroComplete(Blindsided.SaveData.GameData data) => data?.General != null &&
            (data.General.MapStatisticsTutorialStage >= 9 || data.CompletedNpcTasks?.Contains("OpeningTutorialComplete") == true);
    }
}
