using System;
using UnityEngine;
using UnityEngine.Localization;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Book")]
    public sealed class ToolkitBookDefinition : ScriptableObject
    {
        public enum Presentation { Library, Credits }
        public enum TopicGroup { Adventuring, Equipment, Support }
        public Presentation presentation;
        public Section[] sections = Array.Empty<Section>();

        [Serializable]
        public sealed class Text
        {
            public string key;
            public bool bold;
            public bool smallCaps;
            [TextArea] public string fallback;
            public LocalizedString localized = new();
        }

        [Serializable]
        public sealed class Section
        {
            public string id;
            public TopicGroup group;
            public Sprite icon;
            public Text title = new();
            public Text body = new();
            public Text details = new();
            public Illustration[] illustrations = Array.Empty<Illustration>();
            public Text[] referenceColumns = Array.Empty<Text>();
            public ReferenceRow[] referenceRows = Array.Empty<ReferenceRow>();
            public bool expanded;
            public ToolkitVisibilityRule[] visibility = Array.Empty<ToolkitVisibilityRule>();
        }
        [Serializable]
        public sealed class Illustration
        {
            public Sprite sprite;
            public TimelessEchoes.Buffs.BuffRecipe buff;
            public Text caption = new();
            public Text description = new();
        }

        [Serializable]
        public sealed class ReferenceRow
        {
            public Sprite icon;
            public Text label = new();
            public Text value = new();
            public Text extra = new();
        }
    }
}
