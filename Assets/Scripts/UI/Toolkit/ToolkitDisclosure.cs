using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>A shared, themed expandable section with ordinary button input on desktop and touch.</summary>
    public sealed class ToolkitDisclosure : VisualElement
    {
        public VisualElement Content { get; } = new();
        private readonly Image indicator;
        private readonly ToolkitTheme theme;
        private bool expanded;
        public event Action<bool> Changed;
        public bool Expanded { get => expanded; set { expanded = value; Content.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; indicator.sprite = value ? theme.collapse : theme.expand; Changed?.Invoke(value); } }
        public ToolkitDisclosure(string title, ToolkitTheme theme, bool expanded = true)
        {
            this.theme = theme; AddToClassList("eov-disclosure");
            var button = ToolkitControls.Button("section-" + title, () => Expanded = !Expanded, theme.row); button.AddToClassList("eov-disclosure-header");
            var label = ToolkitControls.Text("<b><smallcaps>" + title + "</smallcaps></b>", ToolkitControls.TextRole.Heading); label.style.flexGrow = 1; label.style.unityTextAlign = TextAnchor.MiddleLeft; button.Add(label);
            indicator = ToolkitControls.Icon(null); indicator.style.width = indicator.style.height = 8; button.Add(indicator);
            hierarchy.Add(button); Content.AddToClassList("eov-disclosure-content");  hierarchy.Add(Content); Expanded = expanded;
        }
    }
}
