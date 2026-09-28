using System.Collections.Generic;
using Blindsided.Utilities;
using TimelessEchoes.NpcGeneration;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitAlterEchoesScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitAlterEchoesDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, content;
        private Label power;
        private Button collectAll;
        private AlterEchoGenerationManager manager;
        private readonly List<Row> rows = new();
        private bool rebuild;
        private float displayedPower = float.NaN;
        private bool? collectAllEnabled;
        private sealed class Row
        {
            public AlterEchoGenerator generator;
            public Label collected, rate, pending;
            public Button collect;
            public VisualElement fill;
            public double total = double.NaN, stored = double.NaN, cycle = double.NaN;
            public float interval = float.NaN, cardMultiplier = float.NaN, progress = float.NaN;
            public bool? enabled;
        }
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;
        public float CompanionWidth { get; set; } = 178;
        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = AlterEchoGenerationManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "alter-echoes" }; root.AddToClassList("eov-alter-echoes"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            var header = new VisualElement(); header.AddToClassList("eov-alter-header"); root.Add(header);
            var space = new VisualElement(); space.style.flexGrow = 1; header.Add(space);
            collectAll = Action(header, "collect-all", "Collect All", CollectAll); collectAll.style.width = 70; collectAll.style.height = 16;
            var powerFrame = new VisualElement();  powerFrame.AddToClassList("eov-alter-power-frame"); header.Add(powerFrame);
            power = Text(powerFrame, "", 7); power.name = "echo-power"; displayedPower = float.NaN; collectAllEnabled = null;
            var inset = new VisualElement(); inset.AddToClassList("eov-alter-inset");  root.Add(inset);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "generators", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-scroll"); scroll.AddToClassList("eov-alter-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); inset.Add(scroll); content = scroll.contentContainer;
            manager.OnGeneratorsRebuilt += Rebuild;
            Blindsided.EventHandler.OnQuestHandin += QuestChanged; Blindsided.EventHandler.OnLoadData += Rebuild;
            BuildRows(); Layout(); Refresh(); return true;
        }
        private void BuildRows()
        {
            content.Clear(); rows.Clear();
            foreach (var generator in manager.Generators)
            {
                if (!generator || !generator.RequirementsMet || !generator.Resource) continue;
                var row = new Row { generator = generator };
                var frame = new VisualElement { name = "generator-" + generator.Resource.name }; frame.AddToClassList("eov-alter-row");  content.Add(frame);
                var horizontal = new VisualElement(); horizontal.AddToClassList("eov-alter-row-main"); frame.Add(horizontal);
                var labels = new VisualElement(); labels.AddToClassList("eov-alter-row-labels"); horizontal.Add(labels);
                var name = Text(labels, generator.Resource.name, 7); name.name = "resource-name"; name.style.height = StyleKeyword.Auto;
                row.collected = Text(labels, "", 5); row.collected.name = "total-collected"; row.collected.style.height = StyleKeyword.Auto; row.collected.style.marginTop = -1;
                row.rate = Text(labels, "", 5); row.rate.name = "collection-rate"; row.rate.style.height = StyleKeyword.Auto; row.rate.style.marginTop = -1;
                var resource = generator.Resource;
                var iconButton = ToolkitBuffsScreen.MakeButton("highlight-" + resource.name, () => TownWindowManager.Instance?.TryHighlightNativeResource(resource, true), definition.slot);
                iconButton.AddToClassList("eov-alter-resource"); horizontal.Add(iconButton);
                var icon = new Image { sprite = resource.icon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("eov-alter-resource-icon"); iconButton.Add(icon);
                row.pending = Text(iconButton, "", 5.85f); row.pending.name = "pending-resources"; row.pending.AddToClassList("eov-alter-pending"); row.pending.style.color = StyleKeyword.Null;
                row.collect = Action(horizontal, "collect-" + resource.name, "Collect", () => generator.CollectResources()); row.collect.style.width = 50; row.collect.style.height = 20;
                var track = new VisualElement(); track.AddToClassList("eov-alter-track"); track.AddToClassList("track"); frame.Add(track);
                row.fill = new VisualElement { pickingMode = PickingMode.Ignore }; row.fill.style.height = 4; row.fill.style.overflow = Overflow.Hidden; track.Add(row.fill);
                var sprite = new VisualElement { pickingMode = PickingMode.Ignore }; sprite.style.height = 4; sprite.AddToClassList("fill"); sprite.style.unityBackgroundImageTintColor = definition.fillColor; row.fill.Add(sprite);
                track.RegisterCallback<GeometryChangedEvent>(_ => sprite.style.width = track.contentRect.width);
                rows.Add(row);
            }
        }
        private Label Text(VisualElement parent, string value, float size)
        {
            var label = new Label(value) { pickingMode = PickingMode.Ignore }; label.AddToClassList("eov-alter-text"); label.style.fontSize = Mathf.Max(7,size); label.style.letterSpacing = size * .02f; parent.Add(label); return label;
        }
        private Button Action(VisualElement parent, string name, string text, System.Action action)
        {
            var button = ToolkitBuffsScreen.MakeButton(name, action, definition.button); button.text = "<b>" + text + "</b>"; button.style.fontSize = name == "collect-all" ? 8 : 7; button.AddToClassList("eov-alter-action"); parent.Add(button); return button;
        }
        private void CollectAll()
        {
            var resources = ResourceManager.Instance; resources?.BeginBatch();
            foreach (var generator in manager.Generators) generator?.CollectResources(false);
            resources?.EndBatch(); Blindsided.EventHandler.SaveData(); Refresh();
        }
        private void Refresh()
        {
            var canCollect = false;
            foreach (var row in rows)
            {
                var generator = row.generator; if (!generator) { rebuild = true; continue; }
                var resource = generator.Resource;
                var progress = generator.Interval > 0 ? Mathf.Clamp01(generator.Progress / generator.Interval) * 100 : 0;
                if (row.progress != progress) { row.progress = progress; row.fill.style.width = Length.Percent(progress); }
                var total = generator.GetTotalCollected(resource);
                if (row.total != total) { row.total = total; row.collected.text = CalcUtils.FormatNumber(total, true); }
                var stored = generator.GetStoredAmount(resource);
                if (row.stored != stored) { row.stored = stored; row.pending.text = CalcUtils.FormatNumber(stored, true); }
                var cauldron = CauldronManager.Instance;
                var cardMultiplier = cauldron ? cauldron.GetResourceAlterEchoMultiplier(resource.name) : 1f;
                if (row.interval != generator.Interval || row.cycle != generator.CycleAmount || row.cardMultiplier != cardMultiplier)
                {
                    row.interval = generator.Interval; row.cycle = generator.CycleAmount; row.cardMultiplier = cardMultiplier;
                    row.rate.text = AlterEchoPresentation.Rate(generator);
                }
                var available = stored > 0; canCollect |= available;
                if (row.enabled != available) { row.enabled = available; row.collect.SetEnabled(available); row.collect.style.unityBackgroundImageTintColor = available ? Color.white : definition.disabledColor; }
            }
            if (collectAllEnabled != canCollect) { collectAllEnabled = canCollect; collectAll.SetEnabled(canCollect); collectAll.style.unityBackgroundImageTintColor = canCollect ? Color.white : definition.disabledColor; }
            if (displayedPower != DisciplePercent) { displayedPower = DisciplePercent; power.text = $"Echo Power {DisciplePercent * 100f:0.#}%"; }
        }
        private void Rebuild() => rebuild = true;
        private void QuestChanged(string _) => rebuild = true;
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout() => windowLayout.Fill(root, theme, CompanionWidth);
        private void Update() { if (!IsOpen) return; Layout(); if (rebuild) { rebuild = false; BuildRows(); } Refresh(); }
        public void Hide()
        {
            if (manager) manager.OnGeneratorsRebuilt -= Rebuild;
            Blindsided.EventHandler.OnQuestHandin -= QuestChanged; Blindsided.EventHandler.OnLoadData -= Rebuild;
            root?.RemoveFromHierarchy(); root = null; rows.Clear(); rebuild = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
