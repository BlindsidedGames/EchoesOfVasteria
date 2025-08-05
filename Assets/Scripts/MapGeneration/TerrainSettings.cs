using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using VinTools.BetterRuleTiles;
using UnityEngine.Serialization;

namespace TimelessEchoes.MapGeneration
{
    [CreateAssetMenu(menuName = "SO/Terrain Settings")]
    public class TerrainSettings : ScriptableObject
    {
        [Required]
        public BetterRuleTile tile;

        [Serializable]
        public class TaskSettings
        {
            [Range(0f,1f)] public float taskDensity = 0.1f;

            public bool borderOnly;
            public int topBorderOffset;
            public int bottomBorderOffset;
            public int leftBorderOffset;
            public int rightBorderOffset;
            // When false, spawned tasks are not registered with the TaskController.
            [ToggleLeft]
            public bool addToTaskList = true;
        }

        [Serializable]
        public class DecorSection
        {
            [Range(0f,1f)] public float density = 1f;
            [Searchable]
            [ListDrawerSettings(ListElementLabelName = "Name", ShowFoldout = false)]
            public List<DecorEntry> decor = new();
        }

        public TaskSettings taskSettings = new();
        public DecorSection decor = new();

        [ToggleLeft]
        public bool showValidationGizmos;
    }
}
