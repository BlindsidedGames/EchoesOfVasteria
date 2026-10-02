using System;
using System.Collections.Generic;
using System.Globalization;
using TimelessEchoes.Farming;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native bed list; commands are delegated to the durable farm service.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitFarmScreen : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sourceBehaviour;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private Sprite knownPack;
        [SerializeField] private Sprite unknownPack;
        [SerializeField] private Sprite cropIcon = null;
        private IFarmPresentationSource source;
        private PanelSettings settings;
        private VisualElement root;
        private ScrollView scroll;
        private ScrollView narrowScroll;
        private Image seedIcon;
        private Label seedLabel, seedQuantity, seedDescription, bedSummary, buildCost, error, townStatus;
        private VisualElement workspace, inventory, bedColumn;
        private VisualElement preparation;
        private Button prepare, harvest;
        private readonly List<BedRow> rows = new();
        private readonly ToolkitWindowLayout layout = new();
        private float nextRefresh;
        private bool? stackedLayout;
        private float previousRowsHeight = -1;

        private sealed class BedRow
        {
            public string Id;
            public VisualElement Root, Progress, Planting;
            public Label Title, Status, PlantCost;
            public Image Crop;
            public Button Plant, Focus;
        }

        public float CompanionWidth { get; set; } = 0;
        public bool IsOpen => root != null;
        public bool IsConfigured => theme && runtimeTheme && textSettings && knownPack && unknownPack && (source != null || sourceBehaviour is IFarmPresentationSource);

        public void Configure(IFarmPresentationSource presentationSource, ToolkitTheme toolkitTheme,
            ThemeStyleSheet stylesheet, PanelTextSettings panelTextSettings, Sprite discoveredPack, Sprite undiscoveredPack)
        {
            Hide();
            source = presentationSource;
            sourceBehaviour = presentationSource as MonoBehaviour;
            theme = toolkitTheme;
            runtimeTheme = stylesheet;
            textSettings = panelTextSettings;
            knownPack = discoveredPack;
            unknownPack = undiscoveredPack;
        }

        public bool Show()
        {
            if (IsOpen) return true;
            source ??= sourceBehaviour as IFarmPresentationSource;
            if (!IsConfigured || !source.Ready) return false;
            if (!settings)
            {
                settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                settings.sortingOrder = 100;
            }
            var document = GetComponent<UIDocument>();
            document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "farm" };
            theme.Apply(root);
            ToolkitGameplay.Apply(root, theme);
            root.AddToClassList("menu-surface");
            root.AddToClassList("quests-reviewed");
            document.rootVisualElement.Add(root);
            ToolkitGameplay.L(root, "Farm", "quests-heading");
            townStatus = ToolkitGameplay.L(root, "Return to town to tend the beds.", "muted");
            townStatus.style.marginBottom = 8;
            error = ToolkitGameplay.L(root, "", "status");
            preparation = ToolkitGameplay.E(root, "eov-quest-row");
            ToolkitGameplay.L(preparation, "Barkley · Prepare two beds", "heading");
            buildCost = ToolkitGameplay.L(preparation, "", "muted");
            buildCost.style.marginBottom = 4;
            prepare = ToolkitControls.Button("farm-prepare-beds", () => Execute(source.PrepareBeds), null);
            prepare.text = "Prepare beds";
            prepare.style.alignSelf = Align.FlexStart;
            preparation.Add(prepare);
            narrowScroll = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "farm-narrow-scroll",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            narrowScroll.style.display = DisplayStyle.None;
            narrowScroll.style.flexGrow = 1;
            narrowScroll.style.minHeight = 0;
            narrowScroll.AddToClassList("eov-scroll");
            theme.StyleScroll(narrowScroll);
            ToolkitGameplay.StyleScroll(narrowScroll);
            root.Add(narrowScroll);
            workspace = ToolkitGameplay.E(root, "row workspace");
            inventory = ToolkitGameplay.E(workspace, "column");
            inventory.name = "farm-seed-inventory";
            inventory.style.width = 144;
            inventory.style.flexShrink = 0;
            inventory.style.marginRight = 12;
            ToolkitGameplay.L(inventory, "Seed inventory", "heading");
            var seedCard = ToolkitGameplay.E(inventory, "surface");
            Frame(seedCard);
            seedCard.style.flexShrink = 0;
            seedCard.style.paddingLeft = seedCard.style.paddingRight = 8;
            seedCard.style.paddingTop = seedCard.style.paddingBottom = 8;
            var seedTop = ToolkitGameplay.E(seedCard, "row between");
            seedIcon = ToolkitGameplay.Icon(seedTop, unknownPack, 40);
            var amount = ToolkitGameplay.E(seedTop, "grow");
            seedQuantity = ToolkitGameplay.L(amount, "", "amount");
            ToolkitGameplay.L(amount, "packs", "muted");
            seedLabel = ToolkitGameplay.L(seedCard, "", "heading");
            seedLabel.style.marginTop = 8;
            seedDescription = ToolkitGameplay.L(seedCard, "", "muted");
            bedColumn = ToolkitGameplay.E(workspace, "column grow");
            bedColumn.style.flexBasis = 0;
            var bedHeading = ToolkitGameplay.E(bedColumn, "row between");
            bedHeading.style.marginBottom = 8;
            var bedTitles = ToolkitGameplay.E(bedHeading, "grow");
            ToolkitGameplay.L(bedTitles, "Crop beds", "heading");
            bedSummary = ToolkitGameplay.L(bedTitles, "", "muted");
            harvest = ToolkitControls.Button("farm-harvest-ready", () => Execute(source.HarvestReady), null);
            harvest.text = "Harvest all ready";
            harvest.AddToClassList("primary");
            harvest.style.marginLeft = 8;
            bedHeading.Add(harvest);
            scroll = ToolkitControls.RecessedScroll(bedColumn, "farm-beds", theme);
            source.Changed += Refresh;
            Refresh();
            layout.Fill(root, theme, CompanionWidth);
            return true;
        }

        private void Execute(Func<bool> command)
        {
            if (source == null || !source.Ready) return;
            // Recheck in the command owner; button state is only presentation.
            command();
            Refresh();
        }

        private void CreateRows(FarmBedPresentation[] beds)
        {
            scroll.Clear();
            rows.Clear();
            previousRowsHeight = -1;
            foreach (var bed in beds)
            {
                if (bed == null || string.IsNullOrEmpty(bed.Id)) continue;
                var id = bed.Id;
                var row = new BedRow { Id = id, Root = ToolkitGameplay.E(scroll, "surface") };
                row.Root.name = "farm-bed-" + id;
                Frame(row.Root);
                row.Root.style.marginBottom = 8;
                row.Root.style.paddingLeft = row.Root.style.paddingRight = 8;
                row.Root.style.paddingTop = row.Root.style.paddingBottom = 8;
                var top = ToolkitGameplay.E(row.Root, "row between");
                var title = ToolkitGameplay.E(top, "grow");
                row.Title = ToolkitGameplay.L(title, bed.Title ?? id, "heading quest-name");
                row.Status = ToolkitGameplay.L(title, "", "");
                row.Crop = ToolkitGameplay.Icon(top, cropIcon, 28);
                row.Crop.style.marginRight = 0;
                row.Progress = ToolkitGameplay.Bar(row.Root);
                row.Progress.parent.style.height = 4;
                row.Progress.parent.style.marginTop = 4;
                row.Progress.parent.style.marginBottom = 8;
                var controls = ToolkitGameplay.E(row.Root, "row");
                controls.style.alignItems = Align.Center;
                controls.style.flexWrap = Wrap.Wrap;
                controls.style.marginTop = 8;
                row.Planting = ToolkitGameplay.E(controls, "row grow");
                row.Planting.style.alignItems = Align.Center;
                row.Planting.style.flexWrap = Wrap.Wrap;
                row.Planting.style.minWidth = 96;
                row.Plant = ToolkitControls.Button("farm-plant-" + id, () => Execute(() => source.Plant(id)), null);
                row.Plant.AddToClassList("primary");
                row.Plant.style.marginRight = 8;
                row.Planting.Add(row.Plant);
                row.PlantCost = ToolkitGameplay.L(row.Planting, "1 seed pack", "muted");
                row.Focus = ToolkitControls.Button("farm-focus-" + id, () => source.Focus(id), null);
                row.Focus.text = "View bed";
                row.Focus.AddToClassList("quiet");
                row.Focus.style.marginLeft = StyleKeyword.Auto;
                controls.Add(row.Focus);
                rows.Add(row);
            }
        }

        public void Refresh()
        {
            if (!IsOpen || source == null) return;
            var snapshot = source.Ready ? source.CapturePresentation() : null;
            if (snapshot == null)
            {
                prepare.SetEnabled(false);
                harvest.SetEnabled(false);
                foreach (var row in rows) { row.Plant.SetEnabled(false); row.Focus.SetEnabled(false); }
                error.text = source.LastError ?? string.Empty;
                error.style.display = string.IsNullOrEmpty(error.text) ? DisplayStyle.None : DisplayStyle.Flex;
                return;
            }
            var beds = snapshot.Beds ?? Array.Empty<FarmBedPresentation>();
            var rebuild = rows.Count != beds.Length;
            if (!rebuild)
                for (var i = 0; i < beds.Length; i++)
                    if (beds[i] == null || rows[i].Id != beds[i].Id) { rebuild = true; break; }
            if (rebuild) CreateRows(beds);
            seedIcon.sprite = snapshot.Discovered ? knownPack : unknownPack;
            seedLabel.text = snapshot.Discovered ? "Radish seeds" : "Undiscovered";
            seedQuantity.text = snapshot.Discovered ? snapshot.SeedQuantity.ToString(CultureInfo.InvariantCulture) : "—";
            seedDescription.text = snapshot.Discovered ? "1 pack plants 1 bed." : "No seed pack discovered yet.";
            seedLabel.tooltip = snapshot.Discovered ? "Radish seed packs" : "Undiscovered";
            preparation.style.display = snapshot.Prepared ? DisplayStyle.None : DisplayStyle.Flex;
            buildCost.text = Number(snapshot.LogQuantity) + "/" + Number(snapshot.BuildLogCost) + " Logs   ·   " + Number(snapshot.StickQuantity) + "/" + Number(snapshot.BuildStickCost) + " Sticks";
            prepare.SetEnabled(snapshot.TownActionsAllowed && !snapshot.Prepared && snapshot.LogQuantity >= snapshot.BuildLogCost && snapshot.StickQuantity >= snapshot.BuildStickCost);
            townStatus.style.display = snapshot.TownActionsAllowed ? DisplayStyle.None : DisplayStyle.Flex;
            var readyCount = 0;
            var plantedCount = 0;
            foreach (var row in rows)
            {
                var bed = Array.Find(beds, value => value != null && value.Id == row.Id);
                if (bed == null) continue;
                row.Title.text = bed.Title ?? bed.Id;
                row.Status.text = !snapshot.Prepared ? "Not prepared" : !bed.Planted ? "Empty · ready to plant" : bed.Ready ? "Radish · Ready to harvest" : "Radish · " + FormatTime(Math.Max(0, bed.RemainingSeconds), mspace: false, shortForm: true) + " remaining";
                row.Progress.style.width = Length.Percent(Mathf.Clamp01(bed.Progress01) * 100);
                row.Progress.parent.style.display = snapshot.Prepared && bed.Planted ? DisplayStyle.Flex : DisplayStyle.None;
                row.Root.EnableInClassList("quest-ready", bed.Ready);
                row.Title.EnableInClassList("quest-name", bed.Ready);
                row.Crop.style.display = bed.Planted && snapshot.Discovered && cropIcon ? DisplayStyle.Flex : DisplayStyle.None;
                row.Planting.style.display = snapshot.Prepared && !bed.Planted ? DisplayStyle.Flex : DisplayStyle.None;
                row.Plant.text = snapshot.Discovered ? "Plant Radish" : "Plant";
                row.PlantCost.text = !snapshot.Discovered ? "Discover seeds first" : snapshot.SeedQuantity == 0 ? "No packs owned" : "1 seed pack";
                row.Plant.SetEnabled(snapshot.TownActionsAllowed && snapshot.Prepared && !bed.Planted && snapshot.Discovered && snapshot.SeedQuantity > 0);
                row.Focus.SetEnabled(snapshot.TownActionsAllowed);
                if (bed.Planted) plantedCount++;
                if (bed.Ready && bed.Planted) readyCount++;
            }
            bedSummary.text = readyCount > 0 ? readyCount + " of " + beds.Length + " ready to harvest" : plantedCount > 0 ? plantedCount + " of " + beds.Length + " growing" : snapshot.Prepared ? beds.Length + " beds available" : "Prepare the beds to begin";
            harvest.SetEnabled(snapshot.TownActionsAllowed && readyCount > 0);
            error.text = source.LastError ?? string.Empty;
            error.style.display = string.IsNullOrEmpty(error.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Match the field and thin divider colours used by the native Forge/Quest panels.
        private static void Frame(VisualElement element)
        {
            element.style.backgroundColor = (Color)new Color32(56, 46, 49, 255);
            element.style.borderLeftWidth = element.style.borderRightWidth = .5f;
            element.style.borderTopWidth = element.style.borderBottomWidth = .5f;
            Color line = new Color32(116, 81, 74, 255);
            element.style.borderLeftColor = element.style.borderRightColor = line;
            element.style.borderTopColor = element.style.borderBottomColor = line;
        }

        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private void LayoutColumns()
        {
            if (workspace == null || inventory == null || narrowScroll == null) return;
            if (root.contentRect.width <= 0) return;
            var stacked = root.contentRect.width < 410;
            if (stackedLayout != stacked)
            {
                // Change hierarchy only on a mode transition, outside a geometry callback.
                // Updating it while the panel is laying out can recursively invalidate it.
                if (stacked && workspace.parent != narrowScroll.contentContainer)
                {
                    preparation.RemoveFromHierarchy();
                    workspace.RemoveFromHierarchy();
                    narrowScroll.Add(preparation);
                    narrowScroll.Add(workspace);
                }
                else if (!stacked && workspace.parent != root)
                {
                    preparation.RemoveFromHierarchy();
                    workspace.RemoveFromHierarchy();
                    root.Insert(root.IndexOf(narrowScroll), preparation);
                    root.Add(workspace);
                }
                narrowScroll.style.display = stacked ? DisplayStyle.Flex : DisplayStyle.None;
                workspace.style.flexGrow = stacked ? 0 : 1;
                workspace.style.flexShrink = stacked ? 0 : 1;
                workspace.style.flexBasis = stacked ? StyleKeyword.Auto : new StyleLength(0f);
                workspace.style.flexDirection = stacked ? FlexDirection.Column : FlexDirection.Row;
                inventory.style.width = stacked ? StyleKeyword.Auto : new StyleLength(144);
                inventory.style.marginRight = stacked ? 0 : 12;
                inventory.style.marginBottom = stacked ? 12 : 0;
                bedColumn.style.flexGrow = stacked ? 0 : 1;
                bedColumn.style.flexShrink = stacked ? 0 : 1;
                bedColumn.style.flexBasis = stacked ? StyleKeyword.Auto : new StyleLength(0f);
                scroll.parent.style.flexGrow = stacked ? 0 : 1;
                scroll.parent.style.flexShrink = stacked ? 0 : 1;
                scroll.verticalScrollerVisibility = stacked ? ScrollerVisibility.Hidden : ScrollerVisibility.Auto;
                if (!stacked) scroll.style.height = StyleKeyword.Auto;
                stackedLayout = stacked;
                previousRowsHeight = -1;
            }
            if (!stacked) return;
            // Measure the rows, not the viewport/container whose height this controls.
            // The latter forms a feedback loop through ScrollView's minimum viewport size.
            var height = 0f;
            foreach (var row in rows) height += Mathf.Max(0, row.Root.layout.height) + 8;
            if (float.IsNaN(height) || float.IsInfinity(height)) return;
            if (Mathf.Abs(previousRowsHeight - height) < .5f) return;
            scroll.style.height = height;
            previousRowsHeight = height;
        }

        private void Update()
        {
            if (!IsOpen) return;
            layout.Fill(root, theme, CompanionWidth);
            LayoutColumns();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .25f;
            Refresh();
        }

        public void Hide()
        {
            if (source != null) source.Changed -= Refresh;
            root?.RemoveFromHierarchy();
            root = null;
            scroll = null;
            narrowScroll = null;
            workspace = inventory = bedColumn = null;
            stackedLayout = null;
            previousRowsHeight = -1;
            rows.Clear();
        }

        private void OnDisable() => Hide();
        private void OnDestroy()
        {
            Hide();
            if (settings) Destroy(settings);
        }
    }
}
