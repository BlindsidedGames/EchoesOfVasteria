using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TimelessEchoes.Farming;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>One combined Fields workspace. All economic actions remain owned by the durable service.</summary>
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
        private VisualElement root, columns, seedGrid, bedList, orchardList, roadmap, xpBar, seedColumn, bedColumn, townColumn;
        private Foldout orchard;
        private Label heading, xp, error, selection, status;
        private Image townImage;
        private Button plant, harvest;
        private readonly ToolkitWindowLayout layout = new();
        private string selectedRecipe, selectedBed;
        private float nextRefresh;
        private string presentationKey;
        private bool pointerDown;
        private Camera previewCamera;
        private RenderTexture previewTexture;
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
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "farm" }; theme.Apply(root); ToolkitGameplay.Apply(root, theme);
            root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            root.RegisterCallback<PointerDownEvent>(_ => pointerDown = true, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(_ => pointerDown = false, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(_ => pointerDown = false);
            var header = ToolkitGameplay.E(root, "row between");
            heading = ToolkitGameplay.L(header, "Fields · Flora & Tillman", "quests-heading");
            xp = ToolkitGameplay.L(header, "", "muted"); xpBar = ToolkitGameplay.Bar(root);
            xpBar.parent.style.height = 4; xpBar.parent.style.marginBottom = 6;
            status = ToolkitGameplay.L(root, "", "muted"); error = ToolkitGameplay.L(root, "", "status");
            columns = ToolkitGameplay.E(root, "row workspace"); columns.style.flexGrow = 1; columns.style.minHeight = 0;
            seedColumn = ToolkitGameplay.E(columns, "column"); seedColumn.style.flexShrink = 0;
            ToolkitGameplay.L(seedColumn, "Seeds & saplings", "heading");
            var seeds = ToolkitControls.RecessedScroll(seedColumn, "fields-seeds", theme, compact: true);
            seeds.parent.style.flexGrow = 1; seeds.parent.style.minHeight = 0;
            seeds.parent.style.paddingLeft = seeds.parent.style.paddingRight = 2;
            seeds.style.flexGrow = 1; seeds.style.minHeight = 0;
            seedGrid = ToolkitGameplay.E(seeds, "row"); seedGrid.style.flexWrap = Wrap.Wrap;
            bedColumn = ToolkitGameplay.E(columns, "column grow"); bedColumn.style.minWidth = 220;
            var controls = ToolkitGameplay.E(bedColumn, "row between"); ToolkitGameplay.L(controls, "Beds", "heading");
            harvest = Button(controls, "Harvest ready", () => Execute(source.HarvestReady)); harvest.name = "farm-harvest-ready";
            var beds = ToolkitControls.RecessedScroll(bedColumn, "fields-beds", theme, compact: true);
            beds.parent.style.flexGrow = 1; beds.parent.style.minHeight = 0;
            beds.style.flexGrow = 1; beds.style.minHeight = 0;
            bedList = ToolkitGameplay.E(beds, "column");
            orchard = new Foldout { text = "Orchard plots", name = "fields-orchard", value = false }; beds.Add(orchard);
            orchardList = ToolkitGameplay.E(orchard, "column");
            var planting = ToolkitGameplay.E(bedColumn, "row between"); planting.style.flexWrap = Wrap.Wrap;
            selection = ToolkitGameplay.L(planting, "Select seeds and an empty bed", "muted"); selection.name = "fields-seed-selection"; selection.style.flexShrink = 1; selection.style.whiteSpace = WhiteSpace.Normal;
            plant = Button(planting, "Plant", PlantSelected); plant.name = "fields-plant-selected"; plant.AddToClassList("primary");
            townColumn = ToolkitGameplay.E(columns, "column"); townColumn.style.flexShrink = 0;
            ToolkitGameplay.L(townColumn, "Fields in town", "heading");
            townImage = new Image { name = "fields-town-preview", scaleMode = ScaleMode.ScaleToFit };
            townColumn.Add(townImage);
            Button(townColumn, "Focus Fields", () => source.Focus(selectedBed ?? FarmCommands.WestBedId));
            ToolkitGameplay.L(townColumn, "10 Twins XP per harvest. Growth while playing.", "muted");
            var future = new Foldout { text = "Construction", value = false }; townColumn.Add(future);
            var plans = ToolkitControls.RecessedScroll(future, "fields-construction", theme, compact: true);
            plans.parent.style.maxHeight = 150;
            roadmap = ToolkitGameplay.E(plans, "column");
            source.Changed += Refresh; CreatePreview(); Refresh(); layout.Fill(root, theme, CompanionWidth); return true;
        }
        private static Button Button(VisualElement parent, string text, Action action)
        {
            var button = ToolkitControls.Button("fields-" + text.Replace(' ', '-').ToLowerInvariant(), action, null);
            button.text = text; parent.Add(button); return button;
        }
        private void Execute(Func<bool> command) { if (source?.Ready == true) { command(); Refresh(); } }
        private void PlantSelected()
        {
            if (source is FarmService service && selectedBed != null && selectedRecipe != null)
                Execute(() => service.Plant(selectedBed, selectedRecipe));
        }
        public void Refresh()
        {
            if (!IsOpen || source == null) return;
            var snapshot = source.Ready ? source.CapturePresentation() : null;
            error.text = source.LastError ?? ""; error.style.display = error.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (snapshot == null) { plant.SetEnabled(false); harvest.SetEnabled(false); return; }
            heading.text = snapshot.DisplayName + " · Flora & Tillman · Level " + snapshot.TwinsLevel;
            xp.text = snapshot.TwinsXp + " / " + snapshot.TwinsXpRequired + " XP";
            xpBar.style.width = Length.Percent(Mathf.Clamp01((float)snapshot.TwinsXp / Math.Max(1, snapshot.TwinsXpRequired)) * 100);
            status.text = snapshot.TownActionsAllowed ? "Yield ×" + snapshot.YieldMultiplier.ToString("0.00") + " · Growth time ×" + snapshot.SpeedFactor.ToString("0.0") : "Return to town to tend Fields.";
            FramePreview(snapshot);
            var key = PresentationKey(snapshot);
            if (presentationKey == key)
            {
                foreach (var bed in snapshot.Beds ?? Array.Empty<FarmBedPresentation>())
                {
                    if (bed == null) continue;
                    var row = columns.Q<VisualElement>("farm-bed-" + bed.Id);
                    var label = row?.Q<Label>("bed-status");
                    if (label != null) label.text = BedStatus(bed);
                    var fill = row?.Q<VisualElement>("bed-progress");
                    if (fill != null) fill.style.width = Length.Percent(Mathf.Clamp01(bed.Progress01) * 100);
                }
                return;
            }
            if (pointerDown) return;
            presentationKey = key;
            seedGrid.Clear();
            foreach (var recipe in snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>())
            {
                if (recipe == null) continue;
                var id = recipe.Id;
                var card = Button(seedGrid, "", () => { selectedRecipe = id; Refresh(); });
                card.name = "fields-seed-" + id; card.style.width = 82; card.style.height = 70; card.style.flexShrink = 0;
                card.style.marginRight = 2; card.style.marginBottom = 3;
                card.style.paddingLeft = card.style.paddingRight = 1;
                card.style.flexDirection = FlexDirection.Column; card.style.alignItems = Align.Center;
                ToolkitGameplay.Icon(card, recipe.Discovered ? recipe.Icon : recipe.UnknownIcon ? recipe.UnknownIcon : unknownPack, 24);
                var caption = ToolkitGameplay.L(card, recipe.Discovered ? recipe.Title : "???", "");
                caption.name = "seed-caption"; caption.style.fontSize = 8; caption.style.width = 76;
                caption.style.minHeight = 20; caption.style.whiteSpace = WhiteSpace.Normal;
                caption.style.unityTextAlign = TextAnchor.MiddleCenter;
                caption.tooltip = recipe.Discovered ? recipe.Title : "Discover this seed or sapling first.";
                ToolkitGameplay.L(card, recipe.Discovered ? recipe.SeedQuantity.ToString("0.##", CultureInfo.InvariantCulture) : "—", "muted");
                card.EnableInClassList("primary", selectedRecipe == id);
                card.tooltip = recipe.Discovered ? recipe.Title + " · Farming " + recipe.RequiredHeroLevel + (recipe.Eligible ? "" : " required") : "Discover this seed or sapling first.";
            }
            bedList.Clear(); orchardList.Clear(); var ready = 0;
            foreach (var bed in snapshot.Beds ?? Array.Empty<FarmBedPresentation>())
            {
                if (bed == null) continue;
                var id = bed.Id; var row = ToolkitGameplay.E(bed.Orchard ? orchardList : bedList, "surface column");
                row.name = "farm-bed-" + id; row.style.paddingTop = row.style.paddingBottom = 3;
                row.style.marginBottom = 3; row.EnableInClassList("quest-ready", bed.Ready || selectedBed == id);
                var top = ToolkitGameplay.E(row, "row between");
                var description = ToolkitGameplay.E(top, "grow");
                var bedTitle = ToolkitGameplay.L(description, bed.Title, "heading"); bedTitle.style.fontSize = 10;
                var label = ToolkitGameplay.L(description, BedStatus(bed), "muted"); label.name = "bed-status";
                if (bed.Planted && bed.Icon) ToolkitGameplay.Icon(top, bed.Icon, 24).name = "bed-crop-icon";
                var select = Button(top, "Select", () => { selectedBed = id; Refresh(); }); select.SetEnabled(bed.Unlocked);
                if (bed.Planted)
                {
                    var fill = ToolkitGameplay.Bar(row); fill.name = "bed-progress"; fill.style.width = Length.Percent(Mathf.Clamp01(bed.Progress01) * 100);
                    var actions = ToolkitGameplay.E(row, "row");
                    actions.style.display = selectedBed == id ? DisplayStyle.Flex : DisplayStyle.None;
                    actions.style.flexWrap = Wrap.Wrap;
                    if (source is FarmService service)
                    {
                        var water = Button(actions, bed.Watered ? "Watered" : "Water once", () => Execute(() => service.Water(id)));
                        water.SetEnabled(snapshot.TownActionsAllowed && !bed.Watered && !bed.Ready);
                        var repeat = Button(actions, bed.Repeat ? "Repeat: on" : "Repeat: off", () => Execute(() => service.SetRepeat(id, !bed.Repeat)));
                        repeat.SetEnabled(snapshot.TownActionsAllowed && (snapshot.TwinsLevel >= 20 || bed.Repeat));
                        repeat.tooltip = "Twins level 20: harvest and replant using one matching seed or sapling. Stops when inputs run out.";
                    }
                    Button(actions, "Focus", () => source.Focus(id));
                    if (bed.Ready) ready++;
                }
            }
            var selected = Array.Find(snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>(), r => r?.Id == selectedRecipe);
            var target = Array.Find(snapshot.Beds ?? Array.Empty<FarmBedPresentation>(), b => b?.Id == selectedBed);
            selection.text = selected == null ? "Select seeds and an empty bed" : (selected.Discovered ? selected.Title : "Undiscovered") +
                (target == null ? " · Select an empty bed" : " → " + target.Title);
            plant.SetEnabled(snapshot.TownActionsAllowed && selected?.Eligible == true && selected.Discovered && selected.SeedQuantity >= 1 && target?.Unlocked == true && !target.Planted && target.Orchard == selected.Orchard);
            harvest.SetEnabled(snapshot.TownActionsAllowed && ready > 0);
            roadmap.Clear();
            foreach (var build in snapshot.Builds ?? Array.Empty<FarmBuildPresentation>())
            {
                if (build == null) continue;
                var row = ToolkitGameplay.E(roadmap, "row between"); var detail = ToolkitGameplay.E(row, "grow");
                ToolkitGameplay.L(detail, build.Title + " · Twins " + build.TwinsLevel, "heading");
                ToolkitGameplay.L(detail, build.Completed ? "Completed" : build.Status + " · " + build.Costs, "muted");
                var questId = build.QuestId;
                var handin = Button(row, "Hand in", () => { QuestManager.Instance?.TryTurnInQuest(questId); Refresh(); });
                handin.SetEnabled(snapshot.TownActionsAllowed && build.CanTurnIn && !build.Completed);
            }
        }
        private static string BedStatus(FarmBedPresentation bed) => !bed.Unlocked ? "Locked · construction required" : !bed.Planted ? "Empty" : bed.RecipeTitle + " · " + (bed.Ready ? "Ready" : FormatTime(Math.Max(0, bed.RemainingSeconds), mspace: false, shortForm: true));
        private string PresentationKey(FarmPresentationSnapshot snapshot)
        {
            var key = new StringBuilder(); key.Append(selectedRecipe).Append('|').Append(selectedBed).Append('|').Append(snapshot.TownActionsAllowed).Append('|').Append(snapshot.TwinsLevel);
            foreach (var recipe in snapshot.Recipes ?? Array.Empty<FarmRecipePresentation>())
                if (recipe != null) key.Append('|').Append(recipe.Id).Append(':').Append(recipe.Discovered).Append(':').Append(recipe.Eligible).Append(':').Append(recipe.SeedQuantity).Append(':').Append(recipe.Title);
            foreach (var bed in snapshot.Beds ?? Array.Empty<FarmBedPresentation>())
                if (bed != null) key.Append('|').Append(bed.Id).Append(':').Append(bed.Unlocked).Append(':').Append(bed.Planted).Append(':').Append(bed.Ready).Append(':').Append(bed.Watered).Append(':').Append(bed.Repeat).Append(':').Append(bed.RecipeId);
            foreach (var build in snapshot.Builds ?? Array.Empty<FarmBuildPresentation>())
                if (build != null) key.Append('|').Append(build.QuestId).Append(':').Append(build.Completed).Append(':').Append(build.CanTurnIn).Append(':').Append(build.Status).Append(':').Append(build.Costs);
            return key.ToString();
        }
        private void CreatePreview()
        {
            var camera = Camera.main; if (!camera) return;
            previewTexture = new RenderTexture(560, 640, 16) { name = "Fields town preview" };
            previewTexture.Create();
            var host = new GameObject("Fields preview camera") { hideFlags = HideFlags.DontSave };
            previewCamera = host.AddComponent<Camera>(); previewCamera.CopyFrom(camera);
            previewCamera.targetTexture = previewTexture; previewCamera.tag = "Untagged";
            previewCamera.orthographic = true; previewCamera.orthographicSize = 19;
            previewCamera.transform.position = new Vector3(-57, -13, camera.transform.position.z);
            previewCamera.transform.rotation = camera.transform.rotation;
            previewCamera.rect = new Rect(0, 0, 1, 1); previewCamera.enabled = true;
            townImage.image = previewTexture;
        }
        private void FramePreview(FarmPresentationSnapshot snapshot)
        {
            if (!previewCamera) return;
            var bounds = new Bounds(); var any = false;
            foreach (var bed in snapshot.Beds ?? Array.Empty<FarmBedPresentation>())
            {
                if (bed?.Unlocked != true) continue;
                var index = Array.IndexOf(bed.Orchard ? FarmCommands.OrchardBeds : FarmCommands.GardenBeds, bed.Id);
                if (index < 0) continue;
                var anchor = bed.Orchard ? FieldsWorldView.OrchardAnchors[index] : FieldsWorldView.GardenAnchors[index] + new Vector2(1,1);
                var plot = new Bounds(new Vector3(anchor.x,anchor.y,0), bed.Orchard ? new Vector3(2,3,0) : new Vector3(4,4,0));
                if (!any) { bounds = plot; any = true; } else bounds.Encapsulate(plot);
            }
            if (!any) bounds = new Bounds(new Vector3(-59,2.5f,0),new Vector3(10,7,0));
            // Show unlocked plots together, with room for their trees and enclosure frontage.
            var size = Mathf.Max(3.5f, bounds.extents.y + 1.5f, (bounds.extents.x + 1.5f) / previewCamera.aspect);
            previewCamera.orthographicSize = size;
            previewCamera.transform.position = new Vector3(bounds.center.x,bounds.center.y,previewCamera.transform.position.z);
        }
        private void Update()
        {
            if (!IsOpen) return;
            layout.Fill(root, theme, CompanionWidth);
            var wide = root.contentRect.width >= 600;
            columns.style.flexDirection = wide ? FlexDirection.Row : FlexDirection.Column;
            seedColumn.style.width = 180;
            seedColumn.style.marginRight = wide ? 8 : 0;
            bedColumn.style.marginRight = wide ? 8 : 0;
            bedColumn.style.minHeight = wide ? 0 : 220;
            townColumn.style.width = wide ? 164 : 172;
            townImage.style.width = wide ? 164 : 172; townImage.style.height = wide ? 188 : 196;
            if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .25f; Refresh();
        }
        public void Hide()
        {
            if (source != null) source.Changed -= Refresh;
            root?.RemoveFromHierarchy(); root = null; presentationKey = null; pointerDown = false;
            if (previewCamera) { previewCamera.targetTexture = null; Destroy(previewCamera.gameObject); }
            previewCamera = null;
            if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
            previewTexture = null;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
