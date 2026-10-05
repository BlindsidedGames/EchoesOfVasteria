using System;
using UnityEngine;
using TimelessEchoes.Upgrades;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Shared icon/title/columns row for task and enemy statistics.</summary>
    public sealed class ToolkitStatRow
    {
        public VisualElement Root { get; }
        public Label Title { get; }
        public Label[] Fields { get; }
        public Label[] EnemyValues { get; }
        public Image Icon { get; }
        public Button Toggle { get; }
        public Image ToggleIcon { get; }
        private readonly VisualElement progress, fill;
        private readonly bool taskStyle;
        public ToolkitStatRow(string name, string id, int columns, ToolkitTheme theme, Sprite frame, Sprite iconFrame, Sprite progressTrack, Sprite progressFill, Action toggle = null)
        {
            taskStyle = toggle != null;
            Root = new VisualElement { name = name }; Root.AddToClassList("eov-stat-entry");
            Root.EnableInClassList("eov-task-entry", taskStyle);
            Root.EnableInClassList("eov-enemy-entry", !taskStyle); 
            var well = new VisualElement(); well.AddToClassList("eov-stat-entry-icon");  Root.Add(well);
            Icon = ToolkitControls.Icon(null); Icon.AddToClassList("eov-stat-entry-art");
            var crop = ToolkitGameplay.E(well, "stat-icon-crop"); crop.Add(Icon);
            Icon.style.flexShrink = 0;
            var content = new VisualElement(); content.AddToClassList("eov-stat-entry-content"); Root.Add(content);
            var header = new VisualElement(); header.AddToClassList("eov-stat-entry-header"); content.Add(header);
            Title = ToolkitControls.Text("", ToolkitControls.TextRole.Body); Title.style.flexGrow = 0; Title.style.flexShrink = 1; Title.style.minWidth = 0; header.Add(Title);
            var identifier = ToolkitControls.Text(id, ToolkitControls.TextRole.Body); identifier.AddToClassList("stat-entry-id"); header.Add(identifier);
            var body = new VisualElement(); body.AddToClassList("eov-stat-entry-fields"); content.Add(body);
            Fields = new Label[taskStyle ? columns : 0];
            if (taskStyle)
                for (var i = 0; i < columns; i++) { Fields[i] = ToolkitControls.Text("", ToolkitControls.TextRole.Caption); Fields[i].AddToClassList("eov-stat-entry-field"); Fields[i].EnableInClassList("row-end", i == columns - 1); body.Add(Fields[i]); }
            else
            {
                EnemyValues = new Label[8];
                string[] names = { "Health", "Damage", "Defense", "Attack Rate", "Movement", "Vision", "Kills", "Bonus Damage" };
                StatIconLookup.StatKey?[] keys = { StatIconLookup.StatKey.Health, StatIconLookup.StatKey.Damage, StatIconLookup.StatKey.Defense, StatIconLookup.StatKey.AttackRate, StatIconLookup.StatKey.MoveSpeed, null, null, StatIconLookup.StatKey.Damage };
                for (var col = 0; col < 4; col++)
                {
                    var column = ToolkitGameplay.E(body, "enemy-stat-column");
                    column.EnableInClassList("row-end", col == 3);
                    for (var line = 0; line < 2; line++)
                    {
                        var index = col * 2 + line;
                        var metric = ToolkitGameplay.E(column, "enemy-stat-metric"); metric.tooltip = names[index];
                        if (keys[index].HasValue && StatIconLookup.TryGetIcon(keys[index].Value, out var sprite))
                        {
                            var symbolCrop = ToolkitGameplay.E(metric, "enemy-stat-symbol");
                            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                            image.style.width = sprite.rect.width * 16 / sprite.pixelsPerUnit;
                            image.style.height = sprite.rect.height * 16 / sprite.pixelsPerUnit;
                            image.style.flexShrink = 0; symbolCrop.Add(image);
                        }
                        if (!keys[index].HasValue || index == 7) ToolkitGameplay.L(metric, index == 7 ? "Bonus" : names[index], "enemy-stat-name");
                        EnemyValues[index] = ToolkitGameplay.L(metric, "", "enemy-stat-value");
                    }
                }
            }
            if (toggle != null)
            {
                Toggle = ToolkitControls.Button("toggle-" + name, toggle, null); Toggle.AddToClassList("eov-stat-entry-toggle");
                ToggleIcon = ToolkitControls.Icon(null); ToggleIcon.style.width = 32; ToggleIcon.style.height = 16; Toggle.Add(ToggleIcon);
                ToolkitGameplay.L(Toggle, "Boost", "task-boost-label");
                Root.Add(Toggle);
            }
            progress = new VisualElement(); progress.AddToClassList("eov-stat-entry-progress"); progress.AddToClassList("track"); content.Add(progress);
            fill = new VisualElement(); fill.style.height = 4; fill.AddToClassList("fill"); progress.Add(fill);
            progress.style.display = DisplayStyle.None;
        }
        public void SetEntryIcon(Sprite sprite, bool known, bool boosted = false)
        {
            Icon.sprite = sprite;
            var size = sprite ? sprite.rect.size * (16f / sprite.pixelsPerUnit) : Vector2.zero;
            // Task art may be taller than resource art (for example an orchard tree).
            // Keep its full silhouette inside the task thumbnail's 32px frame.
            if (taskStyle && Mathf.Max(size.x, size.y) > 32f)
                size *= 32f / Mathf.Max(size.x, size.y);
            Icon.style.width = size.x;
            Icon.style.height = size.y;
            Root.EnableInClassList("known", known);
            Root.EnableInClassList("boosted", known && boosted);
        }
        public void SetProgress(bool visible, float value)
        {
            progress.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100);
        }
    }
}
