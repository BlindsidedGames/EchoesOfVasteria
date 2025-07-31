namespace Blindsided.Utilities
{
    public static class TextStrings
    {
        public const string ColourHighlight = "<color=#B6FFFF>";
        public const string ColourGreen = "<color=#98C560>";
        public const string ColourGreenAlt = "<color=#91CC95>";
        public const string ColourWhite = "<color=#CCCCCC>";
        public const string ColourGrey = "<color=#A5A5A5>";
        public const string ColourOrange = "<color=#C69B60>";
        public const string ColourRed = "<color=#C56260>";

        public const string IconWithColour = "<sprite=0 color=#00000>";
        public const string EndColour = "</color>";

        public const string HealthIcon = "<sprite=0>";
        public const string DamageIcon = "<sprite=24>";
        public const string AttackSpeedIcon = "<sprite=150>";
        public const string RangeIcon = "<sprite=5>";
        public const string MoveSpeedIcon = "<sprite=11>";
        public const string GoldIcon = "<sprite=4>";

        public static string ResourceIcon(int resourceID)
        {
            return TimelessEchoes.Upgrades.ResourceIconLookup.GetIconTag(resourceID);
        }
    }
}