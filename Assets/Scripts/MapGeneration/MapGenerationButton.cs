using System;
using TMPro;
using UnityEngine.UI;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class MapGenerationButton
    {
        public Button button;
        public MapGenerationConfig config;
        public TMP_Text topStatsText;
        public TMP_Text bottomStatsText;
    }
}