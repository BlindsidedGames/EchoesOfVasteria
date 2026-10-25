using Screen = UnityEngine.Device.Screen;
using System;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TimelessEchoes.Gear;
using TimelessEchoes.Hero;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class ToolkitRunScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitRunDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, hud, bar, returnControls, death, deathFill, overlay, detail, tooltip, abilityBar;
        private Label runTime,runRatesLeft,runRatesRight;
        private Label deathTitle, detailTitle, tooltipText;
        private Label breakdownTitle, breakdownKills, breakdownMultiplier;
        private VisualElement breakdownTotals;
        private Image breakdownResourceIcon;
        private Button retreat, returnOnDeath, restart, deathReturn, deathSummary, breakdown;
        private Image ivanReturnPointer;
        private Image firstEchoHand;
        private BuffManager guidedBuffManager;
        private ScrollView detailRows;
        private readonly List<(Button button, Image icon, Image auto, VisualElement locked, Label text, VisualElement fill)> slots = new();
        private readonly Dictionary<Resource, double> runResources = new();
        private ResourceManager resources;
        private GameplayStatTracker tracker;
        private bool showingBreakdown, portraitDetails;
        private bool? portraitRun;
        private VisualElement portraitRunActions;
        private VisualElement portraitDashboard;
        private ScrollView portraitStatsScroll;
        private VisualElement focusControls;
        private Button focusToggle;
        private VisualElement focusIcon;
        private float nextRefresh;
        private ToolkitRunResourcesView liveBreakdown;
        private int tooltipSlot = -1;
        public bool IsConfigured => definition && definition.buffs && theme && runtimeTheme && textSettings;

        private void OnEnable()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;
            if (!IsConfigured) return;
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 80;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { pickingMode = PickingMode.Ignore }; root.style.position = Position.Absolute;
            theme.Apply(root); ToolkitGameplay.Apply(root, theme); document.rootVisualElement.Add(root);
            hud = E(root,"hero-corner");hud.style.left=12;hud.style.bottom=12;
            abilityBar=E(hud,"row abilities");abilityBar.name="run-abilities";for(int i=0;i<5;i++)BuildBuff(abilityBar,i);
            SetVisible(abilityBar, false);
            BuildHeroHud(); BuildPortraitResources();
            firstEchoHand = new Image { name = "first-echo-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
            firstEchoHand.style.position = Position.Absolute; firstEchoHand.style.width = firstEchoHand.style.height = 24;
            firstEchoHand.style.rotate = new Rotate(Angle.Degrees(0));
            root.Add(firstEchoHand); SetVisible(firstEchoHand, false);
            returnControls = new VisualElement(); returnControls.AddToClassList("eov-run-return"); root.Add(returnControls);
            retreat = Button(returnControls, "run-return", ToolkitLocalization.Text("run.return-town-title", "Return To Town"), () => { if (!ToolkitMeetingScreen.TryFollowNavigation("return-town")) GameManager.Instance?.RequestRetreat(); });
            returnOnDeath = Button(returnControls, "run-return-on-death", ToolkitLocalization.Text("run.return-on-death-title", "Return On Death"), () => GameManager.Instance?.RequestReturnOnDeath());
            ivanReturnPointer = new Image { name = "ivan-return-pointer", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
            ivanReturnPointer.style.position = Position.Absolute; ivanReturnPointer.style.width = ivanReturnPointer.style.height = 24; root.Add(ivanReturnPointer);
            SetVisible(ivanReturnPointer, false);
            breakdown = Button(root, "run-breakdown", "", ShowBreakdown); breakdown.AddToClassList("eov-run-breakdown");
            ToolkitLocalization.BindTooltip(breakdown, "run.breakdown-heading", "Run breakdown");
            var resourceCount = E(breakdown, "row run-breakdown-resource-count");
            breakdownResourceIcon = ToolkitGameplay.Icon(resourceCount, definition.ResourceIcon(0), 16);
            breakdownTitle = L(resourceCount, "", "strong");
            breakdownTotals = E(breakdown, "row run-breakdown-totals");
            var kills = E(breakdownTotals, "row run-breakdown-kills");
            ToolkitGameplay.Icon(kills, definition.killsIcon, 12);
            breakdownKills = L(kills, "", "strong");
            ToolkitLocalization.BindTooltip(kills, "run.summary.label.kills", "Kills");
            var breakdownBonus = E(breakdownTotals, "row run-breakdown-bonus");
            ToolkitGameplay.Icon(breakdownBonus, definition.bonusIcon, 12);
            breakdownMultiplier = L(breakdownBonus, "", "strong");
            death = Panel(root, "eov-run-death"); deathTitle = Text(death, "", ToolkitControls.TextRole.Heading);
            restart = LocalizedButton(death, "run-restart", "run.restart", "Restart", () => GameManager.Instance?.RequestRestartAfterDeath());
            deathReturn = LocalizedButton(death, "run-death-return", "run.return-town-title", ToolkitLocalization.Text("run.return-town-title", "Return To Town"), () => { if (!ToolkitMeetingScreen.TryFollowNavigation("return-town")) GameManager.Instance?.RequestReturnAfterDeath(); });
            deathSummary = LocalizedButton(death, "run-death-summary", "run.view-summary", "View Summary", ShowBreakdown); deathFill = Progress(death, definition.healthTrack, definition.healthFill);
            overlay = new VisualElement(); overlay.AddToClassList("eov-run-overlay"); root.Add(overlay);
            detail=Panel(overlay,"breakdown-sheet");var head=E(detail,"row breakdown-header");LocalizedLabel(head, "run.base-return-rate", "Base rate (return bonus)","small muted breakdown-rate");detailTitle=L(head,ToolkitLocalization.Text("run.breakdown-heading", "Run breakdown"),"heading breakdown-title");runTime=L(head,"","strong breakdown-time");BuildSummaryOverview();
            detailRows=ToolkitGameplay.Scroll(detail,"run-ledger");detailRows.contentContainer.AddToClassList("run-ledger-grid");var footer=E(detail,"row breakdown-footer");runRatesLeft=L(footer,"","small");runRatesRight=L(footer,"","small");runRatesRight.style.unityTextAlign=TextAnchor.MiddleRight; liveBreakdown = new ToolkitRunResourcesView(definition); detail.Add(liveBreakdown); BuildDetailsClose(); HideDetails();
            tooltip = Panel(root, "eov-run-tooltip", definition.tooltipFrame); tooltipText = Text(tooltip, ""); SetVisible(tooltip, false);
            focusControls = new VisualElement { name = "run-focus-controls", pickingMode = PickingMode.Ignore };
            theme.Apply(focusControls); ToolkitGameplay.Apply(focusControls, theme);
            focusControls.style.position = Position.Absolute;
            focusControls.style.left = focusControls.style.top = 0;
            focusControls.style.width = focusControls.style.height = Length.Percent(100);
            document.rootVisualElement.Add(focusControls);
            focusToggle = ToolkitControls.Button("run-focus-toggle", () =>
            {
                var expand = !ToolkitResponsiveLayout.FocusView;
                if (expand)
                {
                    // Expansion dismisses the menu, including its selected-window
                    // state, so the next navigation tap opens it normally.
                    TownWindowManager.Instance?.CloseAllWindows();
                    HideDetails();
                    SetHeroStatsOpen(false);
                }
                ToolkitResponsiveLayout.SetFocusView(expand);
            }, theme.button);
            focusToggle.style.position = Position.Absolute;
            focusToggle.style.width = focusToggle.style.height = 32;
            focusToggle.AddToClassList("run-focus-toggle");
            focusToggle.style.paddingLeft = focusToggle.style.paddingRight = 3;
            focusToggle.style.paddingTop = focusToggle.style.paddingBottom = 3;
            focusControls.Add(focusToggle);
            focusToggle.style.alignItems = Align.Center;
            focusToggle.style.justifyContent = Justify.Center;
            focusIcon = new VisualElement { name = "run-focus-icon", pickingMode = PickingMode.Ignore };
            focusIcon.style.flexShrink = 0;
            focusToggle.Add(focusIcon);
            ToolkitLocalization.Changed += LocalizationChanged;
            Blindsided.EventHandler.OnRunStarted += RunStarted;
            AttachServices(); Refresh();
        }
        private static Label LocalizedLabel(VisualElement parent, string key, string english, string classes = "")
        { var label = L(parent, "", classes); ToolkitLocalization.Bind(label, key, english); return label; }
        private Button LocalizedButton(VisualElement parent, string name, string key, string english, Action action)
        { var button = Button(parent, name, "", action); ToolkitLocalization.Bind(button, key, english); return button; }
        private void LocalizationChanged()
        {
            nextRefresh = 0;
            RefreshTooltip();
            if (showingBreakdown) { detailTitle.text = ToolkitLocalization.Text("run.breakdown-title", "Run Breakdown"); RefreshBreakdown(); }
            else if (summaryData != null && HasOpenDetails) RefreshSummary();
        }
        private void AttachServices()
        {
            if (!resources && ResourceManager.Instance) { resources = ResourceManager.Instance; resources.OnResourceAdded += ResourceAdded; }
            if (!tracker && GameplayStatTracker.Instance) { tracker = GameplayStatTracker.Instance; tracker.OnRunEnded += RunEnded; }
            if (!guidedBuffManager && BuffManager.Instance) { guidedBuffManager = BuffManager.Instance; guidedBuffManager.OnBuffCast += FirstEchoCast; }
        }
        private void FirstEchoCast(BuffRecipe recipe, bool automatic)
        {
            if (BuffIntroductionGuidance.Stage != 3 || !BuffIntroductionGuidance.IsFirstEcho(recipe) ||
                !GameManager.Instance || !GameManager.Instance.CurrentMap) return;
            Blindsided.Oracle.oracle.saveData.General.BuffTutorialStage = 4;
            SetVisible(firstEchoHand, false);
            Blindsided.EventHandler.SaveData();
        }
        private void RefreshFirstEchoHand()
        {
            var gm = GameManager.Instance;
            var visible = BuffIntroductionGuidance.Stage == 3 && gm && gm.CurrentMap && !gm.HeroIsDead &&
                !HasOpenDetails && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) &&
                Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Count == 0 && guidedBuffManager;
            Button target = null;
            if (visible)
                for (var i = 0; i < slots.Count; i++)
                    if (guidedBuffManager.IsSlotUnlocked(i) && BuffIntroductionGuidance.IsFirstEcho(guidedBuffManager.GetAssigned(i)))
                    { target = slots[i].button; break; }
            SetVisible(firstEchoHand, target != null && firstEchoHand.sprite);
            if (target == null) return;
            var point = root.WorldToLocal(new Vector2(target.worldBound.center.x, target.worldBound.yMax));
            // UI_Icons_12 has a fingertip at (6.5, 2) in its 16 x 16 sprite.
            // Anchor that visible tip, accounting for the transparent padding.
            var fingertip = new Vector2(6.5f, 2) * (24f / 16);
            firstEchoHand.style.left = point.x - fingertip.x;
            firstEchoHand.style.top = point.y - fingertip.y - 1 + .5f * (1 + Mathf.Cos(Time.unscaledTime * 5));
            firstEchoHand.BringToFront();
        }
        private VisualElement Panel(VisualElement parent, string style, Sprite sprite = null)
        { var element = new VisualElement(); element.AddToClassList(style); element.AddToClassList("surface"); parent.Add(element); return element; }
        private Label Text(VisualElement parent, string text, ToolkitControls.TextRole role = ToolkitControls.TextRole.Body)
        { return L(parent,text,role==ToolkitControls.TextRole.Heading?"heading":role==ToolkitControls.TextRole.Caption?"small":""); }
        private Button Button(VisualElement parent, string name, string text, Action action)
        { var button = ToolkitGameplay.B(parent,text,action);button.name=name; button.AddToClassList("eov-run-button"); return button; }
        private VisualElement Progress(VisualElement parent, Sprite track, Sprite fill)
        { var holder = new VisualElement(); holder.AddToClassList("track"); parent.Add(holder); var value = new VisualElement(); value.style.height = Length.Percent(100); value.AddToClassList("fill"); holder.Add(value); return value; }
        private void BuildBuff(VisualElement parent, int index)
        {
            bool suppress = false;
            var button = ToolkitControls.Button("run-buff-" + index, () => { if (suppress) { suppress = false; return; } BuffManager.Instance?.ActivateSlot(index); }, definition.buffs.slot);
            button.style.backgroundImage=StyleKeyword.None;button.AddToClassList("button");button.AddToClassList("ability");if(index==4)button.AddToClassList("last-ability");parent.Add(button);
            var icon = ToolkitGameplay.Icon(button,null,21);
            var auto = ToolkitGameplay.Icon(button,definition.buffs.autoCast,20);auto.AddToClassList("ability-echo");auto.tintColor=definition.buffs.autoCastTint;
            var locked = new ToolkitLockIcon { name = "run-buff-lock-" + index }; button.Add(locked); SetVisible(locked, false);
            var fill = new ToolkitBorderProgress();button.Insert(0,fill);
            var label = Text(button, "", ToolkitControls.TextRole.Caption);
            label.AddToClassList("ability-status");
            label.style.whiteSpace = WhiteSpace.NoWrap;
            var held = button.schedule.Execute(() => { suppress = true; BuffManager.Instance?.ToggleSlotAutoCast(index); }); held.Pause();
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { suppress = false; held.ExecuteLater(1000); } else if (e.button == 1) BuffManager.Instance?.ToggleSlotAutoCast(index); }, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerUpEvent>(_ => held.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<PointerCancelEvent>(_ => held.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<DetachFromPanelEvent>(_ => held.Pause());
            button.RegisterCallback<PointerEnterEvent>(_ => { tooltipSlot = index; RefreshTooltip(); });
            button.RegisterCallback<PointerLeaveEvent>(_ => { held.Pause(); tooltipSlot = -1; SetVisible(tooltip, false); });
            button.RegisterCallback<FocusInEvent>(_ => { tooltipSlot = index; RefreshTooltip(); });
            button.RegisterCallback<FocusOutEvent>(_ => { tooltipSlot = -1; SetVisible(tooltip, false); });
            slots.Add((button, icon, auto, locked, label, fill));
        }
        private void Update()
        {
            if (root == null) return; AttachServices();
            var gm = GameManager.Instance;
            var canFocus = gm && gm.CurrentMap && !gm.HeroIsDead;
            var focusAllowed = canFocus && !gm.FirstReapDialoguePending && !ToolkitMeetingScreen.HasOpenDialogue && BuffIntroductionGuidance.Stage != 3;
            focusToggle.SetEnabled(focusAllowed);
            if (!focusAllowed && !(gm && gm.RunLoadingActive)) ToolkitResponsiveLayout.SetFocusView(false);
            var focus = ToolkitResponsiveLayout.FocusView;
            var area = focus ? ToolkitResponsiveLayout.SafeScreenArea : ToolkitWindowLayout.SafeArea;
            root.style.left = area.x; root.style.top = area.y; root.style.width = area.width; root.style.height = area.height;
            ConfigurePortraitHud(area);
            UpdatePortraitResources(area);
            // Run-summary backdrops also extend outside the HUD safe area.
            var detailTop = PortraitDetailsTop();
            overlay.style.left = ToolkitResponsiveLayout.IsPortrait ? 0 : -area.x; overlay.style.top = ToolkitResponsiveLayout.IsPortrait ? detailTop : -area.y;
            overlay.style.width = ToolkitResponsiveLayout.IsPortrait ? area.width : ToolkitResponsiveLayout.Size.x;
            overlay.style.height = ToolkitResponsiveLayout.IsPortrait ? Mathf.Max(100, area.height - detailTop - ToolkitResponsiveLayout.WindowBottom) : ToolkitResponsiveLayout.Size.y;
            if (showingBreakdown || summaryData != null || ToolkitResponsiveLayout.IsPortrait || portraitDetails) LayoutSummary(area);
            portraitDetails = ToolkitResponsiveLayout.IsPortrait;
            HandleBuffHotkeys();
            RefreshFirstEchoHand();
            var guideReturn = GameManager.Instance && GameManager.Instance.CurrentMap && !GameManager.Instance.FirstReapDialoguePending && (GameManager.Instance.IvanReturnPending || GameManager.Instance.FirstReapReturnPending) && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow);
            SetVisible(ivanReturnPointer, guideReturn);
            if (guideReturn)
            {
                var target = GameManager.Instance.HeroIsDead ? deathReturn : retreat;
                var point = root.WorldToLocal(new Vector2(target.worldBound.center.x, target.worldBound.yMax));
                ivanReturnPointer.style.left = Mathf.Round(point.x - 12); ivanReturnPointer.style.top = Mathf.Round(point.y + 2 + 4 * (1 + Mathf.Cos(Time.unscaledTime * 5)));
                // Orientation changes reparent the return controls later in the
                // root. Keep the guidance above them without moving its target.
                ivanReturnPointer.BringToFront();
            }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { HideDetails(); SetHeroStatsOpen(false); }
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .1f; Refresh(); }
            SetVisible(focusControls, ToolkitResponsiveLayout.IsPortrait && canFocus && !HasOpenDetails);
            var world = ToolkitResponsiveLayout.WorldArea;
            var safe = ToolkitResponsiveLayout.SafeScreenArea;
            var focusSprite = focus ? definition.restoreViewIcon : definition.fullscreenIcon;
            ToolkitTheme.Background(focusIcon, focusSprite);
            focusIcon.style.width = focusIcon.style.height = focus ? 22 : 26;
            focusToggle.style.left = Mathf.Min(world.xMax, safe.xMax) - 40;
            focusToggle.style.top = focus ? safe.yMax - 178 : Mathf.Max(safe.y, world.yMax - 78);
            if (focus)
            {
                SetVisible(portraitDashboard, false);
                SetVisible(portraitResources, false);
                SetVisible(returnControls, false);
                SetVisible(breakdown, false);
            }
        }
        private void LateUpdate()
        {
            // Match town-window dismissal: finish the pointer gesture before hiding UI.
            if (root != null && Mouse.current?.rightButton.wasReleasedThisFrame == true)
            {
                HideDetails();
                SetHeroStatsOpen(false);
            }
        }

        private void ConfigurePortraitHud(Rect area)
        {
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            root.style.overflow = portrait ? Overflow.Visible : Overflow.Hidden;
            var health = hud.Q("run-health-strip");
            var world = ToolkitResponsiveLayout.WorldArea;
            hud.style.left = portrait ? 8 : 12;
            hud.style.top = portrait ? (ToolkitResponsiveLayout.FocusView ? area.height - 84 : 8) : StyleKeyword.Auto;
            hud.style.bottom = portrait ? StyleKeyword.Auto : 12;
            // Reserve the five-slot footprint even before abilities unlock. A hidden
            // row must not make the health strip shrink to its text's intrinsic width.
            hud.style.width = portrait ? Mathf.Min(138, world.width - 220) : 161;
            abilityBar.style.width = portrait ? StyleKeyword.Null : Length.Percent(100);
            for (var i = 0; i < slots.Count; i++)
            {
                slots[i].button.style.marginRight = portrait ? StyleKeyword.Null : i == slots.Count - 1 ? 0 : 4;
            }
            heroStatsPanel.EnableInClassList("focus-stats-content", portrait && ToolkitResponsiveLayout.FocusView);
            if (portraitStatsScroll != null)
            {
                var focus = ToolkitResponsiveLayout.FocusView;
                // Expanded play keeps its camera viewport. Float the stats above
                // the progress strip instead of reopening the suspended dock.
                portraitStatsScroll.style.top = focus ? StyleKeyword.Auto : 52;
                portraitStatsScroll.style.bottom = focus ? area.yMax - world.yMax + 48 : StyleKeyword.Auto;
                portraitStatsScroll.style.maxHeight = focus ? Mathf.Min(240, world.height - 64) : Mathf.Min(180, area.height - 140);
                portraitStatsScroll.style.width = focus ? Mathf.Min(260, area.width - 64) : area.width - 16;
                portraitStatsScroll.style.backgroundColor = focus ? ToolkitColorTheme.HudSurface : Color.clear;
                portraitStatsScroll.style.borderLeftWidth = portraitStatsScroll.style.borderRightWidth =
                    portraitStatsScroll.style.borderTopWidth = portraitStatsScroll.style.borderBottomWidth = focus ? .5f : 0;
                portraitStatsScroll.style.borderLeftColor = portraitStatsScroll.style.borderRightColor =
                    portraitStatsScroll.style.borderTopColor = portraitStatsScroll.style.borderBottomColor = ToolkitColorTheme.HudBorder;
                SetVisible(portraitStatsScroll, portrait && heroStatsOpen);
            }
            if (portrait)
            {
                abilityBar.style.left = area.width - 204;
                abilityBar.style.top = ToolkitResponsiveLayout.FocusView ? area.height - 84 : 8;
            }
            // Colour changes must not repeat the orientation-only reparenting below.
            health.style.backgroundColor = portrait ? Color.clear : ToolkitColorTheme.HudSurface;
            heroStatsPanel.style.backgroundColor = portrait ? Color.clear : ToolkitColorTheme.HudSurface;
            if (portraitRun == portrait) return;
            portraitRun = portrait;
            if (portrait && showingBreakdown && GameManager.Instance && !GameManager.Instance.HeroIsDead) HideDetails();
            health.style.width = Length.Percent(100);
            health.style.height = portrait ? 36 : 24;
            health.style.borderLeftWidth = health.style.borderRightWidth = health.style.borderTopWidth = health.style.borderBottomWidth = portrait ? 0 : .5f;
            health.style.paddingLeft = portrait ? 0 : 5;
            health.style.paddingRight = portrait ? 0 : 3;
            heroStatsToggle.style.backgroundColor = StyleKeyword.Null;
            heroStatsToggle.style.borderLeftWidth = heroStatsToggle.style.borderRightWidth = heroStatsToggle.style.borderTopWidth = heroStatsToggle.style.borderBottomWidth = .5f;
            heroStatsPanel.EnableInClassList("portrait-stats-content", portrait);
            heroStatsPanel.style.position = portrait ? Position.Relative : Position.Absolute;
            heroStatsPanel.style.bottom = portrait ? StyleKeyword.Auto : Length.Percent(100);
            heroStatsPanel.style.width = Length.Percent(100);
            heroStatsPanel.style.marginBottom = portrait ? 0 : 4;
            heroStatsPanel.style.borderLeftWidth = heroStatsPanel.style.borderRightWidth = heroStatsPanel.style.borderTopWidth = heroStatsPanel.style.borderBottomWidth = portrait ? 0 : .5f;
            heroHealthText.style.fontSize = portrait ? 12 : 9.5f;
            heroStatsToggle.style.fontSize = portrait ? 12 : 9.5f;
            heroStatsToggle.style.height = portrait ? 28 : 18;
            heroStatsToggle.style.minHeight = portrait ? 28 : 18;
            heroStatsToggle.style.minWidth = portrait ? 38 : 31;
            heroStatsToggle.style.flexShrink = 0;
            if (portrait)
            {
                root.Add(abilityBar);
                portraitStatsScroll = ToolkitGameplay.Scroll(root, "portrait-combat-stats");
                portraitStatsScroll.Add(heroStatsPanel);
                portraitStatsScroll.style.position = Position.Absolute;
                portraitStatsScroll.style.left = 8;
                portraitStatsScroll.style.top = 52;
                portraitStatsScroll.style.width = area.width - 16;
                portraitStatsScroll.style.maxHeight = Mathf.Min(180, area.height - 140);
                SetVisible(portraitStatsScroll, heroStatsOpen);
                portraitDashboard = E(root, "portrait-run-dashboard");
                portraitRunActions = E(portraitDashboard, "portrait-run-actions row");
                portraitRunActions.Add(returnControls);
                portraitRunActions.Add(breakdown);
                portraitDashboard.Add(death);
            }
            else
            {
                hud.Insert(0, abilityBar);
                abilityBar.PlaceBehind(health);
                hud.Insert(2, heroStatsPanel);
                portraitStatsScroll?.RemoveFromHierarchy();
                portraitStatsScroll = null;
                root.Add(returnControls);
                root.Add(death);
                root.Add(breakdown);
                portraitRunActions?.RemoveFromHierarchy();
                portraitRunActions = null;
                portraitDashboard?.RemoveFromHierarchy();
                portraitDashboard = null;
            }
            abilityBar.EnableInClassList("portrait-map-abilities", portrait);
            abilityBar.style.position = portrait ? Position.Absolute : Position.Relative;
            if (!portrait) abilityBar.style.left = abilityBar.style.top = StyleKeyword.Null;
            returnControls.style.position = portrait ? Position.Relative : Position.Absolute;
            returnControls.style.top = returnControls.style.right = portrait ? StyleKeyword.Auto : StyleKeyword.Null;
            returnControls.style.marginTop = 0;
            returnControls.style.flexDirection = portrait ? FlexDirection.Row : FlexDirection.Column;
            foreach (var control in new VisualElement[] { death, breakdown })
            {
                control.style.position = portrait ? Position.Relative : Position.Absolute;
                control.style.left = control.style.right = control.style.top = control.style.bottom = portrait ? StyleKeyword.Auto : StyleKeyword.Null;
                control.style.translate = portrait ? new Translate(0, 0) : StyleKeyword.Null;
                control.style.width = portrait && control == death ? Length.Percent(100) : StyleKeyword.Null;
                control.style.marginTop = portrait && control == death ? 8 : 0;
            }
            heroStatsPanel.style.position = portrait ? Position.Relative : Position.Absolute;
            heroStatsPanel.style.bottom = portrait ? StyleKeyword.Auto : Length.Percent(100);
            heroStatsPanel.style.width = Length.Percent(100);
            foreach (var label in heroStatsPanel.Query<Label>().ToList())
                label.style.fontSize = portrait ? 12 : label == heroCharacterName ? 11.5f : 9.5f;
            foreach (var value in heroStatValues.Values) value.parent.style.height = portrait ? 26 : 13;
        }

        private void HandleBuffHotkeys()
        {
            if (ToolkitMeetingScreen.HasOpenDialogue || UnityEngine.Device.Application.isMobilePlatform || Keyboard.current == null || (ToolkitConsoleScreen.Instance && ToolkitConsoleScreen.Instance.IsActive)) return;
            var manager = BuffManager.Instance;
            if (!manager || !manager.HasUnlockedAbilities) return;
            for (int i = 0; i < slots.Count; i++)
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame ||
                    Keyboard.current[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame)
                    if (BuffIntroductionGuidance.Stage != 3 || BuffIntroductionGuidance.IsFirstEcho(manager.GetAssigned(i))) manager.ActivateSlot(i);
        }
        private void Refresh()
        {
            var gm = GameManager.Instance; bool inRun = gm && gm.CurrentMap;
            var waitingForReapDialogue = gm && gm.FirstReapDialoguePending;
            SetVisible(hud, inRun); SetVisible(returnControls, inRun&&!(TownWindowManager.Instance&&TownWindowManager.Instance.HasOpenWindow)); SetVisible(breakdown, inRun); SetVisible(death, inRun && gm.DeathPromptVisible && (!waitingForReapDialogue || ToolkitMeetingScreen.AllowsNavigation("return-town")));
            if (portraitDashboard != null) SetVisible(portraitDashboard, inRun && !heroStatsOpen && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow));
            if (ToolkitResponsiveLayout.IsPortrait && inRun)
            {
                SetVisible(returnControls, !gm.HeroIsDead && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow));
                SetVisible(breakdown, false);
            }
            if (!inRun) { SetVisible(abilityBar, false); SetHeroStatsOpen(false); return; }
            var hero = HeroController.Instance; var health = hero ? hero.GetComponent<HeroHealth>() : null;
            RefreshHeroHud(hero, health);
            heroStatsToggle.SetEnabled((!ToolkitMeetingScreen.HasOpenDialogue || ToolkitMeetingScreen.AllowsNavigation("hero-stats")) && BuffIntroductionGuidance.Stage != 3);
            retreat.text = gm.RetreatQueued ? ToolkitLocalization.Text("run.retreating", "Retreating...") : ToolkitLocalization.Text("run.return-town", "Return To Town"); retreat.SetEnabled(!gm.HeroIsDead && ((!waitingForReapDialogue && !ToolkitMeetingScreen.HasOpenDialogue) || ToolkitMeetingScreen.AllowsNavigation("return-town")) && BuffIntroductionGuidance.Stage != 3);
            returnOnDeath.text = gm.ReturnOnDeathQueued ? ToolkitLocalization.Text("run.queued", "Queued") : ToolkitLocalization.Text("run.return-on-death", "Return On Death"); returnOnDeath.SetEnabled(!gm.HeroIsDead && !waitingForReapDialogue && !ToolkitMeetingScreen.HasOpenDialogue && BuffIntroductionGuidance.Stage != 3);
            deathReturn.SetEnabled((!waitingForReapDialogue && !ToolkitMeetingScreen.HasOpenDialogue) || ToolkitMeetingScreen.AllowsNavigation("return-town"));
            restart.SetEnabled(!waitingForReapDialogue && !ToolkitMeetingScreen.HasOpenDialogue);
            deathSummary.SetEnabled((!waitingForReapDialogue && !ToolkitMeetingScreen.HasOpenDialogue) || ToolkitMeetingScreen.AllowsNavigation("run-summary"));
            breakdown.SetEnabled(((!waitingForReapDialogue && !ToolkitMeetingScreen.HasOpenDialogue) || ToolkitMeetingScreen.AllowsNavigation("run-summary")) && BuffIntroductionGuidance.Stage != 3);
            var resourceTotal = runResources.Values.Sum() * (1d + gm.ResourceBonusPercent * .01d);
            breakdownResourceIcon.sprite = definition.ResourceIcon(resourceTotal);
            breakdownTitle.text = ToolkitResponsiveLayout.IsPortrait ? ToolkitLocalization.Text("run.summary-short", "Summary") :
                ToolkitLocalization.Text("run.total-label", "Total") + " " + ToolkitControls.WithoutInlineTextSize(CalcUtils.FormatNumber(resourceTotal, true));
            SetVisible(breakdownResourceIcon, !ToolkitResponsiveLayout.IsPortrait);
            SetVisible(breakdownTotals, !ToolkitResponsiveLayout.IsPortrait);
            breakdownKills.text = (tracker ? tracker.SessionKills : 0).ToString("N0", LocaleNumbers.Culture);
            breakdownMultiplier.text = "×" + gm.ResourceMultiplier.ToString("0.##", LocaleNumbers.Culture);
            deathTitle.text = gm.IvanReturnPending ? ToolkitLocalization.Text("run.return-ivan", "Return to town to meet Ivan") : gm.RunEndedByReaper ? ToolkitLocalization.Text("run.reaped-prompt", "You were reaped...") : ToolkitLocalization.Text("run.died-prompt", "You have Died...");
            SetVisible(deathFill.parent, !gm.IvanReturnPending && !gm.FirstReapReturnPending);
            deathFill.style.width = Length.Percent(gm.DeathPromptProgress * 100);
            RefreshBuffs(!gm.HeroIsDead && health && health.CurrentHealth > 0 && health.gameObject.activeInHierarchy);
            if (showingBreakdown) RefreshBreakdown();
            if (ToolkitResponsiveLayout.IsPortrait && !HasOpenDetails && !heroStatsOpen) RefreshResourceView(portraitResources);
        }
        private void RefreshBuffs(bool alive)
        {
            var manager = BuffManager.Instance;
            SetVisible(abilityBar, manager && manager.HasUnlockedAbilities && GameManager.Instance && GameManager.Instance.CurrentMap);
            if (!manager || !manager.HasUnlockedAbilities) { tooltipSlot = -1; SetVisible(tooltip, false); return; }
            for (int i = 0; i < slots.Count; i++)
            {
                var row = slots[i]; var recipe = manager.GetAssigned(i); row.icon.sprite = recipe ? recipe.buffIcon : null;
                ((ToolkitBorderProgress)row.fill).Value=0; row.text.text = "";
                bool unlocked = i < manager.UnlockedSlots; row.button.SetEnabled(alive && unlocked && recipe && !ToolkitMeetingScreen.HasOpenDialogue && (BuffIntroductionGuidance.Stage != 3 || BuffIntroductionGuidance.IsFirstEcho(recipe))); row.icon.tintColor = Color.white;
                row.button.EnableInClassList("ability-locked", !unlocked);
                SetVisible(row.icon, unlocked && recipe); SetVisible(row.locked, !unlocked);
                SetVisible(row.auto, unlocked && manager.IsSlotAutoCasting(i));
                if (!unlocked) continue; if (!recipe) continue;
                float remain = manager.GetRemaining(recipe), cooldown = manager.GetCooldownRemaining(recipe);
                if (!alive) { row.text.text = ToolkitLocalization.Text("run.dead", "Dead"); row.icon.tintColor = Color.gray; continue; }
                if (recipe.durationType == BuffDurationType.DistancePercent && tracker)
                {
                    float end = Mathf.Max(1, tracker.LongestRun) * recipe.GetDuration();
                    if (tracker.CurrentRunDistance >= end) { row.text.text = ToolkitLocalization.Text("run.too-far", "Too Far"); row.icon.tintColor = Color.gray; continue; }
                    if (remain > 0) { row.text.text = Mathf.FloorToInt(tracker.CurrentRunDistance / end * 100) + LocaleNumbers.PercentSuffix; ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(1 - tracker.CurrentRunDistance / end); continue; }
                }
                if (remain > 0) { row.text.text = CountdownText(remain); ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(remain / recipe.GetDuration()); }
                else if (cooldown > 0) { row.text.text = CountdownText(cooldown); row.icon.tintColor = Color.gray; ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(1 - cooldown / recipe.GetCooldown()); }
            }
        }
        // A clock avoids squeezing language-specific unit names into the small ability buttons.
        private static string CountdownText(double seconds)
        {
            if (!LocaleNumbers.IsFinite(seconds) || seconds >= TimeSpan.MaxValue.TotalSeconds)
                return LocaleNumbers.Format(seconds);
            if (seconds < 10) return LocaleNumbers.Format(seconds, "0.00");
            var whole = Math.Floor(seconds);
            var minutes = Math.Floor(whole / 60);
            return LocaleNumbers.Integer(minutes, false) + ":" + ((int)(whole % 60)).ToString("D2", LocaleNumbers.Culture);
        }

        private void RefreshTooltip()
        {
            var recipe = tooltipSlot >= 0 ? BuffManager.Instance?.GetAssigned(tooltipSlot) : null;
            SetVisible(tooltip, recipe); if (recipe) tooltipText.text = recipe.GetDisplayName() + "\n" + string.Join("\n", recipe.GetDescriptionLines());
        }
        public void BeginOuting() { runResources.Clear(); HideDetails(); }
        private void RunStarted() { SetHeroStatsOpen(false); HideDetails(); }
        private void RunEnded(bool died) { if (died) HideDetails(); }
        private void ResourceAdded(Resource resource, double amount, bool bonus)
        { if (!tracker || !tracker.RunInProgress || !resource || resource.DisableAlterEcho || bonus || amount <= 0) return; runResources.TryGetValue(resource, out var current); runResources[resource] = current + amount; }
        public void ShowBreakdown()
        {
            if (ToolkitMeetingScreen.TryFollowNavigation("run-summary")) return;
            ToolkitResponsiveLayout.SetFocusView(false); HideDetails(); SetHeroStatsOpen(false);
            if (ToolkitResponsiveLayout.IsPortrait && GameManager.Instance && !GameManager.Instance.HeroIsDead) return;
            showingBreakdown = true; overlay.pickingMode = PickingMode.Ignore; detail.AddToClassList("resource-breakdown");
            SetVisible(detailTitle.parent, false); SetVisible(detailRows, false); SetVisible(runRatesLeft.parent, false);
            SetVisible(liveBreakdown, true); SetVisible(overlay, true);
            LayoutSummary(ToolkitWindowLayout.SafeArea); RefreshBreakdown();
        }
        private void RefreshBreakdown() => RefreshResourceView(liveBreakdown);
        public bool HasOpenHeroStats => heroStatsOpen;
        public bool HasOpenDetails => overlay != null && overlay.style.display.value != DisplayStyle.None;
        public void HideDetails() { if(detail!=null)detail.Q(className: "breakdown-rate").style.display=DisplayStyle.None; showingBreakdown = false; if(overlay!=null)overlay.pickingMode=PickingMode.Position; ClearSummary();detail?.RemoveFromClassList("resource-breakdown");SetVisible(liveBreakdown,false);if(detailTitle!=null)SetVisible(detailTitle.parent,true);SetVisible(detailRows,true);if(runTime!=null)runTime.text="";if(runRatesLeft!=null)runRatesLeft.text="";if(runRatesRight!=null)runRatesRight.text="";SetVisible(overlay, false); detailRows?.Clear(); }
        private static void SetVisible(VisualElement element, bool value) { if (element != null) element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; }
        private void OnDisable()
        {
            ToolkitResponsiveLayout.SetFocusView(false);
            focusControls?.RemoveFromHierarchy(); focusControls = null;
            portraitRun = null;
            ToolkitLocalization.Changed -= LocalizationChanged;
            Blindsided.EventHandler.OnRunStarted -= RunStarted;
            if (resources) resources.OnResourceAdded -= ResourceAdded;
            if (tracker) tracker.OnRunEnded -= RunEnded;
            if (guidedBuffManager) guidedBuffManager.OnBuffCast -= FirstEchoCast;
            guidedBuffManager = null;
            heroStatValues.Clear();
            resources = null; tracker = null; root?.RemoveFromHierarchy(); root = null; slots.Clear();
            if (settings) Destroy(settings); settings = null;
        }
    }
}
