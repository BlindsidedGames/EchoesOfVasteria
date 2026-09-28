using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitQuitScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitQuitDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private TownWindowManager windows;
        private PanelSettings panel;
        private VisualElement root, confirmation;
        private Button open, confirm;
        private bool confirming;
        private readonly GameQuitRequest request = new();
        private readonly List<ToolkitTextBinding> bindings = new();

        private void OnEnable()
        {
            if (!definition || !theme || !runtimeTheme || !textSettings) return;
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 150; }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "quit-controls", pickingMode = PickingMode.Ignore };
            root.AddToClassList("eov-quit"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            open = MakeButton("quit-open", definition.quit, definition.iconSize, () => SetConfirmation(true));
            root.Add(open);
            confirmation = new VisualElement { name = "quit-confirmation", pickingMode = PickingMode.Ignore };
            confirmation.style.flexDirection = FlexDirection.Row;
            confirm = MakeButton("quit-confirm", definition.confirm, definition.buttonWidth, Confirm);
            AddLabel(confirm, definition.confirmText);
            var cancel = MakeButton("quit-cancel", definition.cancel, definition.buttonWidth, () => SetConfirmation(false));
            AddLabel(cancel, definition.cancelText);
            confirm.style.marginRight = definition.spacing;
            confirmation.Add(confirm); confirmation.Add(cancel); root.Add(confirmation);
            document.rootVisualElement.Add(root);
            SetConfirmation(confirming); Update();
        }

        private Button MakeButton(string name, Sprite sprite, float width, System.Action action)
        {
            var button = new Button(action) { name = name };
            button.AddToClassList("eov-quit-button");
            button.style.width = width; button.style.height = definition.iconSize;
            button.AddToClassList("button");if(name=="quit-open")button.text="×";
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) Audio.AudioManager.Instance?.PlayUIButtonClick(); });
            button.RegisterCallback<NavigationSubmitEvent>(_ => Audio.AudioManager.Instance?.PlayUIButtonClick());
            return button;
        }

        private void AddLabel(Button button, ToolkitBookDefinition.Text text)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("eov-navigation-label"); if (!Mathf.Approximately(definition.fontSize, 8)) label.style.fontSize = definition.fontSize;
            button.Add(label); bindings.Add(new ToolkitTextBinding(label, text));
        }

        private void SetConfirmation(bool value)
        {
            confirming = value;
            open.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
            confirmation.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Confirm()
        {
            if (request.InProgress) return;
            confirm.SetEnabled(false);
            request.TryQuit(GameQuitRequest.Prepare, GameQuitRequest.Exit);
            if (!request.InProgress) confirm.SetEnabled(true);
        }

        private void Update()
        {
            if (root == null) return;
            var visible = !Application.isMobilePlatform && (GameManager.Instance == null || GameManager.Instance.CurrentMap == null)
                && (!windows || !windows.HasOpenWindow);
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);
            root.style.left = area.xMax; root.style.top = area.y;
        }

        private void OnDisable()
        {
            foreach (var binding in bindings) binding.Dispose();
            bindings.Clear(); root?.RemoveFromHierarchy(); root = null;
        }
        private void OnDestroy() { if (panel) Destroy(panel); }
    }
}

