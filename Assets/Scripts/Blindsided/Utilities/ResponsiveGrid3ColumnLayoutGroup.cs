using UnityEngine;
using UnityEngine.UI;

namespace Blindsided.Utilities
{
    [AddComponentMenu("Layout/Responsive Grid (3 Columns)")]
    [ExecuteAlways]
    public class ResponsiveGrid3ColumnLayoutGroup : GridLayoutGroup
    {
        private const int Columns = 3;

        public override void CalculateLayoutInputHorizontal()
        {
            UpdateCellWidthFromRect();
            // Now let base compute the rest using the updated cellSize
            base.CalculateLayoutInputHorizontal();
        }

        public override void SetLayoutHorizontal()
        {
            UpdateCellWidthFromRect();
            base.SetLayoutHorizontal();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnforceFixedColumns();
            UpdateCellWidthFromRect();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            EnforceFixedColumns();
            UpdateCellWidthFromRect();
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateCellWidthFromRect();
        }

        private void EnforceFixedColumns()
        {
            if (constraint != Constraint.FixedColumnCount || constraintCount != Columns)
            {
                constraint = Constraint.FixedColumnCount;
                constraintCount = Columns;
                SetDirty();
            }
        }

        private void UpdateCellWidthFromRect()
        {
            EnforceFixedColumns();

            var rect = rectTransform.rect;
            var innerWidth = Mathf.Max(0f, rect.width - padding.horizontal);
            var totalSpacing = spacing.x * (Columns - 1);
            var computedWidth = (innerWidth - totalSpacing) / Columns;

            if (float.IsNaN(computedWidth) || float.IsInfinity(computedWidth))
                computedWidth = 0f;

            computedWidth = Mathf.Max(0f, computedWidth);

            if (!Mathf.Approximately(cellSize.x, computedWidth))
            {
                cellSize = new Vector2(computedWidth, cellSize.y);
                SetDirty();
            }
        }
    }
}


