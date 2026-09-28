using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitResourceInventoryScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitResourceInventoryDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root;
        private ToolkitGrid grid;
        private ScrollView scroll;
        private Label selectedName;
        private ResourceManager manager;
        private readonly List<(Button button, VisualElement background, Image icon, VisualElement selection, Label count)> slots = new();
        private int selected = -1;
        private int pendingScroll = -1;
        private float deselectAt = -1;
        private bool dirty;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;
        public Rect Bounds { get; set; }
        public bool ManualLayout { get; set; }

        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = ResourceManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "resource-inventory" }; root.AddToClassList("eov-resource-inventory"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            var title = new VisualElement(); title.AddToClassList("eov-resource-title");  root.Add(title);
            selectedName = new Label { name = "selected-resource", pickingMode = PickingMode.Ignore }; selectedName.AddToClassList("eov-resource-name"); title.Add(selectedName);
            var frame = new VisualElement(); frame.AddToClassList("eov-resource-frame");  root.Add(frame);
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "resource-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-scroll"); scroll.AddToClassList("eov-resource-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); frame.Add(scroll);
            grid = new ToolkitGrid(6, new Vector2(26, 32), Vector2.one); grid.AddToClassList("eov-resource-grid"); scroll.Add(grid);
            scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_=>grid.FitWidth(scroll.contentViewport.contentRect.width));
            for (var i = 0; i < definition.resources.Length; i++)
            {
                var index = i;
                var button = ToolkitBuffsScreen.MakeButton("resource-" + definition.resources[i].name, () => HighlightResource(definition.resources[index], false), null);
                button.AddToClassList("eov-resource-slot"); grid.AddCell(button);
                if (i % 6 == 5) button.style.marginRight = 0;
                if (i / 6 == (definition.resources.Length - 1) / 6) button.style.marginBottom = 0;
                var background = new VisualElement { pickingMode = PickingMode.Ignore }; background.AddToClassList("eov-resource-slot-background"); button.Add(background);
                // The reference inventory deliberately uses an 18 x 17 icon rectangle.
                var icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit }; icon.AddToClassList("eov-resource-icon"); background.Add(icon);
                var selection = new VisualElement { pickingMode = PickingMode.Ignore }; selection.AddToClassList("eov-resource-selection"); selection.AddToClassList("selection-outline"); selection.style.unitySliceScale = .5f; background.Add(selection);
                var count = new Label { pickingMode = PickingMode.Ignore }; count.AddToClassList("eov-resource-count"); button.Add(count);
                slots.Add((button, background, icon, selection, count));
            }
            manager.OnInventoryChanged += Changed; Blindsided.EventHandler.OnLoadData += Changed;
            selected = -1; deselectAt = -1; Refresh(); Layout(); return true;
        }
        private int Tier(Resource resource)
        {
            if (!manager.IsUnlocked(resource)) return 1;
            return Mathf.Max(1, resource.DisableAlterEcho && definition.tierBackgrounds.Length > 0 ? definition.tierBackgrounds.Length : manager.GetTier(resource));
        }
        private static Sprite TierSprite(Sprite[] sprites, bool showTier, int tier) => sprites != null && sprites.Length > 0 ? sprites[showTier ? Mathf.Clamp(tier - 1, 0, sprites.Length - 1) : 0] : null;
        private void Changed() => dirty = true;
        private void Refresh()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var resource = definition.resources[i]; var slot = slots[i]; var tier = Tier(resource);
                for(int t=1;t<=8;t++)slot.button.EnableInClassList("tier-"+t,t==tier);
                
                slot.icon.sprite = manager.IsUnlocked(resource) ? resource.icon : resource.UnknownIcon;
                slot.count.text = "<b>" + FormatNumber(manager.GetAmount(resource), true) + "</b>";
                slot.selection.style.display = i == selected ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        public void HighlightResource(Resource resource, bool scrollToSlot = true)
        {
            var index = System.Array.FindIndex(definition.resources, r => r == resource || r && resource && r.name == resource.name);
            if (index < 0) return;
            if (!IsOpen && !Show()) return;
            selected = index;
            selectedName.text = manager.IsUnlocked(definition.resources[index]) ? definition.resources[index].name + " - Tier " + Tier(definition.resources[index]) : "???";
            FitName(); Refresh();
            if (scrollToSlot)
            {
                // A newly opened document needs its first layout before scrolling.
                pendingScroll = index;
            }
            deselectAt = definition.highlightDuration > 0 ? Time.time + definition.highlightDuration : -1;
        }
        private void FitName()
        {
            var size = 5.87f;
            selectedName.style.fontSize = size;
            var width = selectedName.MeasureTextSize(selectedName.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            if (width > 172) selectedName.style.fontSize = size * 172 / width;
        }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout() => windowLayout.Apply(root, new Rect(Bounds.x + 4, Bounds.y, Mathf.Max(0, Bounds.width - 4), Bounds.height));
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (pendingScroll >= 0 && grid.layout.height > 0 && scroll.contentViewport.layout.height > 0)
            {
                var y = (pendingScroll / grid.Columns) * 33;
                scroll.scrollOffset = new Vector2(0, Mathf.Min(y, Mathf.Max(0, scroll.contentContainer.layout.height - scroll.contentViewport.layout.height)));
                pendingScroll = -1;
            }
            if (dirty) { dirty = false; Refresh(); }
            if (deselectAt >= 0 && Time.time >= deselectAt) { selected = -1; deselectAt = -1; Refresh(); }
        }
        public void Hide()
        {
            if (manager) manager.OnInventoryChanged -= Changed;
            Blindsided.EventHandler.OnLoadData -= Changed; root?.RemoveFromHierarchy(); root = null; slots.Clear(); selected = -1; pendingScroll = -1; deselectAt = -1; dirty = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
