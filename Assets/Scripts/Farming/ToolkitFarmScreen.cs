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
    /// <summary>Stable plot controls over committed state; purchases use the durable farm lane.</summary>
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
        private FieldsWorldAppearance appearance;
        private VisualElement root, board, cropSection, orchardSection, bedList, orchardList, drawer, seedColumn, seedGrid, xpBar;
        private ScrollView workspace, seedScroll;
        private Label heading, xp, error, cropHeading, orchardHeading, seedHeading, undiscovered, selection, detail;
        private Button plant, harvest, water, repeat, buildSelected;
        private readonly ToolkitWindowLayout layout = new();
        private readonly Dictionary<string, SeedCard> seeds = new();
        private readonly Dictionary<string, BedTile> beds = new();
        private readonly List<OrchardPair> orchardPairs = new();
        private string selectedRecipe, selectedBed;
        private float nextRefresh;
        private bool narrow;
        private ToolkitPortraitPages portraitPages;
        public float CompanionWidth { get; set; }
        public bool IsOpen => root != null;
        public bool IsConfigured => theme && runtimeTheme && textSettings && (source != null || sourceBehaviour is IFarmPresentationSource);
        private sealed class SeedCard { public Button Button; public Label Title, Count; public Image Icon; }
        private sealed class BedTile
        {
            public VisualElement Root, Art, Fill;
            public Button Select, Action;
            public Label Title, Status;
            public Image[] Soil, Plants;
            public CostStrip Costs;
            public bool Orchard;
        }
        private sealed class OrchardPair { public string First, Second; public VisualElement Root; public Label Title, Status; public Button Build; public CostStrip Costs; }
        private sealed class CostStrip
        {
            public VisualElement Root;
            public readonly Image[] Icons = new Image[3];
            public readonly Label[] Counts = new Label[3];
        }
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
            var header = E(root, "row between fields-header"); heading = L(header, "", "quests-heading");
            var progress = E(header, "row fields-xp"); xp = L(progress, "", "muted"); xpBar = ToolkitGameplay.Bar(progress);
            error = L(root, "", "status");
            workspace = ToolkitControls.RecessedScroll(root, "fields-workspace", theme, compact: true);
            board = E(workspace, "row fields-board");
            cropSection = E(board, "column fields-crops"); cropHeading = L(cropSection, "", "heading");
            bedList = E(cropSection, "row fields-bed-grid"); bedList.name = "fields-bed-grid";
            orchardSection = E(board, "column fields-orchard"); orchardHeading = L(orchardSection, "", "heading");
            orchardList = E(orchardSection, "column fields-orchard-grid"); orchardList.name = "fields-orchard-grid";
            var snapshot = source.CapturePresentation();
            foreach (var bed in snapshot.Beds.Where(b => b != null && !b.Orchard)) CreateBed(bed, bedList);
            var orchardBeds = snapshot.Beds.Where(b => b?.Orchard == true).ToArray();
            for (var i = 0; i < orchardBeds.Length; i += 2)
            {
                var box = E(orchardList, "column fields-orchard-pair");
                var pair = new OrchardPair { Root = box, First = orchardBeds[i].Id, Second = i + 1 < orchardBeds.Length ? orchardBeds[i + 1].Id : null };
                pair.Title = L(box, "", "fields-pair-title");
                var slots = E(box, "row fields-pair-slots"); CreateBed(orchardBeds[i], slots);
                if (pair.Second != null) CreateBed(orchardBeds[i + 1], slots);
                pair.Status = L(box, "", "muted fields-pair-status"); pair.Costs = CreateCosts(box);
                var first = pair.First;
                pair.Build = Button(box, "", () => ActOnBed(first));
                pair.Build.name = "fields-build-" + first; pair.Build.AddToClassList("fields-pair-build"); orchardPairs.Add(pair);
            }
            drawer = E(workspace, "row fields-drawer");
            var controls = E(drawer, "column fields-selected"); selection = L(controls, "", "fields-selection"); selection.name = "fields-seed-selection";
            detail = L(controls, "", "muted fields-selection-detail"); detail.name = "fields-selected-status";
            plant = Button(controls, "", PlantSelected); plant.name = "fields-plant-selected"; plant.AddToClassList("primary");
            buildSelected = Button(controls, "", () => Build(FindBuild(source.CapturePresentation(), selectedBed), selectedBed)); buildSelected.AddToClassList("primary");
            var tending = E(controls, "row fields-actions");
            water = Button(tending, "", () => { if (source is FarmService service) Execute(() => service.Water(selectedBed)); }); water.name = "fields-water-selected";
            repeat = Button(tending, "", ToggleRepeat); repeat.name = "fields-repeat-selected";
            harvest = Button(controls, "", () => Execute(source.HarvestReady)); harvest.name = "farm-harvest-ready";
            seedColumn = E(drawer, "column fields-seeds-column"); seedHeading = L(seedColumn, "", "heading");
            seedScroll = ToolkitControls.RecessedScroll(seedColumn, "fields-seeds", theme, compact: true);
            seedGrid = E(seedScroll, "row fields-seed-grid");
            foreach (var recipe in snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>())
            {
                if (recipe == null) continue;
                var id = recipe.Id; var button = Button(seedGrid, "", () => SelectRecipe(id)); button.name = "fields-seed-" + id;
                button.AddToClassList("fields-seed-card");
                var icon = ToolkitGameplay.Icon(button, null, 14);
                var title = L(button, "", "fields-seed-caption"); title.name = "seed-caption";
                var count = L(button, "", "fields-seed-count"); count.name = "seed-quantity";
                seeds[id] = new SeedCard { Button = button, Title = title, Count = count, Icon = icon };
            }
            undiscovered = L(seedColumn, "", "muted fields-undiscovered-count");
            portraitPages = new ToolkitPortraitPages(workspace.contentContainer, null, new[] { board }, workspace,
                new ToolkitPortraitPages.Page("Crops", cropSection),
                new ToolkitPortraitPages.Page("Orchard", orchardSection),
                new ToolkitPortraitPages.Page("Planting", drawer));
            source.Changed += Refresh; ToolkitLocalization.Changed += Refresh;
            layout.Fill(root, theme, CompanionWidth); ApplyLayout(); Refresh(); return true;
        }
        private static VisualElement E(VisualElement parent, string classes) => ToolkitGameplay.E(parent, classes);
        private static Label L(VisualElement parent, string text, string classes) => ToolkitGameplay.L(parent, text, classes);
        private static Button Button(VisualElement parent, string text, Action action)
        { var button = ToolkitControls.Button("", action, null); button.text = text; parent.Add(button); return button; }
        private static void Visible(VisualElement element, bool visible) => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        private static CostStrip CreateCosts(VisualElement parent)
        {
            var strip = new CostStrip { Root = E(parent, "row fields-costs") };
            for (var i = 0; i < 3; i++)
            {
                var item = E(strip.Root, "row fields-cost"); strip.Icons[i] = ToolkitGameplay.Icon(item, null, 10);
                strip.Counts[i] = L(item, "", "fields-cost-count");
            }
            return strip;
        }
        private static void RefreshCosts(CostStrip strip, FarmBuildPresentation build)
        {
            Visible(strip.Root, build != null && !build.Completed);
            for (var i = 0; i < strip.Counts.Length; i++)
            {
                var material = build?.Materials?.ElementAtOrDefault(i); Visible(strip.Counts[i].parent, material != null);
                if (material == null) continue;
                strip.Icons[i].sprite = material.Icon; strip.Counts[i].text = material.Required.ToString("N0", CultureInfo.CurrentCulture);
                strip.Counts[i].EnableInClassList("fields-shortfall", material.Owned < material.Required);
                strip.Counts[i].parent.tooltip = ToolkitLocalization.Text("fields.cost-owned", "{0}: {1:N0} owned / {2:N0} needed", material.Title, material.Owned, material.Required);
            }
        }
        private void CreateBed(FarmBedPresentation bed, VisualElement parent)
        {
            var id = bed.Id; var tile = new BedTile { Orchard = bed.Orchard };
            tile.Root = E(parent, bed.Orchard ? "column fields-orchard-slot" : "column fields-plot"); tile.Root.name = "fields-plot-" + id;
            tile.Select = Button(tile.Root, "", () => SelectBed(id)); tile.Select.name = "fields-select-" + id; tile.Select.AddToClassList("fields-plot-select");
            tile.Title = L(tile.Select, "", "fields-plot-title");
            var body = E(tile.Select, "row fields-plot-body"); tile.Art = E(body, "fields-plot-art"); tile.Art.name = "bed-art";
            tile.Soil = new Image[bed.Orchard ? 4 : 9]; tile.Plants = new Image[bed.Orchard ? 1 : 9];
            for (var i = 0; i < tile.Soil.Length; i++)
            {
                var spot = new VisualElement { pickingMode = PickingMode.Ignore }; tile.Art.Add(spot); spot.style.position = Position.Absolute;
                spot.style.left = bed.Orchard ? 4 + i % 2 * 8 : i % 3 * 8; spot.style.top = bed.Orchard ? 16 + i / 2 * 8 : 8 + i / 3 * 8;
                spot.style.width = 8; spot.style.height = 8;
                tile.Soil[i] = new Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
                tile.Soil[i].style.position = Position.Absolute; tile.Soil[i].style.width = 8; tile.Soil[i].style.height = 8; tile.Soil[i].style.bottom = 0; spot.Add(tile.Soil[i]);
                if (bed.Orchard) continue;
                tile.Plants[i] = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                tile.Plants[i].style.position = Position.Absolute; tile.Plants[i].style.width = bed.Orchard ? 24 : 8; tile.Plants[i].style.height = bed.Orchard ? 32 : 16; tile.Plants[i].style.bottom = 0; spot.Add(tile.Plants[i]);
            }
            if (bed.Orchard)
            {
                // One tree on a rounded 2x2 patch, at the same half-scale pixels as crop soil.
                var tree = new Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
                tree.style.position = Position.Absolute; tile.Art.Add(tree); tile.Plants[0] = tree;
            }
            var info = E(body, "column fields-plot-info"); tile.Status = L(info, "", "fields-plot-status"); tile.Status.name = "bed-status";
            tile.Fill = ToolkitGameplay.Bar(info); tile.Fill.name = "bed-progress";
            if (!bed.Orchard)
            {
                tile.Costs = CreateCosts(tile.Root);
                tile.Action = Button(tile.Root, "", () => ActOnBed(id)); tile.Action.name = "fields-action-" + id; tile.Action.AddToClassList("fields-plot-action");
            }
            beds[id] = tile;
        }
        private FarmBuildPresentation FindBuild(FarmPresentationSnapshot snapshot, string id)
        {
            if (id == null) return null;
            var garden = Array.IndexOf(FarmCommands.GardenBeds, id); var orchard = Array.IndexOf(FarmCommands.OrchardBeds, id);
            return snapshot?.Builds?.FirstOrDefault(b => b != null && b.QuestId != FarmContent.IntroductionId &&
                (garden >= 0 ? b.OrchardCapacity == 0 && b.GardenCapacity == garden + 1 : orchard >= 0 && b.OrchardCapacity == (orchard / 2 + 1) * 2));
        }
        private void SelectBed(string id)
        {
            selectedBed = id; var snapshot = source.CapturePresentation(); var bed = snapshot.Beds.FirstOrDefault(b => b.Id == id);
            if (ToolkitResponsiveLayout.IsPortrait) portraitPages?.Select(2);
            var recipe = snapshot.Recipes.FirstOrDefault(r => r.Id == selectedRecipe);
            if (bed != null && (bed.Planted || recipe == null || recipe.Orchard != bed.Orchard))
                selectedRecipe = bed.RecipeId ?? snapshot.Recipes.FirstOrDefault(r => r.Orchard == bed.Orchard && r.Discovered && r.Eligible)?.Id;
            Refresh();
        }
        private void SelectRecipe(string id)
        {
            selectedRecipe = id; var snapshot = source.CapturePresentation(); var recipe = snapshot.Recipes.FirstOrDefault(r => r.Id == id);
            var target = snapshot.Beds.FirstOrDefault(b => b.Id == selectedBed);
            if (recipe != null && (target == null || !target.Unlocked || target.Planted || target.Orchard != recipe.Orchard))
                selectedBed = snapshot.Beds.FirstOrDefault(b => b.Unlocked && !b.Planted && b.Orchard == recipe.Orchard)?.Id;
            Refresh();
        }
        private void Execute(Func<bool> command) { if (source?.Ready == true) { command(); Refresh(); } }
        private void Build(FarmBuildPresentation build, string id)
        {
            if (build?.CanTurnIn != true || source is not FarmService service) return;
            var manager = QuestManager.Instance;
            if (manager) manager.RefreshFieldsProgression();
            if (manager ? manager.TryTurnInQuest(build.QuestId) : service.BuildQuest(build.QuestId)) SelectBed(id);
            Refresh();
        }
        private void ActOnBed(string id)
        {
            var snapshot = source.CapturePresentation(); var bed = snapshot.Beds.First(b => b.Id == id);
            if (!bed.Unlocked) { var build = FindBuild(snapshot, id); if (build?.CanTurnIn == true) Build(build, id); else SelectBed(id); }
            else if (source is FarmService service && bed.Ready) { selectedBed = id; selectedRecipe = bed.RecipeId; Execute(() => service.Harvest(id)); }
            else if (source is FarmService waterService && bed.Planted && !bed.Watered) Execute(() => waterService.Water(id));
            else SelectBed(id);
        }
        private void PlantSelected()
        {
            if (plant.enabledInHierarchy && source is FarmService service && selectedBed != null && selectedRecipe != null)
                Execute(() => service.Plant(selectedBed, selectedRecipe));
        }
        private void ToggleRepeat()
        {
            var bed = source.CapturePresentation().Beds.FirstOrDefault(b => b.Id == selectedBed);
            if (bed != null && source is FarmService service) Execute(() => service.SetRepeat(bed.Id, !bed.Repeat));
        }
        public void Refresh()
        {
            if (!IsOpen || source == null) return;
            var snapshot = source.Ready ? source.CapturePresentation() : null;
            error.text = source.LastError ?? ""; Visible(error, error.text.Length > 0);
            if (snapshot == null) { plant.SetEnabled(false); harvest.SetEnabled(false); water.SetEnabled(false); repeat.SetEnabled(false); buildSelected.SetEnabled(false); return; }
            selectedBed ??= snapshot.Beds.FirstOrDefault(b => b.Unlocked)?.Id ?? snapshot.Beds.FirstOrDefault()?.Id;
            var target = snapshot.Beds.FirstOrDefault(b => b.Id == selectedBed); var orchard = target?.Orchard == true;
            selectedRecipe ??= snapshot.Recipes.FirstOrDefault(r => r.Orchard == orchard && r.Discovered && r.Eligible)?.Id;
            var selected = snapshot.Recipes.FirstOrDefault(r => r.Id == selectedRecipe);
            heading.text = ToolkitLocalization.Text("fields.heading", "{0} · Flora & Tillman · Level {1}", snapshot.DisplayName, snapshot.TwinsLevel);
            xp.text = ToolkitLocalization.Text("fields.xp", "{0} / {1} XP", snapshot.TwinsXp, snapshot.TwinsXpRequired);
            xpBar.style.width = Length.Percent(Mathf.Clamp01((float)snapshot.TwinsXp / Math.Max(1, snapshot.TwinsXpRequired)) * 100);
            cropHeading.text = ToolkitLocalization.Text("fields.crop-capacity", "Crop plots · {0} / 6", snapshot.Beds.Count(b => !b.Orchard && b.Unlocked));
            orchardHeading.text = ToolkitLocalization.Text("fields.orchard-capacity", "Orchard · {0} / 6", snapshot.Beds.Count(b => b.Orchard && b.Unlocked));
            foreach (var bed in snapshot.Beds)
            {
                if (!beds.TryGetValue(bed.Id, out var tile)) continue;
                var build = FindBuild(snapshot, bed.Id);
                tile.Root.EnableInClassList("selected", selectedBed == bed.Id); tile.Root.EnableInClassList("fields-locked", !bed.Unlocked);
                tile.Root.EnableInClassList("fields-ready", bed.Ready); tile.Title.text = bed.Title;
                tile.Status.text = bed.Unlocked ? BedStatus(bed) : ToolkitLocalization.Text("fields.requires-twins", "Twins level {0}", build?.TwinsLevel ?? 1);
                tile.Select.tooltip = bed.Unlocked ? BedStatus(bed) : build?.Status;
                Visible(tile.Fill.parent, bed.Planted); tile.Fill.style.width = Length.Percent(Mathf.Clamp01(bed.Progress01) * 100); RefreshArt(tile, bed);
                if (tile.Action == null) continue;
                RefreshCosts(tile.Costs, bed.Unlocked ? null : build);
                Visible(tile.Action, !bed.Unlocked || !bed.Planted || bed.Ready || !bed.Watered);
                tile.Action.text = !bed.Unlocked ? build?.CanTurnIn == true ? T("fields.build-plot", "Build plot") : T("fields.requirements", "Requirements") :
                    bed.Ready ? T("fields.action.harvest", "Harvest") : bed.Planted ? T("fields.action.water-once", "Water once") : T("fields.choose-seeds", "Choose seeds");
                tile.Action.SetEnabled(!bed.Unlocked || snapshot.TownActionsAllowed);
                tile.Action.EnableInClassList("primary", bed.Ready || !bed.Unlocked && build?.CanTurnIn == true);
                tile.Root.EnableInClassList("fields-unbuilt", !bed.Unlocked);
            }
            foreach (var pair in orchardPairs)
            {
                var first = snapshot.Beds.First(b => b.Id == pair.First); var second = snapshot.Beds.FirstOrDefault(b => b.Id == pair.Second);
                var complete = first.Unlocked && second?.Unlocked != false; var build = FindBuild(snapshot, pair.First);
                pair.Root.EnableInClassList("fields-pair-locked", !first.Unlocked && second?.Unlocked != true);
                pair.Title.text = ToolkitLocalization.Text("fields.orchard-pair", "Orchard {0}–{1}", Array.IndexOf(FarmCommands.OrchardBeds, pair.First) + 1, Array.IndexOf(FarmCommands.OrchardBeds, pair.Second) + 1);
                pair.Status.text = build?.Status ?? ""; Visible(pair.Status, !complete && build?.CanTurnIn != true);
                RefreshCosts(pair.Costs, complete ? null : build); Visible(pair.Build, !complete);
                pair.Build.text = build?.CanTurnIn == true ? T("fields.build-orchard-pair", "Build 2 plots") : T("fields.requirements", "Requirements"); pair.Build.SetEnabled(true);
                pair.Build.EnableInClassList("primary", build?.CanTurnIn == true);
            }
            var available = snapshot.Recipes.Where(r => r.Orchard == orchard).ToArray(); var discovered = available.Count(r => r.Discovered);
            seedHeading.text = ToolkitLocalization.Text(orchard ? "fields.sapling-count" : "fields.seed-count", orchard ? "Saplings · {0} / {1}" : "Seed packs · {0} / {1}", discovered, available.Length);
            var columns = orchard ? 4 : narrow ? 4 : 9;
            foreach (var recipe in snapshot.Recipes)
            {
                if (!seeds.TryGetValue(recipe.Id, out var card)) continue;
                var show = recipe.Orchard == orchard && (recipe.Discovered || discovered == 0 && recipe == available.FirstOrDefault());
                Visible(card.Button, show); card.Button.style.width = Length.Percent(100f / columns - .6f);
                card.Icon.sprite = recipe.Discovered ? recipe.Icon : recipe.UnknownIcon ? recipe.UnknownIcon : unknownPack;
                card.Title.text = recipe.Discovered ? recipe.CropTitle ?? recipe.Title : "???"; card.Count.text = recipe.Discovered ? recipe.SeedQuantity.ToString("0.##", CultureInfo.CurrentCulture) : "—";
                card.Button.EnableInClassList("primary", selectedRecipe == recipe.Id); card.Button.EnableInClassList("fields-undiscovered", !recipe.Discovered);
                card.Button.tooltip = recipe.Discovered ? ToolkitLocalization.Text("fields.recipe-level-required", "{0} · Farming {1} required", recipe.Title, recipe.RequiredHeroLevel) + "\n" + PlantCost(orchard) : T("fields.discover-first", "Discover this seed or sapling first.");
            }
            undiscovered.text = ToolkitLocalization.Text("fields.hidden-seeds", "{0} undiscovered", available.Length - discovered); Visible(undiscovered, discovered < available.Length);
            var selectedBuild = target?.Unlocked == false ? FindBuild(snapshot, target.Id) : null;
            selection.text = target == null ? T("fields.select-seeds-bed", "Select seeds and an empty bed") : target.Title + (selected?.Discovered == true ? " · " + (selected.CropTitle ?? selected.Title) : "");
            var matches = target != null && selected != null && target.Orchard == selected.Orchard;
            var canPlant = snapshot.TownActionsAllowed && target?.Unlocked == true && !target.Planted && matches && selected.Discovered && selected.Eligible && selected.SeedQuantity >= FarmContent.PlantingCost(orchard);
            detail.text = selectedBuild != null ? selectedBuild.Status + "\n" + string.Join("\n", selectedBuild.Materials.Select(m => ToolkitLocalization.Text("fields.cost-owned", "{0}: {1:N0} owned / {2:N0} needed", m.Title, m.Owned, m.Required))) :
                target?.Planted == true ? BedStatus(target) : !matches ? T("fields.select-seeds-bed", "Select seeds and an empty bed") : !selected.Discovered ? T("fields.discover-first", "Discover this seed or sapling first.") : !selected.Eligible ?
                ToolkitLocalization.Text("fields.recipe-level-required", "{0} · Farming {1} required", selected.Title, selected.RequiredHeroLevel) : ToolkitLocalization.Text("fields.plant-balance", "{0:N0} owned / {1} needed", selected.SeedQuantity, FarmContent.PlantingCost(orchard));
            Visible(buildSelected, selectedBuild != null); buildSelected.text = orchard ? T("fields.build-orchard-pair", "Build 2 plots") : T("fields.build-plot", "Build plot"); buildSelected.SetEnabled(selectedBuild?.CanTurnIn == true);
            Visible(plant, selectedBuild == null && target?.Planted != true); plant.SetEnabled(canPlant);
            plant.text = canPlant ? ToolkitLocalization.Text("fields.plant-target", "Plant in {0}", target.Title) : matches && selected.Discovered && selected.SeedQuantity < FarmContent.PlantingCost(orchard) ? T("fields.need-pack", "Need a matching pack") : T("fields.action.plant", "Plant"); plant.tooltip = detail.text;
            Visible(water, target?.Planted == true && !target.Ready && !target.Watered); water.SetEnabled(snapshot.TownActionsAllowed); water.text = T("fields.action.water-once", "Water once");
            Visible(repeat, target?.Unlocked == true && snapshot.TwinsLevel >= 20); repeat.SetEnabled(snapshot.TownActionsAllowed);
            repeat.text = T(target?.Repeat == true ? "fields.action.repeat-on" : "fields.action.repeat-off", target?.Repeat == true ? "Repeat: on" : "Repeat: off");
            repeat.tooltip = T("fields.repeat-help", "Twins level 20: harvest and replant using 1 matching seed pack or 1 matching sapling. Stops when inputs run out.");
            var ready = snapshot.Beds.Count(b => b.Unlocked && b.Ready); Visible(harvest, ready > 0); harvest.SetEnabled(snapshot.TownActionsAllowed && ready > 0); harvest.text = T("fields.action.harvest-ready", "Harvest ready");
        }
        private void RefreshArt(BedTile tile, FarmBedPresentation bed)
        {
            var recipe = (source as FarmService)?.Content.Recipe(bed.RecipeId); var stage = FarmView.GrowthStage(bed.Progress01, bed.Ready);
            var sprite = !bed.Planted ? null : bed.Orchard && stage < 3 && appearance ? appearance.treeStages[stage] : recipe?.stages?.Length == 4 ? recipe.stages[stage] : bed.Icon;
            for (var i = 0; i < tile.Soil.Length; i++)
            {
                var size = bed.Orchard ? 2 : 3; var x = i % size; var y = i / size;
                var index = bed.Orchard ? 0 : x != 1 ? y != 1 ? 0 : 2 : y != 1 ? 1 : 3;
                tile.Soil[i].sprite = appearance ? (bed.Watered ? appearance.wet : appearance.dry)[index] : null;
                tile.Soil[i].style.scale = new Scale(new Vector3(x == size - 1 ? -1 : 1, y == size - 1 ? -1 : 1, 1));
            }
            for (var i = 0; i < tile.Plants.Length; i++)
            {
                tile.Plants[i].sprite = sprite; Visible(tile.Plants[i], sprite);
                if (bed.Orchard && sprite)
                {
                    // Match the world's sprite-rectangle origin to the soil's lower-left tile centre.
                    var width = sprite.rect.width / sprite.pixelsPerUnit * 8;
                    var height = sprite.rect.height / sprite.pixelsPerUnit * 8;
                    tile.Plants[i].style.width = width; tile.Plants[i].style.height = height;
                    tile.Plants[i].style.left = 4; tile.Plants[i].style.top = 32 - height;
                }
            }
        }
        private static string T(string key, string fallback) => ToolkitLocalization.Text(key, fallback);
        private static string PlantCost(bool orchard) => ToolkitLocalization.Text(orchard ? "fields.plant-sapling-cost" : "fields.plant-seed-cost", orchard ? "Requires {0} matching sapling per plot." : "Requires {0} matching seed pack per field.", FarmContent.PlantingCost(orchard));
        private static string BedStatus(FarmBedPresentation bed) => !bed.Unlocked ? T("fields.locked", "Locked · construction required") : !bed.Planted ? T("fields.empty", "Empty") : bed.RecipeTitle + "\n" + (bed.Ready ? T("fields.ready", "Ready") : FormatTime(Math.Max(0, bed.RemainingSeconds), mspace: false, shortForm: true));
        private void ApplyLayout()
        { portraitPages?.Update(); narrow = ToolkitWindowLayout.SafeArea.width - CompanionWidth < 640; root.EnableInClassList("fields-narrow", narrow); }
        private void Update()
        {
            if (!IsOpen) return; layout.Fill(root, theme, CompanionWidth); ApplyLayout();
            if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .25f; Refresh();
        }
        public void Hide()
        {
            if (source != null) source.Changed -= Refresh; ToolkitLocalization.Changed -= Refresh;
            root?.RemoveFromHierarchy(); root = null; seeds.Clear(); beds.Clear(); orchardPairs.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
