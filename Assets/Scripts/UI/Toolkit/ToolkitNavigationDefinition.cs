using System;
using UnityEngine;
using TimelessEchoes.MapGeneration;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Navigation")]
    public sealed class ToolkitNavigationDefinition : ScriptableObject
    {
        public enum Group { Toolbar, Adventure, Hub, Townsfolk }
        public enum Action { Window, AdventureMenu, HubMenu, TownsfolkMenu, BeginMap, Close }
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public Group group;
            public Action action;
            public TownWindowManager.Window window;
            public MapGenerationConfig map;
            public ToolkitBookDefinition.Text label = new();
            public Sprite background, icon, shadow;
            public Vector2 iconSize = new(18, 18);
            public bool iconSliced;
            public float width = 22;
            public float fontSize = 8;
            public ToolkitVisibilityRule[] visibility = Array.Empty<ToolkitVisibilityRule>();
        }
        public Entry[] entries = Array.Empty<Entry>();
        public Sprite forgeAttention, cauldronAttention, questAttention;
        public Sprite autoPinFrame, toggleOn, toggleOff, discord;
        public Sprite progressTrack, progressFill, progressHero, progressReaper;
        public Color progressTextColor;
        public string discordUrl;
        public ToolkitBookDefinition.Text autoPinLabel = new();
    }
}
