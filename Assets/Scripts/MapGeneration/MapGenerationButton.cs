using System;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.Audio;
using TimelessEchoes.References.StatPanel;

namespace TimelessEchoes.MapGeneration
{
    [Serializable]
    public class MapGenerationButton
    {
        [SerializeField] private Button button;
        [SerializeField] private MapGenerationConfig config;
        [SerializeField] private GeneralStatsUIReferences statsUI;
        [SerializeField] private AudioManager.MusicTrack musicTrack;
        [SerializeField] private TimelessEchoes.GameManager.MapScalingMode scalingMode = TimelessEchoes.GameManager.MapScalingMode.DistanceBased;
        [SerializeField] [Min(1)] private int killsPerLevel = 25;

        public Button Button => button;
        public MapGenerationConfig Config => config;
        public GeneralStatsUIReferences StatsUI => statsUI;
        public AudioManager.MusicTrack MusicTrack => musicTrack;
        public TimelessEchoes.GameManager.MapScalingMode ScalingMode => scalingMode;
        public int KillsPerLevel => killsPerLevel;
    }
}

