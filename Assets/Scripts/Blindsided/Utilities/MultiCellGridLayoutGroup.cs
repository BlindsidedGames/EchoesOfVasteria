using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     A LayoutGroup that lets children occupy multiple cells (row/column spans),
///     similar to inventory grids in games like Diablo or Escape From Tarkov.
/// </summary>
[AddComponentMenu("Layout/Multi-Cell Grid Layout Group")]
public class MultiCellGridLayoutGroup : LayoutGroup
{
    [Header("Grid Settings")] [Min(1)] public int Columns = 5;
    public Vector2 CellSize = new(100, 100);
    public Vector2 Spacing = new(5, 5);

    // crude max-row cap; increase if you expect taller inventories
    private const int MaxRows = 100;
    private bool[,] occupancy; // [col, row]

    #region LayoutGroup overrides ------------------------------------------------

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        ArrangeItems();
    }

    public override void CalculateLayoutInputVertical()
    {
    } // handled above

    public override void SetLayoutHorizontal()
    {
    } // nothing to do

    public override void SetLayoutVertical()
    {
    } // nothing to do

    #endregion -------------------------------------------------------------------

    private void ArrangeItems()
    {
        var childCount = rectChildren.Count;
        occupancy = new bool[Columns, MaxRows];

        var maxRowUsed = 0;

        for (var i = 0; i < childCount; i++)
        {
            var child = rectChildren[i];
            var span = child.GetComponent<GridItemSpan>();

            var colSpan = Mathf.Clamp(span ? span.ColumnSpan : 1, 1, Columns);
            var rowSpan = Mathf.Max(1, span ? span.RowSpan : 1);

            if (!FindSpace(colSpan, rowSpan, out var x, out var y))
            {
                Debug.LogWarning($"MultiCellGridLayoutGroup: No space left for {child.name} ({colSpan}×{rowSpan}).");
                continue;
            }

            MarkSpace(x, y, colSpan, rowSpan);

            var posX = padding.left + (CellSize.x + Spacing.x) * x;
            var posY = padding.top + (CellSize.y + Spacing.y) * y;

            SetChildAlongAxis(child, 0, posX,
                CellSize.x * colSpan + Spacing.x * (colSpan - 1));
            SetChildAlongAxis(child, 1, posY,
                CellSize.y * rowSpan + Spacing.y * (rowSpan - 1));

            maxRowUsed = Mathf.Max(maxRowUsed, y + rowSpan);
        }

        // tell Unity the preferred size of this layout group
        var width = padding.horizontal + (CellSize.x + Spacing.x) * Columns - Spacing.x;
        var height = padding.vertical + (CellSize.y + Spacing.y) * maxRowUsed - Spacing.y;

        SetLayoutInputForAxis(width, width, -1, 0);
        SetLayoutInputForAxis(height, height, -1, 1);
    }

    #region grid-helpers ----------------------------------------------------------

    private bool FindSpace(int w, int h, out int outX, out int outY)
    {
        for (var y = 0; y <= MaxRows - h; y++)
        for (var x = 0; x <= Columns - w; x++)
            if (IsAreaFree(x, y, w, h))
            {
                outX = x;
                outY = y;
                return true;
            }

        outX = outY = 0;
        return false;
    }

    private bool IsAreaFree(int x, int y, int w, int h)
    {
        for (var yy = y; yy < y + h; yy++)
        for (var xx = x; xx < x + w; xx++)
            if (occupancy[xx, yy])
                return false;
        return true;
    }

    private void MarkSpace(int x, int y, int w, int h)
    {
        for (var yy = y; yy < y + h; yy++)
        for (var xx = x; xx < x + w; xx++)
            occupancy[xx, yy] = true;
    }

    #endregion -------------------------------------------------------------------
}