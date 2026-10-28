using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitDimScreen : MonoBehaviour
    {
        private static ToolkitDimScreen instance;
        private static int blockThroughFrame = -1;
        private PanelSettings panel;
        private VisualElement cover, fill;
        private Label hint;
        private int pointer = -1, previousRenderInterval;
        private Vector2 pressPosition;
        private float pressedAt, previousBrightness;
        private bool powerApplied, paused;
        public static bool IsActive => instance && instance.cover != null;
        public static bool BlocksInput => IsActive || Time.frameCount <= blockThroughFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; blockThroughFrame = -1; }

        public static void Show(ToolkitTheme art, ThemeStyleSheet theme, PanelTextSettings textSettings)
        {
            if (IsActive) return;
            if (!instance)
            {
                var host = new GameObject("Dim screen", typeof(UIDocument));
                DontDestroyOnLoad(host);
                instance = host.AddComponent<ToolkitDimScreen>();
            }
            instance.Open(art, theme, textSettings);
        }

        private void Open(ToolkitTheme art, ThemeStyleSheet theme, PanelTextSettings textSettings)
        {
            panel = ToolkitPanel.CreateSettings(theme, textSettings);
            panel.sortingOrder = 30000;
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            cover = new VisualElement { name = "dim-screen", focusable = true };
            ToolkitLocaleFonts.Bind(cover, art.gameplayFont ? art.gameplayFont : art.font);
            cover.style.position = Position.Absolute;
            cover.style.left = cover.style.right = cover.style.top = cover.style.bottom = 0;
            cover.style.backgroundColor = Color.black;
            cover.style.alignItems = Align.Center;
            cover.style.justifyContent = Justify.Center;
            hint = new Label { name = "dim-screen-hint", pickingMode = PickingMode.Ignore };
            ToolkitControls.SetTextSize(hint, 8);
            hint.style.color = new Color(.35f, .35f, .35f);
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.maxWidth = Length.Percent(85);
            cover.Add(hint);
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.width = 100; track.style.height = 2; track.style.marginTop = 8;
            track.style.backgroundColor = new Color(.06f, .06f, .06f);
            cover.Add(track);
            fill = new VisualElement { name = "dim-screen-progress", pickingMode = PickingMode.Ignore };
            fill.style.height = Length.Percent(100); fill.style.width = 0;
            fill.style.backgroundColor = new Color(.35f, .35f, .35f);
            track.Add(fill);
            cover.RegisterCallback<PointerDownEvent>(e =>
            {
                e.StopPropagation();
                if (e.button != 0 || pointer >= 0) return;
                pointer = e.pointerId; pressPosition = e.position; pressedAt = Time.unscaledTime;
                cover.CapturePointer(pointer);
            });
            cover.RegisterCallback<PointerMoveEvent>(e =>
            {
                e.StopPropagation();
                if (e.pointerId == pointer && Vector2.Distance(pressPosition, e.position) > 12) CancelHold();
            });
            cover.RegisterCallback<PointerUpEvent>(e => { e.StopPropagation(); if (e.pointerId == pointer) CancelHold(); });
            cover.RegisterCallback<PointerCancelEvent>(e => { e.StopPropagation(); CancelHold(); });
            cover.RegisterCallback<PointerCaptureOutEvent>(_ => CancelHold());
            cover.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            cover.RegisterCallback<WheelEvent>(e => e.StopPropagation());
            cover.RegisterCallback<KeyDownEvent>(e => { e.StopPropagation(); e.PreventDefault(); });
            cover.RegisterCallback<NavigationSubmitEvent>(e => { e.StopPropagation(); e.PreventDefault(); });
            cover.RegisterCallback<NavigationCancelEvent>(e => { e.StopPropagation(); e.PreventDefault(); });
            cover.RegisterCallback<NavigationMoveEvent>(e => { e.StopPropagation(); e.PreventDefault(); });
            document.rootVisualElement.Add(cover);
            cover.Focus();
            RefreshText(); ToolkitLocalization.Changed += RefreshText;
            previousRenderInterval = OnDemandRendering.renderFrameInterval;
            if (Application.isMobilePlatform) previousBrightness = Screen.brightness;
            ApplyPowerSettings();
        }

        private void RefreshText() => hint.text = ToolkitLocalization.Text("options.hold-to-return", "Hold to return");

        private void ApplyPowerSettings()
        {
            if (powerApplied || !IsActive) return;
            var rate = QualitySettings.vSyncCount > 0
                ? (float)Screen.currentResolution.refreshRateRatio.value / QualitySettings.vSyncCount
                : Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
            OnDemandRendering.renderFrameInterval = Mathf.Max(previousRenderInterval, Mathf.CeilToInt(rate / 10f));
            if (Application.isMobilePlatform) Screen.brightness = 0;
            powerApplied = true;
        }

        private void RestorePowerSettings()
        {
            if (!powerApplied) return;
            OnDemandRendering.renderFrameInterval = previousRenderInterval;
            if (Application.isMobilePlatform) Screen.brightness = previousBrightness;
            powerApplied = false;
        }

        private void CancelHold()
        {
            var oldPointer = pointer; pointer = -1;
            if (oldPointer >= 0 && cover != null && cover.HasPointerCapture(oldPointer)) cover.ReleasePointer(oldPointer);
            if (fill != null) fill.style.width = 0;
        }

        private void Update()
        {
            if (cover == null || pointer < 0) return;
            var elapsed = Time.unscaledTime - pressedAt;
            fill.style.width = Length.Percent(Mathf.Clamp01(elapsed) * 100);
            if (elapsed >= 1) Hide();
        }

        private void Hide()
        {
            CancelHold(); RestorePowerSettings();
            ToolkitLocalization.Changed -= RefreshText;
            cover?.RemoveFromHierarchy(); cover = null; hint = null; fill = null;
            blockThroughFrame = Time.frameCount + 1;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref panel);
        }

        private void OnApplicationFocus(bool focus)
        {
            CancelHold();
            if (!focus) RestorePowerSettings(); else if (!paused) ApplyPowerSettings();
        }
        private void OnApplicationPause(bool pause)
        {
            paused = pause; CancelHold();
            if (pause) RestorePowerSettings(); else if (Application.isFocused) ApplyPowerSettings();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (instance == this) instance = null; }
    }
}
