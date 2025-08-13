using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    [CreateAssetMenu(fileName = "Rarity", menuName = "SO/Gear/Rarity")]
    public class RaritySO : ScriptableObject
    {
        [Range(0, 7)] public int tierIndex;
        public string displayName;
        public Color color = Color.white;

        [MinValue(1)] public int affixCount = 1;
        [Range(0f, 100f)] public float floorPercent = 0f;

        [Tooltip("Optional global weight modifier for this rarity.")]
        public float globalWeightMultiplier = 1f;

        public string GetName() => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}


