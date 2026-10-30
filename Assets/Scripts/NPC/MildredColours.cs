using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    /// <summary>Frame-for-frame palette swaps of Mildred's commissioned artwork.</summary>
    public sealed class MildredColours : ScriptableObject
    {
        public Sprite[] original, white, ginger, brown, silver;
        private Dictionary<Sprite, int> frameIndices;

        public const int Count = 5;
        public Sprite[] Frames(int colour) => colour switch
        {
            1 => white, 2 => ginger, 3 => brown, 4 => silver, _ => original
        };

        public Sprite Remap(Sprite sprite, int colour)
        {
            if (!sprite) return sprite;
            if (frameIndices == null)
            {
                frameIndices = new Dictionary<Sprite, int>();
                for (var variant = 0; variant < Count; variant++)
                {
                    var frames = Frames(variant);
                    if (frames == null) continue;
                    for (var i = 0; i < frames.Length; i++)
                        if (frames[i]) frameIndices[frames[i]] = i;
                }
            }
            var target = Frames(colour);
            return frameIndices.TryGetValue(sprite, out var index) && target != null && index < target.Length
                ? target[index] : sprite;
        }
    }
}
