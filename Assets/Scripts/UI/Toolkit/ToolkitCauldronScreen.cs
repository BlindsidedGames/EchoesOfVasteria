using System;
using System.Globalization;
using static Blindsided.Oracle;
using System.Collections.Generic;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;
using System.Linq;
using System.Text;
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
        private VisualElement root, xpFill, ingredientsColumn, tastingColumn, collectionColumn, oddsList, modal;
        private Label selection;private VisualElement mobileTabs;private string mobileTab="Ingredients";private bool wasNarrow;private readonly List<Label> oddsValues=new();
        private Label level, xp, stew, stats, predicted, rollCost;
        private Image portrait, pot, selectedFoodIcon;
        private Button addFood, minusAmount, plusAmount, maxAmount, taste, stop;
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
        private List<(string label, float current, float next, Color color)> weights;
        private readonly StringBuilder statsBuilder = new(512);
        private sealed class FoodSlot
        {
            public Button button;
            public Image icon;
            public Label count;
            public Resource resource;
        }
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;

        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = CauldronManager.Instance) || !(resources = ResourceManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "cauldron" }; root.AddToClassList("eov-cauldron"); theme.Apply(root); document.rootVisualElement.Add(root);
            ToolkitGameplay.Apply(root,theme);root.AddToClassList("live-cauldron");mobileTabs=E(root,"row tabs");foreach(var tab in new[]{"Ingredients","Tasting","Collection"}){string chosen=tab;var button = ToolkitGameplay.B(mobileTabs,"",()=>{mobileTab=chosen;ApplyMobileTabs();},"tab"); button.userData=chosen; ToolkitLocalization.Bind(button, "cauldron.tab-"+chosen.ToLowerInvariant(), chosen);}var work=E(root,"workspace row");ingredientsColumn=E(work,"column ingredients");tastingColumn=E(work,"column brewing");collectionColumn=E(work,"column collection");BuildMixing();BuildDrinking();
            LocalizedLabel(collectionColumn, "cauldron.collection", "Collection","heading");var scroll=ToolkitGameplay.Scroll(collectionColumn,"collection-scroll");var tooltip=E(root,"surface collection-detail");tooltip.name="collection-tooltip";
            collections=new ToolkitCauldronCollections(definition,manager,resources,scroll.contentContainer,tooltip,collectionColumn);collections.Rebuild();membershipKey=collections.MembershipKey();
            resources.OnInventoryChanged += InventoryChanged;
            manager.OnStewChanged += Changed; manager.OnWeightsChanged += Changed;
            manager.OnCardGained += Gained; manager.OnStatsChanged += StatsChanged;
            manager.OnTasteSessionStarted += Started; manager.OnTasteSessionStopped += Stopped;
            Blindsided.EventHandler.OnLoadData += Loaded; Blindsided.EventHandler.OnQuestHandin += QuestChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
            ToolkitLocalization.Changed += LocalizationChanged;
            TownWindowManager.ClearCauldronAttention();
            Refresh(); Layout(); return true;
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
            LocalizedLabel(ingredientsColumn, "cauldron.ingredients", "Ingredients", "heading");
            var scroll = ToolkitGameplay.Scroll(ingredientsColumn, "ingredients-scroll");
            foodGrid = E(scroll, "food-grid");
            var footer = E(ingredientsColumn, "mix-footer conversion-footer");
            var chosen = E(footer, "row conversion-selection");
            selectedFoodIcon = ToolkitGameplay.Icon(chosen, null, 20);
            var detail = E(chosen, "grow");
            selection = L(detail, ToolkitLocalization.Text("cauldron.choose-food", "Choose a food"), "strong");
            availableFood = L(detail, "", "small muted");
            var amountRow = E(footer, "row conversion-quantity");
            LocalizedLabel(amountRow, "cauldron.amount", "Amount", "amount-label");
            minusAmount = ToolkitGameplay.B(amountRow, "−", () => StepAmount(-1), "amount-step");
            ToolkitLocalization.BindTooltip(minusAmount, "cauldron.decrease-amount", "Decrease amount");
            quantityInput = new TextField { name = "conversion-amount", value = "0" };
            quantityInput.AddToClassList("conversion-input"); amountRow.Add(quantityInput);
            quantityInput.RegisterValueChangedCallback(_ =>
            {
                if (TryAmount(out var amount))
                    quantityInput.SetValueWithoutNotify(amount.ToString("0", CultureInfo.InvariantCulture));
                conversionError.text = ""; RefreshConversion();
            });
            plusAmount = ToolkitGameplay.B(amountRow, "+", () => StepAmount(1), "amount-step");
            ToolkitLocalization.BindTooltip(plusAmount, "cauldron.increase-amount", "Increase amount");
            maxAmount = LocalizedButton(amountRow, "cauldron.max", "Max", () => SetAmount(selectedFood ? resources.GetAmount(selectedFood) : 0), "amount-max");
            var gain = E(footer, "row between conversion-gain");
            LocalizedLabel(gain, "cauldron.stew-gained", "Stew gained"); predicted = L(gain, "+0", "strong stew-gain"); predicted.name = "predicted-stew";
            conversionError = L(footer, "", "small muted conversion-error");
            addFood = LocalizedButton(footer, "cauldron.add-food", "Add to Cauldron", AddSelectedFood, "primary");
            addFood.name = "add-to-cauldron";
        }
        private void EnsureFoodSlots(int count)
        {
            while (foodSlots.Count < count)
            {
                var slot = new FoodSlot();
                slot.button = ToolkitGameplay.B(foodGrid, "", () => SelectFood(slot.resource), "food");
                slot.button.name = "food-" + foodSlots.Count;
                slot.icon = ToolkitGameplay.Icon(slot.button, null, 20);
                L(slot.button, "", "small strong").name = "food-name";
                slot.count = L(slot.button, "", "small muted");
                foodSlots.Add(slot);
            }
        }
        private void SetAmount(double value) => quantityInput.value = Math.Floor(value).ToString("0", CultureInfo.InvariantCulture);
        private bool TryAmount(out double amount)
        {
            if (!double.TryParse(quantityInput.value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount) ||
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
            availableFood.text = selected ? ToolkitLocalization.Text("cauldron.available-food", "Available: {0} · {1} stew / unit", CalcUtils.FormatNumber(stock, true), CauldronConversion.UnitValue(selectedFood).ToString("G15", CultureInfo.InvariantCulture)) : "";
            var valid = TryAmount(out var amount) && selected && manager.CanAddToCauldron(selectedFood, amount);
            predicted.text = "+" + CalcUtils.FormatNumber(valid ? amount * CauldronConversion.UnitValue(selectedFood) : 0);
            addFood.SetEnabled(valid && !addingFood);
            quantityInput.SetEnabled(selected && !addingFood);
            minusAmount.SetEnabled(selected && amount > 0 && !addingFood);
            plusAmount.SetEnabled(selected && amount < stock && !addingFood);
            maxAmount.SetEnabled(selected && stock > 0 && !addingFood);
        }
        private void SelectFood(Resource food)
        {
            if (!food || !resources.IsUnlocked(food) || resources.GetAmount(food) <= 0 || addingFood) return;
            selectedFood = food;
            conversionSequence = oracle?.saveData != null && oracle.saveData.CauldronConversionSequence < long.MaxValue
                ? oracle.saveData.CauldronConversionSequence + 1 : 0;
            conversionError.text = ""; SetAmount(Math.Min(1, resources.GetAmount(food))); Refresh();
        }
        private void AddSelectedFood()
        {
            if (addingFood || !selectedFood || !TryAmount(out var amount)) return;
            addingFood = true; RefreshConversion();
            try
            {
                if (manager.TryAddToCauldron(selectedFood, amount, conversionSequence, out _, out var error))
                { selectedFood = null; quantityInput.SetValueWithoutNotify("0"); conversionError.text = ""; }
                else conversionError.text = error;
            }
            finally { addingFood = false; Refresh(); }
        }
        private void BuildDrinking()
        {
            LocalizedLabel(tastingColumn, "cauldron.tasting", "Tasting","heading");var eva=E(tastingColumn,"row eva tasting-eva");portrait=ToolkitGameplay.Icon(eva,definition.portrait.At(0),20);var info=E(eva,"eva-info");level=L(info,"","strong");xpFill=ToolkitGameplay.Bar(info);xp=L(info,"","small muted");
            var scroll=ToolkitGameplay.Scroll(tastingColumn,"tasting-scroll");scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;ToolkitGameplay.StyleScroll(scroll);var summary=E(scroll,"brew-summary");pot=ToolkitGameplay.Icon(summary,definition.pot.At(0),46);stew=L(summary,"","amount");LocalizedLabel(summary, "cauldron.stew", "Stew","muted");
            var rate=E(scroll,"row between readout");LocalizedLabel(rate, "cauldron.rolls-per-second", "Rolls / s","muted");L(rate,definition.config.rollsPerSecond.ToString("N0"),"strong");var cost=E(scroll,"row between readout");LocalizedLabel(cost, "cauldron.stew-per-roll", "Stew / roll","muted");rollCost=L(cost,"","strong");
            taste=LocalizedButton(scroll,"cauldron.start-tasting","Start tasting",()=>manager.StartTasting(),"primary");stop=LocalizedButton(scroll,"cauldron.pause-tasting","Pause tasting",()=>manager.StopTasting());var heading=E(scroll,"row between odds-heading");LocalizedLabel(heading, "cauldron.reward-chances", "Reward chances","strong");ToolkitGameplay.B(heading,"i",ShowTastingDetails,"details-button");oddsList=E(scroll,"odds");
            stats = new Label();
        }
        private void CloseModal(){modal?.RemoveFromHierarchy();modal=null;root?.Q(className:"workspace")?.SetEnabled(true);}
        private VisualElement Modal(string title){CloseModal();root.Q(className:"workspace")?.SetEnabled(false);modal=E(root,"modal");var d=E(modal,"surface dialog mix-dialog");L(d,title,"heading");modal.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){CloseModal();e.StopPropagation();}});return d;}
        private void ShowTastingDetails()
        {
            var d=Modal(ToolkitLocalization.Text("cauldron.tasting-details", "Tasting details"));
            var scroll=ToolkitGameplay.Scroll(d,"tasting-results");
            var heading=E(scroll,"row between odds-row");LocalizedLabel(heading, "cauldron.reward-chances", "Reward chances","strong");LocalizedLabel(heading, "cauldron.current-next", "Current → Next level","muted");
            var currentTotal=weights.Sum(w=>w.current);var nextTotal=weights.Sum(w=>w.next);
            foreach(var weight in weights)
            {
                var row=E(scroll,"row between odds-row");L(row,weight.label.Replace("Vast Surge","Surge"));
                L(row,$"{(currentTotal>0?100*weight.current/currentTotal:0):0.00}% → {(nextTotal>0?100*weight.next/nextTotal:0):0.00}%","strong");
            }
            LocalizedLabel(scroll, "cauldron.tasting-results", "Tasting results","heading");L(scroll,stats.text,"small");L(scroll,definition.rewardHelp,"small muted");
            LocalizedButton(d,"common.close","Close",CloseModal).Focus();
        }
        private void Refresh()
        {
            // Freeze the display while editing an amount; identity, not index, owns selection.
            if (selectedFood && (!resources.IsUnlocked(selectedFood) || resources.GetAmount(selectedFood) <= 0)) selectedFood = null;
            if (foods == null || !selectedFood) foods = CauldronMixingPresentation.BuildDisplayFoods(resources);
            EnsureFoodSlots(foods.Count);
            for (var i = 0; i < foodSlots.Count; i++)
            {
                var slot = foodSlots[i]; var food = i < foods.Count ? foods[i] : null;
                var known = food && resources.IsUnlocked(food);
                slot.resource = food;
                slot.button.style.display = food ? DisplayStyle.Flex : DisplayStyle.None;
                slot.button.Q<Label>("food-name").text = food ? (known ? ToolkitLocalization.Name(food) : "???") : "";
                slot.icon.sprite = food ? (known ? food.icon : food.UnknownIcon) : null;
                slot.count.text = food ? (known ? CalcUtils.FormatNumber(resources.GetAmount(food), true) : ToolkitLocalization.Text("common.undiscovered", "Undiscovered")) : "";
                slot.button.tooltip = food ? (known ? ToolkitLocalization.Name(food) : ToolkitLocalization.Text("common.undiscovered", "Undiscovered")) : "";
                slot.button.EnableInClassList("selected", food && food == selectedFood);
                slot.button.SetEnabled(known && resources.GetAmount(food) > 0 && !addingFood);
            }
            RefreshConversion();
            level.text = ToolkitLocalization.Text("cauldron.eva-level", "Eva · Level {0}", manager.EvaLevel); var needed = 50 + 10 * Mathf.Max(0, manager.EvaLevel - 1);
            xp.text = ToolkitLocalization.Text("cauldron.xp", "xp: {0:N0} / {1:N0}", manager.EvaXp, needed); xpFill.style.width = Length.Percent(Mathf.Clamp01((float)(manager.EvaXp / needed)) * 100);
            stew.text = CalcUtils.FormatNumber(manager.Stew);
            taste.style.display=manager.IsTasting?DisplayStyle.None:DisplayStyle.Flex;stop.style.display=manager.IsTasting?DisplayStyle.Flex:DisplayStyle.None;
            rollCost.text=CalcUtils.FormatNumber(manager.GetStewCostPerRoll());
            taste.SetEnabled(!manager.IsTasting && manager.Stew >= manager.GetStewCostPerRoll()); stop.SetEnabled(manager.IsTasting);
            TastingStatsFormatter.Format(statsBuilder, manager.CurrentStats, showSubcategories: true); stats.text = statsBuilder.ToString();
            RefreshWeights();
        }
        private void RefreshWeights()
        {
            var current = manager.GetEffectiveWeightsAtLevel(manager.EvaLevel); var next = manager.GetEffectiveWeightsAtLevel(manager.EvaLevel + 1);
            weights = CauldronWeightsPresentation.BuildRows(current, next, definition.config);
            var total = CauldronWeightsPresentation.ComputeTotal(current);
            if(oddsValues.Count!=weights.Count){oddsList.Clear();oddsValues.Clear();foreach(var w in weights){var row=E(oddsList,"row between odds-row");L(row,w.label.Replace("Vast Surge","Surge"));oddsValues.Add(L(row,"","strong"));}}for(int i=0;i<weights.Count;i++)oddsValues[i].text=total>0?(100*weights[i].current/total).ToString("0.0")+"%":"0%";
        }
        private void Changed() => dirty = true;
        private void InventoryChanged() { dirty = true; membershipDirty = true; }
        private void Gained(string id, int amount) { collections.Gained(id, amount); dirty = true; }
        private void StatsChanged(CauldronManager.TastingStats _) => dirty = true;
        private void Started() { collections.ClearHighlights(); dirty = true; }
        private void Stopped() { collections.Refresh(); dirty = true; }
        private void Loaded() { selectedFood = null; quantityInput.SetValueWithoutNotify("0"); collections.Rebuild(); membershipKey = collections.MembershipKey(); dirty = true; }
        private void QuestChanged(string _) { collections.Rebuild(); membershipKey = collections.MembershipKey(); dirty = true; }
        private void LocaleChanged(UnityEngine.Localization.Locale _) { collections.Rebuild(); dirty = true; }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout(){var area=ToolkitWindowLayout.SafeArea;windowLayout.Apply(root,new Rect(area.x+12,area.y+44,area.width-24,area.height-56));root.EnableInClassList("compact-width",area.width<650);bool narrow=area.width<350;root.EnableInClassList("narrow",narrow);wasNarrow=narrow;ApplyMobileTabs();}
        private void ApplyMobileTabs(){if(mobileTabs==null)return;mobileTabs.style.display=wasNarrow?DisplayStyle.Flex:DisplayStyle.None;ingredientsColumn.style.display=!wasNarrow||mobileTab=="Ingredients"?DisplayStyle.Flex:DisplayStyle.None;tastingColumn.style.display=!wasNarrow||mobileTab=="Tasting"?DisplayStyle.Flex:DisplayStyle.None;collectionColumn.style.display=!wasNarrow||mobileTab=="Collection"?DisplayStyle.Flex:DisplayStyle.None;foreach(var b in mobileTabs.Query<Button>().ToList())b.EnableInClassList("active",(string)b.userData==mobileTab);}
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (localizationDirty)
            {
                localizationDirty = false;
                collections?.Rebuild(); oddsList?.Clear(); oddsValues.Clear();
                Refresh();
                if (modal != null) ShowTastingDetails();
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
            if (resources) resources.OnInventoryChanged -= InventoryChanged;
            if (manager)
            {
                manager.OnStewChanged -= Changed; manager.OnWeightsChanged -= Changed; manager.OnCardGained -= Gained;
                manager.OnStatsChanged -= StatsChanged; manager.OnTasteSessionStarted -= Started; manager.OnTasteSessionStopped -= Stopped;
            }
            Blindsided.EventHandler.OnLoadData -= Loaded; Blindsided.EventHandler.OnQuestHandin -= QuestChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            ToolkitLocalization.Changed -= LocalizationChanged;
            CloseModal();root?.RemoveFromHierarchy(); root = null; collections = null; foodSlots.Clear();oddsValues.Clear(); dirty = membershipDirty = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
