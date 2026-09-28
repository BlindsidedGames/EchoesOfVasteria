using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Theme")]
    public sealed class ToolkitTheme : ScriptableObject
    {
        public Font font;
        public Font gameplayFont;
        [Min(1)] public float windowWidth = 508;
        [Min(0)] public float windowTopInset = 24;
        [Min(0)] public float windowBottomInset = 2;
        public Sprite window, recessed, row, button, expand, collapse, scrollTrack, scrollThumb;
        public Color textColor = new(0.243f, 0.153f, 0.192f, 1);
        public StyleSheet styles;

        // Toolkit panels must use referenceSpritePixelsPerUnit=16, matching the Canvas.
        // Toolkit applies the sprite PPU conversion itself; do not apply it twice.
        public static void Background(VisualElement element, Sprite sprite, float pixelsPerUnitMultiplier = 1)
        {
            if (element == null || !sprite) return;
            element.style.backgroundImage = new StyleBackground(sprite);
            // uGUI divides border thickness by Image.pixelsPerUnitMultiplier.
            // Preserve that authored value; sprite PPU is handled by PanelSettings.
            element.style.unitySliceScale = 1 / Mathf.Max(.01f, pixelsPerUnitMultiplier);
        }

        public void Apply(VisualElement root)
        {
            root.AddToClassList("eov-theme");
            if (styles) root.styleSheets.Add(styles);
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            root.style.color = textColor;
        }

        public void StyleScroll(ScrollView scroll)
        {
            var slider = scroll.verticalScroller.slider;
            Background(slider.Q("unity-tracker"), scrollTrack);
            Background(slider.Q("unity-dragger"), scrollThumb);
        }
    }
}
