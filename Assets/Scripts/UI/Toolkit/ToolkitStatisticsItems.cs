using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitStatisticsScreen
    {
        private ItemStatisticsPresentation items;
        private ItemStatsPanelUI.SortMode itemSort;
        private VisualElement itemContent;
        private readonly Dictionary<Resource, ItemRow> itemRows = new();
        private readonly List<Button> itemSortButtons = new();
        private ResourceManager resourceManager;
        private sealed class ItemRow { public VisualElement root, iconFrame, rarity; public Image icon; public Label title, totals, detail, count, tier; }
        private void BuildItems()
        {
            items ??= new ItemStatisticsPresentation(); resourceManager = ResourceManager.Instance; itemRows.Clear(); itemSortButtons.Clear();
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            var scroll = ToolkitControls.RecessedScroll(body, "item-statistics", theme, definition.inset);
            itemContent = scroll.contentContainer; itemContent.style.flexDirection = FlexDirection.Row; itemContent.style.flexWrap = Wrap.Wrap;
            foreach (var resource in items.Ordered(itemSort, resourceManager))
            {
                var row = new ItemRow(); row.root = new VisualElement { name = "item-stat-" + resource.name }; row.root.AddToClassList("eov-stat-item");  itemContent.Add(row.root);
                row.iconFrame = new VisualElement(); row.iconFrame.AddToClassList("eov-stat-item-icon-frame"); row.root.Add(row.iconFrame);
                var mask = new VisualElement(); mask.AddToClassList("eov-stat-item-icon-mask");  row.iconFrame.Add(mask);
                row.icon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; row.icon.style.flexShrink = 0; mask.Add(row.icon);
                var text = ToolkitGameplay.E(row.root, "item-stat-content");
                var heading = ToolkitGameplay.E(text, "item-stat-heading");
                row.title = Text(heading, "", 6); row.title.AddToClassList("item-stat-title");
                row.rarity = ToolkitGameplay.E(heading, "item-stat-rarity");
                row.rarity.Add(new ToolkitRarityStar());
                row.tier = Text(row.rarity, "", 6);
                Text(heading, "ID: " + resource.resourceID, 6).AddToClassList("item-stat-id");
                row.count = Text(text, "", 9); row.count.AddToClassList("item-stat-count");
                var fields = ToolkitGameplay.E(text, "item-stat-fields");
                row.totals = Text(fields, "", 4.7f); row.totals.AddToClassList("item-stat-totals");
                row.detail = Text(fields, "", 4.5f); row.detail.AddToClassList("item-stat-detail");
                itemRows.Add(resource, row);
            }
            foreach (ItemStatsPanelUI.SortMode mode in Enum.GetValues(typeof(ItemStatsPanelUI.SortMode)))
                itemSortButtons.Add(Action(footer, "item-sort-" + mode, mode.ToString(), () => SetItemSort(mode)));
            itemContent.RegisterCallback<GeometryChangedEvent>(_ => SizeItems()); SizeItems(); RefreshItems();
        }
        private static VisualElement Column(VisualElement parent)
        {
            var column = new VisualElement(); column.style.flexGrow = 1; column.style.flexBasis = 0; column.style.minWidth = 0; parent.Add(column); return column;
        }
        private void SizeItems()
        {
            if (itemContent == null) return;
            var columns = itemContent.contentRect.width >= 560 ? 2 : 1;
            // Reserve one reference unit for panel pixel rounding so paired rows never overflow by a fraction.
            var width = Mathf.Floor((itemContent.contentRect.width - (columns - 1) * 8 - 1f) / columns);
            if (width <= 0) return;
            var index = 0;
            foreach (var row in itemContent.Children()) { row.style.width = width; row.style.marginRight = index++ % columns == columns - 1 ? 0 : 8; }
        }
        public void SetItemSort(ItemStatsPanelUI.SortMode mode) { itemSort = mode; if (IsOpen && selectedTab == 5) RefreshItems(); }
        private void RefreshItems()
        {
            var ordered = items.Ordered(itemSort, resourceManager);
            for (var index = 0; index < ordered.Count; index++)
            {
                var resource = ordered[index]; var row = itemRows[resource];
                if (itemContent.IndexOf(row.root) != index) itemContent.Insert(index, row.root); var data = items.Describe(resource, resourceManager, definition.itemTiers.Length);
                row.title.text = data.name; row.count.text = data.count + " available";
                row.tier.text = data.tier.ToString();
                row.rarity.style.display = data.icon && !resource.DisableAlterEcho ? DisplayStyle.Flex : DisplayStyle.None;
                row.rarity.tooltip = "Resource tier " + data.tier; row.totals.text = data.totals; row.detail.text = data.detail;
                row.icon.sprite = data.icon; row.icon.style.display = data.icon ? DisplayStyle.Flex : DisplayStyle.None;
                if (data.icon) { var size = data.icon.rect.size * (16f / data.icon.pixelsPerUnit); row.icon.style.width = size.x; row.icon.style.height = size.y; }
                for (var tier = 1; tier <= 8; tier++) row.root.EnableInClassList("tier-" + tier, data.icon && tier == Mathf.Clamp(data.tier, 1, 8));
                row.root.EnableInClassList("known", data.icon);
            }
            for (var i = 0; i < itemSortButtons.Count; i++) StyleSelection(itemSortButtons[i], i == (int)itemSort); SizeItems();
        }
        private void ResourceChanged(Resource _, double __, bool ___) => InventoryChanged();
        private void ResourceTierChanged(Resource _, int __) => InventoryChanged();
    }
}
