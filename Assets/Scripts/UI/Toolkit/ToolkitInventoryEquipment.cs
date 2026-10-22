using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Gear;
using TimelessEchoes.Characters;
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
        private readonly Dictionary<string, (Button button, ToolkitVisibilityIcon icon)> visibilityControls = new();
        private CharacterImage equipmentPreview;
        private Label equipmentCharacterName, equipmentHeading;
        private int previewDirection;
        private float nextPreviewFrame;
        private VisualElement portraitInventoryTabs;
        private bool portraitEquipmentSelected;
        private Button portraitEquipmentTab;
        private Image equipmentTutorialHand;
        private void RefreshEquipmentTutorial()
        {
            if (equipmentTutorialHand == null) return;
            var general = Blindsided.Oracle.oracle?.saveData?.General;
            var active = ToolkitResponsiveLayout.IsPortrait && !embedded && general != null &&
                general.InventoryTutorialStage > 0 && general.InventoryTutorialStage < 3 &&
                (!GameManager.Instance || GameManager.Instance.IsInTown);
            if (active && portraitEquipmentSelected)
            {
                general.InventoryTutorialStage = 3;
                active = false;
            }
            equipmentTutorialHand.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active || portraitEquipmentTab?.panel == null) return;
            var point = root.WorldToLocal(new Vector2(portraitEquipmentTab.worldBound.center.x, portraitEquipmentTab.worldBound.yMin));
            var bounce = Mathf.Round(1.5f * (1 + Mathf.Cos(Time.unscaledTime * 4)));
            equipmentTutorialHand.style.left = Mathf.Round(point.x - 12);
            equipmentTutorialHand.style.top = Mathf.Round(point.y - 26 - bounce);
            equipmentTutorialHand.style.rotate = new Rotate(Angle.Degrees(180));
            if (root.IndexOf(equipmentTutorialHand) != root.childCount - 1) equipmentTutorialHand.BringToFront();
        }

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
            portraitInventoryTabs = ToolkitGameplay.E(root, "row portrait-inventory-tabs");
            root.Add(portraitInventoryTabs);
            var resourcesTab = ToolkitGameplay.B(portraitInventoryTabs, "Resources", () => { portraitEquipmentSelected = false; FitEquipmentColumns(); });
            ToolkitLocalization.Bind(resourcesTab, "inventory.resources", "Resources");
            var equipmentTab = ToolkitGameplay.B(portraitInventoryTabs, "Equipment", () => { portraitEquipmentSelected = true; FitEquipmentColumns(); });
            equipmentTab.name = "inventory-equipment-tab";
            portraitEquipmentTab = equipmentTab;
            ToolkitLocalization.Bind(equipmentTab, "inventory.equipment", "Equipment");
            equipmentTutorialHand = new Image { name = "inventory-equipment-tutorial-hand", sprite = definition.equipmentArt.tutorialHand, pickingMode = PickingMode.Ignore };
            equipmentTutorialHand.style.position = Position.Absolute;
            equipmentTutorialHand.style.width = equipmentTutorialHand.style.height = 24;
            equipmentTutorialHand.style.display = DisplayStyle.None;
            root.Add(equipmentTutorialHand);
            frame.RegisterCallback<GeometryChangedEvent>(_ => FitEquipmentColumns());
            var header = ToolkitGameplay.E(column, "inventory-equipment-header");
            equipmentHeading = ToolkitGameplay.L(header, ToolkitLocalization.Text("inventory.equipped", "Equipped items"), "heading");
            equipmentCharacterName = ToolkitGameplay.L(header, "", "inventory-character-name"); equipmentCharacterName.enableRichText = false;
            var details = new ScrollView(ScrollViewMode.Vertical) { name = "inventory-equipment-scroll" };
            details.style.flexGrow = 1; details.style.minHeight = 0;
            theme.StyleScroll(details); ToolkitGameplay.StyleScroll(details); column.Add(details);
            var previewArea = ToolkitGameplay.E(details.contentContainer, "inventory-character-area");
            var portraitColumn = ToolkitGameplay.E(previewArea, "inventory-character-column");
            void Turn(int step) { previewDirection = (previewDirection + step + 4) % 4; nextPreviewFrame = 0; UpdateEquipmentPreview(); }
            var left = ToolkitGameplay.B(portraitColumn, "‹", () => Turn(-1), "inventory-character-turn");
            left.name = "inventory-character-left"; left.tooltip = "Turn left";
            var portrait = ToolkitGameplay.E(portraitColumn, "inventory-character-preview"); portrait.name = "inventory-character-preview";
            equipmentPreview = new CharacterImage(portrait, 1.8f);
            var right = ToolkitGameplay.B(portraitColumn, "›", () => Turn(1), "inventory-character-turn");
            right.name = "inventory-character-right"; right.tooltip = "Turn right";
            var rows = ToolkitGameplay.E(previewArea, "inventory-equipment-rows");
            foreach (var key in equipmentController.Slots) {
                var slot = key;
                var row = ToolkitGameplay.E(rows, "inventory-equipment-row");
                var button = ToolkitGameplay.B(row, "", () => { selectedGearSlot = slot; RefreshEquipment(); }, "inventory-equipment-slot");
                button.name = "inventory-gear-" + slot;
                var gearIcon = ToolkitControls.Icon(null); gearIcon.AddToClassList("inventory-equipment-icon"); button.Add(gearIcon);
                ToolkitGameplay.L(button, slot == "Chest" ? "Chest & trousers" : slot, "inventory-equipment-slot-name");
                gearSlots.Add(slot, (button, gearIcon));
                if (slot == "Weapon") continue;
                var control = ToolkitGameplay.B(row, "", () => {
                    if (Blindsided.Oracle.oracle && !Blindsided.Oracle.oracle.CanBeginSaveMutation) return;
                    var settings = CharacterEquipmentAppearance.Visibility;
                    settings.Set(slot, !settings.Get(slot));
                    RefreshEquipment();
                }, "inventory-visibility-button");
                control.name = "inventory-visibility-" + slot;
                var eye = new ToolkitVisibilityIcon(); control.Add(eye); visibilityControls.Add(slot, (control, eye));
            }
            equipmentContent = ToolkitGameplay.E(details.contentContainer, "inventory-equipment-details");
            equipmentController.OnEquipmentChanged += Changed;
        }
        private void FitEquipmentColumns()
        {
            if (equipmentHeading != null) equipmentHeading.style.display = ToolkitResponsiveLayout.IsPortrait ? DisplayStyle.None : DisplayStyle.Flex;
            if (equipmentColumn == null || grid == null) return;
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            selectedName.style.visibility = portrait && portraitEquipmentSelected ? Visibility.Hidden : Visibility.Visible;
            portraitInventoryTabs.style.display = portrait ? DisplayStyle.Flex : DisplayStyle.None;
            // Reserve exactly the portrait tab height so both viewports meet its divider.
            root.style.paddingBottom = portrait ? 40 : StyleKeyword.Null;
            portraitInventoryTabs.ElementAt(0).EnableInClassList("active", !portraitEquipmentSelected);
            portraitInventoryTabs.ElementAt(1).EnableInClassList("active", portraitEquipmentSelected);
            equipmentColumn.style.display = portrait && !portraitEquipmentSelected ? DisplayStyle.None : DisplayStyle.Flex;
            scroll.style.display = portrait && portraitEquipmentSelected ? DisplayStyle.None : DisplayStyle.Flex;
            equipmentColumn.style.paddingLeft = portrait ? 0 : 8;
            var available = equipmentColumn.parent.contentRect.width;
            if (portrait)
            {
                scroll.style.width = available;
                grid.FillWidth(scroll.contentViewport.contentRect.width, new Vector2(44, 48));
                return;
            }
            if (available <= 0 || scroll.layout.width <= 0) return;
            var chrome = Mathf.Max(0, scroll.layout.width - scroll.contentViewport.contentRect.width);
            var equipmentWidth = Mathf.Clamp(available * .42f, 130, 240);
            grid.FitWidth(Mathf.Max(40, available - equipmentWidth - chrome), 14);
            scroll.style.width = grid.ContentWidth + chrome;
        }
        private void RefreshEquipment()
        {
            if (!equipmentController || equipmentContent == null) return;
            var settings = CharacterEquipmentAppearance.Visibility;
            foreach (var pair in visibilityControls) {
                var shown = settings.Get(pair.Key);
                pair.Value.icon.SetShown(shown); pair.Value.button.EnableInClassList("shown", shown);
                var label = pair.Key == "Chest" ? "chest & trousers" : pair.Key.ToLowerInvariant();
                pair.Value.button.tooltip = (shown ? "Hide " : "Show ") + label;
            }
            nextPreviewFrame = 0; UpdateEquipmentPreview();
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
        private void UpdateEquipmentPreview()
        {
            if (equipmentPreview == null || Time.unscaledTime < nextPreviewFrame) return;
            nextPreviewFrame = Time.unscaledTime + 1f / 12;
            var profile = CharacterCreation.Current;
            var catalog = CharacterCatalog.Load(); if (!catalog || profile == null) return;
            var visible = CharacterEquipmentAppearance.Resolve(profile);
            // Older saves use the authored default outfit until they have a modular appearance.
            if (!visible.Modular) { visible = catalog.defaults.Copy(); visible.Name = profile.Name; }
            equipmentCharacterName.text = string.IsNullOrWhiteSpace(profile.Name) ? "Your character" : profile.Name;
            var direction = previewDirection == 0 ? "Down" : previewDirection == 2 ? "Up" : "Right";
            equipmentPreview.Set(visible, catalog.Frame("Idle_" + direction, Mathf.FloorToInt(Time.unscaledTime * 12)), showTool: false);
            // Left uses the authored right-facing frames, just as the world renderer does.
            var image = equipmentColumn.Q("character-image");
            if (image != null) image.style.scale = new Scale(new Vector3(previewDirection == 3 ? -1 : 1, 1, 1));
        }
        private void EquipmentStat(StatDefSO stat, float value)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
            row.AddToClassList("inventory-equipment-stat");
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
