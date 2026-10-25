using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    // Draw the joined bed in one coordinate space. Separate Image elements snap
    // their origins and sizes independently at fractional UI scales, opening seams.
    internal sealed class FieldsSoilView : VisualElement
    {
        private Sprite[] sprites;
        private int size;
        public FieldsSoilView()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            generateVisualContent += Draw;
        }
        public void Configure(Sprite[] art, int cells)
        {
            if (ReferenceEquals(sprites, art) && size == cells) return;
            sprites = art; size = cells;
            style.width = style.height = cells * 8;
            MarkDirtyRepaint();
        }
        private void Draw(MeshGenerationContext context)
        {
            if (sprites == null || sprites.Length < 4) return;
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                var index = corner ? 0 : y == 0 || y == size - 1 ? 1 : x == 0 || x == size - 1 ? 2 : 3;
                var sprite = sprites[index]; if (!sprite) continue;
                var flipX = (index == 0 || index == 2) && x == size - 1;
                var flipY = (index == 0 || index == 1) && y == size - 1;
                var points = sprite.vertices; var uv = sprite.uv; var indices = sprite.triangles;
                var mesh = context.Allocate(points.Length, indices.Length, sprite.texture);
                var region = mesh.uvRegion;
                for (var i = 0; i < points.Length; i++)
                {
                    var px = (points[i].x * sprite.pixelsPerUnit + sprite.pivot.x) / sprite.rect.width;
                    var py = 1 - (points[i].y * sprite.pixelsPerUnit + sprite.pivot.y) / sprite.rect.height;
                    mesh.SetNextVertex(new Vertex
                    {
                        position = new Vector3((x + (flipX ? 1 - px : px)) * 8, (y + (flipY ? 1 - py : py)) * 8, Vertex.nearZ),
                        tint = Color.white,
                        uv = new Vector2(region.x + uv[i].x * region.width, region.y + uv[i].y * region.height)
                    });
                }
                for (var i = 0; i < indices.Length; i += 3)
                {
                    mesh.SetNextIndex(indices[i]);
                    mesh.SetNextIndex(indices[i + (flipX != flipY ? 2 : 1)]);
                    mesh.SetNextIndex(indices[i + (flipX != flipY ? 1 : 2)]);
                }
            }
        }
    }
}
