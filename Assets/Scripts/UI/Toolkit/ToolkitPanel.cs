using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public static class ToolkitPanel
    {
        public static PanelSettings CreateSettings(ThemeStyleSheet theme, PanelTextSettings textSettings)
        {
            // Keep an authored asset in the build so Unity includes runtime UI shaders and
            // ICU text data. Runtime-only PanelSettings work in Editor but omit those assets
            // from a player. Clone it so each view owns its sorting/lifetime independently.
            var template = Resources.Load<PanelSettings>("UI/ToolkitPanel");
            var panel = template ? Object.Instantiate(template) : ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "Echoes runtime UI";
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(768, 432);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 1;
            panel.referenceSpritePixelsPerUnit = 16;
            panel.themeStyleSheet = theme;
            panel.textSettings = textSettings;
            return panel;
        }
    }
}
