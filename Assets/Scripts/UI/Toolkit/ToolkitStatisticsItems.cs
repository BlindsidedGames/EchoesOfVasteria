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
        private sealed class ItemRow { public VisualElement root, iconFrame; public Image icon; public Label title, totals, detail; }
        private void BuildItems()
        {
            items ??= new ItemStatisticsPresentation(); resourceManager = ResourceManager.Instance; itemRows.Clear(); itemSortButtons.Clear();
             body.style.paddingTop = 3; body.style.paddingBottom = 3; body.style.paddingLeft = 3; body.style.paddingRight = 3;
            var inset = new VisualElement(); inset.style.flexGrow = 1; inset.style.minHeight = 0; inset.style.paddingLeft = 2; inset.style.paddingRight = 2; inset.style.paddingTop = 2; inset.style.paddingBottom = 2;  body.Add(inset);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "item-statistics", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-buffs-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); inset.Add(scroll); itemContent = scroll.contentContainer; itemContent.style.flexDirection = FlexDirection.Row; itemContent.style.flexWrap = Wrap.Wrap;
            foreach (var resource in items.Ordered(itemSort, resourceManager))
            {
                var row = new ItemRow(); row.root = new VisualElement { name = "item-stat-" + resource.name }; row.root.AddToClassList("eov-stat-item");  itemContent.Add(row.root);
                row.iconFrame = new VisualElement(); row.iconFrame.AddToClassList("eov-stat-item-icon-frame"); row.root.Add(row.iconFrame);
                var mask = new VisualElement(); mask.AddToClassList("eov-stat-item-icon-mask");  row.iconFrame.Add(mask);
                row.icon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; row.icon.style.position = Position.Absolute; mask.Add(row.icon);
                var text = new VisualElement(); text.style.flexGrow = 1; text.style.flexBasis = 0; text.style.flexDirection = FlexDirection.Row; row.root.Add(text);
                var left = Column(text); row.title = Text(left, "", 6); row.title.style.height=StyleKeyword.Auto; row.totals = Text(left, "", 4.7f);
                text.style.justifyContent = Justify.SpaceBetween;
                left.style.flexGrow = 0; left.style.flexBasis = Length.Percent(33.3333f);
                var right = Column(text); right.style.flexGrow = 0; right.style.flexBasis = Length.Percent(33.3333f); var id = Text(right, "#" + resource.resourceID, 6); id.style.height=StyleKeyword.Auto; id.style.unityTextAlign = TextAnchor.UpperRight; row.detail = Text(right, "", 4.5f);
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
            var width = (itemContent.contentRect.width - 6) / 2;
            if (width <= 0) return;
            var index = 0;
            foreach (var row in itemContent.Children()) { row.style.width = width; row.style.marginRight = index++ % 2 == 1 ? 0 : 6; }
        }
        public void SetItemSort(ItemStatsPanelUI.SortMode mode) { itemSort = mode; if (IsOpen && selectedTab == 5) RefreshItems(); }
        private void RefreshItems()
        {
            var ordered = items.Ordered(itemSort, resourceManager);
            foreach (var resource in ordered)
            {
                var row = itemRows[resource]; row.root.BringToFront(); var data = items.Describe(resource, resourceManager, definition.itemTiers.Length);
                row.title.text = data.name; row.totals.text = "<line-height=6.1>" + data.totals + "</line-height>"; row.detail.text = "<line-height=6.1>" + data.detail + "</line-height>";
                row.icon.sprite = data.icon; row.icon.style.display = data.icon ? DisplayStyle.Flex : DisplayStyle.None;
                if (data.icon) { var size = data.icon.rect.size * (16f / data.icon.pixelsPerUnit); row.icon.style.width = size.x; row.icon.style.height = size.y; row.icon.style.left = (24 - size.x) * .5f; row.icon.style.top = (23 - size.y) * .5f; }
                if (definition.itemTiers.Length > 0) ToolkitTheme.Background(row.iconFrame, definition.itemTiers[Mathf.Clamp(data.tier - 1, 0, definition.itemTiers.Length - 1)]);
            }
            for (var i = 0; i < itemSortButtons.Count; i++) StyleSelection(itemSortButtons[i], i == (int)itemSort); SizeItems();
        }
        private void ResourceChanged(Resource _, double __, bool ___) => InventoryChanged();
        private void ResourceTierChanged(Resource _, int __) => InventoryChanged();
    }
}
