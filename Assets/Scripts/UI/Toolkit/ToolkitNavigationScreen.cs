using Screen = UnityEngine.Device.Screen;
using System.Collections.Generic;
using TimelessEchoes.Farming;
using Blindsided.Utilities;
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
        private Entry[] navigationEntries;
        private ToolkitShopScreen shopScreen;
        private PanelSettings panel;
        private VisualElement root, popup, autoPinGroup;
        private VisualElement menuDismiss;
        private Button autoPin, discord, moreButton;
        private Label portraitMenuTitle;
        private VisualElement portraitMenuHeader, portraitMoreHeader;
        private ScrollView portraitGroupScroll, portraitMoreScroll;
        private VisualElement morePopup;
        private bool moreOpen;
        private bool? portraitNavigation;
        private Image forgeAttention, cauldronAttention, questAttention;
        private VisualElement progressRoot, progressFill;
        private Image progressHero, progressReaper;
        private TimelessEchoes.Characters.CharacterImage characterPortrait;
        private Transform progressHeroTransform;
        private Vector3 previousHeroPosition;
        private bool progressHeroFacingLeft;
        private float progressReapStarted = -1;
        private Label progressText, progressDistance;
        private RunProgressPresentation lastProgress;
        private bool hasProgress;
        private bool lastProgressPortrait;
        private Group openGroup;
        private ToolkitRunScreen runScreen;
        private Button menuAnchor;
        private float nextRefresh;
        private bool hooked;
        private bool highlightQuestsUntilOpened;
        private readonly Dictionary<string, VisualElement> tutorialPrompts = new();
        private string highlightedEntryId;
        private float questHighlightStartedAt;
        public Group OpenGroup => openGroup;

        private void OnEnable()
        {
            if (!definition || !theme || !template || !runtimeTheme || !textSettings) return;
            Create();
        }

        private void Create()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;
            navigationEntries = new List<Entry>(definition.entries)
            {
                new Entry { id = "shop", group = Group.Hub, action = NavAction.Window, window = TownWindowManager.Window.Shop,
                    label = new ToolkitBookDefinition.Text { key = "navigation.shop", fallback = "Shop" }, width = 62 }
            }.ToArray();
            TimelessEchoes.Shop.ShopService.Ensure();
            if (!shopScreen) shopScreen = new GameObject("Shop", typeof(UIDocument)).AddComponent<ToolkitShopScreen>();
            shopScreen.Configure(theme, runtimeTheme, textSettings);
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 150; }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = template.CloneTree();
            root.AddToClassList("eov-navigation");
            root.pickingMode = PickingMode.Ignore;
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            root.Q("navigation-bar").pickingMode = PickingMode.Ignore;
            menuDismiss = new VisualElement { name = "navigation-menu-dismiss" };
            menuDismiss.style.position = Position.Absolute;
            menuDismiss.style.display = DisplayStyle.None;
            menuDismiss.RegisterCallback<PointerDownEvent>(evt => { CloseMenu(); Refresh(); evt.StopPropagation(); });
            root.Insert(0, menuDismiss);
            popup = root.Q("navigation-popup");
            portraitMenuHeader = CreatePortraitHeader(popup);
            portraitMenuTitle = ToolkitGameplay.L(portraitMenuHeader, "", "portrait-navigation-title heading");
            portraitMenuTitle.style.flexGrow = 1;
            portraitMenuTitle.style.marginBottom = 0;
            foreach (var entry in navigationEntries)
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
                if(entry.id=="stats") { button.Add(new ToolkitStatisticsGlyph()); ToolkitLocalization.BindTooltip(button,"navigation.stats-tooltip","Statistics"); }
                else if (entry.action == NavAction.Close) { button.text="×";button.AddToClassList("close-control");ToolkitLocalization.BindTooltip(button,"navigation.close-tooltip","Close window"); }
                else if (entry.action == NavAction.Window && entry.window == TownWindowManager.Window.Farm)
                {
                    var label = new Label { name = "fields-navigation-label", text = FarmContent.Load()?.DisplayName ?? "Fields", pickingMode = PickingMode.Ignore };
                    label.AddToClassList("eov-navigation-label"); button.Add(label);
                }
                else if (entry.action == NavAction.TownsfolkMenu)
                {
                    var label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("eov-navigation-label"); button.Add(label);
                    ToolkitLocalization.Bind(label, "navigation.town", "Town");
                }
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
                if (entry.id is "options" or "quests" or "close" or "adventure" or "map-0" or "map-1" or "map-3" or "stats" or "townsfolk" or "forge" or "hub" or "skills" or "inventory" or "buffs")
                {
                    var prompt = new VisualElement { name = entry.id == "quests" ? "quest-tutorial-pointer" : "tutorial-pointer-" + entry.id, pickingMode = PickingMode.Ignore };
                    prompt.style.position = Position.Absolute;
                    prompt.style.width = 24; prompt.style.height = 24; prompt.style.display = DisplayStyle.None;
                    var hand = new Image { sprite = definition.questTutorialHand, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                    hand.style.width = 24; hand.style.height = 24; prompt.Add(hand);
                    tutorialPrompts.Add(entry.id, prompt); root.Add(prompt);
                    button.RegisterCallback<GeometryChangedEvent>(_ => RefreshTutorialPrompt());
                }
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
                if (entry.group == Group.Toolbar)
                {
                    root.Q(entry.action == NavAction.Close ? "navigation-close" : "navigation-buttons").Add(row);
                    if (entry.action == NavAction.Close) row.style.marginRight = 0;
                    if (entry.window == TownWindowManager.Window.Stats && entry.action == NavAction.Window) row.style.marginRight = 5;
                }
                else popup.Add(row);
            }
            moreButton = new Button(ToggleMore) { name = "navigation-more", text = "More" };
            moreButton.AddToClassList("button"); moreButton.AddToClassList("portrait-more");
            morePopup = new VisualElement { name = "navigation-more-popup" };
            portraitMoreHeader = CreatePortraitHeader(morePopup);
            var moreTitle = ToolkitGameplay.L(portraitMoreHeader, "More", "portrait-navigation-title heading");
            moreTitle.style.flexGrow = 1;
            moreTitle.style.marginBottom = 0;
            morePopup.AddToClassList("eov-navigation-popup"); morePopup.AddToClassList("portrait-more-popup"); root.Add(morePopup);
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
            ConfigurePortraitNavigation();
            var context = root.Q("navigation-context");
            context.style.display = ToolkitResponsiveLayout.IsPortrait && !(GameManager.Instance && GameManager.Instance.CurrentMap) ? DisplayStyle.None : DisplayStyle.Flex;
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
            progressReaper = new Image { name = "progress-reaper", sprite = definition.progressReaper, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            progressReaper.AddToClassList("eov-navigation-progress-actor"); progressReaper.AddToClassList("progress-reaper");progressReaper.style.scale=new Scale(new Vector3(-1,1,1)); progressRoot.Add(progressReaper);
            progressHero.name = "progress-hero";
            progressText = new Label { pickingMode = PickingMode.Ignore, enableRichText = true };
            progressText.AddToClassList("eov-navigation-progress-text"); progressText.style.color = definition.progressTextColor;
            progressRoot.Add(progressText); context.Add(progressRoot);
            progressDistance = new Label { name = "run-progress-distance", pickingMode = PickingMode.Ignore, enableRichText = true };
            progressDistance.AddToClassList("eov-navigation-progress-distance");
            progressDistance.style.color = definition.progressTextColor;
            progressRoot.Add(progressDistance);
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

        public void FollowHelpAfterDialogue(string route) => StartCoroutine(FollowAfterDialogue(route));
        private System.Collections.IEnumerator FollowAfterDialogue(string route)
        {
            // Let the existing dialogue completion record its tutorial stage and
            // release its modal before asking the normal navigation gates to open.
            yield return null; yield return null;
            ToolkitHelpLinks.Follow(route);
        }

        /// <summary>Help navigation shares authored progression gates and never begins an adventure.</summary>
        public bool OpenHelpDestination(string route)
        {
            var parts = route.Split('/');
            var entryId = parts[0] == "statistics" ? "stats" : parts[0] == "fields" ? "farm" : parts[0];
            var subpage = parts.Length == 2 ? parts[1] : null;
            if (parts.Length > 2 || navigationEntries == null || root == null) return false;
            if (!windows) windows = TownWindowManager.Instance;
            if (!windows) return false;
            // Conversations and modal lessons must retain ownership of their completion.
            if (ToolkitMeetingScreen.HasOpenDialogue ||
                UnityEngine.Object.FindAnyObjectByType<ToolkitIntroScreen>()?.IsVisible == true ||
                UnityEngine.Object.FindAnyObjectByType<ToolkitDialogScreen>()?.IsVisible == true) return false;
            if (entryId is "hero-stats" or "run-summary")
            {
                if (subpage != null || !GameManager.Instance || entryId == "hero-stats" && !GameManager.Instance.CurrentMap) return false;
                var run = UnityEngine.Object.FindAnyObjectByType<ToolkitRunScreen>();
                if (!run || !run.IsConfigured) return false;
                var summary = UnityEngine.Object.FindAnyObjectByType<Upgrades.RunResourceTrackerUI>();
                if (entryId == "run-summary" && !summary) return false;
                CloseMenu(); windows.CloseAllWindows();
                if (entryId == "hero-stats") return run.ShowHeroStats();
                if (GameManager.Instance.CurrentMap) run.ShowBreakdown();
                else summary.ShowWindow();
                return run.HasOpenDetails;
            }
            var entry = System.Array.Find(navigationEntries, e => e.id == entryId);
            if (entry == null || entry.action is not (NavAction.Window or NavAction.AdventureMenu or NavAction.HubMenu or NavAction.TownsfolkMenu) ||
                !ToolkitVisibilityRule.All(entry.visibility)) return false;
            // Children are subject to the same parent-menu unlock as normal navigation.
            if (entry.group != Group.Toolbar)
            {
                var parent = System.Array.Find(navigationEntries, e => e.action ==
                    (entry.group == Group.Hub ? NavAction.HubMenu : entry.group == Group.Adventure ? NavAction.AdventureMenu : NavAction.TownsfolkMenu));
                if (parent == null || !ToolkitVisibilityRule.All(parent.visibility)) return false;
            }
            var tab = -1;
            if (subpage != null)
            {
                if (entryId == "stats")
                {
                    tab = subpage switch { "general" => 0, "graphs" => 2, "enemies" => 3, "tasks" => 4, "items" => 5, "forging" => 6, _ => -1 };
                    if (tab < 0) return false;
                }
                else if (entryId != "skills" || subpage is not ("Farming" or "Fishing" or "Mining" or "Woodcutting" or "Combat" or "Looting")) return false;
            }
            if (entry.action == NavAction.Window && !windows.CanOpenWindow(entry.window)) return false;
            CloseMenu();
            if (entry.action != NavAction.Window)
            {
                windows.CloseAllWindows(); ToggleMenu(entry.action == NavAction.AdventureMenu ? Group.Adventure :
                    entry.action == NavAction.HubMenu ? Group.Hub : Group.Townsfolk, buttons[entry]);
                return HasOpenMenu;
            }
            if (!windows.IsWindowOpen(entry.window)) windows.OpenWindow(entry.window);
            if (!windows.IsWindowOpen(entry.window)) return false;
            if (tab >= 0) UnityEngine.Object.FindAnyObjectByType<ToolkitStatisticsScreen>()?.SelectTab(tab);
            if (entryId == "skills" && subpage != null)
                return UnityEngine.Object.FindAnyObjectByType<ToolkitSkillsScreen>()?.RevealSkill(subpage) == true;
            Refresh(); return true;
        }

        private void Activate(Entry entry)
        {
            if (!ToolkitVisibilityRule.All(entry.visibility)) return;
            if (entry.id == "shop" && !TimelessEchoes.Shop.ShopService.Ensure().Unlocked) return;
            moreOpen = false; if (morePopup != null) morePopup.style.display = DisplayStyle.None;
            if (!windows) windows = TownWindowManager.Instance;
            switch (entry.action)
            {
                case NavAction.Window:
                    CloseMenu();
                    if (ToolkitResponsiveLayout.IsPortrait && windows && windows.IsWindowOpen(entry.window))
                        windows.CloseAllWindows();
                    else windows?.OpenWindow(entry.window);
                    break;
                case NavAction.Close: windows?.CloseAllWindows(); CloseMenu(); break;
                case NavAction.BeginMap:
                    CloseMenu(); GameManager.Instance?.BeginAdventure(entry.map); break;
                case NavAction.AdventureMenu: ToggleMenu(Group.Adventure, buttons[entry]); break;
                case NavAction.HubMenu:
                    ToggleMenu(Group.Hub, buttons[entry]);
                    break;
                case NavAction.TownsfolkMenu: ToggleMenu(Group.Townsfolk, buttons[entry]); break;
            }
            Refresh();
        }

        private void ToggleMenu(Group group, Button anchor)
        {
            if (openGroup == group || ToolkitResponsiveLayout.IsPortrait &&
                (group == Group.Hub && HubWindowOpen() || group == Group.Townsfolk && TownWindowOpen()))
            {
                windows?.CloseAllWindows(); CloseMenu(); Refresh(); return;
            }
            if (ToolkitResponsiveLayout.IsPortrait) windows?.CloseAllWindows();
            portraitMenuTitle.text = group == Group.Adventure ? ToolkitLocalization.Text("navigation.adventure", "Adventure") : group == Group.Hub ? ToolkitLocalization.Text("navigation.hub", "Hub") : ToolkitLocalization.Text("navigation.town", "Town");
            openGroup = group; menuAnchor = ToolkitResponsiveLayout.IsPortrait && anchor.parent?.parent == morePopup ? moreButton : anchor; popup.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void CloseMenu()
        {
            openGroup = Group.Toolbar; menuAnchor = null; moreOpen = false;
            if (morePopup != null) morePopup.style.display = DisplayStyle.None;
            if (popup != null) popup.style.display = DisplayStyle.None;
        }

        public bool HasOpenMenu => openGroup != Group.Toolbar || moreOpen;

        private void Update()
        {
            if (root == null) return;
            if (!windows) windows = TownWindowManager.Instance;
            if (windows && !hooked)
            {
                windows.ConfigureShop(shopScreen);
                windows.CloseRequested += CloseMenu; windows.WindowsChanged += Refresh; hooked = true;
            }
            ApplyLayout();
            AnimateProgressActors();
            RefreshTutorialPrompt();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            Refresh();
        }

        public void HighlightQuests()
        {
            highlightQuestsUntilOpened = true;
            questHighlightStartedAt = Time.unscaledTime;
            Refresh();
        }

        private void AnimateProgressActors()
        {
            var hero = Hero.HeroController.Instance;
            if (!GameManager.Instance || !GameManager.Instance.CurrentMap || !hero)
            { progressHeroTransform = null; progressReapStarted = -1; return; }
            var position = hero.transform.position;
            var sameHero = progressHeroTransform == hero.transform;
            var delta = sameHero ? position - previousHeroPosition : Vector3.zero;
            var moving = delta.sqrMagnitude > .00000001f;
            if (!sameHero) { progressHeroFacingLeft = hero.MovementController.LastMoveDirection.x < 0; progressReapStarted = -1; }
            if (delta.x < -.0001f) progressHeroFacingLeft = true;
            else if (delta.x > .0001f) progressHeroFacingLeft = false;
            progressHero.style.scale = new Scale(new Vector3(progressHeroFacingLeft ? -1 : 1, 1, 1));
            progressHeroTransform = hero.transform;
            previousHeroPosition = position;
            var frame = Mathf.FloorToInt(Time.time * Mathf.Max(1, definition.progressFramesPerSecond));
            var reaped = false;
            // The authored swing faces right; Carl stands at the right end of the route.
            progressReaper.style.scale = new Scale(new Vector3(-1, 1, 1));
            if (hero.ReaperSpawnedByDistance && definition.progressReaperReapFrames is { Length: > 0 } reapFrames) {
                if (progressReapStarted < 0) progressReapStarted = Time.time;
                var reapFrame = Mathf.Clamp(Mathf.FloorToInt((Time.time - progressReapStarted) * Mathf.Max(1, definition.progressFramesPerSecond)), 0, reapFrames.Length - 1);
                progressReaper.sprite = reapFrames[reapFrame];
                reaped = reapFrame >= definition.progressReapImpactFrame;
            }
            else {
                progressReapStarted = -1;
                if (definition.progressReaperFrames is { Length: > 0 } idleFrames) progressReaper.sprite = idleFrames[frame % idleFrames.Length];
            }
            var heroFrames = moving ? definition.progressHeroRunFrames : definition.progressHeroIdleFrames;
            var profile = TimelessEchoes.Characters.CharacterEquipmentAppearance.Resolve(TimelessEchoes.Characters.CharacterCreation.Current);
            if (reaped && definition.progressHeroGravestone)
            {
                characterPortrait?.Set(null, 0);
                progressHero.sprite = definition.progressHeroGravestone;
                progressHero.style.scale = new Scale(Vector3.one);
                progressHero.PlaceBehind(progressReaper);
            }
            else if (profile is { Created: true, Modular: true } && TimelessEchoes.Characters.CharacterCatalog.Load())
            {
                progressHero.sprite = null;
                characterPortrait ??= new TimelessEchoes.Characters.CharacterImage(progressHero, 2);
                characterPortrait.Set(profile, TimelessEchoes.Characters.CharacterCatalog.Load().Frame(moving ? "Running_Right" : "Idle_Right", frame));
            }
            else
            {
                characterPortrait?.Set(null, 0);
                if (heroFrames != null && heroFrames.Length > 0) progressHero.sprite = heroFrames[frame % heroFrames.Length];
            }
        }

        public void Refresh()
        {
            if (root == null) return;
            if (portraitMenuTitle != null && HasOpenMenu)
                portraitMenuTitle.text = openGroup == Group.Adventure ? ToolkitLocalization.Text("navigation.adventure", "Adventure") : openGroup == Group.Hub ? ToolkitLocalization.Text("navigation.hub", "Hub") : ToolkitLocalization.Text("navigation.town", "Town");
            if (windows != null && windows.IsWindowOpen(TownWindowManager.Window.Quests))
                highlightQuestsUntilOpened = false;
            root.EnableInClassList("eov-navigation--window-open", openGroup != Group.Toolbar || (windows != null && windows.HasOpenWindow));
            SetVisible(menuDismiss, ToolkitResponsiveLayout.IsPortrait && HasOpenMenu);
            foreach (var entry in navigationEntries)
            {
                var visible = ToolkitVisibilityRule.All(entry.visibility) && (entry.group == Group.Toolbar || entry.group == openGroup);
                // Returning players may predate the quest that unlocks Town. Keep
                // the portrait route to Library available without replaying it.
                if (entry.id == "townsfolk" && Blindsided.Oracle.oracle?.saveData?.General.ReturningPlayerIntroductionComplete == true)
                    visible = true;
                if (entry.id == "shop") visible &= TimelessEchoes.Shop.ShopService.Ensure().Unlocked;
                if (ToolkitResponsiveLayout.IsPortrait && entry.group == Group.Toolbar && !PortraitBottomEntry(entry))
                    visible &= openGroup == Group.Townsfolk;
                if (entry.action == NavAction.Close) visible &= !ToolkitResponsiveLayout.IsPortrait && windows != null && windows.HasOpenWindow;
                rows[entry].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (entry.action == NavAction.Window && entry.window == TownWindowManager.Window.Farm)
                {
                    var label = buttons[entry].Q<Label>("fields-navigation-label");
                    if (label != null) label.text = FarmContent.Load()?.DisplayName ?? "Fields";
                }
                buttons[entry].SetEnabled(entry.action != NavAction.Window || windows == null ||
                    windows.IsWindowOpen(entry.window) || windows.CanOpenWindow(entry.window));
                var selected = entry.action == NavAction.Window && windows && windows.IsWindowOpen(entry.window);
                if (entry.id == "hub" && windows) selected = openGroup == Group.Hub || windows.IsWindowOpen(TownWindowManager.Window.Inventory) || windows.IsWindowOpen(TownWindowManager.Window.Skills) || windows.IsWindowOpen(TownWindowManager.Window.Buffs) || windows.IsWindowOpen(TownWindowManager.Window.Shop);
                if (entry.id == "townsfolk") selected = openGroup == Group.Townsfolk || TownWindowOpen();
                if (entry.id == "adventure") selected = openGroup == Group.Adventure;
                buttons[entry].EnableInClassList("active", ToolkitResponsiveLayout.IsPortrait && selected);
            }
            SetVisible(morePopup, ToolkitResponsiveLayout.IsPortrait && moreOpen);
            SetVisible(moreButton, false);
            if (moreButton != null)
            {
                var primary = windows && (windows.IsWindowOpen(TownWindowManager.Window.Inventory) || windows.IsWindowOpen(TownWindowManager.Window.Skills) || windows.IsWindowOpen(TownWindowManager.Window.Buffs) || windows.IsWindowOpen(TownWindowManager.Window.Quests));
                moreButton.EnableInClassList("active", moreOpen || openGroup == Group.Townsfolk || windows && windows.HasOpenWindow && !primary);
            }
            RefreshTutorialPrompt();
            SetVisible(forgeAttention, windows != null && windows.ForgeNeedsAttention);
            SetVisible(cauldronAttention, windows != null && windows.CauldronNeedsAttention);
            SetVisible(questAttention, QuestManager.Instance != null && QuestManager.Instance.HasQuestsReadyForTurnIn());
            SetVisible(autoPinGroup, !ToolkitResponsiveLayout.IsPortrait && windows != null && windows.IsWindowOpen(TownWindowManager.Window.Quests));
            SetVisible(discord, windows != null && windows.IsWindowOpen(TownWindowManager.Window.Options));
            var inRun = GameManager.Instance != null && GameManager.Instance.CurrentMap != null;
            SetVisible(progressRoot, inRun);
            var hero = Hero.HeroController.Instance;
            if (inRun && hero != null)
            {
                var current = RunProgressPresentation.Read(hero.transform.position.x);
                if (!hasProgress || !current.SameText(lastProgress) || lastProgressPortrait != ToolkitResponsiveLayout.IsPortrait)
                {
                    var text = current.FormatText();
                    var split = ToolkitResponsiveLayout.IsPortrait && current.killMode ? text.LastIndexOf('\n') : -1;
                    progressText.text = "<b>" + (split >= 0 ? text.Substring(0, split) : text) + "</b>";
                    progressDistance.text = split >= 0 ? "<b>" + text.Substring(split + 1) + "</b>" : "";
                    SetVisible(progressDistance, split >= 0);
                    lastProgressPortrait = ToolkitResponsiveLayout.IsPortrait;
                }
                progressFill.style.width = Length.Percent(current.normalized * 100);
                progressHero.style.left = -16.5f + current.normalized * (ToolkitResponsiveLayout.IsPortrait ? Mathf.Max(103, ToolkitWindowLayout.SafeArea.width - 20) : 103);
                lastProgress = current; hasProgress = true;
            }
            ToolkitGameplay.SetToggle(autoPin,AutoPinActiveQuests);
        }

        private void RefreshTutorialPrompt()
        {
            var data = Blindsided.Oracle.oracle?.saveData;
            var inTown = GameManager.Instance == null || GameManager.Instance.IsInTown;
            if (data != null && windows != null && windows.IsWindowOpen(TownWindowManager.Window.Forge))
                data.General.ForgeIntroductionSeen = true;
            var hubTutorial = inTown && data != null && data.General.InventoryTutorialStage >= 3 && definition.hubIntroduction && definition.hubIntroduction.HasBeenCompleted &&
                ToolkitVisibilityRule.HasLevelledSkill() && data.General.HubTutorialStage < 3;
            if (hubTutorial && windows != null)
            {
                if (data.General.HubTutorialStage == 0 && windows.IsWindowOpen(TownWindowManager.Window.Skills))
                    data.General.HubTutorialStage = 1;
                if (data.General.HubTutorialStage >= 1 && !windows.IsWindowOpen(TownWindowManager.Window.Skills))
                    data.General.HubTutorialStage = 3;
                hubTutorial = data.General.HubTutorialStage < 3;
            }
            var inventoryTutorial = inTown && data != null && data.General.ReturnedFromFarmlands &&
                data.General.InventoryTutorialStage > 0 && data.General.InventoryTutorialStage < 3;
            if (inventoryTutorial && windows != null)
            {
                if (data.General.InventoryTutorialStage == 1 && windows.IsWindowOpen(TownWindowManager.Window.Inventory))
                    data.General.InventoryTutorialStage = 2;
                if (data.General.InventoryTutorialStage == 2 && !ToolkitResponsiveLayout.IsPortrait && !windows.IsWindowOpen(TownWindowManager.Window.Inventory))
                    data.General.InventoryTutorialStage = 3;
                inventoryTutorial = data.General.InventoryTutorialStage < 3;
            }
            string desired = null;
            if (!runScreen) runScreen = FindAnyObjectByType<ToolkitRunScreen>();
            if (ActiveNpcMeetings.Count > 0 || (runScreen && runScreen.HasOpenDetails)) desired = null;
            else if (highlightQuestsUntilOpened) desired = "quests";
            else if (inTown && BuffIntroductionGuidance.Active)
                desired = windows != null && windows.IsWindowOpen(TownWindowManager.Window.Buffs) ? null : windows != null && windows.HasOpenWindow ? "close" : openGroup == Group.Hub ? "buffs" : "hub";
            else if (hubTutorial)
                desired = windows != null && windows.HasOpenWindow ? "close" : openGroup == Group.Hub ? "skills" : "hub";
            else if (inventoryTutorial)
                desired = ToolkitResponsiveLayout.IsPortrait && windows != null && windows.IsWindowOpen(TownWindowManager.Window.Inventory)
                    ? null : windows != null && windows.HasOpenWindow ? "close" : openGroup == Group.Hub ? "inventory" : "hub";
            else if (data != null && inTown && data.CompletedNpcTasks.Contains("Gill1") && !QuestUtils.QuestCompleted("The names Gill"))
                desired = windows != null && windows.IsWindowOpen(TownWindowManager.Window.Quests) ? null : windows != null && windows.HasOpenWindow ? "close" : "quests";
            else if (data != null && inTown && QuestUtils.QuestCompleted("You get this one for free!") &&
                     definition.farmingIntroduction && !definition.farmingIntroduction.HasBeenCompleted)
                desired = windows != null && windows.HasOpenWindow ? "close" : null;
            else if (data != null && inTown &&
                     ((data.CompletedNpcTasks.Contains("Ivan1") && !QuestUtils.QuestCompleted("Meeting Ivan")) ||
                      (QuestUtils.QuestCompleted("Meeting Ivan") && !TimelessEchoes.Gear.ForgeFirstLesson.Accepted && (data.Forge?.TotalCrafts ?? 0) == 0)))
                desired = windows != null && windows.IsWindowOpen(TownWindowManager.Window.Quests) ? null : "quests";
            else if (data != null && inTown && MapStatisticsGuidance.Active)
                desired = data.General.MapStatisticsTutorialStage >= 7 ?
                    (windows != null && windows.HasOpenWindow ? "close" : null) :
                    data.General.MapStatisticsTutorialStage == 1 ||
                    (windows != null && windows.HasOpenWindow && !windows.IsWindowOpen(TownWindowManager.Window.Stats)) ?
                    (windows != null && windows.HasOpenWindow ? "close" : null) :
                    windows != null && windows.IsWindowOpen(TownWindowManager.Window.Stats) ? null : "stats";
            else if (data != null && inTown && TimelessEchoes.Gear.ForgeFirstLesson.Active)
                desired = windows != null && windows.IsWindowOpen(TownWindowManager.Window.Forge) ? null : windows != null && windows.HasOpenWindow ? "close" : openGroup == Group.Townsfolk ? "forge" : "townsfolk";
            else if (data != null && inTown && data.General.TaskBoostTutorialStage == 1)
                desired = windows != null && windows.IsWindowOpen(TownWindowManager.Window.Stats) ? null :
                    windows != null && windows.HasOpenWindow ? "close" : "stats";
            else if (data != null && inTown && GraphStatisticsGuidance.Active)
                desired = windows != null && windows.IsWindowOpen(TownWindowManager.Window.Stats) ? null :
                    windows != null && windows.HasOpenWindow ? "close" : "stats";
            else if (data != null && inTown && QuestUtils.QuestCompleted("You get this one for free!") &&
                     !data.CompletedNpcTasks.Contains("Reaper1") && !QuestUtils.QuestCompleted("An Intro to Trekking"))
                desired = openGroup == Group.Adventure ? "map-0" : "adventure";
            else if (data != null && inTown && QuestUtils.QuestCompleted("The names Gill") && !data.CompletedNpcTasks.Contains("Barkley1"))
                desired = windows != null && windows.HasOpenWindow ? "close" : openGroup == Group.Adventure ? "map-3" : "adventure";
            else if (data != null && inTown && QuestUtils.QuestCompleted("Another Magicool Gift") && !data.CompletedNpcTasks.Contains("Ivan1"))
                desired = windows != null && windows.HasOpenWindow ? "close" : openGroup == Group.Adventure ? "map-1" : "adventure";
            if (ToolkitResponsiveLayout.IsPortrait && desired == "close" && windows)
                desired = HubWindowOpen() ? "hub" : windows.IsWindowOpen(TownWindowManager.Window.Quests) ? "quests" :
                    windows.IsWindowOpen(TownWindowManager.Window.Stats) ? "stats" :
                    windows.IsWindowOpen(TownWindowManager.Window.Options) ? "options" : "townsfolk";
            if (desired != highlightedEntryId) { highlightedEntryId = desired; questHighlightStartedAt = Time.unscaledTime; }
            if (desired != null) ToolkitResponsiveLayout.SetFocusView(false);
            foreach (var pair in tutorialPrompts)
            {
                var visible = pair.Key == desired;
                SetVisible(pair.Value, visible);
                if (!visible) continue;
                foreach (var entry in navigationEntries)
                {
                    if (entry.id != pair.Key) continue;
                    var phase = Mathf.Repeat(Time.unscaledTime - questHighlightStartedAt, 1.2f) / 1.2f;
                    var approach = .5f - .5f * Mathf.Cos(phase * Mathf.PI * 2);
                    var target = ToolkitResponsiveLayout.IsPortrait && !PortraitBottomEntry(entry) &&
                        entry.group == Group.Toolbar && openGroup != Group.Townsfolk
                        ? buttons[System.Array.Find(navigationEntries, e => e.id == "townsfolk")] : buttons[entry];
                    var anchor = root.WorldToLocal(new Vector2(target.worldBound.center.x, (ToolkitResponsiveLayout.IsPortrait ? target.worldBound.yMin : target.worldBound.yMax)));
                    if (ToolkitResponsiveLayout.IsPortrait && target.worldBound.yMax < root.worldBound.yMin)
                    {
                        var right = root.WorldToLocal(new Vector2(target.worldBound.xMax, target.worldBound.center.y));
                        var left = root.WorldToLocal(new Vector2(target.worldBound.xMin, target.worldBound.center.y));
                        var fromRight = right.x + 28 <= root.layout.width;
                        pair.Value.style.left = Mathf.Round(fromRight ? right.x + 3 + 3 * approach : left.x - 27 - 3 * approach);
                        pair.Value.style.top = Mathf.Round(right.y - 12);
                        pair.Value.style.rotate = new Rotate(Angle.Degrees(fromRight ? 270 : 90));
                        break;
                    }
                    pair.Value.style.left = Mathf.Round(anchor.x - 12);
                    pair.Value.style.top = Mathf.Round(ToolkitResponsiveLayout.IsPortrait ? anchor.y - 22 - 4 * (1 - approach) : anchor.y - 6 + 4 * (1 - approach));
                    pair.Value.style.rotate = new Rotate(new Angle(ToolkitResponsiveLayout.IsPortrait ? 180 : 0, AngleUnit.Degree));
                    break;
                }
            }
        }

        private static void SetVisible(VisualElement element, bool visible)
        { if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }

        private void ConfigurePortraitNavigation()
        {
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            if (portraitNavigation == portrait) return;
            portraitNavigation = portrait;
            moreOpen = false;
            var bar = root.Q("navigation-buttons");
            if (portrait)
            {
                portraitGroupScroll ??= ToolkitGameplay.Scroll(popup, "portrait-navigation-list");
                portraitMoreScroll ??= ToolkitGameplay.Scroll(morePopup, "portrait-more-list");
                popup.Add(portraitGroupScroll); morePopup.Add(portraitMoreScroll);
            }
            foreach (var entry in navigationEntries)
            {
                if (entry.group != Group.Toolbar)
                {
                    if (portrait) portraitGroupScroll.Add(rows[entry]); else popup.Add(rows[entry]);
                    continue;
                }
                if (entry.action == NavAction.Close) continue;
                var bottom = PortraitBottomEntry(entry);
                if (portrait && !bottom) portraitGroupScroll.Add(rows[entry]); else bar.Add(rows[entry]);
                var iconOnly = entry.id is "options" or "stats";
                rows[entry].EnableInClassList("portrait-main-tab", portrait && bottom && !iconOnly);
                rows[entry].EnableInClassList("portrait-icon-tab", portrait && iconOnly);
                buttons[entry].Q<Label>("portrait-menu-label")?.RemoveFromHierarchy();
            }
            if (!portrait) { portraitGroupScroll?.RemoveFromHierarchy(); portraitMoreScroll?.RemoveFromHierarchy(); }
            if (portrait)
            {
                foreach (var id in new[] { "adventure", "hub", "quests", "townsfolk", "stats", "options" })
                {
                    var entry = System.Array.Find(navigationEntries, e => e.id == id);
                    if (entry != null) bar.Add(rows[entry]);
                }
            }
            moreButton.style.display = DisplayStyle.None;
        }

        private static VisualElement CreatePortraitHeader(VisualElement parent)
        {
            var header = new VisualElement();
            header.AddToClassList("portrait-menu-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.flexShrink = 0;
            header.style.marginBottom = 16;
            parent.Add(header);
            return header;
        }

        private static bool PortraitBottomEntry(Entry entry) => entry.id is "adventure" or "hub" or "quests" or "townsfolk" or "stats" or "options";

        private bool TownWindowOpen() => windows && windows.HasOpenWindow && !HubWindowOpen() &&
            !windows.IsWindowOpen(TownWindowManager.Window.Quests) &&
            !windows.IsWindowOpen(TownWindowManager.Window.Stats) &&
            !windows.IsWindowOpen(TownWindowManager.Window.Options);

        private bool HubWindowOpen() => windows &&
            (windows.IsWindowOpen(TownWindowManager.Window.Inventory) ||
             windows.IsWindowOpen(TownWindowManager.Window.Skills) ||
             windows.IsWindowOpen(TownWindowManager.Window.Buffs) || windows.IsWindowOpen(TownWindowManager.Window.Shop));

        private void ToggleMore()
        {
            if (!windows) windows = TownWindowManager.Instance;
            var ownsWindow = windows && windows.HasOpenWindow && !HubWindowOpen() &&
                !windows.IsWindowOpen(TownWindowManager.Window.Quests);
            var close = moreOpen || openGroup == Group.Townsfolk || ownsWindow;
            windows?.CloseAllWindows();
            CloseMenu();
            moreOpen = !close;
            Refresh();
        }

        private void ApplyLayout()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea,
                UnityEngine.Device.Application.isMobilePlatform ? 1 : SafeAreaRatio);
            root.style.left = area.x + (ToolkitResponsiveLayout.IsPortrait ? 0 : 8);
            root.style.top = ToolkitResponsiveLayout.IsPortrait ? area.yMax - 44 : area.y + 8;
            root.style.width = area.width - (ToolkitResponsiveLayout.IsPortrait ? 0 : 16);
            // Continue the same navigation surface through the home-indicator inset.
            root.style.height = ToolkitResponsiveLayout.IsPortrait ? ToolkitResponsiveLayout.Size.y - (area.yMax - 44) : 36;
            ConfigurePortraitNavigation();
            portraitMenuHeader.style.display = DisplayStyle.None;
            portraitMoreHeader.style.display = DisplayStyle.None;
            var context = root.Q("navigation-context");
            context.style.display = ToolkitResponsiveLayout.IsPortrait && !(GameManager.Instance && GameManager.Instance.CurrentMap) ? DisplayStyle.None : DisplayStyle.Flex;
            context.style.top = ToolkitResponsiveLayout.IsPortrait ?
                (ToolkitResponsiveLayout.FocusView ? ToolkitResponsiveLayout.SafeScreenArea.yMax - 92 : ToolkitResponsiveLayout.WorldArea.yMax) - root.resolvedStyle.top - 39 : StyleKeyword.Null;
            progressRoot.style.width = ToolkitResponsiveLayout.IsPortrait ? area.width - 16 : StyleKeyword.Null;
            progressRoot.Q(className: "eov-navigation-progress-track").style.width = ToolkitResponsiveLayout.IsPortrait ? area.width - 20 : StyleKeyword.Null;
            progressReaper.style.left = ToolkitResponsiveLayout.IsPortrait ? area.width - 40 : StyleKeyword.Null;
            if (ToolkitResponsiveLayout.IsPortrait)
                foreach (var entry in navigationEntries) buttons[entry].style.minWidth = entry.group == Group.Toolbar ? StyleKeyword.Auto : entry.width;
            else
                foreach (var entry in navigationEntries) buttons[entry].style.minWidth = entry.width;
            if (ToolkitResponsiveLayout.IsPortrait)
            {
                menuDismiss.style.left = 0; menuDismiss.style.top = -root.resolvedStyle.top;
                menuDismiss.style.width = area.width; menuDismiss.style.height = root.resolvedStyle.top;
                var visibleRows = 0;
                foreach (var entry in navigationEntries)
                    if ((entry.group != Group.Toolbar || !PortraitBottomEntry(entry)) && rows[entry].style.display.value == DisplayStyle.Flex)
                        visibleRows++;
                var width = Mathf.Min(176, area.width - 16);
                var height = Mathf.Min(visibleRows * 40 + 14, area.height - 58);
                var anchor = menuAnchor != null ? root.WorldToLocal(menuAnchor.worldBound.position) : Vector2.zero;
                popup.style.left = Mathf.Clamp(anchor.x, 8, area.width - width - 8);
                popup.style.top = anchor.y - height - 6;
                popup.style.width = width;
                popup.style.height = height;
            }
            else
            {
                popup.style.width = StyleKeyword.Null; popup.style.height = StyleKeyword.Null;
                if (menuAnchor != null)
                {
                    popup.style.left = Mathf.Max(0, Mathf.Min(root.WorldToLocal(menuAnchor.worldBound.position).x, root.layout.width - popup.layout.width));
                    popup.style.top = 32;
                }
            }
        }

        private void OnDisable()
        {
            if (hooked && windows) { windows.CloseRequested -= CloseMenu; windows.WindowsChanged -= Refresh; }
            hooked = false;
            foreach (var binding in bindings) binding.Dispose();
            bindings.Clear(); rows.Clear(); buttons.Clear();
            root?.RemoveFromHierarchy(); root = null; popup = null; menuAnchor = null; morePopup = null; moreButton = null; portraitGroupScroll = null; portraitMoreScroll = null; portraitNavigation = null;
            menuDismiss = null;
            characterPortrait = null;
            tutorialPrompts.Clear(); highlightedEntryId = null;
            openGroup = Group.Toolbar;
        }
        private void OnDestroy()
        {
            if (shopScreen) Destroy(shopScreen.gameObject);
            if (panel) Destroy(panel);
        }
    }
}
