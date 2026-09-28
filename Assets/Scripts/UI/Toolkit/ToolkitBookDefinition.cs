using System;
using UnityEngine;
using UnityEngine.Localization;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Book")]
    public sealed class ToolkitBookDefinition : ScriptableObject
    {
        public enum Presentation { Library, Credits }
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
            public Text title = new();
            public Text body = new();
            public bool expanded;
            public ToolkitVisibilityRule[] visibility = Array.Empty<ToolkitVisibilityRule>();
        }
    }
}
