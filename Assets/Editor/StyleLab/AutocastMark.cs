using UnityEngine;
using UnityEngine.UIElements;

namespace Vasteria.StyleLab
{
    // Vector artwork avoids font-dependent symbol widths or missing glyphs.
    internal sealed class AutocastMark : VisualElement
    {
        private static readonly CustomStyleProperty<Color> Ink = new("--autocast-ink");
        private Color ink;

        public AutocastMark(bool enabled)
        {
            AddToClassList("autocast-mark");
            EnableInClassList("off", !enabled);
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                e.customStyle.TryGetValue(Ink, out ink);
                MarkDirtyRepaint();
            });
            generateVisualContent += context =>
            {
                var p = context.painter2D;
                p.strokeColor = ink;
                p.lineWidth = 1.8f;
                p.lineCap = LineCap.Round;
                p.lineJoin = LineJoin.Round;

                // Opposing arrows form the familiar repeat symbol.
                p.BeginPath();
                p.MoveTo(new Vector2(3, 9));
                p.LineTo(new Vector2(3, 5));
                p.LineTo(new Vector2(16, 5));
                p.MoveTo(new Vector2(13, 2));
                p.LineTo(new Vector2(16, 5));
                p.LineTo(new Vector2(13, 8));
                p.Stroke();
                p.BeginPath();
                p.MoveTo(new Vector2(17, 11));
                p.LineTo(new Vector2(17, 15));
                p.LineTo(new Vector2(4, 15));
                p.MoveTo(new Vector2(7, 12));
                p.LineTo(new Vector2(4, 15));
                p.LineTo(new Vector2(7, 18));
                p.Stroke();

                if (!enabled)
                {
                    p.BeginPath();
                    p.MoveTo(new Vector2(2, 18));
                    p.LineTo(new Vector2(18, 2));
                    p.Stroke();
                }
            };
        }
    }
}
