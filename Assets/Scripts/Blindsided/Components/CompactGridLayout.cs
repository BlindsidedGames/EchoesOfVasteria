using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     Like a GridLayoutGroup that fills horizontally first, but its preferred size
///     is based on the *real* number of children, so a ContentSizeFitter won’t
///     leave empty space.  When child-count ≥ MaxColumns it behaves exactly like a
///     fixed-column grid.
/// </summary>
[AddComponentMenu("Layout/Compact Grid (max columns)")]
public class CompactGridLayout : LayoutGroup
{
    [SerializeField] private Vector2 cellSize = new(100, 100);
    [SerializeField] private Vector2 spacing = Vector2.zero;
    [SerializeField] private int maxColumns = 7;

    /* ---------- Size calculations -------------------------------------------------- */

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal(); // Sets padding.min / flexible = 0
        var cols = Mathf.Max(1, Mathf.Min(maxColumns, rectChildren.Count));
        var width = padding.horizontal +
                    cols * cellSize.x + (cols - 1) * spacing.x;

        SetLayoutInputForAxis(width, width, -1, 0); // min & preferred width
    }

    public override void CalculateLayoutInputVertical()
    {
        var cols = Mathf.Max(1, Mathf.Min(maxColumns, rectChildren.Count));
        var rows = Mathf.CeilToInt(rectChildren.Count / (float)cols);
        var height = padding.vertical +
                     rows * cellSize.y + (rows - 1) * spacing.y;

        SetLayoutInputForAxis(height, height, -1, 1); // min & preferred height
    }

    /* ---------- Child positioning --------------------------------------------------- */

    public override void SetLayoutHorizontal()
    {
        SetCellsAlongAxis(0);
    }

    public override void SetLayoutVertical()
    {
        SetCellsAlongAxis(1);
    }

    private void SetCellsAlongAxis(int axis)
    {
        var cols = Mathf.Max(1, Mathf.Min(maxColumns, rectChildren.Count));

        for (var i = 0; i < rectChildren.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;

            var x = padding.left + col * (cellSize.x + spacing.x);
            var y = padding.top + row * (cellSize.y + spacing.y);

            SetChildAlongAxis(rectChildren[i], 0, x, cellSize.x);
            SetChildAlongAxis(rectChildren[i], 1, y, cellSize.y);
        }
    }
}