using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Authored help links use stable game routes, never translated menu names or URLs.</summary>
    public static class ToolkitHelpLinks
    {
        private sealed class State
        {
            public Func<string, bool> localRoute;
            public Vector2 pressedAt;
            public string pressedLink;
            public bool dragged, linkClick;
            public int pointerId;
        }
        private static readonly ConditionalWeakTable<TextElement, State> States = new();

        public static void Attach(TextElement text, Func<string, bool> localRoute = null)
        {
            if (States.TryGetValue(text, out var existing))
            {
                if (localRoute != null) existing.localRoute = localRoute;
                return;
            }
            var state = new State { localRoute = localRoute }; States.Add(text, state);
            text.pickingMode = PickingMode.Position;
            VisualElement trackingRoot = null;
            void StopTracking()
            {
                if (trackingRoot == null) return;
                trackingRoot.UnregisterCallback<PointerMoveEvent>(TrackMove, TrickleDown.TrickleDown);
                trackingRoot.UnregisterCallback<PointerUpEvent>(TrackUp, TrickleDown.TrickleDown);
                trackingRoot.UnregisterCallback<PointerCancelEvent>(TrackCancel, TrickleDown.TrickleDown);
                trackingRoot = null;
            }
            void TrackMove(PointerMoveEvent evt)
            {
                if (evt.pointerId == state.pointerId && Vector2.Distance(state.pressedAt, evt.position) > 8) state.dragged = true;
            }
            void TrackUp(PointerUpEvent evt)
            {
                if (evt.pointerId != state.pointerId) return;
                if (Vector2.Distance(state.pressedAt, evt.position) > 8) state.dragged = true;
                StopTracking();
            }
            void TrackCancel(PointerCancelEvent evt)
            {
                if (evt.pointerId != state.pointerId) return;
                state.dragged = true; state.pressedLink = null; StopTracking();
            }
            text.RegisterCallback<DetachFromPanelEvent>(_ => StopTracking());
            text.RegisterCallback<PointerDownEvent>(_ => { StopTracking(); state.linkClick = false; state.pressedLink = null; }, TrickleDown.TrickleDown);
            text.RegisterCallback<PointerDownLinkTagEvent>(evt =>
            {
                if (evt.button != 0 || !evt.linkID.StartsWith("help:", StringComparison.Ordinal)) return;
                state.pressedAt = evt.position; state.pointerId = evt.pointerId;
                state.pressedLink = evt.linkID; state.dragged = false; state.linkClick = true;
                // ScrollView can capture the pointer after press. Observe its move
                // at the panel root so a captured drag cannot become a link tap.
                trackingRoot = text.panel?.visualTree;
                trackingRoot?.RegisterCallback<PointerMoveEvent>(TrackMove, TrickleDown.TrickleDown);
                trackingRoot?.RegisterCallback<PointerUpEvent>(TrackUp, TrickleDown.TrickleDown);
                trackingRoot?.RegisterCallback<PointerCancelEvent>(TrackCancel, TrickleDown.TrickleDown);
            });
            text.RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                var activate = evt.button == 0 && !state.dragged && state.pressedLink == evt.linkID;
                state.pressedLink = null;
                if (!evt.linkID.StartsWith("help:", StringComparison.Ordinal)) return;
                evt.StopPropagation();
                if (activate) Follow(evt.linkID.Substring(5), state.localRoute);
            });
            // Clicking a help link inside a quest must not also pin that quest.
            text.RegisterCallback<ClickEvent>(evt =>
            {
                if (state.linkClick) { evt.StopPropagation(); state.linkClick = false; }
            });
            text.RegisterCallback<PointerCancelEvent>(_ => { state.pressedLink = null; state.dragged = true; });
        }

        public static void Follow(string route, Func<string, bool> localRoute = null)
        {
            if (localRoute?.Invoke(route) == true) return;
            var navigation = UnityEngine.Object.FindAnyObjectByType<ToolkitNavigationScreen>();
            if (navigation && navigation.OpenHelpDestination(route))
            { Audio.AudioManager.Instance?.PlayUIButtonClick(); return; }
            var runOnly = (route == "hero-stats" || route == "run-summary") && (!GameManager.Instance || !GameManager.Instance.CurrentMap);
            UnityEngine.Object.FindAnyObjectByType<ToolkitNotificationScreen>()?.ShowLocalized(null,
                runOnly ? "help.destination-run-only" : "help.destination-locked",
                runOnly ? "Start an adventure to view this screen." : "This screen is not available yet. Continue your quests or finish the current lesson.", null, 4);
        }
    }
}
