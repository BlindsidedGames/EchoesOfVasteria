using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    internal static class ToolkitTapDismiss
    {
        public static void Bind(VisualElement surface, Action dismiss, Func<bool> allowed)
        {
            var pointer = -1;
            var origin = Vector2.zero;
            surface.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || !evt.isPrimary || evt.target != surface) return;
                pointer = evt.pointerId; origin = evt.position;
                evt.StopPropagation();
            });
            surface.RegisterCallback<PointerCancelEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 0 || evt.pointerId != pointer || evt.target != surface) return;
                pointer = -1;
                var tapped = ((Vector2)evt.position - origin).sqrMagnitude <= 64;
                evt.StopPropagation();
                // Finish delivery of the release before removing its receiving panel.
                if (tapped) surface.schedule.Execute(() => { if (allowed()) dismiss(); });
            });
        }
    }
}
