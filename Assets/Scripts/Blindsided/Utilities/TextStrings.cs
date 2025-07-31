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

        /// <summary>
        /// Returns a TextMeshPro sprite tag for the given sprite ID.
        /// </summary>
        public static string SpriteTag(int id) => $"<sprite={id}>";

        public const int HealthIcon = 0;
        public const int DamageIcon = 24;
        public const int AttackSpeedIcon = 150;
        public const int RangeIcon = 5;
        public const int MoveSpeedIcon = 11;
        public const int GoldIcon = 4;
    }
}