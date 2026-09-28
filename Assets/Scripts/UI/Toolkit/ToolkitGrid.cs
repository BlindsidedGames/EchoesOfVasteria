using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Pixel-art cells laid out from their index, independent of rounded flex margins.</summary>
    public sealed class ToolkitGrid : VisualElement
    {
        private int columns;
        public int Columns => columns;
        private readonly Vector2 cellSize, spacing;

        public ToolkitGrid(int columns, Vector2 cellSize, Vector2 spacing)
        {
            this.columns = Mathf.Max(1, columns);
            this.cellSize = cellSize;
            this.spacing = spacing;
            style.width = this.columns * cellSize.x + (this.columns - 1) * spacing.x;
            style.flexShrink = 0;
            style.height = 0;
        }

        public void AddCell(VisualElement cell)
        {
            int index = childCount;
            cell.style.position = Position.Absolute;
            cell.style.left = index % columns * (cellSize.x + spacing.x);
            cell.style.top = index / columns * (cellSize.y + spacing.y);
            cell.style.width = cellSize.x;
            cell.style.height = cellSize.y;
            cell.style.marginLeft = cell.style.marginTop = cell.style.marginRight = cell.style.marginBottom = 0;
            Add(cell);
            int rows = (childCount + columns - 1) / columns;
            style.height = rows * cellSize.y + (rows - 1) * spacing.y;
        }
        public void FitWidth(float width, int maximumColumns = 6)
        {
            if(width<=0)return;
            int next=Mathf.Clamp(Mathf.FloorToInt((width+spacing.x)/(cellSize.x+spacing.x)),1,maximumColumns);
            if(next==columns)return;
            columns=next;style.width=columns*cellSize.x+(columns-1)*spacing.x;
            for(int i=0;i<childCount;i++)
            {
                var cell=ElementAt(i);cell.style.left=i%columns*(cellSize.x+spacing.x);cell.style.top=i/columns*(cellSize.y+spacing.y);
            }
            int rows=(childCount+columns-1)/columns;style.height=rows*cellSize.y+Mathf.Max(0,rows-1)*spacing.y;
        }
    }
}
