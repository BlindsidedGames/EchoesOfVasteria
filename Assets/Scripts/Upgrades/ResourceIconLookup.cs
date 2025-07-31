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
                {1, 32}, {2, 29}, {3, 33}, {4, 28}, {5, 30}, {6, 31}, {7, 6},  {8, 9},
                {9, 0},  {10, 18}, {11, 12}, {12, 3},  {13, 15}, {14, 21}, {15, 7},
                {16, 10}, {17, 1},  {18, 19}, {19, 13}, {20, 4},  {21, 16}, {22, 22},
                {23, 8},  {24, 11}, {25, 2},  {26, 20}, {27, 14}, {28, 5},  {29, 17},
                {30, 23}, {31, 43}, {32, 40}, {33, 38}, {34, 39}, {35, 41}, {36, 37},
                {37, 36}, {38, 42}, {39, 63}, {40, 72}, {41, 60}, {42, 67}, {43, 75},
                {44, 69}, {45, 66}, {46, 68}, {47, 76}, {48, 71}, {49, 73}, {50, 59},
                {51, 64}, {52, 74}, {53, 65}, {54, 70}, {55, 62}, {56, 61}, {57, 27}
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
