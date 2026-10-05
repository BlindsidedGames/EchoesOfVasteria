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
        private Label selectedName, selectedTier;
        private VisualElement selectedRarity;
        private ResourceManager manager;
        private readonly List<(Button button, VisualElement background, Image icon, VisualElement selection, Label count)> slots = new();
        private int selected = -1;
        private int pendingScroll = -1;
        private float deselectAt = -1;
        private bool dirty;
        private bool embedded;
        private bool forgePresentation;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;
        public Rect Bounds { get; set; }
        public bool ManualLayout { get; set; }

        // Each owner keeps its own visual tree and subscriptions; only resource data is shared.
        public ToolkitResourceInventoryScreen CreateCompanionView()
        {
            // Keep UIDocument objects independent: parenting below another UIDocument
            // makes Unity inherit its PanelSettings and disallows our own assignment.
            // Quests explicitly owns and disposes this view when its window closes.
            var viewObject = new GameObject("Quests resource inventory", typeof(UIDocument));
            var view = viewObject.AddComponent<ToolkitResourceInventoryScreen>();
            view.definition = definition;
            view.theme = theme;
            view.runtimeTheme = runtimeTheme;
            view.textSettings = textSettings;
            return view;
        }

        public bool Show() => Show(false);
        private bool Show(bool inForge, bool inQuests = false)
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = ResourceManager.Instance)) return false;
            embedded = inForge || inQuests;
            forgePresentation = inForge;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "resource-inventory" }; root.AddToClassList("eov-resource-inventory"); theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface"); document.rootVisualElement.Add(root);
            if (!embedded) root.AddToClassList("inventory-standalone");
            var title = new VisualElement(); title.AddToClassList("eov-resource-title"); root.Add(title);
            if (!embedded)
            {
                var heading = ToolkitGameplay.L(title, string.Empty, "heading");
                ToolkitLocalization.Bind(heading, "forge.inventory", "Inventory");
                heading.style.flexShrink = 0;
            }
            selectedName = new Label { name = "selected-resource", pickingMode = PickingMode.Ignore }; selectedName.AddToClassList("eov-resource-name"); title.Add(selectedName);
            selectedName.text = ToolkitLocalization.Text("inventory.select-resource", "Select a resource");
            if (forgePresentation)
            {
                selectedName.text = ToolkitLocalization.Text("inventory.select-resource", "Select a resource");
                selectedRarity = new VisualElement(); selectedRarity.AddToClassList("forge-resource-rarity");
                selectedRarity.Add(new ToolkitRarityStar()); selectedTier = new Label(); selectedRarity.Add(selectedTier); title.Add(selectedRarity);
                selectedRarity.style.display = DisplayStyle.None;
            }
            if (!embedded)
            {
                selectedName.style.flexGrow = 1;
                selectedName.style.textOverflow = TextOverflow.Ellipsis;
                var close = ToolkitGameplay.B(title, "×", () => TownWindowManager.Instance?.CloseAllWindows());
                close.name = "inventory-close";
                ToolkitLocalization.BindTooltip(close, "navigation.close-tooltip", "Close window");
            }
            var frame = new VisualElement(); frame.AddToClassList("eov-resource-frame");  root.Add(frame);
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "resource-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-scroll"); scroll.AddToClassList("eov-resource-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); frame.Add(scroll);
            var cellSize = inQuests ? new Vector2(26, 32) : embedded ? new Vector2(32, 38) : new Vector2(40, 44);
            grid = new ToolkitGrid(embedded ? 6 : 14, cellSize, inQuests ? Vector2.one : new Vector2(2,2)); grid.AddToClassList("eov-resource-grid"); scroll.Add(grid);
            scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_=>grid.FitWidth(scroll.contentViewport.contentRect.width, embedded ? 6 : 14));
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
                if (!forgePresentation)
                {
                    var quality = new VisualElement { pickingMode = PickingMode.Ignore };
                    quality.AddToClassList("eov-resource-quality"); button.Add(quality);
                }
                slots.Add((button, background, icon, selection, count));
            }
            ToolkitLocalization.Changed += LocaleChanged;
            manager.OnInventoryChanged += Changed; Blindsided.EventHandler.OnLoadData += Changed;
            selected = -1; deselectAt = -1; Refresh(); Layout(); return true;
        }
        public bool ShowIn(VisualElement host) => ShowEmbedded(host, true);
        public bool ShowInQuests(VisualElement host) => ShowEmbedded(host, false);
        private bool ShowEmbedded(VisualElement host, bool inForge)
        {
            Hide();
            if (!Show(inForge, !inForge)) return false;
            root.RemoveFromHierarchy();
            root.RemoveFromClassList("menu-surface");
            root.AddToClassList(inForge ? "forge-embedded-inventory" : "quests-embedded-inventory");
            root.style.position = Position.Relative;
            root.style.left = root.style.top = 0;
            root.style.width = root.style.height = StyleKeyword.Auto;
            root.style.flexGrow = 1;
            root.style.minHeight = 0;
            host.Add(root);
            Refresh();
            return true;
        }

        private int Tier(Resource resource)
        {
            if (!manager.IsUnlocked(resource)) return 1;
            return Mathf.Max(1, forgePresentation && resource.DisableAlterEcho && definition.tierBackgrounds.Length > 0 ? definition.tierBackgrounds.Length : manager.GetTier(resource));
        }
        private static Sprite TierSprite(Sprite[] sprites, bool showTier, int tier) => sprites != null && sprites.Length > 0 ? sprites[showTier ? Mathf.Clamp(tier - 1, 0, sprites.Length - 1) : 0] : null;
        private void LocaleChanged()
        {
            if (selected >= 0)
            {
                var previousExpiry = deselectAt;
                HighlightResource(definition.resources[selected], false);
                deselectAt = previousExpiry;
            }
            else selectedName.text = ToolkitLocalization.Text("inventory.select-resource", "Select a resource");
            dirty = true;
        }
        private void Changed() => dirty = true;
        private void Refresh()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var resource = definition.resources[i]; var slot = slots[i]; var tier = Tier(resource);
                for(int t=1;t<=8;t++)slot.button.EnableInClassList("tier-"+t,t==tier);
                
                slot.icon.sprite = manager.IsUnlocked(resource) ? resource.icon : resource.UnknownIcon;
                var art = slot.icon.sprite;
                if (forgePresentation && art) { slot.icon.style.width = art.rect.width * 16 / art.pixelsPerUnit; slot.icon.style.height = art.rect.height * 16 / art.pixelsPerUnit; }
                slot.button.tooltip = manager.IsUnlocked(resource) ? ToolkitLocalization.Text("inventory.resource-tier-tooltip", "{0} · Tier {1}", ToolkitLocalization.Name(resource), tier) : ToolkitLocalization.Text("common.undiscovered", "Undiscovered");
                slot.count.text = forgePresentation ? FormatNumber(manager.GetAmount(resource), true) : "<b>" + FormatNumber(manager.GetAmount(resource), true) + "</b>";
                slot.button.EnableInClassList("resource-selected", i == selected);
                slot.selection.style.display = i == selected && !forgePresentation ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        public void HighlightResource(Resource resource, bool scrollToSlot = true)
        {
            var index = System.Array.FindIndex(definition.resources, r => r == resource || r && resource && r.name == resource.name);
            if (index < 0) return;
            if (!IsOpen && !Show()) return;
            selected = index;
            selectedName.text = manager.IsUnlocked(definition.resources[index]) ? ToolkitLocalization.Text("inventory.resource-tier", "{0} - Tier {1}", ToolkitLocalization.Name(definition.resources[index]), Tier(definition.resources[index])) : "???";
            if (forgePresentation)
            {
                var known = manager.IsUnlocked(definition.resources[index]);
                selectedName.text = known ? ToolkitLocalization.Name(definition.resources[index]) : ToolkitLocalization.Text("common.undiscovered", "Undiscovered");
                selectedRarity.style.display = known ? DisplayStyle.Flex : DisplayStyle.None;
                selectedTier.text = ToolkitLocalization.Text("inventory.tier", "Tier {0}", Tier(definition.resources[index]));
                for (int tier=1;tier<=8;tier++) selectedRarity.EnableInClassList("tier-"+tier,tier==Tier(definition.resources[index]));
            }
            FitName(); Refresh();
            if (scrollToSlot)
            {
                // A newly opened document needs its first layout before scrolling.
                pendingScroll = index;
            }
            deselectAt = !forgePresentation && definition.highlightDuration > 0 ? Time.time + definition.highlightDuration : -1;
        }
        private void FitName()
        {
            selectedName.style.fontSize = 8;
            if (embedded && !forgePresentation)
            {
                selectedName.style.fontSize = 5.87f;
                var width = selectedName.MeasureTextSize(selectedName.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                if (width > 172) selectedName.style.fontSize = 5.87f * 172 / width;
            }
        }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout() { if (!embedded) windowLayout.Apply(root, new Rect(Bounds.x + 4, Bounds.y, Mathf.Max(0, Bounds.width - 4), Bounds.height)); }
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (pendingScroll >= 0 && grid.layout.height > 0 && scroll.contentViewport.layout.height > 0)
            {
                var y = (pendingScroll / grid.Columns) * (forgePresentation ? 40 : embedded ? 33 : 46);
                scroll.scrollOffset = new Vector2(0, Mathf.Min(y, Mathf.Max(0, scroll.contentContainer.layout.height - scroll.contentViewport.layout.height)));
                pendingScroll = -1;
            }
            if (dirty) { dirty = false; Refresh(); }
            if (deselectAt >= 0 && Time.time >= deselectAt) { selected = -1; deselectAt = -1; Refresh(); }
        }
        public void Hide()
        {
            ToolkitLocalization.Changed -= LocaleChanged;
            if (manager) manager.OnInventoryChanged -= Changed;
            Blindsided.EventHandler.OnLoadData -= Changed; root?.RemoveFromHierarchy(); root = null; embedded = false; slots.Clear(); selected = -1; pendingScroll = -1; deselectAt = -1; dirty = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
