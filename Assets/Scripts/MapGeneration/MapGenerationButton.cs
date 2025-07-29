using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class MapGenerationButton
    {
        [HorizontalGroup("Row")]
        public Button button;
        [HorizontalGroup("Row")]
        public MapGenerationConfig config;
        [HorizontalGroup("Row")]
        public TMP_Text topStatsText;
        [HorizontalGroup("Row")]
        public TMP_Text bottomStatsText;
    }
}
