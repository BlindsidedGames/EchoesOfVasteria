using System;
using System.Globalization;
using static Blindsided.Oracle;
using System.Collections.Generic;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Upgrades.Cauldron;
using TimelessEchoes.UI.Cauldron;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitCauldronScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitCauldronDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private CauldronManager manager;
        private ResourceManager resources;
        private VisualElement root, xpFill, ingredientsColumn, tastingColumn, collectionColumn, modal;
        private VisualElement workspace, tastingTop, tastingFooter, tastingInfo, tastingControls, tastingActions;
        private VisualElement tasteRateRow, tasteCostRow, tasteCostLabel;
        private ScrollView tastingScroll;
        private bool? tastingPortrait;
        private Label selection;private VisualElement mobileTabs;private string mobileTab="Ingredients";private bool wasNarrow;
        private Label level, xp, stew, mixPrefix, predicted, mixAllPrefix, mixAllAmount, tasteCost, tasteRate, unitValue;
        private Image portrait, pot, selectedFoodIcon;
        private Button mixAll, addFood, minusAmount, plusAmount, maxAmount, taste, stop;
        private TextField quantityInput;
        private Label availableFood, conversionError;
        private VisualElement foodGrid;
        private long conversionSequence;
        private bool addingFood;
        private readonly List<FoodSlot> foodSlots = new();
        private List<Resource> foods;
        private Resource selectedFood;
        private bool dirty, membershipDirty, localizationDirty;
        private float nextMembershipCheck;
        private string membershipKey;
        private ToolkitCauldronCollections collections;
        private string collectionDetailsId;
        private Label collectionDetailsText;
        private Label collectionCardLabel, collectionCardValue, collectionGroupLabel, collectionGroupValue;
        private Label collectionTotalLabel, collectionTotalValue, collectionNextStep;
        private sealed class FoodSlot
        {
            public Button button;
            public Image icon;
            public Label name, count;
            public Resource resource;
            public bool nameFitQueued;
        }
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;

        public bool Show()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = CauldronManager.Instance) || !(resources = ResourceManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "cauldron" }; root.AddToClassList("eov-cauldron"); theme.Apply(root); document.rootVisualElement.Add(root);
            ToolkitGameplay.Apply(root,theme);root.AddToClassList("live-cauldron");mobileTabs=E(root,"row tabs");foreach(var tab in new[]{"Ingredients","Tasting","Collection"}){string chosen=tab;var button = ToolkitGameplay.B(mobileTabs,"",()=>{mobileTab=chosen;ApplyMobileTabs();},"tab"); button.userData=chosen; ToolkitLocalization.Bind(button, "cauldron.tab-"+chosen.ToLowerInvariant(), chosen);}workspace=E(root,"workspace row");ingredientsColumn=E(workspace,"column ingredients");tastingColumn=E(workspace,"column brewing");collectionColumn=E(workspace,"column collection");BuildMixing();BuildDrinking();
            var scroll=ToolkitGameplay.Scroll(collectionColumn,"collection-scroll");var tooltip=E(root,"surface collection-detail");tooltip.name="collection-tooltip";
            collections=new ToolkitCauldronCollections(definition,manager,resources,scroll.contentContainer,tooltip,collectionColumn,ShowCollectionDetails);collections.Rebuild();membershipKey=collections.MembershipKey();
            resources.OnInventoryChanged += InventoryChanged;
            manager.OnStewChanged += Changed; manager.OnWeightsChanged += Changed;
            manager.OnCardGained += Gained;
            manager.OnTasteSessionStarted += Started; manager.OnTasteSessionStopped += Stopped;
            Blindsided.EventHandler.OnLoadData += Loaded; Blindsided.EventHandler.OnQuestHandin += QuestChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
            ToolkitLocalization.Changed += LocalizationChanged;
            TownWindowManager.ClearCauldronAttention();
            RestoreFoodSelection(); Refresh(); Layout(); return true;
        }
        private static Label LocalizedLabel(VisualElement parent, string key, string english, string classes = "")
        { var label = L(parent, "", classes); ToolkitLocalization.Bind(label, key, english); return label; }
        private static Button LocalizedButton(VisualElement parent, string key, string english, Action action, string classes = "")
        { var button = ToolkitGameplay.B(parent, "", action, classes); ToolkitLocalization.Bind(button, key, english); return button; }
        private void LocalizationChanged()
        {
            localizationDirty = true; dirty = true;
        }
        private void BuildMixing()
        {
            var scroll = ToolkitGameplay.Scroll(ingredientsColumn, "ingredients-scroll");
            foodGrid = E(scroll, "food-grid");
            var footer = E(ingredientsColumn, "mix-footer conversion-footer");
            var chosen = E(footer, "row conversion-selection");
            selectedFoodIcon = ToolkitGameplay.Icon(chosen, null, 20);
            var detail = E(chosen, "grow");
            selection = L(detail, ToolkitLocalization.Text("cauldron.choose-food", "Choose a food"), "strong");
            var availability = E(detail, "row stew-availability");
            availableFood = L(availability, "", "small muted");
            var unit = E(availability, "row stew-unit");
            ToolkitGameplay.Icon(unit, definition.stewIcon, 12);
            unitValue = L(unit, "", "small muted");
            addFood = ToolkitGameplay.B(chosen, "", AddSelectedFood, "primary mix-preview mix-selected");
            addFood.name = "add-to-cauldron";
            mixPrefix = L(addFood, "", "mix-label mix-caption"); mixPrefix.pickingMode = PickingMode.Ignore;
            var result = E(addFood, "row mix-yield"); result.pickingMode = PickingMode.Ignore;
            ToolkitGameplay.Icon(result, definition.stewIcon, 16).AddToClassList("mix-stew-icon");
            predicted = L(result, "", "mix-label"); predicted.name = "predicted-stew"; predicted.pickingMode = PickingMode.Ignore;
            var amountRow = E(footer, "row conversion-quantity");
            LocalizedLabel(amountRow, "cauldron.amount", "Amount", "amount-label");
            minusAmount = ToolkitGameplay.B(amountRow, "−", () => StepAmount(-1), "amount-step");
            ToolkitLocalization.BindTooltip(minusAmount, "cauldron.decrease-amount", "Decrease amount");
            quantityInput = new TextField { name = "conversion-amount", value = "0" };
            quantityInput.AddToClassList("conversion-input"); amountRow.Add(quantityInput);
            quantityInput.RegisterValueChangedCallback(_ =>
            {
                if (TryAmount(out var amount))
                {
                    quantityInput.SetValueWithoutNotify(LocaleNumbers.Input(amount));
                    RememberAmount(amount);
                }
                conversionError.text = ""; RefreshConversion();
            });
            plusAmount = ToolkitGameplay.B(amountRow, "+", () => StepAmount(1), "amount-step");
            ToolkitLocalization.BindTooltip(plusAmount, "cauldron.increase-amount", "Increase amount");
            maxAmount = LocalizedButton(amountRow, "cauldron.max", "Max", () => SetAmount(selectedFood ? resources.GetAmount(selectedFood) : 0), "amount-max");
            conversionError = L(footer, "", "small muted conversion-error");
            var actions = E(footer, "mix-actions");
            actions.Add(addFood);
            mixAll = ToolkitGameplay.B(actions, "", MixAllFoods, "mix-preview mix-all-main");
            mixAll.name = "mix-all";
            mixAllPrefix = L(mixAll, "", "mix-label mix-caption"); mixAllPrefix.pickingMode = PickingMode.Ignore;
            var allResult = E(mixAll, "row mix-yield"); allResult.pickingMode = PickingMode.Ignore;
            ToolkitGameplay.Icon(allResult, definition.stewIcon, 16).AddToClassList("mix-stew-icon");
            mixAllAmount = L(allResult, "", "mix-label"); mixAllAmount.name = "mix-all-stew"; mixAllAmount.pickingMode = PickingMode.Ignore;
        }
        private void EnsureFoodSlots(int count)
        {
            while (foodSlots.Count < count)
            {
                var slot = new FoodSlot();
                slot.button = ToolkitGameplay.B(foodGrid, "", () => SelectFood(slot.resource), "food");
                slot.button.name = "food-" + foodSlots.Count;
                slot.icon = ToolkitGameplay.Icon(slot.button, null, 20);
                slot.name = L(slot.button, "", "small strong food-name"); slot.name.name = "food-name";
                slot.button.RegisterCallback<GeometryChangedEvent>(e =>
                {
                    if (!Mathf.Approximately(e.newRect.width, e.oldRect.width)) FitFoodName(slot);
                });
                slot.count = L(slot.button, "", "small muted");
                foodSlots.Add(slot);
            }
        }
        private static void FitFoodName(FoodSlot slot)
        {
            if (slot.nameFitQueued) return;
            slot.nameFitQueued = true;
            slot.name.style.fontSize = StyleKeyword.Null;
            slot.name.schedule.Execute(() =>
            {
                slot.nameFitQueued = false;
                if (slot.name.panel == null || slot.name.contentRect.width <= 0) return;
                var available = slot.name.contentRect.width - .5f;
                var measured = slot.name.MeasureTextSize(slot.name.text, 0, VisualElement.MeasureMode.Undefined,
                    0, VisualElement.MeasureMode.Undefined).x;
                if (measured > available && available > 0)
                    slot.name.style.fontSize = slot.name.resolvedStyle.fontSize * available / measured;
            });
        }
        private void SetAmount(double value) => quantityInput.value = LocaleNumbers.Input(value);
        private void RememberAmount(double amount)
        {
            if (selectedFood) SetCauldronFoodAmount(selectedFood.name, amount);
        }
        private double RememberedAmount(Resource food)
        {
            return food ? GetCauldronFoodAmount(food.name) : 1;
        }
        private void RestoreFoodSelection()
        {
            selectedFood = null;
            quantityInput.SetValueWithoutNotify("0");
            MigrateCauldronFoodPreferences(oracle?.saveData?.SavedPreferences);
            var name = LastSelectedCauldronFood;
            if (string.IsNullOrEmpty(name)) return;
            var food = CauldronMixingPresentation.BuildFoodDefinitions().FirstOrDefault(r => r.name == name);
            if (food && resources.IsUnlocked(food)) SelectFood(food);
        }
        private void NextConversion()
        {
            conversionSequence = oracle?.saveData != null && oracle.saveData.CauldronConversionSequence < long.MaxValue
                ? oracle.saveData.CauldronConversionSequence + 1 : 0;
        }
        private bool TryAmount(out double amount)
        {
            if (!LocaleNumbers.TryInput(quantityInput.value, out amount) ||
                !CauldronConversion.IsFinite(amount)) return false;
            amount = Math.Floor(amount);
            return true;
        }
        private void StepAmount(int step)
        {
            if (!selectedFood) return;
            if (!TryAmount(out var value)) value = 0;
            SetAmount(Math.Max(0, Math.Min(resources.GetAmount(selectedFood), value + step)));
        }
        private void RefreshConversion()
        {
            var selected = selectedFood != null && resources.IsUnlocked(selectedFood);
            var stock = selected ? Math.Floor(resources.GetAmount(selectedFood)) : 0;
            selection.text = selected ? ToolkitLocalization.Name(selectedFood) : ToolkitLocalization.Text("cauldron.choose-food", "Choose a food");
            selectedFoodIcon.sprite = selected ? selectedFood.icon : null;
            selectedFoodIcon.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
            conversionError.style.display = string.IsNullOrEmpty(conversionError.text) ? DisplayStyle.None : DisplayStyle.Flex;
            availableFood.parent.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
            availableFood.text = selected ? ToolkitLocalization.Text("cauldron.available-amount", "Available: {0} ·", CalcUtils.FormatNumber(stock, true)) : "";
            unitValue.text = selected ? ToolkitLocalization.Text("cauldron.per-unit", "{0} / unit", LocaleNumbers.Format(CauldronConversion.UnitValue(selectedFood), "G15")) : "";
            var valid = TryAmount(out var amount) && selected && manager.CanAddToCauldron(selectedFood, amount);
            SetMixPreview(mixPrefix, predicted, "cauldron.mix-preview", "Mix {0} {1}", valid ? amount * CauldronConversion.UnitValue(selectedFood) : 0);
            addFood.SetEnabled(valid && !addingFood);
            double allStew = 0;
            foreach (var food in CauldronMixingPresentation.BuildEligibleFoods(resources))
            {
                var quantity = Math.Floor(resources.GetAmount(food));
                if (manager.CanAddToCauldron(food, quantity)) allStew += quantity * CauldronConversion.UnitValue(food);
            }
            var canMixAll = CauldronConversion.IsFinite(allStew) && allStew > 0;
            SetMixPreview(mixAllPrefix, mixAllAmount, "cauldron.mix-all-preview", "Mix All {0} {1}", canMixAll ? allStew : 0);
            mixAll.SetEnabled(!addingFood && canMixAll);
            quantityInput.SetEnabled(selected && !addingFood);
            minusAmount.SetEnabled(selected && amount > 0 && !addingFood);
            plusAmount.SetEnabled(selected && amount < stock && !addingFood);
            maxAmount.SetEnabled(selected && stock > 0 && !addingFood);
        }
        private static void SetMixPreview(Label prefix, Label amountLabel, string key, string fallback, double amount)
        {
            const string iconToken = "{stew}";
            var text = ToolkitLocalization.Text(key, fallback, iconToken, CalcUtils.FormatNumber(amount));
            var iconPosition = text.IndexOf(iconToken, StringComparison.Ordinal);
            prefix.text = iconPosition >= 0 ? text.Substring(0, iconPosition).TrimEnd() : "";
            prefix.style.display = string.IsNullOrEmpty(prefix.text) ? DisplayStyle.None : DisplayStyle.Flex;
            amountLabel.text = iconPosition >= 0 ? text.Substring(iconPosition + iconToken.Length).TrimStart() : text;
        }
        private void SelectFood(Resource food)
        {
            if (!food || !resources.IsUnlocked(food) || addingFood) return;
            selectedFood = food;
            LastSelectedCauldronFood = food.name;
            NextConversion();
            conversionError.text = ""; SetAmount(RememberedAmount(food)); Refresh();
        }
        private void AddSelectedFood()
        {
            if (addingFood || !selectedFood || !TryAmount(out var amount)) return;
            addingFood = true; RefreshConversion();
            try
            {
                if (manager.TryAddToCauldron(selectedFood, amount, conversionSequence, out _, out var error))
                { NextConversion(); conversionError.text = ""; }
                else conversionError.text = error;
            }
            finally { addingFood = false; Refresh(); }
        }
        private void MixAllFoods()
        {
            if (addingFood || oracle?.saveData == null) return;
            addingFood = true; RefreshConversion();
            try
            {
                conversionError.text = "";
                foreach (var food in CauldronMixingPresentation.BuildEligibleFoods(resources))
                {
                    var amount = Math.Floor(resources.GetAmount(food));
                    if (!manager.CanAddToCauldron(food, amount)) continue;
                    var sequence = oracle.saveData.CauldronConversionSequence;
                    if (sequence == long.MaxValue) { conversionError.text = "Conversion limit reached."; break; }
                    if (!manager.TryAddToCauldron(food, amount, sequence + 1, out _, out var error))
                    { conversionError.text = error; break; }
                }
            }
            finally { NextConversion(); addingFood = false; Refresh(); }
        }
        private void BuildDrinking()
        {
            tastingPortrait = null;
            tastingScroll=ToolkitGameplay.Scroll(tastingColumn,"tasting-scroll");tastingScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;ToolkitGameplay.StyleScroll(tastingScroll);
            tastingTop = E(tastingScroll, "tasting-top");
            var eva=E(tastingTop,"row eva tasting-eva");portrait=ToolkitGameplay.Icon(eva,definition.portrait.At(0),20);var info=E(eva,"eva-info");level=L(info,"","strong");xpFill=ToolkitGameplay.Bar(info);xp=L(info,"","small muted");
            tastingInfo=E(tastingTop,"row between odds-heading");LocalizedLabel(tastingInfo, "cauldron.reward-chances", "Reward chances","strong");ToolkitGameplay.B(tastingInfo,"i",ShowTastingDetails,"details-button");
            tastingFooter = E(tastingColumn, "tasting-footer");
            var overview = E(tastingFooter, "tasting-overview");
            var summary=E(overview,"brew-summary");pot=ToolkitGameplay.Icon(summary,definition.pot.At(0),46);
            var total = E(summary, "row stew-total");
            ToolkitGameplay.Icon(total, definition.stewIcon, 14);
            stew = L(total, "", "stew-amount");
            tastingControls = E(overview, "tasting-controls");
            var readouts = E(tastingControls, "tasting-readouts");
            tasteRateRow=E(readouts,"row between readout");LocalizedLabel(tasteRateRow, "cauldron.tastes-per-second", "Tastes / s","muted taste-rate-caption");tasteRate=L(tasteRateRow, "");
            tasteRate.AddToClassList("taste-rate-value");
            tasteCostRow = E(readouts, "row between readout");
            tasteCostLabel = E(tasteCostRow, "row stew-cost-label");
            ToolkitGameplay.Icon(tasteCostLabel, definition.stewIcon, 12);
            LocalizedLabel(tasteCostLabel, "cauldron.per-taste", "/ Taste", "muted taste-cost-caption");
            tasteCost = L(tasteCostRow, "");
            tasteCost.AddToClassList("taste-cost-value");
            tastingActions = E(tastingControls, "tasting-actions");
            taste=LocalizedButton(tastingActions,"cauldron.taste","Taste",()=>manager.StartTasting(),"primary");stop=LocalizedButton(tastingActions,"cauldron.pause","Pause",()=>manager.StopTasting());
        }
        private void ArrangeTastingForOrientation(bool portraitLayout)
        {
            if (tastingPortrait == portraitLayout) return;
            tastingPortrait = portraitLayout;
            if (portraitLayout)
            {
                if (mobileTab == "Collection") mobileTab = "Tasting";
                tastingColumn.Insert(0, tastingTop);
                tastingTop.Add(tastingFooter);
                tastingTop.Add(tastingInfo);
                tastingColumn.Add(collectionColumn);
                tastingControls.Insert(0, tastingActions);
                tasteRateRow.Insert(0, tasteRate);
                tasteCostLabel.Insert(1, tasteCost);
                tastingScroll.style.display = DisplayStyle.None;
                mobileTabs.BringToFront();
            }
            else
            {
                tastingScroll.Add(tastingTop);
                tastingTop.Add(tastingFooter);
                tastingControls.Add(tastingActions);
                tasteRateRow.Add(tasteRate);
                tasteCostRow.Add(tasteCost);
                tastingTop.Add(tastingInfo);
                workspace.Add(collectionColumn);
                tastingScroll.style.display = DisplayStyle.Flex;
            }
        }
        private void CloseModal(){modal?.RemoveFromHierarchy();modal=null;collectionDetailsId=null;collectionDetailsText=null;collectionCardLabel=null;collectionCardValue=null;collectionGroupLabel=null;collectionGroupValue=null;collectionTotalLabel=null;collectionTotalValue=null;collectionNextStep=null;root?.Q(className:"workspace")?.SetEnabled(true);}
        private VisualElement Modal(string title){CloseModal();root.Q(className:"workspace")?.SetEnabled(false);modal=E(root,"modal");var d=E(modal,"surface dialog mix-dialog");if(!string.IsNullOrEmpty(title))L(d,title,"heading");modal.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){CloseModal();e.StopPropagation();}});return d;}
        private void ShowCollectionDetails(string id)
        {
            collections.HideTooltip();
            var dialog = Modal(null);
            dialog.name = "collection-details-dialog";
            dialog.AddToClassList("collection-dialog");
            collectionDetailsId = id;
            var scroll = ToolkitGameplay.Scroll(dialog, "collection-details-scroll");
            scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            ToolkitGameplay.StyleScroll(scroll);
            var resourceDetails = collections.TryGetResource(id, out _);
            collectionDetailsText = L(scroll, "", resourceDetails ? "collection-details-title" : "collection-details-text");
            collectionDetailsText.name = "collection-details-text";
            if (resourceDetails)
            {
                var card = E(scroll, "row collection-detail-row");
                collectionCardLabel = L(card, "", "collection-detail-name");
                collectionCardValue = L(card, "", "collection-detail-value");
                var group = E(scroll, "row collection-detail-row");
                collectionGroupLabel = L(group, "", "collection-detail-name");
                collectionGroupValue = L(group, "", "collection-detail-value");
                var total = E(scroll, "row collection-detail-row collection-detail-total");
                collectionTotalLabel = L(total, "", "collection-detail-name strong");
                collectionTotalValue = L(total, "", "collection-detail-value strong");
                collectionNextStep = L(scroll, "", "collection-details-text collection-detail-next muted");
            }
            RefreshCollectionDetails();
            LocalizedButton(dialog, "common.close", "Close", CloseModal).Focus();
        }
        private void RefreshCollectionDetails()
        {
            if (collectionDetailsText == null || collectionDetailsId == null) return;
            if (collectionCardLabel == null || !collections.TryGetResource(collectionDetailsId, out var resource))
            {
                collectionDetailsText.text = collections.GetDetailsText(collectionDetailsId)
                    .Replace("<line-height=50%>\n</line-height>\n", "\n\n");
                return;
            }
            var name = ToolkitLocalization.Name(resource);
            var group = manager.GetResourceGroup(resource);
            var groupName = CauldronCollectionPresentation.FormatGroupName(group);
            var cardTier = manager.GetResourceTier(resource.name);
            var groupTier = CauldronResourceYield.CategoryTier(oracle?.saveData, group, definition.config);
            var cardBonus = CauldronResourceYield.CardBonusPercent(cardTier, definition.config);
            var groupBonus = CauldronResourceYield.CategoryBonusPercent(groupTier, definition.config);
            collectionDetailsText.text = name;
            collectionCardLabel.text = ToolkitLocalization.Text("cauldron.details.card", "Card · Tier {0}", cardTier);
            collectionGroupLabel.text = ToolkitLocalization.Text("cauldron.details.collection", "{0} collection · Tier {1}", groupName, groupTier);
            collectionCardValue.text = "+" + LocaleNumbers.PercentPoints(cardBonus);
            collectionGroupValue.text = "+" + LocaleNumbers.PercentPoints(groupBonus);
            collectionTotalLabel.text = ToolkitLocalization.Text("cauldron.details.total", "Total yield bonus");
            collectionTotalValue.text = "+" + LocaleNumbers.PercentPoints(cardBonus + groupBonus);
            var maxTier = Mathf.Min(definition.config.resourceTierThresholds.Length, definition.config.categoryYieldBonusPerTier.Length);
            collectionNextStep.style.display = groupTier < maxTier ? DisplayStyle.Flex : DisplayStyle.None;
            if (groupTier < maxTier)
                collectionNextStep.text = ToolkitLocalization.Text("cauldron.details.next-collection",
                    "Raise all unlocked {0} cards to Tier {1} for a +{2} collection bonus.",
                    groupName, groupTier + 1, LocaleNumbers.PercentPoints(CauldronResourceYield.CategoryBonusPercent(groupTier + 1, definition.config)));
        }
        private void ShowTastingDetails()
        {
            var current = manager.GetEffectiveWeightsAtLevel(manager.EvaLevel);
            var next = manager.GetEffectiveWeightsAtLevel(manager.EvaLevel + 1);
            var weights = CauldronWeightsPresentation.BuildRows(current, next, definition.config);
            var d=Modal(ToolkitLocalization.Text("cauldron.tasting-details", "Tasting details"));
            var scroll=ToolkitGameplay.Scroll(d,"tasting-results");
            var heading=E(scroll,"row between odds-row");LocalizedLabel(heading, "cauldron.reward-chances", "Reward chances","strong");LocalizedLabel(heading, "cauldron.current-next", "Current → Next level","muted");
            var currentTotal=weights.Sum(w=>w.current);var nextTotal=weights.Sum(w=>w.next);
            foreach(var weight in weights)
            {
                var row=E(scroll,"row between odds-row");L(row,weight.label);
                L(row,LocaleNumbers.Format($"{(currentTotal>0?100*weight.current/currentTotal:0):0.00}{LocaleNumbers.PercentSuffix} → {(nextTotal>0?100*weight.next/nextTotal:0):0.00}{LocaleNumbers.PercentSuffix}"),"strong");
            }
            LocalizedButton(d,"common.close","Close",CloseModal).Focus();
        }
        private void Refresh()
        {
            // Freeze the display while editing an amount; identity, not index, owns selection.
            if (selectedFood && !resources.IsUnlocked(selectedFood)) selectedFood = null;
            if (foods == null || !selectedFood) foods = CauldronMixingPresentation.BuildDisplayFoods(resources);
            EnsureFoodSlots(foods.Count);
            for (var i = 0; i < foodSlots.Count; i++)
            {
                var slot = foodSlots[i]; var food = i < foods.Count ? foods[i] : null;
                var known = food && resources.IsUnlocked(food);
                slot.resource = food;
                slot.button.style.display = food ? DisplayStyle.Flex : DisplayStyle.None;
                var name = food ? (known ? ToolkitLocalization.Name(food) : "???") : "";
                if (slot.name.text != name) { slot.name.text = name; FitFoodName(slot); }
                slot.icon.sprite = food ? (known ? food.icon : food.UnknownIcon) : null;
                slot.count.text = food && known ? CalcUtils.FormatNumber(resources.GetAmount(food), true) : "";
                slot.count.style.display = food && known ? DisplayStyle.Flex : DisplayStyle.None;
                slot.button.tooltip = food && known ? ToolkitLocalization.Name(food) : "";
                slot.button.EnableInClassList("selected", food && food == selectedFood);
                slot.button.SetEnabled(known && resources.GetAmount(food) > 0 && !addingFood);
            }
            RefreshConversion();
            level.text = ToolkitLocalization.Text("cauldron.eva-level", "Eva · Level {0}", manager.EvaLevel); var needed = 50 + 10 * Mathf.Max(0, manager.EvaLevel - 1);
            xp.text = ToolkitLocalization.Text("cauldron.xp", "xp: {0:N0} / {1:N0}", manager.EvaXp, needed); xpFill.style.width = Length.Percent(Mathf.Clamp01((float)(manager.EvaXp / needed)) * 100);
            stew.text = CalcUtils.FormatNumber(manager.Stew);
            tasteRate.text = LocaleNumbers.Integer(manager.TastesPerSecond, false);
            taste.style.display=manager.IsTasting?DisplayStyle.None:DisplayStyle.Flex;stop.style.display=manager.IsTasting?DisplayStyle.Flex:DisplayStyle.None;
            tasteCost.text=CalcUtils.FormatNumber(manager.GetStewCostPerRoll());
            taste.SetEnabled(!manager.IsTasting && manager.Stew >= manager.GetStewCostPerRoll()); stop.SetEnabled(manager.IsTasting);
            RefreshCollectionDetails();
        }
        private void Changed() => dirty = true;
        private void InventoryChanged() { dirty = true; membershipDirty = true; }
        private void Gained(string id, int amount) { collections.Gained(id, amount); dirty = true; }
        private void Started() { collections.ClearHighlights(); dirty = true; }
        private void Stopped() { collections.Refresh(); dirty = true; }
        private void Loaded() { RestoreFoodSelection(); collections.Rebuild(); membershipKey = collections.MembershipKey(); dirty = true; }
        private void QuestChanged(string _) { collections.Rebuild(); membershipKey = collections.MembershipKey(); dirty = true; }
        private void LocaleChanged(UnityEngine.Localization.Locale _) { if (selectedFood) quantityInput.SetValueWithoutNotify(LocaleNumbers.Input(RememberedAmount(selectedFood))); foreach (var slot in foodSlots) FitFoodName(slot); collections.Rebuild(); dirty = true; }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout()
        {
            var area = ToolkitWindowLayout.SafeArea;
            windowLayout.Apply(root, new Rect(area.x + 12, area.y + ToolkitResponsiveLayout.WindowTop,
                area.width - 24, area.height - ToolkitResponsiveLayout.WindowTop - ToolkitResponsiveLayout.WindowBottom));
            var phone = ToolkitResponsiveLayout.IsPortrait;
            ArrangeTastingForOrientation(phone);
            root.EnableInClassList("compact-width", area.width < 650);
            wasNarrow = phone || area.width < 350;
            root.EnableInClassList("narrow", wasNarrow);
            mobileTabs.EnableInClassList("portrait-page-tabs", phone);
            ApplyMobileTabs();
        }
        private void ApplyMobileTabs()
        {
            if (mobileTabs == null) return;
            var phone = tastingPortrait == true;
            var combined = phone && mobileTab == "Tasting";
            root.style.paddingBottom = phone ? (combined ? 0 : 48) : StyleKeyword.Null;
            mobileTabs.style.display = wasNarrow ? DisplayStyle.Flex : DisplayStyle.None;
            ingredientsColumn.style.display = !wasNarrow || mobileTab == "Ingredients" ? DisplayStyle.Flex : DisplayStyle.None;
            tastingColumn.style.display = !wasNarrow || mobileTab == "Tasting" ? DisplayStyle.Flex : DisplayStyle.None;
            collectionColumn.style.display = phone || !wasNarrow || mobileTab == "Collection" ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var button in mobileTabs.Query<Button>().ToList())
            {
                button.style.display = phone && (string)button.userData == "Collection" ? DisplayStyle.None : DisplayStyle.Flex;
                button.EnableInClassList("active", (string)button.userData == mobileTab);
            }
        }
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (localizationDirty)
            {
                localizationDirty = false;
                collections?.Rebuild();
                Refresh();
                if (modal != null)
                {
                    if (collectionDetailsId != null) ShowCollectionDetails(collectionDetailsId);
                    else ShowTastingDetails();
                }
            }
            portrait.sprite = definition.portrait.At(Time.time); pot.sprite = definition.pot.At(Time.time);
            if (dirty) { dirty = false; Refresh(); }
            if (membershipDirty && Time.unscaledTime >= nextMembershipCheck)
            {
                membershipDirty = false; nextMembershipCheck = Time.unscaledTime + .75f;
                var key = collections.MembershipKey(); if (key != membershipKey) { membershipKey = key; collections.Rebuild(); }
            }
            collections.Tick();
        }
        public void Hide()
        {
            if (IsOpen) PlayerPrefs.Save();
            if (resources) resources.OnInventoryChanged -= InventoryChanged;
            if (manager)
            {
                manager.OnStewChanged -= Changed; manager.OnWeightsChanged -= Changed; manager.OnCardGained -= Gained;
                manager.OnTasteSessionStarted -= Started; manager.OnTasteSessionStopped -= Stopped;
            }
            Blindsided.EventHandler.OnLoadData -= Loaded; Blindsided.EventHandler.OnQuestHandin -= QuestChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            ToolkitLocalization.Changed -= LocalizationChanged;
            CloseModal();root?.RemoveFromHierarchy(); root = null; collections = null; foodSlots.Clear(); dirty = membershipDirty = false;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref settings);
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
