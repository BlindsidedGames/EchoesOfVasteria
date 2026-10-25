using Screen = UnityEngine.Device.Screen;
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
        private Button open, confirm, discord;
        private bool confirming;
        private ToolkitNavigationScreen navigation;
        private ToolkitRunScreen run;
        private readonly GameQuitRequest request = new();
        private readonly List<ToolkitTextBinding> bindings = new();

        private void OnEnable()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;
            if (!definition || !theme || !runtimeTheme || !textSettings) return;
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 150; }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "quit-controls", pickingMode = PickingMode.Ignore };
            root.AddToClassList("eov-quit"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            root.style.alignItems = Align.Center;
            discord = MakeButton("discord", definition.discord, 24, () => Application.OpenURL(definition.discordUrl));
            discord.tooltip = "Discord";
            discord.style.height = 24;
            discord.style.marginRight = 4;
            var discordIcon = new Image { sprite = definition.discord, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            discordIcon.style.width = 16; discordIcon.style.height = 16;
            discord.Add(discordIcon);
            root.Add(discord);
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
            if (!windows) windows = TownWindowManager.Instance;
            if (!navigation) navigation = FindAnyObjectByType<ToolkitNavigationScreen>();
            if (!run) run = FindAnyObjectByType<ToolkitRunScreen>();
            var inTown = ToolkitResponsiveLayout.TownView;
            var showControls = inTown && (!windows || !windows.HasOpenWindow) && (!navigation || !navigation.HasOpenMenu) && (!run || !run.HasOpenDetails);
            var showQuit = showControls && Screen.fullScreen && !UnityEngine.Device.Application.isMobilePlatform && !ToolkitResponsiveLayout.IsPortrait;
            if (!showQuit && confirming) SetConfirmation(false);
            open.style.display = showQuit && !confirming ? DisplayStyle.Flex : DisplayStyle.None;
            confirmation.style.display = showQuit && confirming ? DisplayStyle.Flex : DisplayStyle.None;
            discord.style.marginRight = showQuit ? 4 : 0;
            var touchSize = ToolkitResponsiveLayout.IsPortrait ? 32 : 24;
            discord.style.width = touchSize; discord.style.height = touchSize;
            root.style.display = showControls ? DisplayStyle.Flex : DisplayStyle.None;
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea, UnityEngine.Device.Application.isMobilePlatform ? 1 : SafeAreaRatio);
            root.style.left = area.xMax - (ToolkitResponsiveLayout.IsPortrait ? 6 : 0);
            root.style.top = area.y + (ToolkitResponsiveLayout.IsPortrait ? 6 : 0);
        }

        private void OnDisable()
        {
            foreach (var binding in bindings) binding.Dispose();
            bindings.Clear(); root?.RemoveFromHierarchy(); root = null;
        }
        private void OnDestroy() { if (panel) Destroy(panel); }
    }
}

