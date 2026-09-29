using System;
using System.Linq;
using System.Text;
using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Gear.UI;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitForgeScreen
    {
        private sealed class ConversionRow { public VisualElement root; public Image first, second, result, arrow; public Label firstCount, secondCount, resultCount, maximum; public Button button; public TextField amount; }
        private void BuildConversions()
        {
            var grid = new VisualElement(); grid.AddToClassList("eov-forge-conversions"); main.Add(grid);
            foreach (var type in new[] { ConversionType.Chunk, ConversionType.Crystal, ConversionType.Ingot, ConversionType.Core })
            {
                var row = new ConversionRow(); row.root = Panel(grid, "eov-forge-conversion"); row.root.name = "conversion-" + type;
                Label(row.root, type.ToString() + "s", ToolkitControls.TextRole.Heading).AddToClassList("forge-conversion-title");
                var recipe = Row(row.root); var costs = new VisualElement(); recipe.Add(costs);
                var first = Row(costs); first.AddToClassList("forge-ingredient"); row.first = ToolkitControls.Icon(null); first.Add(row.first); row.firstCount = Label(first, "", ToolkitControls.TextRole.Caption);
                var second = Row(costs); second.AddToClassList("forge-ingredient"); row.second = ToolkitControls.Icon(null); second.Add(row.second); row.secondCount = Label(second, "", ToolkitControls.TextRole.Caption);
                row.arrow = ToolkitControls.Icon(null); recipe.Add(row.arrow); var result = new VisualElement(); recipe.Add(result); row.result = ToolkitControls.Icon(null); result.Add(row.result); row.resultCount = Label(result, "", ToolkitControls.TextRole.Caption);
                var control = new VisualElement(); control.AddToClassList("eov-forge-conversion-control"); control.style.flexGrow = 1; row.root.Add(control);
                row.maximum = Label(control, "", ToolkitControls.TextRole.Caption);
                row.button = Button(control, "smelt-" + type, "Smelt", () => session.Convert(type));
                // Keep press-and-hold conversion behavior while using native pointer capture.
                ToolkitControls.RepeatWhileHeld(row.button, () => session.Convert(type));
                row.amount = new TextField { name = "amount-" + type, keyboardType = TouchScreenKeyboardType.NumberPad }; row.amount.AddToClassList("eov-number-field"); row.amount.tooltip = "Conversion batch size"; control.Insert(1, row.amount);
                row.amount.SetValueWithoutNotify(session.Desired(type).ToString("0"));
                row.amount.RegisterValueChangedCallback(e => { session.SetAmount(type, double.TryParse(e.newValue, out var value) ? value : 1); row.amount.SetValueWithoutNotify(session.Desired(type).ToString("0")); }); conversions.Add(type, row);
            }
        }
        private Sprite ResourceSprite(Resource resource, double needed = 0) => resource ? resources.IsUnlocked(resource) && resources.GetAmount(resource) >= needed ? resource.icon : resource.UnknownIcon : null;
        private void Refresh()
        {
            dirty = false; nextRefresh = Time.unscaledTime + .1f;
            var (level, current, needed) = crafting.GetIvanXpState(); ivan.text = "Ivan | Level " + Math.Max(0, level); xp.text = $"{current:N0}/{needed:N0}"; xpFill.style.width = Length.Percent(needed > 0 ? Mathf.Clamp01(current / needed) * 100 : 0);
            foreach (var entry in gearSlots)
            {
                var item = equipment.GetEquipped(entry.Key); entry.Value.icon.sprite = definition.GearSprite(item, entry.Key); entry.Value.selected.style.display = session.Slot == entry.Key ? DisplayStyle.Flex : DisplayStyle.None; entry.Value.tier.text = item?.rarity ? "Tier " + Mathf.Clamp(item.rarity.tierIndex + 1, 1, 8) : "";
            }
            for (var i = 0; i < coreSlots.Count; i++)
            {
                var binding = definition.catalog.cores[i]; var row = coreSlots[i]; var amount = binding.coreResource ? resources.GetAmount(binding.coreResource) : 0; var known = binding.coreResource && resources.IsUnlocked(binding.coreResource);
                row.icon.sprite = known ? amount >= 1 ? definition.coreIcons[i] : binding.coreResource.UnknownIcon : null;
                row.selected.style.display = session.Core == binding.core ? DisplayStyle.Flex : DisplayStyle.None;
                row.cores.text = CalcUtils.FormatNumber(Math.Floor(amount), hideDecimal: Math.Abs(Math.Floor(amount)) < 1000);
                var count = Math.Floor(amount); if (binding.core && binding.core.ingotCost > 0) count = Math.Min(count, Math.Floor(binding.ingotResource ? resources.GetAmount(binding.ingotResource) : 0) / binding.core.ingotCost);
                row.crafts.text = CalcUtils.FormatNumber(count, hideDecimal: Math.Abs(count) < 1000);
            }
            var core = session.Core; var bindingSelected = definition.catalog.Find(core); var ingot = bindingSelected?.ingotResource ? bindingSelected.ingotResource : core?.requiredIngot;
            costCore.sprite = ResourceSprite(bindingSelected?.coreResource, 1); costIngot.sprite = ResourceSprite(ingot, core ? core.ingotCost : 0); ingotCost.text = core ? core.ingotCost.ToString("0") : "";
            coreCost.EnableInClassList("forge-loss", !(bindingSelected?.coreResource) || resources.GetAmount(bindingSelected.coreResource) < 1);
            ingotCost.EnableInClassList("forge-loss", !ingot || resources.GetAmount(ingot) < (core ? core.ingotCost : 0));
            craftArrow.sprite = session.CanCraft() ? definition.arrowValid : definition.arrowInvalid;
            maxCrafts.text = $"{session.MaxCrafts:N0} crafts available"; craft.text = session.IsAutoCrafting ? "Stop" : "Craft"; craft.SetEnabled(session.IsAutoCrafting || session.CanCraft()); autoCraft.SetEnabled(!session.IsAutoCrafting && session.CanCraft()); replace.SetEnabled(session.CanReplace);
            var pending = session.HasResult ? session.Pending : null;
            RefreshComparison(pending, equipment.GetEquipped(session.Slot));
            resultIcon.sprite = definition.GearSprite(pending, session.Slot); resultTier.text = pending?.rarity ? "Tier " + Mathf.Clamp(pending.rarity.tierIndex + 1, 1, 8) : "";
            ToolkitGameplay.SetToggle(stopIcon,StopAutocraftOnVastium);ToolkitGameplay.SetToggle(lockIcon,LockAutocraftStatSet);
            var weights = RarityOddsCalculator.BuildRarityWeightInfo(core).weights;
            odds.SetWeights(weights); RefreshOddsPopup(weights);
            foreach (var entry in conversions) RefreshConversion(entry.Key, entry.Value);
            var visibleConversions = conversions.Values.Count(r => r.root.style.display.value != DisplayStyle.None);
            foreach (var entry in conversions.Values) entry.root.style.width = Length.Percent(100f / Math.Max(1, visibleConversions));
            foreach (var entry in gearSlots) NativeSize(entry.Value.icon);
            foreach (var entry in coreSlots) NativeSize(entry.icon);
            if (!showInventory)
            {
                RefreshEquipmentTotals();
                RefreshStatistics();
            }
        }
        private void RefreshStatistics()
        {
            if (!statisticsDirty || showInventory || Time.unscaledTime < nextStatsRefresh) return;
            RefreshHistory(statsPresentation.BuildStatsText(Oracle.oracle?.saveData?.Forge));
            nextStatsRefresh = Time.unscaledTime + .75f; statisticsDirty = false;
        }
        private void RefreshConversion(ConversionType type, ConversionRow row)
        {
            var core = session.Core; Resource first = null, second = null, result = null; double costA = 0, costB = 0, output = 1; bool visible = true;
            if (core) switch (type)
            {
                case ConversionType.Ingot: first = core.chunkResource; second = core.crystalResource; result = core.requiredIngot; costA = core.chunkCostPerIngot; costB = core.crystalCostPerIngot; break;
                case ConversionType.Crystal: first = core.chunkResource; second = definition.catalog.slime; result = core.crystalResource; costA = 2; costB = 1; break;
                case ConversionType.Chunk: first = core.crystalResource; second = definition.catalog.stone; result = core.chunkResource; costA = 1; costB = 2; break;
                case ConversionType.Core: var pair = definition.catalog.ConversionResources(core); first = pair.currentCore; second = result = pair.nextCore; visible = !pair.finalTier; costA = 5; costB = 1; output = 2; break;
            }
            row.root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; if (!visible) return;
            row.amount.SetValueWithoutNotify(session.Desired(type).ToString("0"));
            var amount = session.ConversionAmount(type); row.first.sprite = ResourceSprite(first, costA); row.second.sprite = ResourceSprite(second, costB); row.result.sprite = ResourceSprite(result);
            var shownAmount = Math.Max(1, amount);
            row.firstCount.text = CalcUtils.FormatNumber(costA * shownAmount, true); row.secondCount.text = CalcUtils.FormatNumber(costB * shownAmount, true); row.resultCount.text = CalcUtils.FormatNumber(output * shownAmount, true);
            row.firstCount.EnableInClassList("forge-loss", first && resources.GetAmount(first) < costA * shownAmount);
            row.secondCount.EnableInClassList("forge-loss", second && resources.GetAmount(second) < costB * shownAmount);
            var max = session.ConversionMaximum(type); var desired = session.Desired(type); var batches = max <= 0 ? 0 : desired > max ? 1 : Math.Floor(max / desired); row.maximum.text = "Batches: " + CalcUtils.FormatNumber(batches, true);
            row.button.SetEnabled(session.CanConvert(type)); row.arrow.sprite = session.CanConvert(type) ? definition.arrowValid : definition.arrowInvalid;
        }
    }
}
