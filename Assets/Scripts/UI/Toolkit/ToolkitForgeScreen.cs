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
        [SerializeField] private ToolkitResourceInventoryScreen inventory;
        private ForgeSession session;
        private PanelSettings settings;
        private VisualElement root, main, sidebar, xpFill;
        private Label ivan, xp, pendingStats, equippedStats, maxCrafts, resultTier, totals, weaponChest, helmetBoots, forgeStats, oddsText;
        private Button craft, replace, autoCraft, infoButton;
        private Image resultIcon, costCore, costIngot, craftArrow, stopIcon, lockIcon;
        private Label coreCost, ingotCost;
        private ToolkitForgeOdds odds;
        private ToolkitDisclosure totalsFoldout, statsFoldout;
        private readonly Dictionary<string, (Image icon, VisualElement selected, Label tier)> gearSlots = new();
        private readonly List<(Image icon, VisualElement selected, Label cores, Label crafts)> coreSlots = new();
        private readonly Dictionary<ConversionType, ConversionRow> conversions = new();
        private readonly ForgeStatisticsPresentation statsPresentation = new();
        private bool dirty, showInventory, statisticsDirty;
        private float nextRefresh, nextStatsRefresh;
        private ResourceManager resources;
        private CraftingService crafting;
        private EquipmentController equipment;
        private readonly ToolkitWindowLayout layout = new();
        private Image ivanPortrait;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && definition.catalog && theme && runtimeTheme && textSettings;
        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured) return false;
            if (!session)
            {
                session = gameObject.AddComponent<ForgeSession>();
                session.AutomationStopped += AutomationStopped;
            }
            session.Initialize(definition.catalog);
            if (!inventory) inventory = FindAnyObjectByType<ToolkitResourceInventoryScreen>();
            resources = ResourceManager.Instance; crafting = CraftingService.Instance; equipment = EquipmentController.Instance;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "forge" }; root.AddToClassList("eov-forge"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility=ScrollerVisibility.Hidden, verticalScrollerVisibility=ScrollerVisibility.Auto }; ToolkitGameplay.StyleScroll(mainScroll); main = mainScroll; main.AddToClassList("eov-forge-main"); root.Add(main);
            sidebar = new VisualElement(); sidebar.AddToClassList("eov-forge-sidebar"); root.Add(sidebar);
            BuildHeader(); BuildWork(); BuildConversions(); BuildInfo();
            resources.OnInventoryChanged += MarkDirty; equipment.OnEquipmentChanged += MarkDirty; crafting.OnIvanXpChanged += XpChanged; session.Changed += MarkDirty;
            showInventory = false; statisticsDirty = true; nextRefresh = nextStatsRefresh = 0; Refresh(); Layout();
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
            infoButton = Button(selections, "forge-inventory", "Inventory", ToggleInventory);
            var gear = Row(selections);
            foreach (var entry in definition.gear)
            {
                var column = new VisualElement(); column.AddToClassList("eov-forge-slot-column"); gear.Add(column);
                var button = ToolkitControls.Button("gear-" + entry.slot, () => session.SelectSlot(entry.slot), entry.frame); button.AddToClassList("eov-forge-slot"); column.Add(button);
                var icon = ToolkitControls.Icon(entry.unknown); button.Add(icon);
                var selected = Selection(button); var tier = Label(column, "", ToolkitControls.TextRole.Caption); gearSlots.Add(entry.slot, (icon, selected, tier));
            }
            var coreGrid = new VisualElement(); coreGrid.AddToClassList("eov-forge-core-grid"); selections.Add(coreGrid);
            for (var i = 0; i < definition.catalog.cores.Length; i++)
            {
                var binding = definition.catalog.cores[i];
                var button = ToolkitControls.Button("core-" + binding.core.name, () => session.SelectCore(binding.core), null); button.AddToClassList("eov-forge-core"); coreGrid.Add(button);
                var count = Label(button, "", ToolkitControls.TextRole.Caption);
                var frame = new VisualElement(); frame.AddToClassList("eov-forge-core-frame");  button.Add(frame);
                var icon = ToolkitControls.Icon(definition.coreIcons[i]); frame.Add(icon); var selected = Selection(frame);
                var crafts = Label(button, "", ToolkitControls.TextRole.Caption); coreSlots.Add((icon, selected, crafts, count));
            }
            var actions = new VisualElement(); actions.AddToClassList("eov-forge-actions"); columns.Add(actions);
            craft = Button(actions, "forge-craft", "Craft", session.Craft);
            autoCraft = Button(actions, "forge-auto", "Craft Until Upgrade", session.StartAutoCrafting);
            var toggles = Row(actions); toggles.AddToClassList("forge-toggles"); stopIcon = Toggle(toggles, "forge-stop-vastium", "Stop for Vastium", () => { StopAutocraftOnVastium = !StopAutocraftOnVastium; MarkDirty(); }); lockIcon = Toggle(toggles, "forge-lock-stats", "Stat Matching", () => { LockAutocraftStatSet = !LockAutocraftStatSet; MarkDirty(); });
            var recipe = Row(actions); recipe.style.alignItems = Align.Center;
            odds = new ToolkitForgeOdds { name = "forge-odds" }; recipe.Add(odds);
            oddsText = Label(actions, "", ToolkitControls.TextRole.Caption); oddsText.style.display = DisplayStyle.None;
            odds.RegisterCallback<PointerEnterEvent>(_ => oddsText.style.display = DisplayStyle.Flex); odds.RegisterCallback<PointerLeaveEvent>(_ => oddsText.style.display = DisplayStyle.None);
            odds.RegisterCallback<ClickEvent>(_ => oddsText.style.display = oddsText.resolvedStyle.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None);
            var costs = new VisualElement(); recipe.Add(costs); var first = Row(costs); costCore = ToolkitControls.Icon(null); first.Add(costCore); coreCost = Label(first, "1"); var second = Row(costs); costIngot = ToolkitControls.Icon(null); second.Add(costIngot); ingotCost = Label(second, "");
            craftArrow = ToolkitControls.Icon(null); recipe.Add(craftArrow);
            var result = new VisualElement(); recipe.Add(result); resultIcon = ToolkitControls.Icon(null); resultIcon.style.width = resultIcon.style.height = 24; result.Add(resultIcon); resultTier = Label(result, "", ToolkitControls.TextRole.Caption);
            maxCrafts = Label(actions, "", ToolkitControls.TextRole.Caption);
            var results = new VisualElement(); results.AddToClassList("eov-forge-results"); columns.Add(results);
            replace = Button(results, "forge-replace", "Replace", session.Replace);
            var comparison = Row(results); comparison.AddToClassList("eov-forge-comparison");
            pendingStats = StatColumn(comparison, "Pending");
            var comparisonArrow = ToolkitControls.Icon(definition.arrowValid); comparisonArrow.AddToClassList("eov-forge-comparison-arrow"); comparison.Add(comparisonArrow);
            equippedStats = StatColumn(comparison, "Equipped");
        }
        private void BuildInfo()
        {
            var heading = new VisualElement(); heading.AddToClassList("eov-forge-information-title");  Label(heading, "<b><smallcaps>Forge Information</smallcaps></b>", ToolkitControls.TextRole.Heading); sidebar.Add(heading);
            var scroll = ToolkitControls.RecessedScroll(sidebar, "forge-info", theme, definition.inset);
            totalsFoldout = new ToolkitDisclosure("Equipment Totals", theme); scroll.Add(totalsFoldout);
            var line = Row(totalsFoldout.Content); totals = Label(line, ""); weaponChest = Label(line, ""); helmetBoots = Label(line, ""); foreach (var label in new[] { totals, weaponChest, helmetBoots }) { label.style.flexGrow = 1; label.style.flexBasis = 0; label.style.fontSize = 6; }
            statsFoldout = new ToolkitDisclosure("Stats", theme); scroll.Add(statsFoldout); forgeStats = Label(statsFoldout.Content, "");
            statsFoldout.Changed += _ => { nextStatsRefresh = 0; MarkDirty(); }; totalsFoldout.Changed += _ => MarkDirty();
        }
        private Label StatColumn(VisualElement parent, string title)
        {
            var column = new VisualElement(); column.AddToClassList("eov-forge-stat-column"); column.style.flexGrow = 1; column.style.flexBasis = 0; column.style.minWidth = 0; parent.Add(column);
            Label(column, title, ToolkitControls.TextRole.Body).AddToClassList("eov-forge-stat-title"); var text = Label(column, "");  text.style.paddingTop = text.style.paddingBottom = text.style.paddingLeft = text.style.paddingRight = 3; text.style.flexGrow = 1; text.style.minHeight = 80; return text;
        }
        private VisualElement Panel(VisualElement parent, string className) { var panel = new VisualElement(); panel.AddToClassList(className); panel.AddToClassList("eov-forge-panel");  parent.Add(panel); return panel; }
        private static VisualElement Row(VisualElement parent) { var row = new VisualElement(); row.AddToClassList("eov-forge-row"); parent.Add(row); return row; }
        private static Label Label(VisualElement parent, string text, ToolkitControls.TextRole role = ToolkitControls.TextRole.Body) { var label = ToolkitControls.Text(text, role); parent.Add(label); return label; }
        private Button Button(VisualElement parent, string name, string title, Action action) { var button = ToolkitControls.Button(name, action, definition.button); button.AddToClassList("eov-forge-button"); button.text = title; parent.Add(button); return button; }
        private VisualElement Selection(VisualElement parent) { var selected = new VisualElement { pickingMode = PickingMode.Ignore }; selected.AddToClassList("eov-forge-selection"); selected.AddToClassList("selection-outline"); parent.Add(selected); return selected; }
        private Image Toggle(VisualElement parent, string name, string title, Action action) { var button = Button(parent, name, "", action); button.AddToClassList("eov-forge-toggle"); var image = ToolkitControls.Icon(null); image.style.width = 24; image.style.height = 12; button.Add(image); Label(button, title, ToolkitControls.TextRole.Caption); return image; }
        private void MarkDirty() { dirty = true; statisticsDirty = true; }
        private void XpChanged(int _, float __, float ___) => MarkDirty();
        private void AutomationStopped() { FindAnyObjectByType<TaskbarFlasher>()?.FlashNow(); if (!IsOpen) TownWindowManager.ShowForgeAttention(); }
        private void Update() { if (!IsOpen) return; var frame = definition.portraitAnimation?.At(Time.time); if (frame && ivanPortrait != null) ivanPortrait.sprite = frame; Layout(); if (dirty && Time.unscaledTime >= nextRefresh) Refresh(); RefreshStatistics(); }
        private void Layout()
        {
            layout.Centered(root, theme);root.EnableInClassList("compact-width",ToolkitWindowLayout.SafeArea.width<650);
            if (showInventory && inventory && sidebar != null) inventory.Bounds = sidebar.worldBound;
        }
        private void ToggleInventory()
        {
            if (!inventory) return;
            showInventory = !showInventory; infoButton.text = showInventory ? "Info" : "Inventory";
            sidebar.style.visibility = showInventory ? Visibility.Hidden : Visibility.Visible;
            inventory.ManualLayout = showInventory;
            if (showInventory) { inventory.Show(); inventory.Bounds = sidebar.worldBound; } else { inventory.Hide(); nextStatsRefresh = 0; MarkDirty(); }
        }
        public void Hide()
        {
            if (resources) resources.OnInventoryChanged -= MarkDirty;
            if (equipment) equipment.OnEquipmentChanged -= MarkDirty;
            if (crafting) crafting.OnIvanXpChanged -= XpChanged;
            if (session) session.Changed -= MarkDirty;
            if (showInventory && inventory) { inventory.Hide(); inventory.ManualLayout = false; }
            showInventory = false; root?.RemoveFromHierarchy(); root = null; gearSlots.Clear(); coreSlots.Clear(); conversions.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (session) session.AutomationStopped -= AutomationStopped; if (settings) Destroy(settings); }
    }
}
