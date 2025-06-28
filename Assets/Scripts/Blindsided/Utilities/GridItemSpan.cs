using UnityEngine;

/// <summary>
///     Add this to any UI element inside a MultiCellGridLayoutGroup
///     if you want it to span more than one grid cell.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class GridItemSpan : MonoBehaviour
{
    [Min(1)] public int ColumnSpan = 1;
    [Min(1)] public int RowSpan = 1;
}