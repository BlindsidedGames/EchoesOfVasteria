using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TimelessEchoes.Farming;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Committed Fields state, presented in stable controls so a click never destroys its own target.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitFarmScreen : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sourceBehaviour;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private Sprite knownPack, unknownPack, cropIcon;
        private IFarmPresentationSource source;
        private PanelSettings settings;
        private VisualElement root, columns, seedColumn, plotsColumn, plotSections, bedList, orchardList, roadmap, xpBar;
        private ScrollView workspace, seedScroll;
        private Label heading, xp, error, selection, status, detail;
        private Button plant, harvest, water, repeat;
        private Foldout construction;
        private readonly ToolkitWindowLayout layout = new();
        private readonly Dictionary<string, SeedCard> seeds = new();
        private readonly Dictionary<string, BedTile> beds = new();
        private readonly Dictionary<string, BuildRow> builds = new();
        private FieldsWorldAppearance appearance;
        private string selectedRecipe, selectedBed;
        private float nextRefresh;
        private bool? compactLayout;
        private sealed class SeedCard { public Button Button; public Label Title, Count; public Image Icon; }
        private sealed class BedTile { public Button Button; public Label Title, Status; public VisualElement Fill, Art; public bool Orchard; public Image[] Soil, Plants; }
        private sealed class BuildRow { public Label Title, Status; public Button HandIn; }
        public float CompanionWidth { get; set; }
        public bool IsOpen => root != null;
        public bool IsConfigured => theme && runtimeTheme && textSettings && (source != null || sourceBehaviour is IFarmPresentationSource);

        public void Configure(IFarmPresentationSource presentationSource, ToolkitTheme toolkitTheme,
            ThemeStyleSheet stylesheet, PanelTextSettings panelTextSettings, Sprite discoveredPack, Sprite undiscoveredPack)
        {
            Hide(); source = presentationSource; sourceBehaviour = presentationSource as MonoBehaviour;
            theme = toolkitTheme; runtimeTheme = stylesheet; textSettings = panelTextSettings;
            knownPack = discoveredPack; unknownPack = undiscoveredPack;
        }
        public bool Show()
        {
            if (IsOpen) return true;
            source ??= sourceBehaviour as IFarmPresentationSource;
            if (!IsConfigured || !source.Ready) return false;
            appearance = Resources.Load<FieldsWorldAppearance>("Farming/FieldsWorldAppearance");
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "farm" }; theme.Apply(root); ToolkitGameplay.Apply(root, theme);
            root.AddToClassList("menu-surface"); root.AddToClassList("fields-ui"); document.rootVisualElement.Add(root);
            var header = ToolkitGameplay.E(root, "row between fields-header");
            heading = ToolkitGameplay.L(header, "", "quests-heading"); xp = ToolkitGameplay.L(header, "", "muted");
            xpBar = ToolkitGameplay.Bar(root); xpBar.parent.style.height = 4; xpBar.parent.style.marginBottom = 5;
            status = ToolkitGameplay.L(root, "", "muted fields-rates"); error = ToolkitGameplay.L(root, "", "status");
            workspace = ToolkitControls.RecessedScroll(root, "fields-workspace", theme, compact: true);
            workspace.parent.style.flexGrow = 1; workspace.parent.style.minHeight = 0;
            workspace.parent.style.paddingLeft = workspace.parent.style.paddingRight = 0;
            workspace.parent.style.paddingTop = workspace.parent.style.paddingBottom = 0;
            workspace.style.flexGrow = 1; workspace.style.minHeight = 0;
            columns = ToolkitGameplay.E(workspace, "row"); columns.style.minHeight = 0;
            seedColumn = ToolkitGameplay.E(columns, "column"); seedColumn.style.flexShrink = 0;
            LocalizedLabel(seedColumn, "fields.seeds-title", "Seeds & saplings", "heading");
            seedScroll = ToolkitControls.RecessedScroll(seedColumn, "fields-seeds", theme, compact: true);
            seedScroll.parent.style.paddingLeft = seedScroll.parent.style.paddingRight = 0;
            seedScroll.parent.style.paddingTop = seedScroll.parent.style.paddingBottom = 0;
            var seedGrid = ToolkitGameplay.E(seedScroll, "row fields-seed-grid"); seedGrid.style.flexWrap = Wrap.Wrap;
            plotsColumn = ToolkitGameplay.E(columns, "column grow");
            plotSections = ToolkitGameplay.E(plotsColumn, "row");
            var fieldsSection = ToolkitGameplay.E(plotSections, "column grow"); fieldsSection.style.flexBasis = 0;
            LocalizedLabel(fieldsSection, "farm.display-name", "Fields", "heading");
            bedList = ToolkitGameplay.E(fieldsSection, "row"); bedList.name = "fields-bed-grid"; bedList.style.flexWrap = Wrap.Wrap;
            var orchardSection = ToolkitGameplay.E(plotSections, "column grow"); orchardSection.style.flexBasis = 0; orchardSection.style.marginLeft = 6;
            LocalizedLabel(orchardSection, "fields.orchard-title", "Orchard plots", "heading");
            orchardList = ToolkitGameplay.E(orchardSection, "row"); orchardList.name = "fields-orchard-grid"; orchardList.style.flexWrap = Wrap.Wrap;
            var snapshot = source.CapturePresentation();
            foreach (var recipe in snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>())
            {
                if (recipe == null) continue;
                var id = recipe.Id;
                var button = Button(seedGrid, "", () => SelectRecipe(id)); button.name = "fields-seed-" + id;
                button.AddToClassList("fields-seed-card");
                var icon = ToolkitGameplay.Icon(button, null, 22); icon.style.marginRight = 0;
                var title = ToolkitGameplay.L(button, "", "fields-seed-caption"); title.name = "seed-caption";
                var count = ToolkitGameplay.L(button, "", "fields-seed-count"); count.name = "seed-quantity";
                seeds[id] = new SeedCard { Button = button, Title = title, Icon = icon, Count = count };
            }
            foreach (var bed in snapshot.Beds ?? Array.Empty<FarmBedPresentation>())
                if (bed != null) CreateBed(bed);
            var actionArea = ToolkitGameplay.E(plotsColumn, "column fields-action-area");
            var selectionStrip = ToolkitGameplay.E(actionArea, "row between fields-selection-strip");
            selectionStrip.style.flexWrap = Wrap.Wrap;
            selection = ToolkitGameplay.L(selectionStrip, "", "fields-selection"); selection.name = "fields-seed-selection";
            selection.style.flexGrow = 1;
            detail = ToolkitGameplay.L(selectionStrip, "", "muted fields-selection-detail"); detail.name = "fields-selected-status";
            var actions = ToolkitGameplay.E(actionArea, "row fields-actions"); actions.style.flexWrap = Wrap.Wrap;
            plant = Button(actions, "Plant", PlantSelected); plant.name = "fields-plant-selected"; plant.AddToClassList("primary");
            harvest = Button(actions, "Harvest ready", () => Execute(source.HarvestReady)); harvest.name = "farm-harvest-ready";
            water = Button(actions, "Water once", () => { if (source is FarmService service) Execute(() => service.Water(selectedBed)); }); water.name = "fields-water-selected";
            repeat = Button(actions, "Repeat: off", () => {
                var bed = source.CapturePresentation()?.Beds?.FirstOrDefault(b => b?.Id == selectedBed);
                if (source is FarmService service && bed != null) Execute(() => service.SetRepeat(bed.Id, !bed.Repeat));
            }); repeat.name = "fields-repeat-selected";
            LocalizedLabel(actionArea, "fields.harvest-help", "{0} Twins XP per harvest. Growth while playing.", "muted fields-harvest-help", (source as FarmService)?.Content.harvestXp ?? 10);
            construction = new Foldout { name = "fields-construction-foldout", value = false };
            plotsColumn.Add(construction); roadmap = ToolkitGameplay.E(construction, "column");
            foreach (var build in snapshot.Builds ?? Array.Empty<FarmBuildPresentation>())
            {
                if (build == null) continue;
                var row = ToolkitGameplay.E(roadmap, "row between fields-build-row"); var text = ToolkitGameplay.E(row, "grow");
                var title = ToolkitGameplay.L(text, "", "heading"); var description = ToolkitGameplay.L(text, "", "muted");
                var id = build.QuestId; var button = Button(row, "Hand in", () => { QuestManager.Instance?.TryTurnInQuest(id); Refresh(); });
                builds[id] = new BuildRow { Title = title, Status = description, HandIn = button };
            }
            source.Changed += Refresh; ToolkitLocalization.Changed += Refresh;
            layout.Fill(root, theme, CompanionWidth); ApplyLayout(); Refresh(); return true;
        }
        private static Label LocalizedLabel(VisualElement parent, string key, string english, string classes, params object[] args)
        { var label = ToolkitGameplay.L(parent, english, classes); ToolkitLocalization.Bind(label, key, english, args); return label; }
        private static Button Button(VisualElement parent, string text, Action action)
        {
            var button = ToolkitControls.Button("fields-" + text.Replace(' ', '-').ToLowerInvariant(), action, null);
            if (!string.IsNullOrEmpty(text)) ToolkitLocalization.Bind(button, "fields.action." + text.Replace(' ', '-').Replace(":", "").ToLowerInvariant(), text);
            parent.Add(button); return button;
        }
        private void CreateBed(FarmBedPresentation bed)
        {
            var id = bed.Id; var grid = bed.Orchard ? orchardList : bedList;
            var rowEnd = grid.childCount % 3 == 2;
            var button = Button(grid, "", () => { selectedBed = id; Refresh(); });
            button.name = "fields-select-" + id; button.AddToClassList("fields-plot");
            button.EnableInClassList("fields-row-end", rowEnd);
            var title = ToolkitGameplay.L(button, bed.Title, "fields-plot-title");
            var art = ToolkitGameplay.E(button, "row fields-plot-art"); art.name = "bed-art"; art.style.width = 36; art.style.height = 36;
            art.style.flexWrap = Wrap.Wrap; art.style.flexShrink = 0;
            var soil = new Image[bed.Orchard ? 1 : 9]; var plants = new Image[soil.Length];
            for (var i = 0; i < soil.Length; i++)
            {
                var spot = new VisualElement { pickingMode = PickingMode.Ignore };
                spot.style.width = bed.Orchard ? 36 : 12; spot.style.height = bed.Orchard ? 36 : 12; art.Add(spot);
                soil[i] = new Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
                soil[i].style.position = Position.Absolute; soil[i].style.width = bed.Orchard ? 36 : 12;
                soil[i].style.height = bed.Orchard ? 12 : 12; soil[i].style.bottom = 0; spot.Add(soil[i]);
                plants[i] = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                plants[i].style.position = Position.Absolute; plants[i].style.width = bed.Orchard ? 36 : 12;
                plants[i].style.height = bed.Orchard ? 36 : 12; plants[i].style.bottom = 0; spot.Add(plants[i]);
            }
            var status = ToolkitGameplay.L(button, "", "fields-plot-status"); status.name = "bed-status";
            var fill = ToolkitGameplay.Bar(button); fill.name = "bed-progress"; fill.parent.style.width = Length.Percent(100);
            beds[id] = new BedTile { Button = button, Title = title, Status = status, Fill = fill, Art = art, Orchard = bed.Orchard, Soil = soil, Plants = plants };
        }
        private void SelectRecipe(string id)
        {
            selectedRecipe = id;
            var snapshot = source.CapturePresentation();
            var recipe = snapshot?.Recipes?.FirstOrDefault(r => r?.Id == id);
            var target = snapshot?.Beds?.FirstOrDefault(b => b?.Id == selectedBed);
            if (recipe != null && (target == null || !target.Unlocked || target.Planted || target.Orchard != recipe.Orchard))
                selectedBed = snapshot.Beds.FirstOrDefault(b => b?.Unlocked == true && !b.Planted && b.Orchard == recipe.Orchard)?.Id;
            Refresh();
        }
        private void Execute(Func<bool> command) { if (source?.Ready == true) { command(); Refresh(); } }
        private void PlantSelected()
        {
            if (plant.enabledInHierarchy && source is FarmService service && selectedBed != null && selectedRecipe != null)
                Execute(() => service.Plant(selectedBed, selectedRecipe));
        }
        public void Refresh()
        {
            if (!IsOpen || source == null) return;
            var snapshot = source.Ready ? source.CapturePresentation() : null;
            error.text = source.LastError ?? ""; error.style.display = error.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (snapshot == null) { plant.SetEnabled(false); harvest.SetEnabled(false); water.SetEnabled(false); repeat.SetEnabled(false); return; }
            heading.text = ToolkitLocalization.Text("fields.heading", "{0} · Flora & Tillman · Level {1}", snapshot.DisplayName, snapshot.TwinsLevel);
            xp.text = ToolkitLocalization.Text("fields.xp", "{0} / {1} XP", snapshot.TwinsXp, snapshot.TwinsXpRequired);
            xpBar.style.width = Length.Percent(Mathf.Clamp01((float)snapshot.TwinsXp / Math.Max(1, snapshot.TwinsXpRequired)) * 100);
            status.text = snapshot.TownActionsAllowed ? ToolkitLocalization.Text("fields.rates", "Yield ×{0:0.00} · Growth time ×{1:0.0}", snapshot.YieldMultiplier, snapshot.SpeedFactor) : ToolkitLocalization.Text("fields.return-town", "Return to town to tend Fields.");
            construction.text = ToolkitLocalization.Text("fields.construction-title", "Construction");
            foreach (var recipe in snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>())
            {
                if (recipe == null || !seeds.TryGetValue(recipe.Id, out var card)) continue;
                card.Icon.sprite = recipe.Discovered ? recipe.Icon : recipe.UnknownIcon ? recipe.UnknownIcon : unknownPack;
                card.Title.text = recipe.Discovered ? recipe.Title : "???";
                card.Count.text = recipe.Discovered ? recipe.SeedQuantity.ToString("0.##", CultureInfo.InvariantCulture) : "—";
                card.Button.EnableInClassList("primary", selectedRecipe == recipe.Id);
                card.Button.EnableInClassList("fields-undiscovered", !recipe.Discovered);
                card.Button.tooltip = recipe.Discovered ? ToolkitLocalization.Text(recipe.Eligible ? "fields.recipe-level" : "fields.recipe-level-required", recipe.Eligible ? "{0} · Farming {1}" : "{0} · Farming {1} required", recipe.Title, recipe.RequiredHeroLevel) + "\n" + PlantCost(recipe.Orchard) : ToolkitLocalization.Text("fields.discover-first", "Discover this seed or sapling first.");
            }
            var ready = 0;
            foreach (var bed in snapshot.Beds ?? Array.Empty<FarmBedPresentation>())
            {
                if (bed == null || !beds.TryGetValue(bed.Id, out var tile)) continue;
                tile.Button.SetEnabled(bed.Unlocked); tile.Button.EnableInClassList("selected", selectedBed == bed.Id);
                tile.Button.EnableInClassList("fields-locked", !bed.Unlocked);
                tile.Button.EnableInClassList("fields-ready", bed.Ready);
                tile.Title.text = bed.Title; tile.Status.text = bed.Unlocked ? BedStatus(bed) : ToolkitLocalization.Text("common.locked", "Locked"); tile.Button.tooltip = BedStatus(bed);
                tile.Fill.parent.style.visibility = bed.Planted ? Visibility.Visible : Visibility.Hidden;
                tile.Fill.style.width = Length.Percent(Mathf.Clamp01(bed.Progress01) * 100);
                var recipe = (source as FarmService)?.Content.Recipe(bed.RecipeId);
                var stage = FarmView.GrowthStage(bed.Progress01, bed.Ready);
                var sprite = !bed.Planted ? null : bed.Orchard && stage < 3 && appearance ? appearance.treeStages[stage] : recipe?.stages?.Length == 4 ? recipe.stages[stage] : bed.Icon;
                for (var i = 0; i < tile.Soil.Length; i++)
                {
                    var x = i % 3; var y = i / 3;
                    var soilIndex = bed.Orchard ? 3 : x != 1 ? y != 1 ? 0 : 2 : y != 1 ? 1 : 3;
                    tile.Soil[i].sprite = appearance ? (bed.Watered ? appearance.wet : appearance.dry)[soilIndex] : null;
                    tile.Soil[i].style.scale = new Scale(new Vector3(!bed.Orchard && x == 2 ? -1 : 1, !bed.Orchard && y == 2 ? -1 : 1, 1));
                    tile.Plants[i].sprite = sprite; tile.Plants[i].style.display = sprite ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (bed.Unlocked && bed.Ready) ready++;
            }
            var selected = snapshot.Recipes?.FirstOrDefault(r => r?.Id == selectedRecipe);
            var target = snapshot.Beds?.FirstOrDefault(b => b?.Id == selectedBed && b.Unlocked);
            selection.text = selected == null ? ToolkitLocalization.Text("fields.select-seeds-bed", "Select seeds and an empty bed") :
                (selected.Discovered ? selected.Title : ToolkitLocalization.Text("fields.undiscovered", "Undiscovered")) +
                (target == null ? ToolkitLocalization.Text("fields.select-empty-bed", " · Select an empty bed") : ToolkitLocalization.Text("fields.selected-bed", " → {0}", target.Title));
            var canPlant = snapshot.TownActionsAllowed && selected?.Eligible == true && selected.Discovered && selected.SeedQuantity >= FarmContent.PlantingCost(selected.Orchard) && target != null && !target.Planted && target.Orchard == selected.Orchard;
            var reason = target?.Planted == true ? BedStatus(target) : selected == null ? "" : !selected.Discovered ? ToolkitLocalization.Text("fields.discover-first", "Discover this seed or sapling first.") : !selected.Eligible ? ToolkitLocalization.Text("fields.recipe-level-required", "{0} · Farming {1} required", selected.Title, selected.RequiredHeroLevel) : target == null || target.Orchard != selected.Orchard ? ToolkitLocalization.Text("fields.select-empty-bed", " · Select an empty bed") : PlantCost(selected.Orchard);
            detail.text = reason; detail.style.display = target?.Planted == true ? DisplayStyle.None : DisplayStyle.Flex;
            plant.tooltip = reason; plant.SetEnabled(canPlant);
            harvest.SetEnabled(snapshot.TownActionsAllowed && ready > 0);
            water.SetEnabled(snapshot.TownActionsAllowed && target?.Planted == true && !target.Watered && !target.Ready);
            water.text = ToolkitLocalization.Text(target?.Watered == true ? "fields.action.watered" : "fields.action.water-once", target?.Watered == true ? "Watered" : "Water once");
            repeat.text = ToolkitLocalization.Text(target?.Repeat == true ? "fields.action.repeat-on" : "fields.action.repeat-off", target?.Repeat == true ? "Repeat: on" : "Repeat: off");
            repeat.SetEnabled(snapshot.TownActionsAllowed && target != null && (snapshot.TwinsLevel >= 20 || target.Repeat));
            repeat.tooltip = ToolkitLocalization.Text("fields.repeat-help", "Twins level 20: harvest and replant using 9 matching seed packs or 1 matching sapling. Stops when inputs run out.");
            foreach (var build in snapshot.Builds ?? Array.Empty<FarmBuildPresentation>())
            {
                if (build == null || !builds.TryGetValue(build.QuestId, out var row)) continue;
                row.Title.text = ToolkitLocalization.Text("fields.build-level", "{0} · Twins {1}", build.Title, build.TwinsLevel);
                row.Status.text = build.Completed ? ToolkitLocalization.Text("fields.completed", "Completed") : ToolkitLocalization.Text("fields.build-status", "{0} · {1}", build.Status, build.Costs);
                row.HandIn.SetEnabled(snapshot.TownActionsAllowed && build.CanTurnIn && !build.Completed);
            }
        }
        private static string PlantCost(bool orchard) => ToolkitLocalization.Text(orchard ? "fields.plant-sapling-cost" : "fields.plant-seed-cost", orchard ? "Requires {0} matching sapling per plot." : "Requires {0} matching seed packs per field.", FarmContent.PlantingCost(orchard));
        private static string BedStatus(FarmBedPresentation bed) => !bed.Unlocked ? ToolkitLocalization.Text("fields.locked", "Locked · construction required") : !bed.Planted ? ToolkitLocalization.Text("fields.empty", "Empty") : bed.RecipeTitle + "\n" + (bed.Ready ? ToolkitLocalization.Text("fields.ready", "Ready") : FormatTime(Math.Max(0, bed.RemainingSeconds), mspace: false, shortForm: true));
        private void ApplyLayout()
        {
            var wide = ToolkitWindowLayout.SafeArea.width - CompanionWidth >= 640;
            var compact = wide && ToolkitWindowLayout.SafeArea.height < 405;
            root.EnableInClassList("fields-compact", compact);
            if (compactLayout != compact)
            {
                compactLayout = compact;
                var artSize = compact ? 30f : 36f;
                foreach (var tile in beds.Values)
                {
                    tile.Art.style.width = tile.Art.style.height = artSize;
                    var index = 0;
                    foreach (var spot in tile.Art.Children())
                    {
                        var size = tile.Orchard ? artSize : artSize / 3;
                        spot.style.width = spot.style.height = size;
                        tile.Soil[index].style.width = size; tile.Soil[index].style.height = artSize / 3;
                        tile.Plants[index].style.width = tile.Plants[index].style.height = size;
                        index++;
                    }
                }
            }
            columns.style.flexDirection = wide ? FlexDirection.Row : FlexDirection.Column;
            var seedHeight = wide ? Mathf.Max(80, ToolkitWindowLayout.SafeArea.height - 165) : 200;
            seedScroll.style.maxHeight = seedHeight; seedScroll.parent.style.maxHeight = seedHeight;
            seedColumn.style.width = wide ? new Length(148) : Length.Percent(100); seedColumn.style.marginRight = wide ? 10 : 0;
            plotSections.style.flexDirection = wide ? FlexDirection.Row : FlexDirection.Column;
            foreach (var section in plotSections.Children())
            {
                section.style.marginLeft = wide && section == plotSections.ElementAt(1) ? 8 : 0;
                section.style.marginTop = !wide && section == plotSections.ElementAt(1) ? 8 : 0;
                section.style.flexBasis = wide ? new StyleLength(0f) : new StyleLength(StyleKeyword.Auto);
                section.style.flexGrow = wide ? 1 : 0;
            }
        }
        private void Update()
        {
            if (!IsOpen) return;
            layout.Fill(root, theme, CompanionWidth); ApplyLayout();
            if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .25f; Refresh();
        }
        public void Hide()
        {
            if (source != null) source.Changed -= Refresh;
            ToolkitLocalization.Changed -= Refresh;
            root?.RemoveFromHierarchy(); root = null; compactLayout = null; seeds.Clear(); beds.Clear(); builds.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
