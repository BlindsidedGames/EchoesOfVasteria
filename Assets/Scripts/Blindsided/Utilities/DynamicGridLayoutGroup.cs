using UnityEngine;
using UnityEngine.UI;

namespace Blindsided.Utilities
{
    [AddComponentMenu("Layout/Dynamic Grid Layout Group with Thresholds")]
    [ExecuteAlways]
    public class DynamicGridLayoutGroupThresholds : LayoutGroup
    {
        /* ───── inspector ───── */
        [Header("Spacing & Padding")] public Vector2 spacing = new(10f, 10f);

        [Header("Columns (0 = unlimited)")] public int maxColumns = 6;

        [Header("Target & Limits")] public float preferredCardWidth = 250f;
        public float minCardWidth = 200f;
        public float maxCardWidth = 300f;

        [Header("Aspect Ratio (width ÷ height)")]
        public float aspectRatio = 1f;

        /* ───── runtime state ───── */
        private int columns;
        private int rows;
        private float cardWidth, cardHeight;

        /* ───── layout pipeline ───── */
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            var inner = rectTransform.rect.width - padding.horizontal;
            columns = PickColumnCount(inner);
            cardWidth = (inner - (columns - 1) * spacing.x) / columns;
            if (cardWidth > maxCardWidth) cardWidth = maxCardWidth;

            SetLayoutInputForAxis(0, inner, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            rows = Mathf.CeilToInt(rectChildren.Count / (float)columns);
            cardHeight = CardHeight(cardWidth);

            var total = rows * cardHeight + (rows - 1) * spacing.y + padding.vertical;
            SetLayoutInputForAxis(total, total, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            LayoutChildren();
        }

        public override void SetLayoutVertical()
        {
            /* no-op */
        }

        /* ───── helpers ───── */

        private int PickColumnCount(float inner)
        {
            var upper = maxColumns <= 0
                ? Mathf.Max(1, Mathf.FloorToInt(inner / (minCardWidth + spacing.x)))
                : Mathf.Max(1, maxColumns);

            var chosen = 1;

            for (var c = 1; c <= upper; c++)
            {
                var w = (inner - (c - 1) * spacing.x) / c;

                if (w < minCardWidth)
                    break; // no more room – stick with previous count

                chosen = c; // remember last feasible count

                if (w <= maxCardWidth)
                    break; // perfect range – stop here
            }

            return chosen;
        }

        private float CardHeight(float width)
        {
            if (aspectRatio <= 0f) aspectRatio = 1f;
            return width / aspectRatio;
        }

        private void LayoutChildren()
        {
            float startX = padding.left;
            float startY = padding.top;

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var row = i / columns;
                var col = i % columns;

                var x = startX + col * (cardWidth + spacing.x);
                var y = startY + row * (cardHeight + spacing.y);

                SetChildAlongAxis(rectChildren[i], 0, x, cardWidth);
                SetChildAlongAxis(rectChildren[i], 1, y, cardHeight);
            }
        }

        /* ───── keep it live ───── */
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty(); // force rebuild when the parent resizes
        }
    }
}