using System;
using TimelessEchoes.MapGeneration;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Statistics")]
    public sealed class ToolkitStatisticsDefinition : ScriptableObject
    {
        public Sprite selectedButton, playerRow;
        public Sprite toggleOn, toggleOff, progressTrack, progressFill, sliderTrack, sliderFill, sliderHandle;
        public Sprite button, panel, inset, row, iconFrame, portraitFrame, portrait;
        public Sprite graphFrame, graphFill, averageLine, tooltipLeft, tooltipRight;
        public Color selectedTint, selectedText, averageColor;
        public GraphColors distanceColors, resourceColors, killColors;
        public Sprite[] itemTiers = Array.Empty<Sprite>();
        public MapEntry[] maps;
        [Serializable] public struct GraphColors { public Color death, retreat, abandoned, reaped, bonus; }
        [Serializable] public struct MapEntry
        {
            public MapGenerationConfig config;
            public string label;
            public float labelSize;
            public bool killScaling;
        }
    }
}
