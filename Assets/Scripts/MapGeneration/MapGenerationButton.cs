using System;
using UnityEngine.UI;
using TimelessEchoes.Audio;
using TimelessEchoes.References.StatPanel;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class MapGenerationButton
    {
        public Button button;
        public MapGenerationConfig config;
        public GeneralStatsUIReferences statsUI;
        public AudioManager.MusicTrack musicTrack;
    }
}
