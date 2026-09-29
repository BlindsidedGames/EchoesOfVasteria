using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;
using Entry = TimelessEchoes.UI.Toolkit.ToolkitNavigationDefinition.Entry;
using Group = TimelessEchoes.UI.Toolkit.ToolkitNavigationDefinition.Group;
using NavAction = TimelessEchoes.UI.Toolkit.ToolkitNavigationDefinition.Action;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitNavigationScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitNavigationDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private VisualTreeAsset template;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private TownWindowManager windows;
        private readonly List<ToolkitTextBinding> bindings = new();
        private readonly Dictionary<Entry, VisualElement> rows = new();
        private readonly Dictionary<Entry, Button> buttons = new();
        private PanelSettings panel;
        private VisualElement root, popup, autoPinGroup;
        private Button autoPin, discord;
        private Image forgeAttention, cauldronAttention, questAttention;
        private Label echoBalance;
        private VisualElement progressRoot, progressFill;
        private Image progressHero;
        private Label progressText;
        private RunProgressPresentation lastProgress;
        private bool hasProgress;
        private Group openGroup;
        private Button menuAnchor;
        private float nextRefresh;
        private bool hooked;
        public Group OpenGroup => openGroup;

        private void OnEnable()
        {
            if (!definition || !theme || !template || !runtimeTheme || !textSettings) return;
            Create();
        }

        private void Create()
        {
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 150; }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = template.CloneTree();
            root.AddToClassList("eov-navigation");
            root.pickingMode = PickingMode.Ignore;
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            root.Q("navigation-bar").pickingMode = PickingMode.Ignore;
            popup = root.Q("navigation-popup");
            foreach (var entry in definition.entries)
            {
                var row = new VisualElement { name = entry.id + "-row", pickingMode = PickingMode.Ignore };
                row.AddToClassList("eov-navigation-row");
                var shadow = new VisualElement { pickingMode = PickingMode.Ignore };
                shadow.AddToClassList("eov-navigation-shadow");
                ToolkitTheme.Background(shadow, entry.shadow);
                // Approved navigation has no detached drop-shadow layer.
                var button = new Button(() => Activate(entry)) { name = entry.id };
                button.AddToClassList("eov-navigation-button");
                button.style.minWidth = entry.width;
                button.AddToClassList("button");
                button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) Audio.AudioManager.Instance?.PlayUIButtonClick(); });
                button.RegisterCallback<NavigationSubmitEvent>(_ => Audio.AudioManager.Instance?.PlayUIButtonClick());
                if(entry.id=="stats") { button.Add(new ToolkitStatisticsGlyph()); button.tooltip="Statistics"; }
                else if (entry.action == NavAction.Close) { button.text="×";button.AddToClassList("close-control");button.tooltip="Close window"; }
                else if (entry.icon)
                {
                    VisualElement icon;
                    if (entry.iconSliced) { icon = new Image { sprite=entry.icon,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore }; }
                    else icon = new Image { sprite = entry.icon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("eov-navigation-icon");
                    icon.style.width = entry.iconSize.x; icon.style.height = entry.iconSize.y;
                    button.Add(icon);
                }
                else
                {
                    var label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("eov-navigation-label"); 
                    button.Add(label); bindings.Add(new ToolkitTextBinding(label, entry.label));
                }
                row.Add(button);
                rows.Add(entry, row); buttons.Add(entry, button);
                if (entry.action == NavAction.TownsfolkMenu)
                {
                    forgeAttention = AddAttention(button, definition.forgeAttention);
                    cauldronAttention = AddAttention(button, definition.cauldronAttention);
                }
                if (entry.action == NavAction.Window && entry.window == TownWindowManager.Window.Quests)
                {
                    questAttention = AddAttention(button, definition.questAttention);
                    questAttention.AddToClassList("eov-navigation-quest-attention");
                }
                if (entry.group == Group.Hub && entry.window == TownWindowManager.Window.AlterEchoes)
                {
                    echoBalance = new Label { name = "echo-balance", pickingMode = PickingMode.Ignore };
                    echoBalance.AddToClassList("eov-navigation-balance-label");
                    echoBalance.style.color = definition.balanceColor;
                    var balanceFrame = new VisualElement { pickingMode = PickingMode.Ignore };
                    balanceFrame.AddToClassList("eov-navigation-balance");
                    balanceFrame.AddToClassList("surface");
                    balanceFrame.Add(echoBalance); row.Add(balanceFrame);
                }
                if (entry.group == Group.Toolbar)
                {
                    root.Q(entry.action == NavAction.Close ? "navigation-close" : "navigation-buttons").Add(row);
                    if (entry.action == NavAction.Close) row.style.marginRight = 0;
                    if (entry.window == TownWindowManager.Window.Stats && entry.action == NavAction.Window) row.style.marginRight = 5;
                }
                else popup.Add(row);
            }
            CreateContext();
            document.rootVisualElement.Add(root);
            CloseMenu();
            Refresh();
        }

        private Image AddAttention(Button button, Sprite sprite)
        {
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList("eov-navigation-attention"); button.Add(image); return image;
        }

        private void CreateContext()
        {
            var context = root.Q("navigation-context");
            progressRoot = new VisualElement { name = "run-progress", pickingMode = PickingMode.Ignore };
            progressRoot.AddToClassList("eov-navigation-progress");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("eov-navigation-progress-track");
            track.AddToClassList("track");
            progressFill = new VisualElement { pickingMode = PickingMode.Ignore };
            progressFill.style.height = Length.Percent(100);
            progressFill.AddToClassList("fill"); track.Add(progressFill); progressRoot.Add(track);
            progressHero = new Image { sprite = definition.progressHero, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            progressHero.AddToClassList("eov-navigation-progress-actor");progressHero.AddToClassList("progress-hero"); progressRoot.Add(progressHero);
            var reaper = new Image { sprite = definition.progressReaper, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            reaper.AddToClassList("eov-navigation-progress-actor"); reaper.AddToClassList("progress-reaper");reaper.style.scale=new Scale(new Vector3(-1,1,1)); progressRoot.Add(reaper);
            progressText = new Label { pickingMode = PickingMode.Ignore, enableRichText = true };
            progressText.AddToClassList("eov-navigation-progress-text"); progressText.style.color = definition.progressTextColor;
            progressRoot.Add(progressText); context.Add(progressRoot);
            autoPinGroup = new VisualElement { name = "auto-pin", pickingMode = PickingMode.Ignore };
            autoPinGroup.AddToClassList("eov-navigation-auto-pin");
            
            autoPin = new Button(() => { AutoPinActiveQuests = !AutoPinActiveQuests; Refresh(); });
            autoPin.AddToClassList("eov-navigation-toggle");autoPin.AddToClassList("button"); autoPinGroup.Add(autoPin);
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("eov-navigation-auto-pin-label"); autoPinGroup.Add(label);
            bindings.Add(new ToolkitTextBinding(label, definition.autoPinLabel));
            context.Add(autoPinGroup);
            discord = new Button(() => Application.OpenURL(definition.discordUrl)) { name = "discord" };
            discord.AddToClassList("eov-navigation-button"); discord.style.width = 22;
            discord.AddToClassList("button");
            var icon = new Image { sprite = definition.discord, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("eov-navigation-icon"); discord.Add(icon);
            discord.style.marginRight = 2;
            root.Q("navigation-close").Insert(0, discord);
        }

        private void Activate(Entry entry)
        {
            if (!ToolkitVisibilityRule.All(entry.visibility)) return;
            if (!windows) windows = TownWindowManager.Instance;
            switch (entry.action)
            {
                case NavAction.Window: CloseMenu(); windows?.OpenWindow(entry.window); break;
                case NavAction.Close: windows?.CloseAllWindows(); CloseMenu(); break;
                case NavAction.BeginMap:
                    CloseMenu(); GameManager.Instance?.BeginAdventure(entry.map); break;
                case NavAction.AdventureMenu: ToggleMenu(Group.Adventure, buttons[entry]); break;
                case NavAction.HubMenu: ToggleMenu(Group.Hub, buttons[entry]); break;
                case NavAction.TownsfolkMenu: ToggleMenu(Group.Townsfolk, buttons[entry]); break;
            }
            Refresh();
        }

        private void ToggleMenu(Group group, Button anchor)
        {
            if (openGroup == group) { CloseMenu(); return; }
            openGroup = group; menuAnchor = anchor; popup.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void CloseMenu()
        {
            openGroup = Group.Toolbar; menuAnchor = null;
            if (popup != null) popup.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (root == null) return;
            if (!windows) windows = TownWindowManager.Instance;
            if (windows && !hooked)
            {
                windows.CloseRequested += CloseMenu; windows.WindowsChanged += Refresh; hooked = true;
            }
            ApplyLayout();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            Refresh();
        }

        public void Refresh()
        {
            if (root == null) return;
            root.EnableInClassList("eov-navigation--window-open", openGroup != Group.Toolbar || (windows != null && windows.HasOpenWindow));
            foreach (var entry in definition.entries)
            {
                var visible = ToolkitVisibilityRule.All(entry.visibility) && (entry.group == Group.Toolbar || entry.group == openGroup);
                if (entry.action == NavAction.Close) visible &= windows != null && windows.HasOpenWindow;
                rows[entry].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                buttons[entry].SetEnabled(entry.action != NavAction.Window || windows == null || windows.CanOpenWindow(entry.window));
            }
            SetVisible(forgeAttention, windows != null && windows.ForgeNeedsAttention);
            SetVisible(cauldronAttention, windows != null && windows.CauldronNeedsAttention);
            SetVisible(questAttention, QuestManager.Instance != null && QuestManager.Instance.HasQuestsReadyForTurnIn());
            SetVisible(autoPinGroup, windows != null && windows.IsWindowOpen(TownWindowManager.Window.Quests));
            SetVisible(discord, windows != null && windows.IsWindowOpen(TownWindowManager.Window.Options));
            var inRun = GameManager.Instance != null && GameManager.Instance.CurrentMap != null;
            SetVisible(progressRoot, inRun);
            var hero = Hero.HeroController.Instance;
            if (inRun && hero != null)
            {
                var current = RunProgressPresentation.Read(hero.transform.position.x);
                if (!hasProgress || !current.SameText(lastProgress)) progressText.text = "<b>" + current.FormatText() + "</b>";
                progressFill.style.width = Length.Percent(current.normalized * 100);
                progressHero.style.left = -16.5f + current.normalized * 103;
                lastProgress = current; hasProgress = true;
            }
            ToolkitGameplay.SetToggle(autoPin,AutoPinActiveQuests);
            if (echoBalance != null && openGroup == Group.Hub)
            {
                double total = 0;
                var manager = AlterEchoGenerationManager.Instance;
                if (manager != null) foreach (var generator in manager.Generators)
                    if (generator != null && generator.RequirementsMet && generator.Resource != null)
                        total += generator.GetStoredAmount(generator.Resource);
                echoBalance.text = "<b>" + CalcUtils.FormatNumber(total, true) + "</b>";
            }
        }

        private static void SetVisible(VisualElement element, bool visible)
        { if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }

        private void ApplyLayout()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea,
                Application.isMobilePlatform ? 1 : SafeAreaRatio);
            root.style.left = area.x+8;root.style.top=area.y+8;root.style.width=area.width-16;root.style.height=36;
            if (menuAnchor != null)
            {
                popup.style.left = Mathf.Max(0, Mathf.Min(menuAnchor.worldBound.x - area.x, area.width - popup.layout.width));
                popup.style.top = 32;
            }
        }

        private void OnDisable()
        {
            if (hooked && windows) { windows.CloseRequested -= CloseMenu; windows.WindowsChanged -= Refresh; }
            hooked = false;
            foreach (var binding in bindings) binding.Dispose();
            bindings.Clear(); rows.Clear(); buttons.Clear();
            root?.RemoveFromHierarchy(); root = null; popup = null; menuAnchor = null;
            openGroup = Group.Toolbar;
        }
        private void OnDestroy() { if (panel) Destroy(panel); }
    }
}

