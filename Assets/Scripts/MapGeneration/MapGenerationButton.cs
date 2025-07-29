using System;
using UnityEngine.UI;
using TimelessEchoes.References.StatPanel;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class MapGenerationButton
    {
        public Button button;
        public MapGenerationConfig config;
        public GeneralStatsUIReferences statsUI;
    }
}
