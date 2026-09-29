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
        private static float Weight(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0,value);
        private void Draw(MeshGenerationContext context)
        {
            if (weights == null) return;
            float total=0; foreach(var entry in weights) total+=Weight(entry.w);
            if(total<=0)return;
            int triangles=0; foreach(var entry in weights) if(Weight(entry.w)>0) triangles+=Mathf.CeilToInt(Weight(entry.w)/total*90);
            var mesh=context.Allocate(triangles*3,triangles*3);
            var centre=contentRect.center;float radius=Mathf.Min(contentRect.width,contentRect.height)*.5f,angle=-Mathf.PI*.5f;
            ushort index=0;
            foreach(var entry in weights)
            {
                var weight=Weight(entry.w);if(weight<=0)continue;
                float sweep=weight/total*Mathf.PI*2;int steps=Mathf.CeilToInt(weight/total*90);
                var colour=(Color32)(entry.r?entry.r.color:Color.white);
                // Explicit triangle fans handle majority and full-circle slices without
                // depending on Painter2D's arc/path joining and tessellation rules.
                for(int step=0;step<steps;step++)
                {
                    float a=angle+sweep*step/steps,b=angle+sweep*(step+1)/steps;
                    mesh.SetNextVertex(new Vertex{position=new Vector3(centre.x,centre.y,Vertex.nearZ),tint=colour});
                    mesh.SetNextVertex(new Vertex{position=new Vector3(centre.x+Mathf.Cos(a)*radius,centre.y+Mathf.Sin(a)*radius,Vertex.nearZ),tint=colour});
                    mesh.SetNextVertex(new Vertex{position=new Vector3(centre.x+Mathf.Cos(b)*radius,centre.y+Mathf.Sin(b)*radius,Vertex.nearZ),tint=colour});
                    mesh.SetNextIndex(index++);mesh.SetNextIndex(index++);mesh.SetNextIndex(index++);
                }
                angle+=sweep;
            }
        }
    }
}
