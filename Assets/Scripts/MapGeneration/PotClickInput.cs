using System.Collections.Generic;
using TimelessEchoes.Characters;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TimelessEchoes.MapGeneration
{
    // One input reader for all pots; no physics obstacles or per-pot input polling.
    public sealed class PotClickInput : MonoBehaviour
    {
        private static PotClickInput instance;
        private readonly List<RaycastResult> uiHits = new();
        private Vector2 pressPosition;
        private BreakablePot pressedPot;
        private int pointerId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        internal static void EnsureActive()
        {
            if (!instance) instance = new GameObject("Mine pot clicks").AddComponent<PotClickInput>();
        }

        private bool Blocked(Vector2 position)
        {
            if (ToolkitDimScreen.BlocksInput || CharacterCreation.Required || ToolkitCharacterCreator.BlocksWorldInput ||
                (TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow)) return true;
            if (!EventSystem.current) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            return uiHits.Count != 0;
        }

        private static BreakablePot Pick(Camera camera, Vector2 screen)
        {
            if (!camera.pixelRect.Contains(screen)) return null;
            var world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            BreakablePot picked = null;
            foreach (var pot in BreakablePot.Active)
            {
                if (!pot || pot.Broken || !pot.Visual.enabled) continue;
                var bounds = pot.Visual.bounds;
                if (world.x < bounds.min.x || world.x > bounds.max.x || world.y < bounds.min.y || world.y > bounds.max.y) continue;
                if (!picked || pot.Visual.sortingOrder > picked.Visual.sortingOrder ||
                    (pot.Visual.sortingOrder == picked.Visual.sortingOrder && pot.transform.position.y < picked.transform.position.y)) picked = pot;
            }
            return picked;
        }

        private void Update()
        {
            if (BreakablePot.Active.Count == 0 || !Application.isFocused) { pressedPot = null; return; }
            var camera = Camera.main;
            if (!camera) { pressedPot = null; return; }
            Vector2 position;
            bool down, released;
            int id;
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame))
            {
                var count = 0;
                foreach (var contact in Touchscreen.current.touches) if (contact.press.isPressed) count++;
                if (count > 1) { pressedPot = null; return; }
                position = touch.position.ReadValue(); id = touch.touchId.ReadValue();
                down = touch.press.wasPressedThisFrame; released = touch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue(); id = -1;
                down = Mouse.current.leftButton.wasPressedThisFrame; released = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else { pressedPot = null; return; }

            if (down)
            {
                pressPosition = position; pointerId = id;
                pressedPot = Blocked(position) ? null : Pick(camera, position);
            }
            if (pressedPot && (pointerId != id || Vector2.Distance(pressPosition, position) > 12f || Blocked(position))) pressedPot = null;
            if (!released) return;
            if (pressedPot && Pick(camera, position) == pressedPot) pressedPot.Break();
            pressedPot = null;
        }

        private void OnDisable() => pressedPot = null;
    }
}
