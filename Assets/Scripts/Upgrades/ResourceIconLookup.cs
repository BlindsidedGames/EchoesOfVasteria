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

            // New indices for the alternating sheet (known icons at even slots).
            idToIndex = new Dictionary<int, int>(65)
            {
                {1, 64},  {2, 58},  {3, 66},  {4, 56},  {5, 60},  {6, 62},  {7, 12},  {8, 18},
                {9, 0},   {10, 36}, {11, 24}, {12, 6},  {13, 30}, {14, 42}, {15, 14}, {16, 20},
                {17, 2},  {18, 38}, {19, 26}, {20, 8},  {21, 32}, {22, 44}, {23, 16}, {24, 22},
                {25, 4},  {26, 40}, {27, 28}, {28, 10}, {29, 34}, {30, 46}, {31, 86}, {32, 80},
                {33, 76}, {34, 78}, {35, 82}, {36, 74}, {37, 72}, {38, 84}, {39, 126},{40, 144},
                {41, 120},{42, 134},{43, 150},{44, 138},{45, 132},{46, 136},{47, 152},{48, 142},
                {49, 146},{50, 118},{51, 128},{52, 148},{53, 130},{54, 140},{55, 124},{56, 122},
                {57, 54}, {58, 172},{59, 174},{60, 176},{61, 178},{62, 180},{63, 182},{64, 184}, 
                {65, 186},{0, 188}
            };

            // With the alternating layout, each unknown sprite is adjacent: +1 from its known index.
            idToUnknownIndex = new Dictionary<int, int>(idToIndex.Count);
            foreach (var pair in idToIndex)
            {
                idToUnknownIndex[pair.Key] = pair.Value + 1;
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
