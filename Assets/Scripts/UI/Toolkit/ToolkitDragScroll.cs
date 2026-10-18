using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Mouse dragging from content, leaving controls to handle their own gestures.</summary>
    public sealed class ToolkitDragScroll : PointerManipulator
    {
        private readonly ScrollView scroll;
        private Vector2 origin, offset;
        private int pointerId = -1;
        private bool dragging, suppressClick;

        public const string DragSourceClass = "content-drag-source";

        // Captured button moves go directly to the button, bypassing its viewport.
        public static void EnableFrom(ScrollView scroll, Button source)
        {
            if (source.ClassListContains(DragSourceClass)) return;
            source.AddToClassList(DragSourceClass);
            source.AddManipulator(new ToolkitDragScroll(scroll));
        }

        public static void Enable(ScrollView scroll)
        {
            scroll.mouseWheelScrollSize = 8;
            const string marker = "content-drag-scroll";
            if (scroll.ClassListContains(marker)) return;
            scroll.AddToClassList(marker);
            scroll.contentViewport.AddManipulator(new ToolkitDragScroll(scroll));
            scroll.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                foreach (var button in scroll.contentContainer.Query<Button>().ToList()) EnableFrom(scroll, button);
                // Put phone scrollbars in the outer gutter, without taking width
                // away from the content. Leave inset dialogs and nested columns alone.
                var gap = ToolkitResponsiveLayout.Size.x - scroll.worldBound.xMax;
                var edge = ToolkitResponsiveLayout.IsPortrait && gap >= 0 && gap <= 24;
                scroll.EnableInClassList("portrait-edge-scroll", edge);
                scroll.verticalScroller.style.right = edge ? new StyleLength(2 - gap) : StyleKeyword.Null;
            });
        }

        public ToolkitDragScroll(ScrollView scroll) => this.scroll = scroll;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
            target.RegisterCallback<DetachFromPanelEvent>(Detach);
            target.RegisterCallback<ClickEvent>(Click, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
            target.UnregisterCallback<DetachFromPanelEvent>(Detach);
            target.UnregisterCallback<ClickEvent>(Click, TrickleDown.TrickleDown);
            ResetDrag();
        }

        private void Down(PointerDownEvent evt)
        {
            ResetDrag();
            suppressClick = false;
            if (evt.button != 0) return;
            for (var element = evt.target as VisualElement; element != null && element != target; element = element.parent)
            {
                // Unity's logical parent chain can skip the internal viewport.
                if (element is ScrollView owner)
                {
                    if (owner != scroll) return;
                    break;
                }
                if (element is Button || (element is BindableElement && element.focusable)) return;
            }
            pointerId = evt.pointerId; origin = evt.position; offset = scroll.scrollOffset;
        }

        private void Move(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointerId || pointerId < 0) return;
            // A control can consume pointer-up before it reaches the viewport.
            if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && (evt.pressedButtons & 1) == 0) { ResetDrag(); return; }
            var delta = (Vector2)evt.position - origin;
            if (!dragging && delta.sqrMagnitude < 25) return;
            if (!dragging) { dragging = true; target.CapturePointer(pointerId); }
            scroll.scrollOffset = new Vector2(offset.x, Mathf.Clamp(offset.y - delta.y, 0, scroll.verticalScroller.highValue));
            evt.StopImmediatePropagation();
        }

        private void Up(PointerUpEvent evt)
        {
            if (evt.pointerId != pointerId) return;
            var wasDragging = dragging;
            if (wasDragging && target.HasPointerCapture(pointerId)) target.ReleasePointer(pointerId);
            pointerId = -1; dragging = false; suppressClick = wasDragging;
            if (wasDragging) evt.StopImmediatePropagation();
        }

        private void CaptureOut(PointerCaptureOutEvent evt)
        {
            // A draggable button may capture the press before we claim an actual drag.
            if (dragging && evt.target == target && evt.pointerId == pointerId && !target.HasPointerCapture(pointerId))
            { pointerId = -1; dragging = false; }
        }

        private void Detach(DetachFromPanelEvent evt) { ResetDrag(); suppressClick = false; }

        private void ResetDrag()
        {
            if (pointerId >= 0 && target.HasPointerCapture(pointerId)) target.ReleasePointer(pointerId);
            pointerId = -1;
            dragging = false;
        }

        private void Click(ClickEvent evt)
        {
            if (suppressClick) { suppressClick = false; evt.StopImmediatePropagation(); }
        }
    }
}
