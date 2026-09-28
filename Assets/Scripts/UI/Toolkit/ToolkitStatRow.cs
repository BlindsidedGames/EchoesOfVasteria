using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Shared icon/title/columns row for task and enemy statistics.</summary>
    public sealed class ToolkitStatRow
    {
        public VisualElement Root { get; }
        public Label Title { get; }
        public Label[] Fields { get; }
        public Image Icon { get; }
        public Button Toggle { get; }
        public Image ToggleIcon { get; }
        private readonly VisualElement progress, fill;
        public ToolkitStatRow(string name, string id, int columns, ToolkitTheme theme, Sprite frame, Sprite iconFrame, Sprite progressTrack, Sprite progressFill, Action toggle = null)
        {
            Root = new VisualElement { name = name }; Root.AddToClassList("eov-stat-entry"); 
            var well = new VisualElement(); well.AddToClassList("eov-stat-entry-icon");  Root.Add(well);
            Icon = ToolkitControls.Icon(null); Icon.AddToClassList("eov-stat-entry-art"); well.Add(Icon);
            var content = new VisualElement(); content.AddToClassList("eov-stat-entry-content"); Root.Add(content);
            var header = new VisualElement(); header.AddToClassList("eov-stat-entry-header"); content.Add(header);
            Title = ToolkitControls.Text("", ToolkitControls.TextRole.Body); Title.style.flexGrow = 1; header.Add(Title);
            var identifier = ToolkitControls.Text(id, ToolkitControls.TextRole.Body); header.Add(identifier);
            var body = new VisualElement(); body.AddToClassList("eov-stat-entry-fields"); content.Add(body);
            Fields = new Label[columns];
            for (var i = 0; i < columns; i++) { Fields[i] = ToolkitControls.Text("", ToolkitControls.TextRole.Caption); Fields[i].AddToClassList("eov-stat-entry-field"); body.Add(Fields[i]); }
            if (toggle != null)
            {
                Toggle = ToolkitControls.Button("toggle-" + name, toggle, null); Toggle.AddToClassList("eov-stat-entry-toggle");
                ToggleIcon = ToolkitControls.Icon(null); ToggleIcon.style.width = 32; ToggleIcon.style.height = 16; Toggle.Add(ToggleIcon); body.Add(Toggle);
            }
            progress = new VisualElement(); progress.AddToClassList("eov-stat-entry-progress"); progress.AddToClassList("track"); content.Add(progress);
            fill = new VisualElement(); fill.style.height = 4; fill.AddToClassList("fill"); progress.Add(fill);
            progress.style.display = DisplayStyle.None;
        }
        public void SetProgress(bool visible, float value)
        {
            progress.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100);
        }
    }
}
