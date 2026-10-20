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
        private Button retreat, returnOnDeath, restart, deathReturn, deathSummary, breakdown;
        private Image ivanReturnPointer;
        private Image firstEchoArrow;
        private BuffManager guidedBuffManager;
        private ScrollView detailRows;
        private readonly List<(Button button, Image icon, Image auto, Label text, VisualElement fill)> slots = new();
        private readonly Dictionary<Resource, double> runResources = new();
        private readonly Dictionary<Resource, VisualElement> resourceRows = new();
        private ResourceManager resources;
        private GameplayStatTracker tracker;
        private bool showingBreakdown, portraitDetails;
        private bool? portraitRun;
        private VisualElement portraitRunActions;
        private VisualElement portraitDashboard;
        private ScrollView portraitStatsScroll;
        private float nextRefresh, elapsedAtEnd, runStartedAt;
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
            StatIconLookup.TryGetIcon(StatIconLookup.StatKey.UpArrow, out var echoArrow);
            firstEchoArrow = new Image { name = "first-echo-arrow", sprite = echoArrow, pickingMode = PickingMode.Ignore };
            firstEchoArrow.style.position = Position.Absolute; firstEchoArrow.style.width = firstEchoArrow.style.height = 12;
            firstEchoArrow.style.rotate = new Rotate(new Angle(180, AngleUnit.Degree));
            root.Add(firstEchoArrow); SetVisible(firstEchoArrow, false);
            returnControls = new VisualElement(); returnControls.AddToClassList("eov-run-return"); root.Add(returnControls);
            retreat = Button(returnControls, "run-return", ToolkitLocalization.Text("run.return-town-title", "Return To Town"), () => GameManager.Instance?.RequestRetreat());
            returnOnDeath = Button(returnControls, "run-return-on-death", ToolkitLocalization.Text("run.return-on-death-title", "Return On Death"), () => GameManager.Instance?.RequestReturnOnDeath());
            ivanReturnPointer = new Image { name = "ivan-return-pointer", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
            ivanReturnPointer.style.position = Position.Absolute; ivanReturnPointer.style.width = ivanReturnPointer.style.height = 24; root.Add(ivanReturnPointer);
            SetVisible(ivanReturnPointer, false);
            breakdown = Button(root, "run-breakdown", ToolkitLocalization.Text("run.breakdown-title", "Run Breakdown"), ShowBreakdown); breakdown.AddToClassList("eov-run-breakdown");
            death = Panel(root, "eov-run-death"); deathTitle = Text(death, "", ToolkitControls.TextRole.Heading);
            restart = LocalizedButton(death, "run-restart", "run.restart", "Restart", () => GameManager.Instance?.RequestRestartAfterDeath());
            deathReturn = LocalizedButton(death, "run-death-return", "run.return-town-title", ToolkitLocalization.Text("run.return-town-title", "Return To Town"), () => GameManager.Instance?.RequestReturnAfterDeath());
            deathSummary = LocalizedButton(death, "run-death-summary", "run.view-summary", "View Summary", ShowBreakdown); deathFill = Progress(death, definition.healthTrack, definition.healthFill);
            overlay = new VisualElement(); overlay.AddToClassList("eov-run-overlay"); root.Add(overlay);
            detail=Panel(overlay,"breakdown-sheet");var head=E(detail,"row breakdown-header");LocalizedLabel(head, "run.base-return-rate", "Base rate (return bonus)","small muted breakdown-rate");detailTitle=L(head,ToolkitLocalization.Text("run.breakdown-heading", "Run breakdown"),"heading breakdown-title");runTime=L(head,"","strong breakdown-time");BuildSummaryOverview();
            detailRows=ToolkitGameplay.Scroll(detail,"run-ledger");detailRows.contentContainer.AddToClassList("run-ledger-grid");var footer=E(detail,"row breakdown-footer");runRatesLeft=L(footer,"","small");runRatesRight=L(footer,"","small");runRatesRight.style.unityTextAlign=TextAnchor.MiddleRight; BuildDetailsClose(); HideDetails();
            tooltip = Panel(root, "eov-run-tooltip", definition.tooltipFrame); tooltipText = Text(tooltip, ""); SetVisible(tooltip, false);
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
            foreach (var pair in resourceRows) pair.Value.tooltip = ToolkitLocalization.Name(pair.Key);
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
            SetVisible(firstEchoArrow, false);
            Blindsided.EventHandler.SaveData();
        }
        private void RefreshFirstEchoArrow()
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
            SetVisible(firstEchoArrow, target != null && firstEchoArrow.sprite);
            if (target == null) return;
            var point = root.WorldToLocal(new Vector2(target.worldBound.center.x, target.worldBound.yMin));
            firstEchoArrow.style.left = Mathf.Round(point.x - 6);
            firstEchoArrow.style.top = Mathf.Round(point.y - 18 - 4 * (1 + Mathf.Cos(Time.unscaledTime * 5)));
            firstEchoArrow.BringToFront();
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
            slots.Add((button, icon, auto, label, fill));
        }
        private void Update()
        {
            if (root == null) return; AttachServices();
            var area = ToolkitWindowLayout.SafeArea;
            root.style.left = area.x; root.style.top = area.y; root.style.width = area.width; root.style.height = area.height;
            ConfigurePortraitHud(area);
            UpdatePortraitResources(area);
            // Run-summary backdrops also extend outside the HUD safe area.
            overlay.style.left = ToolkitResponsiveLayout.IsPortrait ? 0 : -area.x; overlay.style.top = ToolkitResponsiveLayout.IsPortrait ? 0 : -area.y;
            overlay.style.width = ToolkitResponsiveLayout.IsPortrait ? area.width : ToolkitResponsiveLayout.Size.x;
            overlay.style.height = ToolkitResponsiveLayout.IsPortrait ? area.height : ToolkitResponsiveLayout.Size.y;
            if (summaryData != null || ToolkitResponsiveLayout.IsPortrait || portraitDetails) LayoutSummary(area);
            portraitDetails = ToolkitResponsiveLayout.IsPortrait;
            HandleBuffHotkeys();
            RefreshFirstEchoArrow();
            var guideReturn = GameManager.Instance && GameManager.Instance.CurrentMap && !GameManager.Instance.FirstReapDialoguePending && (GameManager.Instance.IvanReturnPending || GameManager.Instance.FirstReapReturnPending) && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow);
            SetVisible(ivanReturnPointer, guideReturn);
            if (guideReturn)
            {
                var target = GameManager.Instance.HeroIsDead ? deathReturn : retreat;
                var point = root.WorldToLocal(new Vector2(target.worldBound.center.x, target.worldBound.yMax));
                ivanReturnPointer.style.left = Mathf.Round(point.x - 12); ivanReturnPointer.style.top = Mathf.Round(point.y + 2 + 4 * (1 + Mathf.Cos(Time.unscaledTime * 5)));
            }
            if (Mouse.current?.rightButton.wasPressedThisFrame == true || Keyboard.current?.escapeKey.wasPressedThisFrame == true) { HideDetails(); SetHeroStatsOpen(false); }
            if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .1f; Refresh();
        }
        private void ConfigurePortraitHud(Rect area)
        {
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            root.style.overflow = portrait ? Overflow.Visible : Overflow.Hidden;
            var health = hud.Q("run-health-strip");
            var world = ToolkitResponsiveLayout.WorldArea;
            hud.style.left = portrait ? 8 : 12;
            hud.style.top = portrait ? 8 : StyleKeyword.Auto;
            hud.style.bottom = portrait ? StyleKeyword.Auto : 12;
            hud.style.width = portrait ? Mathf.Min(138, world.width - 220) : 154;
            if (portraitStatsScroll != null)
            {
                portraitStatsScroll.style.maxHeight = Mathf.Min(180, area.height - 140);
                portraitStatsScroll.style.width = area.width - 16;
                SetVisible(portraitStatsScroll, portrait && heroStatsOpen);
            }
            if (portrait)
            {
                abilityBar.style.left = area.width - 204;
                abilityBar.style.top = 8;
            }
            if (portraitRun == portrait) return;
            portraitRun = portrait;
            health.style.width = portrait ? Length.Percent(100) : 154;
            health.style.height = portrait ? 36 : 24;
            health.style.backgroundColor = portrait ? Color.clear : (Color)new Color32(43, 36, 37, 255);
            health.style.borderLeftWidth = health.style.borderRightWidth = health.style.borderTopWidth = health.style.borderBottomWidth = portrait ? 0 : .5f;
            health.style.paddingLeft = portrait ? 0 : 5;
            health.style.paddingRight = portrait ? 0 : 3;
            heroStatsToggle.style.backgroundColor = portrait ? Color.clear : (Color)new Color32(43, 36, 37, 255);
            heroStatsToggle.style.borderLeftWidth = heroStatsToggle.style.borderRightWidth = heroStatsToggle.style.borderTopWidth = heroStatsToggle.style.borderBottomWidth = portrait ? 0 : .5f;
            heroStatsPanel.EnableInClassList("portrait-stats-content", portrait);
            heroStatsPanel.style.position = portrait ? Position.Relative : Position.Absolute;
            heroStatsPanel.style.bottom = portrait ? StyleKeyword.Auto : Length.Percent(100);
            heroStatsPanel.style.width = portrait ? Length.Percent(100) : 154;
            heroStatsPanel.style.marginBottom = portrait ? 0 : 4;
            heroStatsPanel.style.backgroundColor = portrait ? Color.clear : (Color)new Color32(43, 36, 37, 255);
            heroStatsPanel.style.borderLeftWidth = heroStatsPanel.style.borderRightWidth = heroStatsPanel.style.borderTopWidth = heroStatsPanel.style.borderBottomWidth = portrait ? 0 : .5f;
            heroHealthText.style.fontSize = portrait ? 9 : 8;
            heroStatsToggle.style.fontSize = portrait ? 9 : 8;
            heroStatsToggle.style.height = portrait ? 28 : 18;
            heroStatsToggle.style.minWidth = portrait ? 34 : 31;
            if (portrait)
            {
                root.Add(abilityBar);
                portraitStatsScroll = ToolkitGameplay.Scroll(hud, "portrait-combat-stats");
                portraitStatsScroll.Add(heroStatsPanel);
                portraitStatsScroll.style.position = Position.Absolute;
                portraitStatsScroll.style.top = 44;
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
            heroStatsPanel.style.width = portrait ? Length.Percent(100) : 154;
            foreach (var label in heroStatsPanel.Query<Label>().ToList())
                label.style.fontSize = portrait ? 10 : label == heroCharacterName ? 9 : 8;
            foreach (var value in heroStatValues.Values) value.parent.style.height = portrait ? 26 : 13;
        }

        private void HandleBuffHotkeys()
        {
            if (UnityEngine.Device.Application.isMobilePlatform || Keyboard.current == null || (ToolkitConsoleScreen.Instance && ToolkitConsoleScreen.Instance.IsActive)) return;
            var manager = BuffManager.Instance;
            if (!manager || !manager.HasUnlockedAbilities) return;
            for (int i = 0; i < slots.Count; i++)
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame ||
                    Keyboard.current[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame)
                    manager.ActivateSlot(i);
        }
        private void Refresh()
        {
            var gm = GameManager.Instance; bool inRun = gm && gm.CurrentMap;
            var waitingForReapDialogue = gm && gm.FirstReapDialoguePending;
            SetVisible(hud, inRun); SetVisible(returnControls, inRun&&!waitingForReapDialogue&&!(TownWindowManager.Instance&&TownWindowManager.Instance.HasOpenWindow)); SetVisible(breakdown, inRun&&!waitingForReapDialogue); SetVisible(death, inRun && gm.DeathPromptVisible && !waitingForReapDialogue);
            if (portraitDashboard != null) SetVisible(portraitDashboard, inRun && !heroStatsOpen && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow));
            if (ToolkitResponsiveLayout.IsPortrait && ToolkitMeetingScreen.HasVisibleMeeting) SetVisible(hud, false);
            SetVisible(abilityBar, inRun && BuffManager.Instance && BuffManager.Instance.HasUnlockedAbilities && !ToolkitMeetingScreen.HasVisibleMeeting && !HasOpenDetails);
            if (ToolkitResponsiveLayout.IsPortrait && inRun)
            {
                SetVisible(returnControls, !gm.HeroIsDead && !waitingForReapDialogue && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow));
                SetVisible(breakdown, !gm.HeroIsDead && !waitingForReapDialogue);
            }
            if (!inRun) { SetHeroStatsOpen(false); return; }
            var hero = HeroController.Instance; var health = hero ? hero.GetComponent<HeroHealth>() : null;
            RefreshHeroHud(hero, health);
            retreat.text = gm.RetreatQueued ? ToolkitLocalization.Text("run.retreating", "Retreating...") : ToolkitLocalization.Text("run.return-town", "Return To Town"); retreat.SetEnabled(!gm.HeroIsDead);
            returnOnDeath.text = gm.ReturnOnDeathQueued ? ToolkitLocalization.Text("run.queued", "Queued") : ToolkitLocalization.Text("run.return-on-death", "Return On Death"); returnOnDeath.SetEnabled(!gm.HeroIsDead);
            deathReturn.SetEnabled(!waitingForReapDialogue);
            restart.SetEnabled(!waitingForReapDialogue);
            deathSummary.SetEnabled(!waitingForReapDialogue);
            breakdown.SetEnabled(!waitingForReapDialogue);
            breakdown.text = ToolkitResponsiveLayout.IsPortrait ? ToolkitLocalization.Text("run.summary-short", "Summary") :
                ToolkitLocalization.Text("run.breakdown-bonus", "Run breakdown\n<size=75%>+{0:0}% Resources</size>", (tracker ? tracker.CurrentRunKills : 0) * gm.BonusPercentPerKill);
            deathTitle.text = gm.IvanReturnPending ? ToolkitLocalization.Text("run.return-ivan", "Return to town to meet Ivan") : gm.RunEndedByReaper ? ToolkitLocalization.Text("run.reaped-prompt", "You were reaped...") : ToolkitLocalization.Text("run.died-prompt", "You have Died...");
            SetVisible(deathFill.parent, !gm.IvanReturnPending && !gm.FirstReapReturnPending);
            deathFill.style.width = Length.Percent(gm.DeathPromptProgress * 100);
            RefreshBuffs(!gm.HeroIsDead && health && health.CurrentHealth > 0 && health.gameObject.activeInHierarchy);
            if (showingBreakdown) RefreshBreakdown();
        }
        private void RefreshBuffs(bool alive)
        {
            var manager = BuffManager.Instance;
            SetVisible(abilityBar, manager && manager.HasUnlockedAbilities &&
                (!ToolkitResponsiveLayout.IsPortrait || (!ToolkitMeetingScreen.HasVisibleMeeting && !HasOpenDetails)));
            if (!manager || !manager.HasUnlockedAbilities) { tooltipSlot = -1; SetVisible(tooltip, false); return; }
            for (int i = 0; i < slots.Count; i++)
            {
                var row = slots[i]; var recipe = manager.GetAssigned(i); row.icon.sprite = recipe ? recipe.buffIcon : null;
                SetVisible(row.auto, manager.IsSlotAutoCasting(i)); ((ToolkitBorderProgress)row.fill).Value=0; row.text.text = "";
                bool unlocked = i < manager.UnlockedSlots; row.button.SetEnabled(alive && unlocked && recipe); row.icon.tintColor = Color.white;
                row.button.EnableInClassList("ability-locked", !unlocked);
                SetVisible(row.icon, unlocked && recipe);
                if (!unlocked) { row.text.text = ToolkitLocalization.Text("common.locked", "Locked"); continue; } if (!recipe) continue;
                float remain = manager.GetRemaining(recipe), cooldown = manager.GetCooldownRemaining(recipe);
                if (!alive) { row.text.text = ToolkitLocalization.Text("run.dead", "Dead"); row.icon.tintColor = Color.gray; continue; }
                if (recipe.durationType == BuffDurationType.DistancePercent && tracker)
                {
                    float end = Mathf.Max(1, tracker.LongestRun) * recipe.GetDuration();
                    if (tracker.CurrentRunDistance >= end) { row.text.text = ToolkitLocalization.Text("run.too-far", "Too Far"); row.icon.tintColor = Color.gray; continue; }
                    if (remain > 0) { row.text.text = Mathf.FloorToInt(tracker.CurrentRunDistance / end * 100) + "%"; ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(1 - tracker.CurrentRunDistance / end); continue; }
                }
                if (remain > 0) { row.text.text = CalcUtils.FormatTime(remain, remain < 10, shortForm: true); ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(remain / recipe.GetDuration()); }
                else if (cooldown > 0) { row.text.text = CalcUtils.FormatTime(cooldown, cooldown < 10, shortForm: true); row.icon.tintColor = Color.gray; ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(1 - cooldown / recipe.GetCooldown()); }
            }
        }
        private void RefreshTooltip()
        {
            var recipe = tooltipSlot >= 0 ? BuffManager.Instance?.GetAssigned(tooltipSlot) : null;
            SetVisible(tooltip, recipe); if (recipe) tooltipText.text = recipe.GetDisplayName() + "\n" + string.Join("\n", recipe.GetDescriptionLines());
        }
        private void RunStarted() { portraitResourcesDirty = true; SetHeroStatsOpen(false); runResources.Clear(); elapsedAtEnd = 0; runStartedAt = Time.time; HideDetails(); }
        private void RunEnded(bool died) { elapsedAtEnd = tracker ? tracker.LastRunDuration : 0; if (died) HideDetails(); }
        private void ResourceAdded(Resource resource, double amount, bool bonus)
        { if (!tracker || !tracker.RunInProgress || !resource || resource.DisableAlterEcho || bonus || amount <= 0) return; runResources.TryGetValue(resource, out var current); runResources[resource] = current + amount; portraitResourcesDirty = true; }
        public void ShowBreakdown() { HideDetails(); detail.Q(className: "breakdown-rate").style.display=DisplayStyle.Flex; showingBreakdown = true; detailTitle.text = ToolkitLocalization.Text("run.breakdown-title", "Run Breakdown"); SetVisible(overlay, true); RefreshBreakdown(); }
        private void RefreshBreakdown()
        {
            double elapsed = tracker && tracker.RunInProgress ? Time.time - runStartedAt : elapsedAtEnd; double seconds = Math.Max(.0001, elapsed);
            runTime.text=CalcUtils.FormatTime((float)elapsed);runRatesLeft.text=ToolkitLocalization.Text("run.distance-rate-only", "Distance / min: {0:N0}", (tracker?tracker.CurrentRunDistance:0)*60/seconds);runRatesRight.text=ToolkitLocalization.Text("run.combat-rates", "Damage / s: {0:N0}\nKills / min: {1:N0}", (tracker?tracker.CurrentRunDamageDealt:0)/seconds, (tracker?tracker.CurrentRunKills:0)*60/seconds);
            double bonus = (tracker ? tracker.CurrentRunKills : 0) * (GameManager.Instance ? GameManager.Instance.BonusPercentPerKill : 0) * .01;
            foreach (var pair in runResources.OrderBy(x => x.Key.resourceID)) SetResourceRow(pair.Key, ToolkitLocalization.Text("run.resource-rates", "Earned: {0} ({1})\nPer minute: {2} ({3})", CalcUtils.FormatNumber(pair.Value,true), CalcUtils.FormatNumber(pair.Value*(1+bonus),true), CalcUtils.FormatNumber(pair.Value*60/seconds,true), CalcUtils.FormatNumber(pair.Value*(1+bonus)*60/seconds,true)));
        }
        private void SetResourceRow(Resource resource, string value)
        {
            if (!resourceRows.TryGetValue(resource, out var row)) { row = E(detailRows,"row ledger-entry");row.tooltip=ToolkitLocalization.Name(resource);ToolkitGameplay.Icon(row,resource.icon,18);L(row,"","small").name="amount"; resourceRows.Add(resource, row); }
            row.Q<Label>("amount").text = value;
        }
        public bool HasOpenHeroStats => heroStatsOpen;
        public bool HasOpenDetails => overlay != null && overlay.style.display.value != DisplayStyle.None;
        public void HideDetails() { if(detail!=null)detail.Q(className: "breakdown-rate").style.display=DisplayStyle.None; showingBreakdown = false; ClearSummary();if(runTime!=null)runTime.text="";if(runRatesLeft!=null)runRatesLeft.text="";if(runRatesRight!=null)runRatesRight.text="";SetVisible(overlay, false); detailRows?.Clear(); resourceRows.Clear(); }
        private static void SetVisible(VisualElement element, bool value) { if (element != null) element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; }
        private void OnDisable()
        {
            portraitRun = null;
            ToolkitLocalization.Changed -= LocalizationChanged;
            Blindsided.EventHandler.OnRunStarted -= RunStarted;
            if (resources) resources.OnResourceAdded -= ResourceAdded;
            if (tracker) tracker.OnRunEnded -= RunEnded;
            if (guidedBuffManager) guidedBuffManager.OnBuffCast -= FirstEchoCast;
            guidedBuffManager = null;
            heroStatValues.Clear();
            resources = null; tracker = null; root?.RemoveFromHierarchy(); root = null; slots.Clear(); resourceRows.Clear();
            if (settings) Destroy(settings); settings = null;
        }
    }
}
