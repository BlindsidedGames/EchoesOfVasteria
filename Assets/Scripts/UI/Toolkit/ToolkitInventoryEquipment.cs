using System.Collections.Generic;

using System.Linq;

using TimelessEchoes.Gear;

using TimelessEchoes.Upgrades;

using UnityEngine;

using UnityEngine.UIElements;



namespace TimelessEchoes.UI.Toolkit

{

    public sealed partial class ToolkitResourceInventoryScreen

    {

        private EquipmentController equipmentController;

        private VisualElement equipmentContent, equipmentColumn;

        private string selectedGearSlot = "Boots";

        private readonly Dictionary<string, (Button button, Image icon)> gearSlots = new();



        private void BuildEquipment(VisualElement frame)

        {

            equipmentController = EquipmentController.Instance;

            if (!equipmentController || !definition.equipmentArt) return;

            frame.style.flexDirection = FlexDirection.Row;

            scroll.style.flexGrow = 0; scroll.style.flexShrink = 0; scroll.style.minWidth = 40;

            var column = equipmentColumn = new VisualElement { name = "inventory-equipment" };

            column.style.flexGrow = 1; column.style.flexBasis = 0;

            column.style.minWidth = 130; column.style.paddingLeft = 8; column.style.flexShrink = 0;

            frame.Add(column);
            frame.RegisterCallback<GeometryChangedEvent>(_ => FitEquipmentColumns());

            ToolkitGameplay.L(column, ToolkitLocalization.Text("inventory.equipped", "Equipped items"), "heading");

            var buttons = new VisualElement(); buttons.style.flexDirection = FlexDirection.Row;

            buttons.style.flexWrap = Wrap.Wrap; column.Add(buttons);

            foreach (var slot in equipmentController.Slots)

            {

                var key = slot;

                var button = ToolkitGameplay.B(buttons, "", () => { selectedGearSlot = key; RefreshEquipment(); });

                button.name = "inventory-gear-" + slot; button.tooltip = slot;

                button.style.width = button.style.height = 30; button.style.marginRight = 3;

                var icon = ToolkitControls.Icon(null); icon.style.width = icon.style.height = 20;

                button.Add(icon); gearSlots.Add(slot, (button, icon));

            }

            var details = new ScrollView(ScrollViewMode.Vertical) { name = "inventory-equipment-scroll" };

            details.style.flexGrow = 1; details.style.minHeight = 0;

            theme.StyleScroll(details); ToolkitGameplay.StyleScroll(details); column.Add(details);

            equipmentContent = details.contentContainer;

            equipmentController.OnEquipmentChanged += Changed;

        }



        private void FitEquipmentColumns()
        {
            if (equipmentColumn == null || grid == null) return;
            var available = equipmentColumn.parent.contentRect.width;
            if (available <= 0 || scroll.layout.width <= 0) return;
            var chrome = Mathf.Max(0, scroll.layout.width - scroll.contentViewport.contentRect.width);
            var equipmentWidth = Mathf.Clamp(available * .42f, 130, 240);
            grid.FitWidth(Mathf.Max(40, available - equipmentWidth - chrome), 14);
            scroll.style.width = grid.ContentWidth + chrome;
        }

        private void RefreshEquipment()

        {

            if (!equipmentController || equipmentContent == null) return;

            foreach (var pair in gearSlots)

            {

                var item = equipmentController.GetEquipped(pair.Key);

                pair.Value.icon.sprite = definition.equipmentArt.GearSprite(item, pair.Key);

                pair.Value.button.EnableInClassList("active", pair.Key == selectedGearSlot);

                pair.Value.button.tooltip = item != null ? item.GetName() : pair.Key + " - " + ToolkitLocalization.Text("inventory.empty-slot", "Nothing equipped");

            }

            equipmentContent.Clear();

            var selectedItem = equipmentController.GetEquipped(selectedGearSlot);

            var heading = selectedItem == null ? selectedGearSlot : selectedItem.GetName();

            ToolkitGameplay.L(equipmentContent, heading, "heading");

            var selectedAffixes = selectedItem?.affixes?.Where(a => a?.stat != null).ToList() ?? new List<GearAffix>();

            if (selectedItem == null) ToolkitGameplay.L(equipmentContent, ToolkitLocalization.Text("inventory.empty-slot", "Nothing equipped"));

            foreach (var affix in selectedAffixes) EquipmentStat(affix.stat, affix.value);

            ToolkitGameplay.L(equipmentContent, ToolkitLocalization.Text("inventory.equipment-total", "Total equipment bonuses"), "heading");

            var all = equipmentController.Slots.Select(equipmentController.GetEquipped).Where(i => i != null)

                .SelectMany(i => i.affixes).Where(a => a?.stat != null).ToList();

            var stats = all.Select(a => a.stat).GroupBy(s => s.heroMapping).Select(g => g.First()).ToList();

            stats.Sort((a,b) => StatSortOrder.Compare(a.heroMapping,b.heroMapping));

            foreach (var stat in stats) EquipmentStat(stat, all.Where(a => a.stat.heroMapping == stat.heroMapping).Sum(a => a.value));

            if (all.Count == 0) ToolkitGameplay.L(equipmentContent, ToolkitLocalization.Text("inventory.no-equipment-bonuses", "Equip an item to gain its bonuses."));

        }



        private void EquipmentStat(StatDefSO stat, float value)

        {

            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;

            row.style.alignItems = Align.Center; row.style.marginBottom = 3; row.style.paddingLeft = 6; row.style.paddingRight = 4; equipmentContent.Add(row);

            if (StatIconLookup.TryGetIcon(stat.heroMapping, out var sprite))

            {

                var icon = ToolkitControls.Icon(sprite); icon.style.width = icon.style.height = 10;

                icon.style.marginRight = 4; row.Add(icon);

            }

            var label = ToolkitGameplay.L(row, stat.GetName()); label.style.flexGrow = 1;

            ToolkitGameplay.L(row, "+" + value.ToString("0.##") + (stat.isPercent ? "%" : ""));

        }

    }

}

