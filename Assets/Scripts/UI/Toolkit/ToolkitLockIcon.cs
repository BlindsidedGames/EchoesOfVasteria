using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    internal sealed class ToolkitLockIcon : VisualElement
    {
        public ToolkitLockIcon()
        {
            AddToClassList("buff-lock-icon"); pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            generateVisualContent += context =>
            {
                var p = context.painter2D; var r = contentRect;
                Vector2 At(float x, float y) => new(r.x + r.width * x / 20, r.y + r.height * y / 20);
                p.strokeColor = resolvedStyle.color; p.lineWidth = r.width * .1f;
                p.BeginPath(); p.MoveTo(At(6,9)); p.LineTo(At(6,6));
                p.BezierCurveTo(At(6,0), At(14,0), At(14,6)); p.LineTo(At(14,9)); p.Stroke();
                p.fillColor = resolvedStyle.color;
                p.BeginPath(); p.MoveTo(At(3,8)); p.LineTo(At(17,8)); p.LineTo(At(17,19)); p.LineTo(At(3,19)); p.ClosePath(); p.Fill();
                p.strokeColor = ToolkitColorTheme.Surface; p.lineWidth = r.width * .12f;
                p.BeginPath(); p.MoveTo(At(10,11)); p.LineTo(At(10,16)); p.Stroke();
            };
        }
    }
}
