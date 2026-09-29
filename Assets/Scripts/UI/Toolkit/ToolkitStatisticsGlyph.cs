using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    internal sealed class ToolkitStatisticsGlyph : VisualElement
    {
        public ToolkitStatisticsGlyph()
        {
            pickingMode = PickingMode.Ignore;
            style.width = 13; style.height = 13;
            generateVisualContent += context =>
            {
                var p = context.painter2D; p.fillColor = resolvedStyle.color;
                var r = contentRect;
                for (int i = 0; i < 3; i++)
                {
                    float x = r.x + r.width * (.1f + .3f * i), width = r.width * .2f;
                    float h = r.height * (.35f + .25f * i), bottom = r.yMax - r.height * .075f;
                    p.BeginPath(); p.MoveTo(new Vector2(x, bottom)); p.LineTo(new Vector2(x + width, bottom));
                    p.LineTo(new Vector2(x + width, bottom - h)); p.LineTo(new Vector2(x, bottom - h)); p.ClosePath(); p.Fill();
                }
            };
        }
    }
}
