using UnityEngine;
using TimelessEchoes.Gear;

namespace TimelessEchoes.Upgrades
{
    [CreateAssetMenu(fileName = "InfinityCauldronStat", menuName = "SO/Cauldron/Infinity Stat")] 
    public class InfinityCauldronStatSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Stat Mapping")] 
        [SerializeField] private HeroStatMapping stat;
        [Tooltip("If true, this stat should be treated as percent for display purposes.")]
        [SerializeField] private bool isPercent;

        [Header("Scaling")]
        [Tooltip("Decimal exponent applied to CardCount (value = CardCount^exponent).")]
        [Min(0f)] [SerializeField] private float exponent = 1f;

        [Header("Display")]
        [Tooltip("Decimal places to show for effect values in UI.")]
        [Min(0)] [SerializeField] private int decimalPlaces = 2;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public HeroStatMapping Stat => stat;
        // Backward-compatibility shims for removed fields
        [System.Obsolete("Use Exponent; BaseAmount is no longer used.")]
        public float BaseAmount => 0f;
        [System.Obsolete("Use Exponent; LogBase is no longer used.")]
        public float LogBase => 10f;
        public bool IsPercent => isPercent;
        public float Exponent => exponent;
        public int DecimalPlaces => decimalPlaces;
    }
}
