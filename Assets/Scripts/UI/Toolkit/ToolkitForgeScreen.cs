using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Gear.UI;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;
namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class ToolkitForgeScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitForgeDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private ForgeSession session;
        private PanelSettings settings;
        private VisualElement root, main, xpFill;
        private Label ivan, xp, maxCrafts, resultTier;
        private Button craft, replace;
        private VisualElement craftArea, craftRow, craftRecipe;
        private bool craftStacked;
        private static readonly CustomStyleProperty<float> CraftInlineGap = new("--forge-inline-gap");
        private bool repeatCraft;
        private VisualElement automationControls;
        private Label automationLock;
        private Image resultIcon, costCore, costIngot, craftArrow, stopIcon, lockIcon, repeatIcon, neverStopIcon;
        private VisualElement lockedCore;
        private Image lockedCoreIcon;
        private Label coreCost, ingotCost;
        private ToolkitForgeOdds odds;
        private readonly Dictionary<string, (Image icon, VisualElement selected, Label tier)> gearSlots = new();
        private readonly List<(Image icon, VisualElement selected, Label cores, Label crafts, Button button)> coreSlots = new();
        private readonly Dictionary<ConversionType, ConversionRow> conversions = new();
        private bool dirty;
        private float nextRefresh;
        private ResourceManager resources;
        private CraftingService crafting;
        private EquipmentController equipment;
        private readonly ToolkitWindowLayout layout = new();
        private Image ivanPortrait;
        private VisualElement comparisonRows;
        private ToolkitPortraitPages portraitPages;
        private GearItem portraitPending;
        private VisualElement rarityOddsSection;

        public bool IsOpen => root != null;
        public bool IsConfigured => definition && definition.catalog && theme && runtimeTheme && textSettings;
        public bool Show()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
            if (IsOpen) return true;
            if (!IsConfigured) return false;
            if (!session)
            {
                session = gameObject.AddComponent<ForgeSession>();
                session.AutomationStopped += AutomationStopped;
            }
            session.Initialize(definition.catalog);
            resources = ResourceManager.Instance; crafting = CraftingService.Instance; equipment = EquipmentController.Instance;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "forge" }; root.AddToClassList("eov-forge"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility=ScrollerVisibility.Hidden, verticalScrollerVisibility=ScrollerVisibility.Auto }; ToolkitGameplay.StyleScroll(mainScroll); main = mainScroll; main.AddToClassList("eov-forge-main"); root.Add(main);
            mainScroll.contentContainer.style.flexGrow = 1;
            mainScroll.contentViewport.RegisterCallback<GeometryChangedEvent>(e => mainScroll.contentContainer.style.minHeight = e.newRect.height);
            BuildHeader(); BuildWork(); BuildConversions(); BuildFirstLesson();
            var content = mainScroll.contentContainer;
            portraitPages = new ToolkitPortraitPages(content, null, new[] { root.Q(className: "eov-forge-work") }, mainScroll,
                new ToolkitPortraitPages.Page("forge.craft", "Craft", root.Q(className: "eov-forge-selections"), root.Q(className: "eov-forge-actions"), root.Q(className: "forge-comparison-section")),
                new ToolkitPortraitPages.Page("forge.materials", "Materials", root.Q(className: "eov-forge-conversions"), rarityOddsSection));
            ToolkitLocalization.Changed += LocalizationChanged;
            resources.OnInventoryChanged += MarkDirty; equipment.OnEquipmentChanged += MarkDirty; crafting.OnIvanXpChanged += XpChanged; session.Changed += MarkDirty;
            nextRefresh = 0; comparisonSignature = null; Refresh(); Layout();
            return true;
        }
        private void BuildHeader()
        {
            var header = Panel(main, "eov-forge-header");
            var portraitFrame = new VisualElement(); portraitFrame.AddToClassList("eov-forge-portrait");  header.Add(portraitFrame);
            ivanPortrait = ToolkitControls.Icon(definition.portrait); ivanPortrait.style.width = ivanPortrait.style.height = 24; portraitFrame.Add(ivanPortrait);
            var text = new VisualElement(); text.style.flexGrow = 1; header.Add(text);
            var line = Row(text); ivan = Label(line, "", ToolkitControls.TextRole.Heading); ivan.style.flexGrow = 1; xp = Label(line, "");
            var track = new VisualElement(); track.AddToClassList("eov-forge-xp"); track.AddToClassList("track"); text.Add(track);
            xpFill = new VisualElement(); xpFill.style.height = Length.Percent(100); xpFill.AddToClassList("fill"); track.Add(xpFill);
        }
        private void BuildWork()
        {
            var work = Panel(main, "eov-forge-work");
            var columns = Row(work); columns.AddToClassList("eov-forge-columns");
            var selections = new VisualElement(); selections.AddToClassList("eov-forge-selections"); columns.Add(selections);
            var equipmentSelection = new VisualElement(); equipmentSelection.AddToClassList("forge-equipment-selector"); selections.Add(equipmentSelection);
            LocalizedLabel(equipmentSelection, "forge.equipment", "Equipment", ToolkitControls.TextRole.Heading);
            var gear = Row(equipmentSelection); gear.AddToClassList("forge-equipment-grid");
            foreach (var entry in definition.gear)
            {
                var column = new VisualElement(); column.AddToClassList("eov-forge-slot-column"); gear.Add(column);
                var button = ToolkitControls.Button("gear-" + entry.slot, () => session.SelectSlot(entry.slot), entry.frame); button.AddToClassList("eov-forge-slot"); column.Add(button);
                var icon = FramedIcon(button, entry.unknown);
                var selected = Selection(button); var tier = Label(column, "", ToolkitControls.TextRole.Caption); gearSlots.Add(entry.slot, (icon, selected, tier));
            }
            var coreSelection = new VisualElement(); coreSelection.AddToClassList("forge-core-selector"); selections.Add(coreSelection);
            LocalizedLabel(coreSelection, "forge.cores", "Cores", ToolkitControls.TextRole.Heading);
            var coreGrid = new VisualElement(); coreGrid.AddToClassList("eov-forge-core-grid"); coreSelection.Add(coreGrid);
            for (var i = 0; i < definition.catalog.cores.Length; i++)
            {
                var binding = definition.catalog.cores[i];
                var button = ToolkitControls.Button("core-" + binding.core.name, () => session.SelectCore(binding.core), null); button.AddToClassList("eov-forge-core"); coreGrid.Add(button);
                var count = Label(button, "", ToolkitControls.TextRole.Caption); ToolkitLocalization.BindTooltip(count, "forge.available-crafts", "Available crafts"); count.style.display = DisplayStyle.None;
                var frame = new VisualElement(); frame.AddToClassList("eov-forge-core-frame");  button.Add(frame);
                var icon = FramedIcon(frame, definition.coreIcons[i]); var selected = Selection(frame);
                var crafts = Label(button, "", ToolkitControls.TextRole.Caption); ToolkitLocalization.BindTooltip(crafts, "forge.owned-cores", "Owned cores"); coreSlots.Add((icon, selected, crafts, count, button));
            }
            var locked = ToolkitControls.Button("forge-locked-cores", () => { }, null); locked.AddToClassList("eov-forge-core"); locked.SetEnabled(false); coreGrid.Add(locked); lockedCore = locked;
            ToolkitLocalization.BindTooltip(locked, "forge.more-cores", "Discover more cores as your Combat level increases.");
            var lockedFrame = new VisualElement(); lockedFrame.AddToClassList("eov-forge-core-frame"); locked.Add(lockedFrame); lockedCoreIcon = FramedIcon(lockedFrame, null);
            Label(locked, "—", ToolkitControls.TextRole.Caption).name = "forge-locked-core-count";
            var actions = new VisualElement(); actions.AddToClassList("eov-forge-actions"); columns.Add(actions);
            craftArea = new VisualElement(); craftArea.AddToClassList("forge-craft-area"); actions.Add(craftArea);
            craftRow = Row(craftArea); craftRow.AddToClassList("forge-craft-controls");
            craft = LocalizedButton(craftRow, "forge-craft", "forge.craft", "Craft", CraftSelected);
            var recipe = Row(craftRow); recipe.style.alignItems = Align.Center; recipe.AddToClassList("forge-craft-recipe");
            craftRecipe = recipe; craftStacked = false;
            var costs = new VisualElement(); costs.AddToClassList("forge-recipe-costs"); recipe.Add(costs); var first = Row(costs); first.AddToClassList("forge-ingredient"); costCore = ToolkitControls.Icon(null); first.Add(costCore); coreCost = Label(first, "1"); var second = Row(costs); second.AddToClassList("forge-ingredient"); costIngot = ToolkitControls.Icon(null); second.Add(costIngot); ingotCost = Label(second, "");
            craftArrow = ToolkitControls.Icon(null); recipe.Add(craftArrow);
            var result = new VisualElement(); result.AddToClassList("forge-recipe-result"); recipe.Add(result); resultIcon = ToolkitControls.Icon(null); resultIcon.style.width = resultIcon.style.height = 24; result.Add(resultIcon); resultTier = Label(result, "", ToolkitControls.TextRole.Caption);
            replace = LocalizedButton(craftRow, "forge-replace", "forge.equip", "Equip", session.Replace);
            maxCrafts = Label(actions, "", ToolkitControls.TextRole.Caption);
            var automationArea = new VisualElement { name = "forge-automation-area" }; actions.Add(automationArea);
            automationControls = new VisualElement(); automationArea.Add(automationControls);
            var toggles = Row(automationControls); toggles.AddToClassList("forge-toggles"); stopIcon = Toggle(toggles, "forge-stop-vastium", "forge.stop-vastium", "Stop for Vathrium", () => { StopAutocraftOnVastium = !StopAutocraftOnVastium; MarkDirty(); }); lockIcon = Toggle(toggles, "forge-lock-stats", "forge.stat-matching", "Stat Matching", () => { LockAutocraftStatSet = !LockAutocraftStatSet; MarkDirty(); });
            repeatIcon = Toggle(toggles, "forge-repeat-craft", "forge.repeat-craft", "Repeat craft", () => { repeatCraft = !repeatCraft; if (!repeatCraft) session.StopAutoCrafting(); MarkDirty(); });
            ToolkitLocalization.BindTooltip(repeatIcon.parent, "forge.repeat-craft-help", "Repeat crafting until an upgrade or another stop condition is reached.");
            neverStopIcon = Toggle(toggles, "forge-never-stop", "forge.never-stop", "Never stop", () => { NeverStopAutocraft = !NeverStopAutocraft; MarkDirty(); });
            ToolkitLocalization.BindTooltip(neverStopIcon.parent, "forge.never-stop-help", "Keep crafting and salvaging until stopped or out of materials, including upgrades and Vathrium.");
            automationLock = Label(automationArea, ""); automationLock.name = "forge-automation-locked";
            automationLock.style.position = Position.Absolute;
            automationLock.style.left = automationLock.style.right = automationLock.style.top = automationLock.style.bottom = 0;
            automationLock.style.unityTextAlign = TextAnchor.MiddleCenter;
            automationLock.style.whiteSpace = WhiteSpace.Normal;
            rarityOddsSection = new VisualElement(); rarityOddsSection.AddToClassList("forge-rarity-section"); actions.Add(rarityOddsSection);
            var oddsSummary = Row(rarityOddsSection); oddsSummary.AddToClassList("forge-odds-summary");
            odds = new ToolkitForgeOdds { name = "forge-odds", pickingMode = PickingMode.Ignore }; oddsSummary.Add(odds);
            BuildOddsLegend(oddsSummary);
            var results = new VisualElement(); results.AddToClassList("forge-comparison-section"); columns.Add(results);
            comparisonRows = new VisualElement(); results.Add(comparisonRows);
        }
        private void CraftSelected()
        {
            if (session.IsAutoCrafting) session.StopAutoCrafting();
            else if ((repeatCraft || NeverStopAutocraft) && session.AutomationUnlocked) session.StartAutoCrafting();
            else session.Craft();
        }
        private VisualElement Panel(VisualElement parent, string className) { var panel = new VisualElement(); panel.AddToClassList(className); panel.AddToClassList("eov-forge-panel");  parent.Add(panel); return panel; }
        private static VisualElement Row(VisualElement parent) { var row = new VisualElement(); row.AddToClassList("eov-forge-row"); parent.Add(row); return row; }
        private static Label Label(VisualElement parent, string text, ToolkitControls.TextRole role = ToolkitControls.TextRole.Body) { var label = ToolkitControls.Text(text, role); parent.Add(label); return label; }
        private Button Button(VisualElement parent, string name, string title, Action action) { var button = ToolkitControls.Button(name, action, definition.button); button.AddToClassList("eov-forge-button"); button.text = title; parent.Add(button); return button; }
        private VisualElement Selection(VisualElement parent) { var selected = new VisualElement { pickingMode = PickingMode.Ignore }; selected.AddToClassList("eov-forge-selection"); selected.AddToClassList("selection-outline"); parent.Add(selected); return selected; }
        private Image Toggle(VisualElement parent, string name, string key, string title, Action action) { var button = Button(parent, name, "", action); button.AddToClassList("eov-forge-toggle"); var image = ToolkitControls.Icon(null); image.style.width = 24; image.style.height = 12; button.Add(image); LocalizedLabel(button, key, title, ToolkitControls.TextRole.Caption); return image; }
        private static Label LocalizedLabel(VisualElement parent, string key, string english, ToolkitControls.TextRole role = ToolkitControls.TextRole.Body)
        { var label = Label(parent, "", role); ToolkitLocalization.Bind(label, key, english); return label; }
        private Button LocalizedButton(VisualElement parent, string name, string key, string english, Action action)
        { var button = Button(parent, name, "", action); ToolkitLocalization.Bind(button, key, english); return button; }
        private void LocalizationChanged()
        { comparisonSignature = null; comparisonSlot = null; nextRefresh = 0; MarkDirty(); }
        private void MarkDirty() { dirty = true; }
        private void XpChanged(int _, float __, float ___) => MarkDirty();
        private void AutomationStopped() { FindAnyObjectByType<TaskbarFlasher>()?.FlashNow(); if (!IsOpen) TownWindowManager.ShowForgeAttention(); }
        private void Update() { if (!IsOpen) return; var frame = definition.portraitAnimation?.At(Time.time); if (frame && ivanPortrait != null) ivanPortrait.sprite = frame; Layout(); if (dirty && Time.unscaledTime >= nextRefresh) Refresh(); PositionFirstLessonHand(); }
        private void Layout()
        {
            var area = ToolkitWindowLayout.SafeArea;
            var width = Mathf.Min(744, area.width - 24);
            layout.Apply(root, new Rect(area.center.x-width/2, area.y+ToolkitResponsiveLayout.WindowTop, width, Mathf.Max(0, area.height-ToolkitResponsiveLayout.WindowTop-ToolkitResponsiveLayout.WindowBottom)));
            root.EnableInClassList("forge-narrow", width < 620);
            root.EnableInClassList("forge-compact-landscape", width < 620 && !ToolkitResponsiveLayout.IsPortrait);
            RefreshPortraitForge();
            LayoutCraftControls();
            LayoutConversions();
        }
        private void LayoutCraftControls()
        {
            if (craftArea == null || craftArea.contentRect.width <= 0 || craft.resolvedStyle.width <= 0) return;
            float TextWidth(Label label, string text)
            {
                var width = label.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                return float.IsFinite(width) ? width : 0;
            }
            float IconWidth(Image icon) => icon.resolvedStyle.width + icon.resolvedStyle.marginLeft + icon.resolvedStyle.marginRight;
            var costsWidth = Mathf.Max(IconWidth(costCore) + TextWidth(coreCost, coreCost.text),
                IconWidth(costIngot) + TextWidth(ingotCost, ingotCost.text));
            var tierWidth = Mathf.Max(TextWidth(resultTier, resultTier.text),
                TextWidth(resultTier, ToolkitLocalization.Text("inventory.tier", "Tier {0}", 8)));
            var recipeWidth = Mathf.Ceil(costsWidth + IconWidth(craftArrow) + Mathf.Max(IconWidth(resultIcon), tierWidth));
            if (!float.IsFinite(recipeWidth)) return;
            craftRecipe.style.minWidth = recipeWidth;
            // Both actions use the same width. Reserve Equip even while hidden so
            // crafting an item cannot squeeze the recipe into a different layout.
            var buttonWidth = craft.resolvedStyle.flexBasis.value;
            // Measure the inline spacing even when the stacked layout uses tighter gaps.
            var margins = (craftArea.customStyle.TryGetValue(CraftInlineGap, out var gap) ? gap : 7) * 2;
            var requiredWidth = buttonWidth * 2 + margins + recipeWidth;
            var availableWidth = craftArea.contentRect.width;
            var stacked = craftStacked ? availableWidth < requiredWidth + .5f : availableWidth < requiredWidth;
            if (stacked == craftStacked) return;
            craftStacked = stacked;
            craftArea.EnableInClassList("forge-craft-stacked", stacked);
            craftArea.parent.EnableInClassList("forge-actions-stacked", stacked);
            if (stacked) craftArea.Insert(0, craftRecipe);
            else craftRow.Insert(1, craftRecipe);
        }
        private void RefreshPortraitForge()
        {
            portraitPages?.Update();
            if (!ToolkitResponsiveLayout.IsPortrait) return;
            if (!session.IsAutoCrafting && session.Pending != null && portraitPending == null) portraitPages.Select(0);
            portraitPending = session.Pending;
            if (TimelessEchoes.Gear.ForgeFirstLesson.Active)
                portraitPages.Select(TimelessEchoes.Gear.ForgeFirstLesson.Stage == 0 ? 1 : 0);
        }
        public void Hide()
        {
            ToolkitLocalization.Changed -= LocalizationChanged;
            if (resources) resources.OnInventoryChanged -= MarkDirty;
            if (equipment) equipment.OnEquipmentChanged -= MarkDirty;
            if (crafting) crafting.OnIvanXpChanged -= XpChanged;
            if (session) session.Changed -= MarkDirty;
            oddsRows = null; oddsLabels.Clear(); root?.RemoveFromHierarchy(); root = null; gearSlots.Clear(); coreSlots.Clear(); conversions.Clear();
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref settings);
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (session) session.AutomationStopped -= AutomationStopped; if (settings) Destroy(settings); }
    }
}
