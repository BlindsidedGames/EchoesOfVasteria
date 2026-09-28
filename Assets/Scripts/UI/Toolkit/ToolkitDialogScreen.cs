using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native two-action dialog. The caller retains ownership of all operations.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitDialogScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private VisualTreeAsset template;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private Sprite frame, primarySprite;
        [SerializeField] private float maximumWidth = 480;
        private PanelSettings panel;
        private VisualElement root;
        private Label titleLabel, bodyLabel;
        private Button primary, secondary;
        private Action primaryAction, secondaryAction;
        private bool interactable;
        public bool IsVisible => root != null;

        public void Show(string title, string body, string primaryText, string secondaryText,
            Action onPrimary, Action onSecondary, bool enabled = true)
        {
            if (root == null) Create();
            titleLabel.text = "<b><smallcaps>" + title + "</smallcaps></b>";
            bodyLabel.text = body;
            primary.text = primaryText;
            secondary.text = secondaryText;
            primaryAction = onPrimary;
            secondaryAction = onSecondary;
            SetInteractable(enabled);
            ApplyLayout();
        }

        private void Create()
        {
            if (!theme || !template || !runtimeTheme || !textSettings || !frame || !primarySprite)
                throw new InvalidOperationException("Native dialog assets are not configured.");
            if (!panel)
            {
                panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                panel.sortingOrder = 1000;
            }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = template.CloneTree();
            root.AddToClassList("eov-dialog");
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            root.Q("dialog-frame").AddToClassList("surface");
            titleLabel = root.Q<Label>("dialog-title");
            titleLabel.enableRichText = true;
            bodyLabel = root.Q<Label>("dialog-body");
            // Diagnostics may contain markup-like text; display it literally.
            bodyLabel.enableRichText = false;
            var scroll = root.Q<ScrollView>("dialog-scroll");
            
            theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll);
            primary = root.Q<Button>("dialog-primary");
            secondary = root.Q<Button>("dialog-secondary");
            primary.AddToClassList("button");primary.AddToClassList("primary");
            secondary.AddToClassList("button");
            primary.clicked += InvokePrimary;
            secondary.clicked += InvokeSecondary;
            document.rootVisualElement.Add(root);
        }

        private void InvokePrimary() { if (interactable) primaryAction?.Invoke(); }
        private void InvokeSecondary() { if (interactable) secondaryAction?.Invoke(); }

        public void SetInteractable(bool value)
        {
            interactable = value;
            primary?.SetEnabled(value);
            secondary?.SetEnabled(value);
        }

        private void Update() { if (root != null) ApplyLayout(); }

        private void ApplyLayout()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height),
                Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);
            root.style.left = area.center.x;
            root.style.top = area.center.y;
            root.style.width = Mathf.Max(1, Mathf.Min(maximumWidth, area.width - 12));
            root.style.maxHeight = Mathf.Max(1, area.height - 12);
            root.Q<ScrollView>("dialog-scroll").style.maxHeight = Mathf.Max(1, area.height - 70);
        }

        public void Hide()
        {
            if (primary != null) primary.clicked -= InvokePrimary;
            if (secondary != null) secondary.clicked -= InvokeSecondary;
            primaryAction = secondaryAction = null;
            root?.RemoveFromHierarchy();
            root = null;
            primary = secondary = null;
            interactable = false;
        }

        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (panel) Destroy(panel); }
    }
}
