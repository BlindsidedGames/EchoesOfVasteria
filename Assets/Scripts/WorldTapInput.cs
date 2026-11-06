using System.Collections.Generic;
using TimelessEchoes.Characters;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes
{
    // Shared tap filtering for small world actors. Dragging town or touching UI never selects one.
    public sealed class WorldTapInput
    {
        private readonly List<RaycastResult> hits = new();
        private Vector2 start;
        private int pointerId;
        private bool pressed;

        public bool Released(out Vector2 position)
        {
            position = default;
            if (!Application.isFocused) { pressed = false; return false; }
            bool down, released;
            int id;
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame))
            {
                var count = 0;
                foreach (var contact in Touchscreen.current.touches) if (contact.press.isPressed) count++;
                if (count > 1) { pressed = false; return false; }
                position = touch.position.ReadValue(); id = touch.touchId.ReadValue();
                down = touch.press.wasPressedThisFrame; released = touch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue(); id = -1;
                down = Mouse.current.leftButton.wasPressedThisFrame; released = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else { pressed = false; return false; }
            if (down) { start = position; pointerId = id; pressed = !Blocked(position); }
            if (pressed && (pointerId != id || Vector2.Distance(start, position) > 12 || Blocked(position))) pressed = false;
            if (!released) return false;
            var result = pressed;
            pressed = false;
            return result;
        }

        private bool Blocked(Vector2 position)
        {
            if (ToolkitDimScreen.BlocksInput || ToolkitMildredPicker.BlocksInput || ToolkitConversationScreen.BlocksInput ||
                CharacterCreation.Required || ToolkitCharacterCreator.BlocksWorldInput ||
                ToolkitMeetingScreen.HasVisibleMeeting || WorldPresentation.IsBusy ||
                TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) return true;
            var navigation = Object.FindAnyObjectByType<ToolkitNavigationScreen>();
            if (navigation && (navigation.HasOpenMenu || !navigation.CanDismissWindow)) return true;
            if (!EventSystem.current) return false;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            foreach (var hit in hits)
            {
                if (hit.module is PanelRaycaster toolkit)
                {
                    var point = new Vector2(position.x, UnityEngine.Device.Screen.height - position.y);
                    if (toolkit.panel != null && toolkit.panel.Pick(RuntimePanelUtils.ScreenToPanel(toolkit.panel, point)) != null) return true;
                }
                else return true;
            }
            return false;
        }
    }
}
