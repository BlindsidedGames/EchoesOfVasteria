using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitBookScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitBookDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private VisualTreeAsset template;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private UIDocument document;
        private PanelSettings panel;
        private ToolkitBookView view;
        private Rect lastSafeArea;
        private Vector2Int lastSize;
        private float lastRatio = -1;
        private Dictionary<string, bool> expandedState;
        private Vector2 scrollOffset;

        public bool IsOpen => view != null;
        public bool IsConfigured => definition && theme && template && runtimeTheme && textSettings;

        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured) return false;
            document = GetComponent<UIDocument>();
            if (!panel)
            {
                panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                panel.sortingOrder = 100;
            }
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            view = new ToolkitBookView(definition, theme, template, expandedState);
            view.Root.style.position = Position.Absolute;
            document.rootVisualElement.Add(view.Root);
            ApplyLayout();
            // Restore after native layout has established the scroll bounds.
            var openedView = view;
            openedView.Root.schedule.Execute(() =>
            {
                if (view == openedView) openedView.Scroll.scrollOffset = scrollOffset;
            });
            return true;
        }

        public void Hide()
        {
            if (view != null)
            {
                expandedState = view.CaptureExpanded();
                scrollOffset = view.Scroll.scrollOffset;
            }
            view?.Dispose();
            view = null;
        }

        private void OnEnable()
        {
            Blindsided.EventHandler.OnLoadData += RefreshVisibility;
            Blindsided.EventHandler.OnQuestHandin += OnQuestHandin;
        }

        private void OnDisable()
        {
            Blindsided.EventHandler.OnLoadData -= RefreshVisibility;
            Blindsided.EventHandler.OnQuestHandin -= OnQuestHandin;
            Hide();
        }

        private void RefreshVisibility() => view?.RefreshVisibility();
        private void OnQuestHandin(string _) => RefreshVisibility();

        private void OnDestroy()
        {
            Hide();
            if (panel) Destroy(panel);
        }

        private void Update()
        {
            if (!IsOpen) return;
            var ratio = Application.isMobilePlatform ? 1f : SafeAreaRatio;
            if (lastSize.x != Screen.width || lastSize.y != Screen.height ||
                lastSafeArea != Screen.safeArea || !Mathf.Approximately(lastRatio, ratio))
                ApplyLayout();
        }

        private void ApplyLayout()
        {
            lastSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
            lastRatio = Application.isMobilePlatform ? 1f : SafeAreaRatio;
            var area = CalculateSafeArea(lastSize, lastSafeArea, lastRatio);
            view.Root.style.left = area.x+12;
            view.Root.style.top = area.y + 44;
            view.Root.style.width = area.width-24;
            view.Root.style.height = Mathf.Max(0, area.height - 56);
        }

        /// <summary>Matches the existing six-unit safe inset and saved 16:9–32:9 width setting.</summary>
        public static Rect CalculateSafeArea(Vector2Int screen, Rect deviceSafeArea, float ratio)
        {
            if (screen.x <= 0 || screen.y <= 0) return Rect.zero;
            const float height = 432, minimum = 16f / 9f;
            var scale = height / screen.y;
            var width = screen.x * scale;
            var left = Mathf.Max(6, deviceSafeArea.xMin * scale);
            var right = Mathf.Max(6, (screen.x - deviceSafeArea.xMax) * scale);
            var top = Mathf.Max(6, (screen.y - deviceSafeArea.yMax) * scale);
            var bottom = Mathf.Max(6, deviceSafeArea.yMin * scale);
            var availableWidth = width - left - right;
            var availableHeight = height - top - bottom;
            // A narrow safe area needs vertical letterboxing, as in ScreenSafeArea.
            if (availableWidth / availableHeight < minimum)
            {
                var inset = (availableHeight - availableWidth / minimum) * .5f;
                top += inset;
                bottom += inset;
                availableHeight = height - top - bottom;
            }
            var maximum = Mathf.Lerp(minimum, 32f / 9f, Mathf.Clamp01(ratio));
            if (Mathf.Abs(screen.x / (float)screen.y - minimum) >= .0005f && availableWidth / availableHeight > maximum)
            {
                var inset = (availableWidth - availableHeight * maximum) * .5f;
                left += inset;
                right += inset;
            }
            return new Rect(left, top, Mathf.Max(0, width - left - right), Mathf.Max(0, height - top - bottom));
        }
    }
}
