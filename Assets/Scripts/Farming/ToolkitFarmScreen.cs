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
        private Sprite wateringCan;
        private VisualElement root, board, bedList, drawer, seedColumn, seedGrid, xpBar, yieldRow;
        private VisualElement[] selectorTiles;
        private ScrollView workspace, seedScroll;
        private Image floraPortrait, tillmanPortrait;
        private Label heading, xp, error, seedHeading, detail;
        private Label buildRequirement, yieldAmount, yieldXp, secondaryAmount;
        private Image yieldIcon, secondaryIcon;
        private VisualElement secondaryReward;
        private CostStrip selectedCosts;
        private Button plant, harvest, water, repeat, buildSelected;
        private readonly ToolkitWindowLayout layout = new();
        private readonly Dictionary<string, SeedCard> seeds = new();
        private readonly Dictionary<string, BedTile> beds = new();
        private readonly List<OrchardPair> orchardPairs = new();
        private readonly Dictionary<Sprite, float> previewCropInsets = new();
        private readonly List<Vector2> previewOutline = new();
        private string selectedRecipe, selectedBed;
        private float nextRefresh;
        private BedTile preview, orchardPreview;
        private string feedback;
        private float feedbackUntil;
        private Label feedbackLabel;
        private bool narrow;
        public float CompanionWidth { get; set; }
        public bool IsOpen => root != null;
        public bool IsConfigured => theme && runtimeTheme && textSettings && (source != null || sourceBehaviour is IFarmPresentationSource);
        private sealed class SeedCard { public Button Button; public Label Count; public Image Icon; }
        private sealed class BedTile
        {
            public VisualElement Root, Art, Fill;
            public Button Select;
            public Label Title, Status, Requirement, ProgressStatus;
            public Image[] Soil, Plants;
            public Image[] WetSoils;
            public bool Orchard, Preview;
        }
        private sealed class OrchardPair { public string First, Second; public VisualElement Root, Slots; public Label Title, Requirement; public Button LockedSelect; }
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
            wateringCan = TimelessEchoes.Characters.CharacterCatalog.Load()?.skillToolIcons.ElementAtOrDefault(2);
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "farm" }; theme.Apply(root); ToolkitGameplay.Apply(root, theme);
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Fields"));
            root.AddToClassList("menu-surface"); root.AddToClassList("fields-ui"); document.rootVisualElement.Add(root);
            var header = E(root, "row fields-header");
            var portraits = E(header, "row fields-portraits");
            floraPortrait = ToolkitGameplay.Icon(portraits, null, 24);
            floraPortrait.AddToClassList("fields-portrait-leading");
            tillmanPortrait = ToolkitGameplay.Icon(portraits, null, 24);
            var progress = E(header, "column fields-header-progress");
            var levelLine = E(progress, "row between"); heading = L(levelLine, "", "heading");
            xp = L(levelLine, "", "muted"); xpBar = ToolkitGameplay.Bar(progress);
            error = L(root, "", "status");
            feedbackLabel = L(root, "", "fields-feedback");
            workspace = ToolkitControls.RecessedScroll(root, "fields-workspace", theme, compact: true);
            board = E(workspace, "row fields-board");
            drawer = E(board, "column fields-drawer");
            bedList = E(board, "row fields-selector-grid"); bedList.name = "fields-bed-grid";
            var snapshot = source.CapturePresentation();
            foreach (var bed in snapshot.Beds.Where(b => b != null && !b.Orchard)) CreateBed(bed, bedList);
            var orchardBeds = snapshot.Beds.Where(b => b?.Orchard == true).ToArray();
            for (var i = 0; i < orchardBeds.Length; i += 2)
            {
                var box = E(bedList, "column fields-selector-tile fields-orchard-pair");
                var pair = new OrchardPair { Root = box, First = orchardBeds[i].Id, Second = i + 1 < orchardBeds.Length ? orchardBeds[i + 1].Id : null };
                var pairHeader = E(box, "row fields-plot-heading");
                pair.Title = L(pairHeader, "", "fields-pair-title");
                var slots = E(box, "row fields-pair-slots"); pair.Slots = slots;
                var firstTree = CreateBed(orchardBeds[i], slots);
                if (pair.Second != null)
                {
                    firstTree.Root.style.marginRight = 4;
                    CreateBed(orchardBeds[i + 1], slots);
                }
                var first = pair.First;
                pair.LockedSelect = Button(box, "", () => SelectBed(first));
                pair.LockedSelect.AddToClassList("fields-pair-lock");
                pair.Requirement = L(pair.LockedSelect, "", "fields-level-requirement"); orchardPairs.Add(pair);
            }
            var controls = E(drawer, "column fields-selected");
            buildRequirement = L(controls, "", "fields-level-requirement");
            preview = CreateBed(snapshot.Beds.First(), controls, false); preview.Root.AddToClassList("fields-preview");
            orchardPreview = CreateBed(snapshot.Beds.First(b => b.Orchard), controls, false); orchardPreview.Root.AddToClassList("fields-preview");
            selectedCosts = CreateCosts(controls);
            yieldRow = E(controls, "row fields-yield");
            var output = E(yieldRow, "row fields-yield-output"); yieldIcon = ToolkitGameplay.Icon(output, null, 14);
            yieldAmount = L(output, "", "fields-yield-amount");
            secondaryReward = E(output, "row fields-yield-secondary"); secondaryReward.name = "fields-secondary-reward";
            secondaryIcon = ToolkitGameplay.Icon(secondaryReward, null, 14);
            secondaryAmount = L(secondaryReward, "", "fields-yield-amount");
            yieldXp = L(yieldRow, "", "muted fields-yield-xp");
            detail = L(controls, "", "muted fields-selection-detail"); detail.name = "fields-selected-status";
            var primaryActions = E(controls, "row fields-primary-actions");
            plant = Button(primaryActions, "", PlantSelected); plant.name = "fields-plant-selected"; plant.AddToClassList("primary");
            buildSelected = Button(primaryActions, "", () => Build(FindBuild(source.CapturePresentation(), selectedBed), selectedBed)); buildSelected.AddToClassList("primary");
            var tending = E(controls, "row fields-harvest-actions");
            repeat = Button(tending, "", ToggleRepeat); repeat.name = "fields-repeat-selected";
            var mainAction = E(tending, "fields-main-action");
            water = Button(mainAction, "", () => { if (source is FarmService service) Execute(() => service.Water(selectedBed)); }); water.name = "fields-water-selected";
            water.AddToClassList("primary");
            harvest = Button(mainAction, "", () => { if (source is FarmService service) Execute(() => service.Harvest(selectedBed)); });
            harvest.name = "fields-harvest-selected"; harvest.AddToClassList("primary");
            seedColumn = E(drawer, "column fields-seeds-column"); seedHeading = L(seedColumn, "", "heading");
            seedScroll = ToolkitControls.RecessedScroll(seedColumn, "fields-seeds", theme, compact: true);
            seedGrid = E(seedScroll, "row fields-seed-grid");
            foreach (var recipe in snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>())
            {
                if (recipe == null) continue;
                var id = recipe.Id; var button = Button(seedGrid, "", () => SelectRecipe(id)); button.name = "fields-seed-" + id;
                button.AddToClassList("fields-seed-card");
                var content = E(button, "row fields-seed-content");
                var icon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("icon"); content.Add(icon);
                var count = L(content, "", "fields-seed-count"); count.name = "seed-quantity";
                seeds[id] = new SeedCard { Button = button, Count = count, Icon = icon };
            }
            selectorTiles = bedList.Children().ToArray();
            source.Changed += Refresh; ToolkitLocalization.Changed += Refresh;
            layout.Fill(root, theme, CompanionWidth); ApplyLayout(); Refresh(); return true;
        }
        private static VisualElement E(VisualElement parent, string classes) => ToolkitGameplay.E(parent, classes);
        private static Label L(VisualElement parent, string text, string classes) => ToolkitGameplay.L(parent, text, classes);
        private static Button Button(VisualElement parent, string text, Action action)
        { var button = ToolkitControls.Button("", action, null); button.text = text; parent.Add(button); return button; }
        private void SetWaterLabel(Button button, string text, bool showIcon)
        {
            var row = button.Q<VisualElement>(className: "fields-water-content");
            if (!showIcon) { Visible(row, false); button.text = text; return; }
            if (row == null)
            {
                row = E(button, "row fields-water-content"); row.pickingMode = PickingMode.Ignore;
                var icon = new Image { sprite = wateringCan, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("fields-water-icon"); row.Add(icon);
                L(row, "", "fields-water-label").pickingMode = PickingMode.Ignore;
            }
            button.text = ""; Visible(row, true); row.Q<Label>().text = text;
        }
        private static void Visible(VisualElement element, bool visible) { if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }
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
        private static void RefreshLevelRequirement(Label label, FarmBuildPresentation build, int level)
        {
            Visible(label, build != null && !build.Completed);
            label.text = build == null ? "" : ToolkitLocalization.Text("fields.requires-lvl", "Requires lvl {0}", build.TwinsLevel);
            label.EnableInClassList("fields-shortfall", build != null && level < build.TwinsLevel);
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
        private BedTile CreateBed(FarmBedPresentation bed, VisualElement parent, bool register = true)
        {
            var id = bed.Id; var tile = new BedTile { Orchard = bed.Orchard, Preview = !register };
            tile.Root = E(parent, bed.Orchard ? "column fields-orchard-slot" : "column fields-plot"); tile.Root.name = "fields-plot-" + id;
            if (register && !bed.Orchard) tile.Root.AddToClassList("fields-selector-tile");
            tile.Select = Button(tile.Root, "", () => SelectBed(id)); tile.Select.name = "fields-select-" + id; tile.Select.AddToClassList("fields-plot-select");
            var plotHeader = E(tile.Select, "row fields-plot-heading");
            tile.Title = L(plotHeader, "", "fields-plot-title");
            tile.Requirement = L(plotHeader, "", "fields-level-requirement");
            var body = E(tile.Select, "row fields-plot-body"); tile.Art = E(body, "fields-plot-art"); tile.Art.name = "bed-art";
            var size = bed.Orchard ? 2 : 3;
            tile.Soil = new Image[size * size]; tile.WetSoils = new Image[size * size];
            tile.Plants = new Image[bed.Orchard ? 1 : 9];
            for (var i = 0; i < tile.Soil.Length; i++)
            {
                var spot = new VisualElement { pickingMode = PickingMode.Ignore }; tile.Art.Add(spot);
                spot.style.position = Position.Absolute;
                spot.style.left = (bed.Orchard ? 4 : 0) + i % size * 8;
                spot.style.top = (bed.Orchard ? 16 : register ? 8 : 0) + i / size * 8;
                spot.style.width = 8; spot.style.height = 8;
                tile.Soil[i] = CreateArtImage(spot); tile.WetSoils[i] = CreateArtImage(spot);
                foreach (var image in new[] { tile.Soil[i], tile.WetSoils[i] })
                { image.style.width = 8; image.style.height = 8; image.style.bottom = 0; }
                if (!bed.Orchard) tile.Plants[i] = CreateArtImage(spot);
            }
            if (bed.Orchard) tile.Plants[0] = CreateArtImage(tile.Art);
            var info = E(body, "column fields-plot-info");
            var caption = register ? info : E(info, "row fields-preview-heading");
            tile.Status = L(caption, "", "fields-plot-status"); tile.Status.name = "bed-status";
            if (!register) tile.ProgressStatus = L(caption, "", "fields-progress-status");
            tile.Fill = ToolkitGameplay.Bar(info); tile.Fill.name = "bed-progress";
            if (register) beds[id] = tile;
            else { tile.Root.name = "fields-selected-preview"; tile.Select.name = "fields-preview-select-" + id; tile.Select.pickingMode = PickingMode.Ignore; }
            return tile;
        }
        private static Image CreateArtImage(VisualElement parent)
        {
            var image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute; parent.Add(image); return image;
        }
        private static void PlaceCropSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            if (!sprite) return;
            // Each soil cell is eight reference UI pixels wide. Align crop pivots
            // to its centre so every growth stage stays grounded within the field.
            var scale = 8f / sprite.pixelsPerUnit;
            image.style.width = sprite.rect.width * scale;
            image.style.height = sprite.rect.height * scale;
            image.style.left = 4 - sprite.pivot.x * scale;
            image.style.top = 4 - (sprite.rect.height - sprite.pivot.y) * scale;
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
            var recipe = snapshot.Recipes.FirstOrDefault(r => r.Id == selectedRecipe);
            if (bed != null && (bed.Planted || recipe == null || recipe.Orchard != bed.Orchard))
                selectedRecipe = bed.RecipeId ?? snapshot.Recipes.FirstOrDefault(r => r.Orchard == bed.Orchard && r.Discovered && r.Eligible)?.Id;
            Refresh();
        }
        private void SelectRecipe(string id)
        {
            var snapshot = source.CapturePresentation();
            if (snapshot.Recipes.FirstOrDefault(r => r.Id == id)?.Discovered != true) return;
            var target = snapshot.Beds.FirstOrDefault(b => b.Id == selectedBed);
            if (target?.Planted == true) return;
            selectedRecipe = id; var recipe = snapshot.Recipes.FirstOrDefault(r => r.Id == id);
            if (recipe != null && (target == null || target.Orchard != recipe.Orchard))
                selectedBed = snapshot.Beds.FirstOrDefault(b => b.Unlocked && !b.Planted && b.Orchard == recipe.Orchard)?.Id
                    ?? snapshot.Beds.FirstOrDefault(b => b.Orchard == recipe.Orchard)?.Id;
            Refresh();
        }
        private void Execute(Func<bool> command)
        {
            if (source?.Ready != true) return;
            var before = source.CapturePresentation();
            if (command())
            {
                var after = source.CapturePresentation();
                var harvested = before.Beds.Where(b => b.Ready && after.Beds.Any(a => a.Id == b.Id && !a.Ready)).ToArray();
                if (harvested.Length > 0)
                {
                    feedback = string.Join(" · ", harvested.Select(b => "+" + Compact(b.ExpectedYield) + " " + b.YieldTitle +
                        (b.SecondaryExpectedYield > 0 ? " · +" + Compact(b.SecondaryExpectedYield) + " " + b.SecondaryTitle : ""))) +
                        " · +" + harvested.Sum(b => b.HarvestXp) + " Fields XP";
                    feedbackUntil = Time.unscaledTime + 6;
                }
            }
            Refresh();
        }
        private static string Compact(double value) => System.Text.RegularExpressions.Regex.Replace(FormatNumber(value, hideDecimal: true), "<[^>]+>", "");
        private void Build(FarmBuildPresentation build, string id)
        {
            if (build?.CanTurnIn != true || source is not FarmService service) return;
            var manager = QuestManager.Instance;
            if (manager) manager.RefreshFieldsProgression();
            if (manager ? manager.TryTurnInQuest(build.QuestId) : service.BuildQuest(build.QuestId)) SelectBed(id);
            Refresh();
        }
        private void PlantSelected()
        {
            if (!plant.enabledInHierarchy || source is not FarmService service || selectedBed == null) return;
            if (selectedRecipe != null) Execute(() => service.Plant(selectedBed, selectedRecipe));
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
            feedbackLabel.text = feedback ?? "";
            feedbackLabel.style.visibility = Time.unscaledTime < feedbackUntil ? Visibility.Visible : Visibility.Hidden;
            selectedBed ??= snapshot.Beds.FirstOrDefault(b => b.Unlocked)?.Id ?? snapshot.Beds.FirstOrDefault()?.Id;
            var target = snapshot.Beds.FirstOrDefault(b => b.Id == selectedBed); var orchard = target?.Orchard == true;
            selectedRecipe ??= snapshot.Recipes.FirstOrDefault(r => r.Orchard == orchard && r.Discovered && r.Eligible)?.Id;
            var selected = snapshot.Recipes.FirstOrDefault(r => r.Id == selectedRecipe);
            heading.text = ToolkitLocalization.Text("fields.level-heading", "Fields | Level {0}", snapshot.TwinsLevel);
            xp.text = ToolkitLocalization.Text("fields.xp", "{0} / {1} XP", snapshot.TwinsXp, snapshot.TwinsXpRequired);
            xpBar.style.width = Length.Percent(Mathf.Clamp01((float)snapshot.TwinsXp / Math.Max(1, snapshot.TwinsXpRequired)) * 100);
            foreach (var bed in snapshot.Beds)
            {
                if (!beds.TryGetValue(bed.Id, out var tile)) continue;
                var build = FindBuild(snapshot, bed.Id);
                tile.Root.EnableInClassList("selected", selectedBed == bed.Id);
                tile.Root.EnableInClassList("fields-locked", !bed.Unlocked);
                tile.Root.EnableInClassList("fields-ready", bed.Ready);
                tile.Title.text = "";
                Visible(tile.Title.parent, !bed.Unlocked);
                tile.Status.text = BedStatus(bed);
                RefreshLevelRequirement(tile.Requirement, bed.Unlocked ? null : build, snapshot.TwinsLevel);
                tile.Select.tooltip = bed.Unlocked ? bed.Title + "\n" + BedStatus(bed) : bed.Title + "\n" + build?.Status;
                Visible(tile.Fill.parent, bed.Unlocked);
                tile.Fill.style.width = Length.Percent(bed.Planted ? Mathf.Clamp01(bed.Progress01) * 100 : 0);
                RefreshArt(tile, bed);
            }
            foreach (var pair in orchardPairs)
            {
                var first = snapshot.Beds.First(b => b.Id == pair.First);
                var second = snapshot.Beds.FirstOrDefault(b => b.Id == pair.Second);
                var unlocked = first.Unlocked || second?.Unlocked == true;
                var build = FindBuild(snapshot, pair.First);
                pair.Root.EnableInClassList("selected", selectedBed == pair.First || selectedBed == pair.Second);
                pair.Root.EnableInClassList("fields-locked", !unlocked);
                pair.Title.text = ToolkitLocalization.Text("fields.plot-pair", "Plot {0}–{1}", Array.IndexOf(FarmCommands.OrchardBeds, pair.First) + 1, Array.IndexOf(FarmCommands.OrchardBeds, pair.Second) + 1);
                Visible(pair.Title.parent, false); Visible(pair.Slots, unlocked); Visible(pair.LockedSelect, !unlocked);
                RefreshLevelRequirement(pair.Requirement, unlocked ? null : build, snapshot.TwinsLevel);
                pair.LockedSelect.tooltip = pair.Title.text + "\n" + build?.Status;
            }
            var available = snapshot.Recipes.Where(r => r.Orchard == orchard).ToArray();
            var seedContent = (source as FarmService)?.Content ?? FarmContent.Load();
            var selectedDefinition = seedContent.Recipe(selected?.Id);
            var dryTime = selectedDefinition == null ? 0 : seedContent.GrowthDuration(selectedDefinition, snapshot.TwinsLevel);
            var growthTime = target?.Planted == true && target.RecipeId == selected?.Id && target.GrowthSeconds > 0 ? target.GrowthSeconds : dryTime;
            seedHeading.text = selected == null ? "" : selected.CropTitle + " • " + FormatTime(growthTime, mspace: false, shortForm: true);
            seedHeading.tooltip = selected == null ? "" : "Dry: " + FormatTime(dryTime, mspace: false, shortForm: true) + " · Watered: " + FormatTime(dryTime / 2, mspace: false, shortForm: true);
            var columns = narrow ? 4 : 5;
            var seedIndex = 0;
            // Leave a pixel for the button border and fractional layout rounding,
            // otherwise the last seed wraps and creates a third row at some scales.
            var cardWidth = Mathf.Floor((seedGrid.contentRect.width - (columns - 1) * 4) / columns) - 1;
            foreach (var recipe in snapshot.Recipes)
            {
                if (!seeds.TryGetValue(recipe.Id, out var card)) continue;
                var show = recipe.Orchard == orchard;
                Visible(card.Button, show);
                card.Button.SetEnabled(recipe.Discovered && target?.Planted != true);
                card.Button.style.width = cardWidth > 0 ? new Length(cardWidth) : Length.Percent(100f / columns - 1.1f);
                card.Button.style.marginRight = show && ++seedIndex % columns != 0 ? 4 : 0;
                card.Icon.sprite = recipe.Discovered ? recipe.Icon : recipe.UnknownIcon ? recipe.UnknownIcon : unknownPack;
                card.Count.text = recipe.Discovered ? Compact(recipe.SeedQuantity) : "—";
                card.Button.EnableInClassList("primary", recipe.Discovered && selectedRecipe == recipe.Id); card.Button.EnableInClassList("fields-undiscovered", !recipe.Discovered);
                card.Button.tooltip = recipe.Discovered ? RecipeRequirement(recipe) + "\n" + PlantCost(orchard) : T("fields.discover-first", "Discover this seed or sapling first.");
            }
            var seedRows = (available.Length + columns - 1) / columns;
            var seedHeight = seedRows * (ToolkitResponsiveLayout.IsPortrait ? 47 : 35) + 4;
            seedScroll.style.height = seedHeight; seedScroll.style.maxHeight = seedHeight;
            seedScroll.parent.style.height = seedHeight; seedScroll.parent.style.maxHeight = seedHeight;
            var selectedBuild = target?.Unlocked == false ? FindBuild(snapshot, target.Id) : null;
            var matches = target != null && selected != null && target.Orchard == selected.Orchard;
            var canPlant = snapshot.TownActionsAllowed && target?.Unlocked == true && !target.Planted && matches && selected.Discovered && selected.Eligible && selected.SeedQuantity >= FarmContent.PlantingCost(orchard);
            RefreshCosts(selectedCosts, selectedBuild);
            // Costs remain visible here even when the player's Fields level is too low.
            for (var i = 0; i < selectedCosts.Counts.Length; i++)
            {
                var material = selectedBuild?.Materials?.ElementAtOrDefault(i);
                if (material != null) selectedCosts.Counts[i].text = Compact(material.Owned) + " / " + Compact(material.Required);
            }
            RefreshLevelRequirement(buildRequirement, selectedBuild, snapshot.TwinsLevel);
            Visible(yieldRow, target?.Planted == true);
            yieldIcon.sprite = target?.Icon;
            yieldAmount.text = target == null ? "" : Compact(target.ExpectedYield);
            Visible(secondaryReward, target?.SecondaryExpectedYield > 0);
            secondaryIcon.sprite = target?.SecondaryIcon;
            secondaryAmount.text = target == null ? "" : Compact(target.SecondaryExpectedYield);
            secondaryReward.tooltip = target?.SecondaryTitle;
            yieldXp.text = target == null ? "" : "+" + target.HarvestXp + " XP";
            yieldRow.tooltip = target?.YieldTitle;
            detail.text = selectedBuild != null || target?.Planted == true ? "" : !matches ? T("fields.find-seeds", "Find seed packs by completing crop tasks on adventures.") : !selected.Discovered ? T("fields.discover-first", "Discover this seed or sapling first.") : !selected.Eligible ?
                RecipeRequirement(selected) : "";
            Visible(detail, detail.text.Length > 0);
            Visible(buildSelected, selectedBuild != null); buildSelected.name = "fields-build-selected";
            buildSelected.text = T("fields.construct", "Construct"); buildSelected.SetEnabled(selectedBuild?.CanTurnIn == true);
            Visible(plant, selectedBuild == null && target?.Planted != true);
            plant.SetEnabled(canPlant);
            plant.text = T("fields.action.plant", "Plant");
            var needsWater = target?.Unlocked == true && target.Planted && !target.Ready && !target.Watered;
            Visible(water, needsWater);
            Visible(harvest, !needsWater);
            water.SetEnabled(snapshot.TownActionsAllowed && target?.Watered == false);
            SetWaterLabel(water, T("fields.action.water-once", "Water"), true);
            repeat.SetEnabled(snapshot.TownActionsAllowed && target?.Unlocked == true && snapshot.TwinsLevel >= 20);
            repeat.EnableInClassList("primary", target?.Repeat == true);
            repeat.text = T("fields.auto", "Auto") + " • " + (target?.Repeat == true ? T("common.on", "On") : T("common.off", "Off"));
            repeat.tooltip = T("fields.repeat-help-paid-trees", "Fields level 20: harvest automatically, then replant using one matching seed pack or sapling. Stops when you run out.");
            Visible(preview.Root, !orchard && selectedBuild == null); Visible(orchardPreview.Root, orchard && selectedBuild == null);
            if (target != null)
            {
                var selectedPreview = orchard ? orchardPreview : preview;
                var previewInfo = selectedPreview.Fill.parent.parent;
                if (yieldRow.parent != previewInfo) previewInfo.Add(yieldRow);
                selectedPreview.Title.text = "";
                selectedPreview.Status.text = target.Planted ? target.RecipeTitle : selected?.CropTitle ?? T("fields.empty", "Empty");
                selectedPreview.ProgressStatus.text = target.Ready ? T("fields.ready", "Ready") : FormatTime(Math.Max(0, target.RemainingSeconds), mspace: false, shortForm: true);
                Visible(selectedPreview.ProgressStatus, target.Planted);
                Visible(selectedPreview.Requirement, false);
                RefreshArt(selectedPreview, target);
                Visible(selectedPreview.Fill.parent, target.Planted); selectedPreview.Fill.style.width = Length.Percent(target.Progress01 * 100);
                selectedPreview.Root.EnableInClassList("fields-ready", target.Ready);
            }
            Visible(seedColumn, target?.Unlocked == true);
            harvest.SetEnabled(snapshot.TownActionsAllowed && target?.Ready == true);
            harvest.text = target?.Timber == true ? T("fields.action.chop", "Chop") : T("fields.action.harvest", "Harvest");
        }
        private void RefreshArt(BedTile tile, FarmBedPresentation bed)
        {
            var recipe = (source as FarmService)?.Content.Recipe(bed.RecipeId); var stage = FarmView.GrowthStage(bed.Progress01, bed.Ready);
            var sprite = !bed.Planted ? null : recipe?.stages?.Length == 4 ? recipe.stages[stage] : bed.Orchard && stage < 3 && appearance ? appearance.treeStages[stage] : bed.Icon;
            var size = bed.Orchard ? 2 : 3;
            var previewCropInset = tile.Preview && !bed.Orchard && sprite ? PreviewCropInset(sprite) : 0;
            for (var i = 0; i < tile.Soil.Length; i++)
            {
                var x = i % size; var y = i / size;
                if (tile.Preview && !bed.Orchard)
                    tile.Soil[i].parent.style.top = previewCropInset + y * 8;
                var corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                var index = corner ? 0 : y == 0 || y == size - 1 ? 1 : x == 0 || x == size - 1 ? 2 : 3;
                var flip = new Scale(new Vector3((index == 0 || index == 2) && x == size - 1 ? -1 : 1,
                    (index == 0 || index == 1) && y == size - 1 ? -1 : 1, 1));
                tile.Soil[i].sprite = appearance ? appearance.dry[index] : null;
                tile.WetSoils[i].sprite = appearance ? appearance.wet[index] : null;
                tile.Soil[i].style.scale = tile.WetSoils[i].style.scale = flip;
                Visible(tile.WetSoils[i], bed.Watered);
            }
            for (var i = 0; i < tile.Plants.Length; i++)
            {
                tile.Plants[i].sprite = sprite; Visible(tile.Plants[i], sprite);
                if (!bed.Orchard) PlaceCropSprite(tile.Plants[i], sprite);
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
        private float PreviewCropInset(Sprite sprite)
        {
            if (previewCropInsets.TryGetValue(sprite, out var inset)) return inset;
            // Use the authored outline to exclude transparent space above the plant.
            var top = (sprite.rect.height - sprite.pivot.y) / sprite.pixelsPerUnit;
            if (sprite.GetPhysicsShapeCount() > 0)
            {
                top = float.NegativeInfinity;
                for (var shape = 0; shape < sprite.GetPhysicsShapeCount(); shape++)
                {
                    previewOutline.Clear();
                    sprite.GetPhysicsShape(shape, previewOutline);
                    foreach (var point in previewOutline) top = Mathf.Max(top, point.y);
                }
            }
            inset = Mathf.Max(0, top * 8 - 4);
            previewCropInsets[sprite] = inset;
            return inset;
        }
        private static string T(string key, string fallback) => ToolkitLocalization.Text(key, fallback);
        private static string RecipeRequirement(FarmRecipePresentation recipe) => ToolkitLocalization.Text("fields.recipe-skill-required", "{0} · {1} {2} required", recipe.Title, recipe.RequiredSkill, recipe.RequiredHeroLevel);
        private static string PlantCost(bool orchard) => ToolkitLocalization.Text(orchard ? "fields.plant-sapling-cost" : "fields.plant-seed-cost", orchard ? "Requires {0} matching sapling per plot." : "Requires {0} matching seed pack per field.", FarmContent.PlantingCost(orchard));
        private static string BedStatus(FarmBedPresentation bed) => !bed.Unlocked ? T("fields.locked", "Locked · construction required") : !bed.Planted ? T("fields.empty", "Empty") : bed.RecipeTitle + "\n" + (bed.Ready ? T("fields.ready", "Ready") : FormatTime(Math.Max(0, bed.RemainingSeconds), mspace: false, shortForm: true));
        private void ApplyLayout()
        {
            narrow = ToolkitResponsiveLayout.IsPortrait || ToolkitWindowLayout.SafeArea.width - CompanionWidth < 500;
            root.EnableInClassList("fields-narrow", narrow);
            // The selector stays visible first on phones; desktop keeps details on the left.
            var first = narrow ? bedList : drawer;
            if (board.IndexOf(first) != 0) board.Insert(0, first);
            for (var i = 0; i < selectorTiles.Length; i++)
            {
                selectorTiles[i].style.marginRight = i % 3 == 2 ? 0 : 4;
            }
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (appearance) {
                var flora = appearance.floraPortrait?.At(Time.time); if (flora) floraPortrait.sprite = flora;
                var tillman = appearance.tillmanPortrait?.At(Time.time); if (tillman) tillmanPortrait.sprite = tillman;
            }
            layout.Fill(root, theme, CompanionWidth); ApplyLayout();
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
