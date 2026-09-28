using System;
using System.Collections.Generic;
using TimelessEchoes.Gear;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ToolkitForgeOdds : VisualElement
    {
        private List<(RaritySO r, float w)> weights;
        public ToolkitForgeOdds() { AddToClassList("eov-forge-odds"); generateVisualContent += Draw; }
        public void SetWeights(List<(RaritySO r, float w)> value) { weights = value; MarkDirtyRepaint(); }
        private void Draw(MeshGenerationContext context)
        {
            if (weights == null) return;
            float total = 0; foreach (var entry in weights) total += Mathf.Max(0, entry.w); if (total <= 0) return;
            var painter = context.painter2D; var center = contentRect.center; float radius = Mathf.Min(contentRect.width, contentRect.height) * .5f, angle = -90;
            foreach (var entry in weights)
            {
                float next = angle + Mathf.Max(0, entry.w) / total * 360;
                if (next > angle) { painter.fillColor = entry.r ? entry.r.color : Color.white; painter.BeginPath(); painter.MoveTo(center); painter.Arc(center, radius, new Angle(angle, AngleUnit.Degree), new Angle(next, AngleUnit.Degree)); painter.ClosePath(); painter.Fill(); }
                angle = next;
            }
        }
    }
}
