using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitIntroScreen : MonoBehaviour
    {
        private const string CompletedKey = "TimelessEchoes.IntroScreenCompleted";
        [SerializeField] private ToolkitIntroDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private VisualTreeAsset template;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private readonly List<ToolkitTextBinding> bindings = new();
        private PanelSettings panel;
        private VisualElement root, progress, fill;
        private Label closeLabel;
        private Button close;
        private float remaining;
        public bool IsVisible => root != null;
        public bool IsReady => IsVisible && remaining <= 0;

        private void Start()
        {
            if (PlayerPrefs.GetInt(CompletedKey, 0) != 1) Show();
        }

        public void Show()
        {
            if (root != null) return;
            if (!definition || !theme || !template || !runtimeTheme || !textSettings)
            {
                Debug.LogError("Native introduction assets are not configured.", this);
                return;
            }
            var document = GetComponent<UIDocument>();
            if (!panel)
            {
                panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                panel.sortingOrder = 200;
            }
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = template.CloneTree();
            root.AddToClassList("eov-intro");
            root.style.width = definition.width;
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            close = root.Q<Button>("intro-close");
            close.AddToClassList("surface");
            
            progress = root.Q("intro-progress");
            fill = root.Q("intro-fill");
            progress.AddToClassList("track");
            fill.AddToClassList("fill");
            closeLabel = root.Q<Label>("intro-close-label");
            bindings.Add(new ToolkitTextBinding(root.Q<Label>("intro-title"), definition.title));
            bindings.Add(new ToolkitTextBinding(root.Q<Label>("intro-body"), definition.body));
            bindings.Add(new ToolkitTextBinding(closeLabel, definition.close));
            close.clicked += TryClose;
            remaining = Mathf.Max(0, definition.countdownSeconds);
            document.rootVisualElement.Add(root);
            Refresh();
        }

        private void Update()
        {
            if (!IsVisible) return;
            remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
            Refresh();
        }

        private void Refresh()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height),
                Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);
            root.style.left = area.center.x;
            root.style.top = area.center.y;
            var ready = remaining <= 0;
            close.SetEnabled(ready);
            progress.style.display = ready ? DisplayStyle.None : DisplayStyle.Flex;
            closeLabel.style.display = ready ? DisplayStyle.Flex : DisplayStyle.None;
            fill.style.width = Length.Percent(definition.countdownSeconds <= 0 ? 100 :
                100 * (1 - Mathf.Clamp01(remaining / definition.countdownSeconds)));
        }

        public void TryClose()
        {
            if (!IsReady) return;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            Audio.AudioManager.Instance?.PlayUIButtonClick();
            Hide();
        }

        public void Hide()
        {
            if (close != null) close.clicked -= TryClose;
            foreach (var binding in bindings) binding.Dispose();
            bindings.Clear();
            root?.RemoveFromHierarchy();
            root = null;
            close = null;
        }

        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (panel) Destroy(panel); }
    }
}
