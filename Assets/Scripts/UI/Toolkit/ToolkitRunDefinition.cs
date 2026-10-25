using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Run HUD")]
    public sealed class ToolkitRunDefinition : ScriptableObject
    {
        public Sprite panel, portrait, portraitFrame, portraitInset, detailFrame, tooltipFrame, healthTrack, healthFill;
        public Sprite dadJokeBubble;
        public Sprite reaperIcon;
        public Sprite scytheIcon;
        public Sprite graveIcon;
        public Sprite killsIcon, bonusIcon, timeIcon, stepsIcon;
        [System.Serializable]
        public struct IconTier
        {
            public string name;
            public double minimum;
            public Sprite icon;
        }
        public IconTier[] timeIconTiers = System.Array.Empty<IconTier>();
        public IconTier[] resourceIconTiers = System.Array.Empty<IconTier>();
        public Sprite tutorialHand;
        public Sprite fullscreenIcon, restoreViewIcon;
        public Font dadJokeFont;
        public ToolkitBuffsDefinition buffs;

        public Sprite TimeIcon(double seconds) => TierIcon(timeIconTiers, seconds, timeIcon);
        public Sprite ResourceIcon(double amount) => TierIcon(resourceIconTiers, amount, null);

        private static Sprite TierIcon(IconTier[] tiers, double value, Sprite fallback)
        {
            var result = fallback;
            if (tiers != null)
                foreach (var tier in tiers)
                    if (tier.icon && value >= tier.minimum) result = tier.icon;
            return result;
        }
    }
}
