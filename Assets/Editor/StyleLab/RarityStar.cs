using UnityEngine;
using UnityEngine.UIElements;

namespace Vasteria.StyleLab
{
    internal sealed class RarityStar : VisualElement
    {
        private static readonly CustomStyleProperty<Color> StarColour = new("--star-colour");
        private Color colour;

        public RarityStar()
        {
            AddToClassList("rarity-star");
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                e.customStyle.TryGetValue(StarColour, out colour);
                MarkDirtyRepaint();
            });
            generateVisualContent += context =>
            {
                var painter = context.painter2D;
                var centre = contentRect.center;
                float radius = Mathf.Min(contentRect.width, contentRect.height) * .5f;
                painter.fillColor = colour;
                painter.BeginPath();
                for (int i = 0; i < 10; i++)
                {
                    float angle = (-90 + i * 36) * Mathf.Deg2Rad;
                    float r = radius * (i % 2 == 0 ? 1 : .44f);
                    var point = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    if (i == 0) painter.MoveTo(point);
                    else painter.LineTo(point);
                }
                painter.ClosePath();
                painter.Fill();
            };
        }
    }
}
