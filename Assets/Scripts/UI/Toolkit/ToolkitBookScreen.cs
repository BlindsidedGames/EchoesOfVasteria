using Screen = UnityEngine.Device.Screen;
using UnityEngine;
using UnityEngine.UIElements;
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
        private float nextSharedEffectsRefresh;
        private string selectedSection;
        private bool referenceSelected;
        private Vector2 scrollOffset;
        private readonly System.Collections.Generic.Dictionary<string, int> selectedIllustrations = new();

        public bool IsOpen => view != null;
        public bool IsConfigured => definition && theme && template && runtimeTheme && textSettings;

        public bool Show()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
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
            view = new ToolkitBookView(definition, theme, template, selectedSection, referenceSelected, selectedIllustrations);
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
                selectedSection = view.SelectedSectionId;
                referenceSelected = view.ReferenceSelected;
                scrollOffset = view.Scroll.scrollOffset;
            }
            view?.Dispose();
            view = null;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref panel);
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
            if (Time.unscaledTime >= nextSharedEffectsRefresh)
            {
                nextSharedEffectsRefresh = Time.unscaledTime + .5f;
                view.RefreshSharedEffects();
            }
            ToolkitResponsiveLayout.ApplyFocusPresentation(view.Root);
            var ratio = UnityEngine.Device.Application.isMobilePlatform ? 1f : SafeAreaRatio;
            if (lastSize.x != Screen.width || lastSize.y != Screen.height ||
                lastSafeArea != Screen.safeArea || !Mathf.Approximately(lastRatio, ratio))
                ApplyLayout();
        }

        private void ApplyLayout()
        {
            lastSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
            lastRatio = UnityEngine.Device.Application.isMobilePlatform ? 1f : SafeAreaRatio;
            var area = CalculateSafeArea(lastSize, lastSafeArea, lastRatio);
            var width=ToolkitResponsiveLayout.IsPortrait ? area.width : Mathf.Min(definition.presentation == ToolkitBookDefinition.Presentation.Library ? 920 : 640,area.width-24);
            view.Root.EnableInClassList("library-narrow", width < 500);
            view.RefreshPortrait();
            view.Root.style.left = area.center.x-width/2;
            view.Root.style.top = area.y + ToolkitResponsiveLayout.WindowTop;
            view.Root.style.width = width;
            view.Root.style.height = Mathf.Max(0, area.height - ToolkitResponsiveLayout.WindowTop - ToolkitResponsiveLayout.WindowBottom);
        }

        /// <summary>Matches the existing six-unit safe inset and saved 16:9–32:9 width setting.</summary>
        public static Rect CalculateSafeArea(Vector2Int screen, Rect deviceSafeArea, float ratio)
        {
            if (screen.x <= 0 || screen.y <= 0) return Rect.zero;
            if (ToolkitResponsiveLayout.Portrait(screen)) return ToolkitResponsiveLayout.DockArea(screen, deviceSafeArea);
            const float height = 432, minimum = 16f / 9f;
            var scale = height / screen.y;
            var width = screen.x * scale;
            var left = Mathf.Max(6, deviceSafeArea.xMin * scale);
            var right = Mathf.Max(6, (screen.x - deviceSafeArea.xMax) * scale);
            var top = Mathf.Max(6, (screen.y - deviceSafeArea.yMax) * scale);
            var bottom = Mathf.Max(6, deviceSafeArea.yMin * scale);
            var availableWidth = width - left - right;
            var availableHeight = height - top - bottom;
            // Narrow windows use the full safe height. The camera crops the
            // scene independently; UI padding must not reintroduce letterboxing.
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
