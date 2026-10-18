using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.Editor
{
    // Unity 6 draws a rectangular preview underneath device overlays. Transparent
    // exterior corners therefore need a matching screen mesh, not a safe-area inset.
    [InitializeOnLoad]
    internal static class iPhone15ProScreenMask
    {
        private const string OverlayPath = "Assets/Editor/DeviceSimulator/iPhone15Pro/Overlays/iPhone 15 Pro.png";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static double nextCheck;

        static iPhone15ProScreenMask() => EditorApplication.update += Update;

        private static void Update()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + .1;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().FullName != "UnityEditor.DeviceSimulation.SimulatorWindow") continue;
                window.rootVisualElement.Query<VisualElement>().ForEach(view =>
                {
                    var type = view.GetType();
                    if (type.FullName != "UnityEditor.DeviceSimulation.DeviceView") return;
                    var overlay = type.GetField("m_OverlayTexture", Fields)?.GetValue(view) as Texture;
                    if (!overlay || AssetDatabase.GetAssetPath(overlay) != OverlayPath) return;
                    var meshField = type.GetField("m_ScreenMesh", Fields);
                    var dirtyField = type.GetField("m_ScreenMeshDirty", Fields);
                    var create = type.GetMethod("CreateScreenMesh", Fields);
                    if (meshField == null || dirtyField == null || create == null) return;
                    // Retain Unity's orientation-dependent UVs and screen insets.
                    create.Invoke(view, null);
                    var mesh = meshField.GetValue(view) as Mesh;
                    if (!mesh || mesh.vertexCount != 4) return;
                    RoundCorners(mesh, 180);
                    view.MarkDirtyRepaint();
                });
            }
        }

        private static void RoundCorners(Mesh mesh, float radius)
        {
            var corners = mesh.vertices;
            var sourceUV = mesh.uv;
            var left = corners[0].x;
            var top = corners[0].y;
            var width = corners[2].x - left;
            var height = corners[2].y - top;
            radius = Mathf.Min(radius, Mathf.Min(width, height) * .5f);
            const int segments = 24;
            const int perimeter = 4 * (segments + 1);
            var vertices = new Vector3[perimeter + 1];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[perimeter * 3];
            vertices[0] = new Vector3(left + width * .5f, top + height * .5f, corners[0].z);
            uv[0] = (sourceUV[0] + sourceUV[2]) * .5f;
            for (var corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(corner == 0 || corner == 3 ? left + radius : left + width - radius,
                    corner < 2 ? top + radius : top + height - radius);
                for (var step = 0; step <= segments; step++)
                {
                    var angle = (180 + corner * 90 + step * 90f / segments) * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    var index = 1 + corner * (segments + 1) + step;
                    vertices[index] = new Vector3(point.x, point.y, corners[0].z);
                    var x = (point.x - left) / width;
                    var y = (point.y - top) / height;
                    uv[index] = Vector2.Lerp(Vector2.Lerp(sourceUV[0], sourceUV[1], x),
                        Vector2.Lerp(sourceUV[3], sourceUV[2], x), y);
                    triangles[(index - 1) * 3] = 0;
                    triangles[(index - 1) * 3 + 1] = index;
                    triangles[(index - 1) * 3 + 2] = index == perimeter ? 1 : index + 1;
                }
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }
    }
}
