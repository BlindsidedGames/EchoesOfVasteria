using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    /// Utility for resolving sprite tags for resources.
    /// </summary>
    public static class ResourceIconLookup
    {
        private const string SpriteAssetPath = "Fonts/FloatingTextIcons";
        private const int UnknownBlockStartIndex = 86; // First index of the unknown variants inside the same sprite asset
        private static TMP_SpriteAsset spriteAsset;
        private static Dictionary<int, int> idToIndex;
        private static Dictionary<int, int> idToUnknownIndex;

        static ResourceIconLookup()
        {
            spriteAsset = Blindsided.Utilities.AssetCache.GetOne<TMP_SpriteAsset>(SpriteAssetPath);

            // Pre-populate the dictionary so values can be manually edited.
            // Each key corresponds to a ResourceID with a default index of 0.
            idToIndex = new Dictionary<int, int>(57)
            {
                {1, 32}, {2, 29}, {3, 33}, {4, 28}, {5, 30}, {6, 31}, {7, 6},  {8, 9},
                {9, 0},  {10, 18}, {11, 12}, {12, 3},  {13, 15}, {14, 21}, {15, 7},
                {16, 10}, {17, 1},  {18, 19}, {19, 13}, {20, 4},  {21, 16}, {22, 22},
                {23, 8},  {24, 11}, {25, 2},  {26, 20}, {27, 14}, {28, 5},  {29, 17},
                {30, 23}, {31, 43}, {32, 40}, {33, 38}, {34, 39}, {35, 41}, {36, 37},
                {37, 36}, {38, 42}, {39, 63}, {40, 72}, {41, 60}, {42, 67}, {43, 75},
                {44, 69}, {45, 66}, {46, 68}, {47, 76}, {48, 71}, {49, 73}, {50, 59},
                {51, 64}, {52, 74}, {53, 65}, {54, 70}, {55, 62}, {56, 61}, {57, 27}
            };

            // Build a parallel lookup for unknown variants inside the same sprite asset.
            // Each unknown icon mirrors the ordering of its known counterpart with a fixed offset.
            idToUnknownIndex = new Dictionary<int, int>(idToIndex.Count);
            foreach (var pair in idToIndex)
            {
                idToUnknownIndex[pair.Key] = pair.Value + UnknownBlockStartIndex;
            }
        }

        /// <summary>
        /// Sprite asset containing the resource icons.
        /// </summary>
        public static TMP_SpriteAsset SpriteAsset
        {
            get => spriteAsset;
        }

        /// <summary>
        /// Attempts to resolve the sprite index for the given resource id.
        /// </summary>
        /// <param name="resourceID">The integer id stored on a <see cref="Resource"/>.</param>
        /// <param name="index">The matching sprite index in the FloatingTextIcons sheets.</param>
        /// <returns>True if a mapping exists; otherwise false.</returns>
        public static bool TryGetIconIndex(int resourceID, out int index)
        {
            if (idToIndex == null)
            {
                index = 0;
                return false;
            }

            return idToIndex.TryGetValue(resourceID, out index);
        }

        /// <summary>
        /// Attempts to resolve the UNKNOWN sprite index for the given resource id.
        /// </summary>
        public static bool TryGetUnknownIconIndex(int resourceID, out int index)
        {
            if (idToUnknownIndex == null)
            {
                index = 0;
                return false;
            }
            return idToUnknownIndex.TryGetValue(resourceID, out index);
        }

        /// <summary>
        /// Returns a rich tag string referencing the sprite for the given resource ID.
        /// </summary>
        public static string GetIconTag(int resourceID)
        {
            return idToIndex != null && idToIndex.TryGetValue(resourceID, out var idx)
                ? $"<sprite={idx}>"
                : string.Empty;
        }

        /// <summary>
        /// Returns a rich tag string referencing the UNKNOWN sprite for the given resource ID.
        /// </summary>
        public static string GetUnknownIconTag(int resourceID)
        {
            return idToUnknownIndex != null && idToUnknownIndex.TryGetValue(resourceID, out var idx)
                ? $"<sprite={idx}>"
                : string.Empty;
        }
    }
}
