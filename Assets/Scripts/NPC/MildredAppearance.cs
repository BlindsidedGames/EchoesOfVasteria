using System.Collections.Generic;
using TimelessEchoes.Characters;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.NPC
{
    [DisallowMultipleComponent]
    public sealed class MildredAppearance : MonoBehaviour
    {
        public const string PreferenceKey = "Appearance.MildredColour";
        private static MildredColours colours;
        private static int selected = -1;
        private SpriteRenderer visual;
        private readonly List<RaycastResult> uiHits = new();
        private Vector2 pressPosition;
        private int pointerId;
        private bool pressed;

        public static MildredColours Colours => colours ? colours : colours = Resources.Load<MildredColours>("MildredColours/Colours");
        public static int Selected => selected < 0 ? selected = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, MildredColours.Count - 1) : selected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { colours = null; selected = -1; }

        public static void Select(int colour)
        {
            selected = Mathf.Clamp(colour, 0, MildredColours.Count - 1);
            PlayerPrefs.SetInt(PreferenceKey, selected);
            PlayerPrefs.Save();
        }

        private void Awake() => visual = GetComponentInChildren<SpriteRenderer>();

        private void LateUpdate()
        {
            // Animator owns frame timing; only replace its chosen frame after animation evaluation.
            if (visual && Colours) visual.sprite = Colours.Remap(visual.sprite, Selected);
        }

        private bool Blocked(Vector2 position)
        {
            if (ToolkitDimScreen.BlocksInput || ToolkitMildredPicker.BlocksInput || CharacterCreation.Required ||
                ToolkitCharacterCreator.BlocksWorldInput || ToolkitMeetingScreen.HasOpenDialogue || WorldPresentation.IsBusy ||
                (TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow)) return true;
            var navigation = FindAnyObjectByType<ToolkitNavigationScreen>();
            if (navigation && (navigation.HasOpenMenu || !navigation.CanDismissWindow)) return true;
            if (!EventSystem.current) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            foreach (var hit in uiHits)
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

        private bool Hit(Camera camera, Vector2 position)
        {
            if (!visual || !visual.enabled || !visual.sprite || !camera.pixelRect.Contains(position)) return false;
            var bounds = visual.bounds;
            var a = camera.WorldToScreenPoint(bounds.min); var b = camera.WorldToScreenPoint(bounds.max);
            // Keep the small cat tappable on phones without adding a physics obstacle.
            var size = new Vector2(Mathf.Max(44, b.x - a.x), Mathf.Max(44, b.y - a.y));
            return new Rect((Vector2)(a + b) * .5f - size * .5f, size).Contains(position);
        }

        private void Update()
        {
            var camera = Camera.main;
            if (!camera || !Application.isFocused) { pressed = false; return; }
            Vector2 position; bool down, released; int id;
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame))
            {
                var count = 0;
                foreach (var contact in Touchscreen.current.touches) if (contact.press.isPressed) count++;
                if (count > 1) { pressed = false; return; }
                position = touch.position.ReadValue(); id = touch.touchId.ReadValue();
                down = touch.press.wasPressedThisFrame; released = touch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue(); id = -1;
                down = Mouse.current.leftButton.wasPressedThisFrame; released = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else { pressed = false; return; }
            if (down) { pressPosition = position; pointerId = id; pressed = !Blocked(position) && Hit(camera, position); }
            if (pressed && (pointerId != id || Vector2.Distance(pressPosition, position) > 12f || Blocked(position))) pressed = false;
            if (!released) return;
            if (pressed && Hit(camera, position)) FindAnyObjectByType<ToolkitNavigationScreen>()?.ShowMildredColours();
            pressed = false;
        }

        private void OnDisable() => pressed = false;
    }
}
