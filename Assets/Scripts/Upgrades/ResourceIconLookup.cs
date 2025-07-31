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
        private static TMP_SpriteAsset spriteAsset;
        private static Dictionary<int, int> idToIndex;

        static ResourceIconLookup()
        {
            spriteAsset = Resources.Load<TMP_SpriteAsset>(SpriteAssetPath);

            // Pre-populate the dictionary so values can be manually edited.
            // Each key corresponds to a ResourceID with a default index of 0.
            idToIndex = new Dictionary<int, int>(57)
            {
                {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}, {6, 0}, {7, 0}, {8, 0},
                {9, 0}, {10, 0}, {11, 0}, {12, 0}, {13, 0}, {14, 0}, {15, 0},
                {16, 0}, {17, 0}, {18, 0}, {19, 0}, {20, 0}, {21, 0}, {22, 0},
                {23, 0}, {24, 0}, {25, 0}, {26, 0}, {27, 0}, {28, 0}, {29, 0},
                {30, 0}, {31, 0}, {32, 0}, {33, 0}, {34, 0}, {35, 0}, {36, 0},
                {37, 0}, {38, 0}, {39, 0}, {40, 0}, {41, 0}, {42, 0}, {43, 0},
                {44, 0}, {45, 0}, {46, 0}, {47, 0}, {48, 0}, {49, 0}, {50, 0},
                {51, 0}, {52, 0}, {53, 0}, {54, 0}, {55, 0}, {56, 0}, {57, 0}
            };
        }

        /// <summary>
        /// Sprite asset containing the resource icons.
        /// </summary>
        public static TMP_SpriteAsset SpriteAsset
        {
            get => spriteAsset;
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
    }
}
