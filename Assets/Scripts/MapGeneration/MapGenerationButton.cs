using System;
using Sirenix.OdinInspector;
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
    }
}
