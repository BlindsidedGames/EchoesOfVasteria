using TimelessEchoes.Gear;

namespace TimelessEchoes.Upgrades.Cauldron
{
    /// <summary>
    /// Static utility class for constructing and parsing cauldron card ID strings.
    /// Card IDs use prefixes to identify the type: RES: for resources, BUFF: for buffs, INF: for infinity stats.
    /// </summary>
    public static class CardIdentifierFactory
    {
        public const string ResourcePrefix = "RES:";
        public const string BuffPrefix = "BUFF:";
        public const string InfinityPrefix = "INF:";

        /// <summary>
        /// Creates a card ID for a resource.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <returns>A card ID in the format "RES:{name}".</returns>
        public static string ForResource(string name) => $"{ResourcePrefix}{name}";

        /// <summary>
        /// Creates a card ID for a buff.
        /// </summary>
        /// <param name="name">The buff name.</param>
        /// <returns>A card ID in the format "BUFF:{name}".</returns>
        public static string ForBuff(string name) => $"{BuffPrefix}{name}";

        /// <summary>
        /// Creates a card ID for an infinity stat.
        /// </summary>
        /// <param name="stat">The hero stat mapping.</param>
        /// <returns>A card ID in the format "INF:{stat}".</returns>
        public static string ForInfinity(HeroStatMapping stat) => $"{InfinityPrefix}{stat}";

        /// <summary>
        /// Checks if the card ID represents a resource card.
        /// </summary>
        /// <param name="id">The card ID to check.</param>
        /// <returns>True if the ID starts with the resource prefix.</returns>
        public static bool IsResource(string id) => id?.StartsWith(ResourcePrefix) ?? false;

        /// <summary>
        /// Checks if the card ID represents a buff card.
        /// </summary>
        /// <param name="id">The card ID to check.</param>
        /// <returns>True if the ID starts with the buff prefix.</returns>
        public static bool IsBuff(string id) => id?.StartsWith(BuffPrefix) ?? false;

        /// <summary>
        /// Checks if the card ID represents an infinity stat card.
        /// </summary>
        /// <param name="id">The card ID to check.</param>
        /// <returns>True if the ID starts with the infinity prefix.</returns>
        public static bool IsInfinity(string id) => id?.StartsWith(InfinityPrefix) ?? false;

        /// <summary>
        /// Extracts the name portion from a card ID by removing the prefix.
        /// </summary>
        /// <param name="id">The card ID to parse.</param>
        /// <returns>The name without the prefix, or the original ID if no known prefix is found.</returns>
        public static string ExtractName(string id)
        {
            if (IsResource(id)) return id.Substring(ResourcePrefix.Length);
            if (IsBuff(id)) return id.Substring(BuffPrefix.Length);
            if (IsInfinity(id)) return id.Substring(InfinityPrefix.Length);
            return id;
        }
    }
}
