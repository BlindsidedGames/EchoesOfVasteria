using UnityEngine;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Resource inventory")]
    public sealed class ToolkitResourceInventoryDefinition : ScriptableObject
    {
        public Resource[] resources;
        public ToolkitForgeDefinition equipmentArt;
        public Sprite title, inset, selection;
        public Sprite[] tierBorders, tierBackgrounds;
        public bool showTierBorder, showTierBackground;
        public float highlightDuration = 3;
    }
}
