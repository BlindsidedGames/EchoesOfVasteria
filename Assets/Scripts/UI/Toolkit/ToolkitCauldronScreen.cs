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
        private VisualElement root, xpFill, weightsTooltip, pie, ingredientsColumn, tastingColumn, collectionColumn, oddsList, modal;
        private Label selection;private VisualElement mobileTabs;private string mobileTab="Ingredients";private bool wasNarrow;private readonly List<Label> oddsValues=new();
        private Label level, xp, stew, stats, predicted, rollCost;
        private Image portrait, pot, selectedIconA, selectedIconB, arrow;
        private Button mix, mixAll, taste, stop;
        private readonly List<FoodSlot> foodSlots = new();
        private List<Resource> foods;
        private Resource selectedA, selectedB;
        private bool nextGreen = true, dirty, membershipDirty;
        private float nextMembershipCheck;
        private string membershipKey;
        private ToolkitCauldronCollections collections;
        private List<(string label, float current, float next, Color color)> weights;
        private readonly StringBuilder statsBuilder = new(512);
        private readonly List<Label> weightColumns = new();
        private sealed class FoodSlot
        {
            public Button button;
            public Image icon;
            public Label count;
            public VisualElement green, white;
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
            ToolkitGameplay.Apply(root,theme);root.AddToClassList("live-cauldron");mobileTabs=E(root,"row tabs");foreach(var tab in new[]{"Ingredients","Tasting","Collection"}){string chosen=tab;ToolkitGameplay.B(mobileTabs,tab,()=>{mobileTab=chosen;ApplyMobileTabs();},"tab");}var work=E(root,"workspace row");ingredientsColumn=E(work,"column ingredients");tastingColumn=E(work,"column brewing");collectionColumn=E(work,"column collection");BuildMixing();BuildDrinking();
            L(collectionColumn,"Collection","heading");var scroll=ToolkitGameplay.Scroll(collectionColumn,"collection-scroll");var tooltip=E(root,"surface collection-detail");tooltip.name="collection-tooltip";
            collections=new ToolkitCauldronCollections(definition,manager,resources,scroll.contentContainer,tooltip,collectionColumn);collections.Rebuild();membershipKey=collections.MembershipKey();
            resources.OnInventoryChanged += InventoryChanged;
            manager.OnStewChanged += Changed; manager.OnWeightsChanged += Changed;
            manager.OnCardGained += Gained; manager.OnStatsChanged += StatsChanged;
            manager.OnTasteSessionStarted += Started; manager.OnTasteSessionStopped += Stopped;
            Blindsided.EventHandler.OnLoadData += Loaded; Blindsided.EventHandler.OnQuestHandin += QuestChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
            TownWindowManager.ClearCauldronAttention();
            Refresh(); Layout(); return true;
        }
        private void BuildMixing()
        {
            L(ingredientsColumn,"Ingredients","heading");var scroll=ToolkitGameplay.Scroll(ingredientsColumn,"ingredients-scroll");var grid=E(scroll,"food-grid");
            for(int i=0;i<30;i++){int index=i;var slot=new FoodSlot();slot.button=ToolkitGameplay.B(grid,"",()=>SelectFood(index),"food");slot.button.name="food-"+i;slot.icon=ToolkitGameplay.Icon(slot.button,null,20);L(slot.button,"","small strong").name="food-name";slot.count=L(slot.button,"","small muted");slot.green=new VisualElement();slot.white=new VisualElement();foodSlots.Add(slot);}
            var footer=E(ingredientsColumn,"mix-footer");var summary=E(footer,"row mix-summary");selection=L(summary,"Choose two ingredients","selection");predicted=L(summary,"","stew-gain");predicted.name="predicted-stew";
            mix=ToolkitGameplay.B(footer,"Mix",()=>ConfirmMix(false),"primary");mix.name="mix";mixAll=ToolkitGameplay.B(footer,"Mix all pairs",()=>ConfirmMix(true));mixAll.name="mix-all";
            selectedIconA=new Image();selectedIconB=new Image();arrow=new Image();
        }
        private void BuildDrinking()
        {
            L(tastingColumn,"Tasting","heading");var eva=E(tastingColumn,"row eva tasting-eva");portrait=ToolkitGameplay.Icon(eva,definition.portrait.At(0),20);var info=E(eva,"eva-info");level=L(info,"","strong");xpFill=ToolkitGameplay.Bar(info);xp=L(info,"","small muted");
            var scroll=ToolkitGameplay.Scroll(tastingColumn,"tasting-scroll");scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;ToolkitGameplay.StyleScroll(scroll);var summary=E(scroll,"brew-summary");pot=ToolkitGameplay.Icon(summary,definition.pot.At(0),46);stew=L(summary,"","amount");L(summary,"Stew","muted");
            var rate=E(scroll,"row between readout");L(rate,"Rolls / s","muted");L(rate,definition.config.rollsPerSecond.ToString("N0"),"strong");var cost=E(scroll,"row between readout");L(cost,"Stew / roll","muted");rollCost=L(cost,"","strong");
            taste=ToolkitGameplay.B(scroll,"Start tasting",()=>manager.StartTasting(),"primary");stop=ToolkitGameplay.B(scroll,"Pause tasting",()=>manager.StopTasting());var heading=E(scroll,"row between odds-heading");L(heading,"Reward chances","strong");ToolkitGameplay.B(heading,"i",ShowTastingDetails,"details-button");oddsList=E(scroll,"odds");
            stats=new Label();pie=new VisualElement();weightsTooltip=new VisualElement();for(int i=0;i<4;i++)weightColumns.Add(new Label());
        }
        private void CloseModal(){modal?.RemoveFromHierarchy();modal=null;root?.Q(className:"workspace")?.SetEnabled(true);}
        private VisualElement Modal(string title){CloseModal();root.Q(className:"workspace")?.SetEnabled(false);modal=E(root,"modal");var d=E(modal,"surface dialog mix-dialog");L(d,title,"heading");modal.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){CloseModal();e.StopPropagation();}});return d;}
        private void ShowTastingDetails()
        {
            var d=Modal("Tasting details");
            var scroll=ToolkitGameplay.Scroll(d,"tasting-results");
            var heading=E(scroll,"row between odds-row");L(heading,"Reward chances","strong");L(heading,"Current → Next level","muted");
            var currentTotal=weights.Sum(w=>w.current);var nextTotal=weights.Sum(w=>w.next);
            foreach(var weight in weights)
            {
                var row=E(scroll,"row between odds-row");L(row,weight.label.Replace("Vast Surge","Surge"));
                L(row,$"{(currentTotal>0?100*weight.current/currentTotal:0):0.00}% → {(nextTotal>0?100*weight.next/nextTotal:0):0.00}%","strong");
            }
            L(scroll,"Tasting results","heading");L(scroll,stats.text,"small");L(scroll,definition.rewardHelp,"small muted");
            ToolkitGameplay.B(d,"Close",CloseModal).Focus();
        }
        private void ConfirmMix(bool all)
        {
            var targets=all?CauldronMixingPresentation.BuildEligibleFoods(resources).Where(r=>resources.GetAmount(r)>0).ToList():new List<Resource>{selectedA,selectedB};if(all&&targets.Count%2!=0)targets.RemoveAt(targets.Count-1);if(targets.Count<2||targets.Any(r=>!r))return;
            var d=Modal(all?$"Mix {targets.Count/2} pairs":"Mix ingredients");L(d,$"Consumes {targets.Count} ingredient stacks.","muted confirmation-copy");var list=ToolkitGameplay.Scroll(d,"confirmation-items");list.style.maxHeight=144;double total=0;
            foreach(var r in targets){var row=E(list,"row confirmation-item");ToolkitGameplay.Icon(row,r.icon,12);L(row,r.name,"small confirmation-name");double amount=resources.GetAmount(r);L(row,CalcUtils.FormatNumber(amount,true),"small confirmation-count");total+=amount*r.baseValue*r.valueMultiplier/100;}
            var reward=E(d,"row between confirmation-total");L(reward,"Stew","muted");L(reward,"+"+CalcUtils.FormatNumber(total,true),"heading");var actions=E(d,"row actions");ToolkitGameplay.B(actions,"Cancel",CloseModal,"cancel-action").Focus();ToolkitGameplay.B(actions,"Mix",()=>{CloseModal();for(int i=0;i+1<targets.Count;i+=2)if(resources.GetAmount(targets[i])>0&&resources.GetAmount(targets[i+1])>0)manager.MixMax(targets[i],targets[i+1]);selectedA=selectedB=null;nextGreen=true;Refresh();},"primary");
        }
        private static void Place(VisualElement element, float x, float y, float width, float height)
        {
            element.style.position = Position.Absolute; element.style.left = x; element.style.top = y; element.style.width = width; element.style.height = height;
        }
        private VisualElement Frame(VisualElement parent, Sprite sprite, float x, float y, float width, float height)
        {
            var element = new VisualElement(); Place(element, x, y, width, height); ToolkitTheme.Background(element, sprite); parent.Add(element); return element;
        }
        private Image Icon(VisualElement parent, Sprite sprite, float x, float y, float width, float height)
        {
            var image = new Image { sprite = sprite, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit }; Place(image, x, y, width, height); parent.Add(image); return image;
        }
        private Label Text(VisualElement parent, string value, float size, float x, float y, float width, float height)
        {
            var text = new Label(value) { pickingMode = PickingMode.Ignore }; text.AddToClassList("eov-cauldron-text"); text.style.fontSize = size; text.style.letterSpacing = size * .02f; Place(text, x, y, width, height); parent.Add(text); return text;
        }
        private Button Button(VisualElement parent, string name, string text, System.Action action, float x, float y, float width, float height)
        {
            var button = ToolkitBuffsScreen.MakeButton(name, action, definition.button); button.text = "<b>" + text + "</b>"; button.style.fontSize = 7; button.AddToClassList("eov-cauldron-action"); Place(button, x, y, width, height); parent.Add(button); return button;
        }
        private VisualElement Fill(VisualElement parent, float width, float height)
        {
            var clip = new VisualElement { pickingMode = PickingMode.Ignore }; clip.style.height = height; clip.style.overflow = Overflow.Hidden; parent.Add(clip);
            var image = Frame(clip, definition.xpFill, 0, 0, width, height); image.style.unityBackgroundImageTintColor = definition.xpColor; return clip;
        }
        private void SelectFood(int index)
        {
            if (index >= foods.Count) return; var food = foods[index];
            if (resources.GetAmount(food) <= 0 || food == selectedA || food == selectedB) return;
            if (!selectedA) { selectedA = food; nextGreen = false; }
            else if (!selectedB) { selectedB = food; nextGreen = true; }
            else if (nextGreen) { selectedA = food; nextGreen = false; }
            else { selectedB = food; nextGreen = true; }
            Refresh();
        }
        private void Mix()
        {
            if (!selectedA || !selectedB) return;
            manager.MixMax(selectedA, selectedB); selectedA = selectedB = null; nextGreen = true; Refresh();
        }
        private void MixAll()
        {
            var stocked = CauldronMixingPresentation.BuildEligibleFoods(resources).Where(r => resources.GetAmount(r) > 0).ToList();
            for (var i = 0; i + 1 < stocked.Count; i += 2)
                if (resources.GetAmount(stocked[i]) > 0 && resources.GetAmount(stocked[i + 1]) > 0) manager.MixMax(stocked[i], stocked[i + 1]);
            selectedA = selectedB = null; nextGreen = true; Refresh();
        }
        private void Refresh()
        {
            foods = CauldronMixingPresentation.BuildEligibleFoods(resources);
            if (selectedA && (!foods.Contains(selectedA) || resources.GetAmount(selectedA) <= 0)) selectedA = null;
            if (selectedB && (!foods.Contains(selectedB) || resources.GetAmount(selectedB) <= 0)) selectedB = null;
            for (var i = 0; i < foodSlots.Count; i++)
            {
                var slot = foodSlots[i]; var food = i < foods.Count ? foods[i] : null;
                slot.button.Q<Label>("food-name").text=food?food.name:"";slot.button.style.display=food?DisplayStyle.Flex:DisplayStyle.None;slot.button.EnableInClassList("selected",food&&(food==selectedA||food==selectedB));slot.icon.sprite = food ? food.icon : null; slot.count.text = food ? CalcUtils.FormatNumber(resources.GetAmount(food), true) : "";
                slot.button.SetEnabled(food && resources.GetAmount(food) > 0 && food != selectedA && food != selectedB);
                slot.green.style.display = food && ((food == selectedA && !nextGreen) || (food == selectedB && nextGreen)) ? DisplayStyle.Flex : DisplayStyle.None;
                slot.white.style.display = food && ((food == selectedA && nextGreen) || (food == selectedB && !nextGreen)) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            selectedIconA.sprite = selectedA ? selectedA.icon : null; selectedIconB.sprite = selectedB ? selectedB.icon : null;
            var canMix = selectedA && selectedB && selectedA != selectedB;
            mix.SetEnabled(canMix); mixAll.SetEnabled(foods.Count(r => resources.GetAmount(r) > 0) >= 2);
            mix.style.unityBackgroundImageTintColor = mix.enabledSelf ? Color.white : definition.disabledColor;
            mixAll.style.unityBackgroundImageTintColor = mixAll.enabledSelf ? Color.white : definition.disabledColor;
            selection.text=(selectedA?selectedA.name:"Choose first")+" + "+(selectedB?selectedB.name:"Choose second");
            predicted.text = "+"+CalcUtils.FormatNumber(canMix ? (resources.GetAmount(selectedA) * selectedA.baseValue * selectedA.valueMultiplier + resources.GetAmount(selectedB) * selectedB.baseValue * selectedB.valueMultiplier) / 100 : 0)+" stew";
            arrow.sprite = canMix ? definition.arrowGreen : definition.arrowRed;
            level.text = "Eva · Level " + manager.EvaLevel; var needed = 50 + 10 * Mathf.Max(0, manager.EvaLevel - 1);
            xp.text = $"xp: {manager.EvaXp:N0} / {needed:N0}"; xpFill.style.width = Length.Percent(Mathf.Clamp01((float)(manager.EvaXp / needed)) * 100);
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
            var total = CauldronWeightsPresentation.ComputeTotal(current); var nextTotal = CauldronWeightsPresentation.ComputeTotal(next);
            if (nextTotal <= 0) nextTotal = 1;
            var columns = new[] { new StringBuilder("<b>Current</b>\n"), new StringBuilder("<sprite=9>\n"), new StringBuilder("<b>Next</b>\n"), new StringBuilder("\n") };
            if (total > 0) for (var i = 0; i < weights.Count; i++)
            {
                if (i > 0) foreach (var column in columns) column.Append('\n');
                var weight = weights[i]; columns[0].Append($"<b>{Mathf.Clamp01(weight.current / total) * 100:F2}%</b>");
                columns[1].Append($"<sprite=9 color=#{ColorUtility.ToHtmlStringRGB(weight.color)}>");
                columns[2].Append($"{Mathf.Clamp01(weight.next / nextTotal) * 100:F2}%"); columns[3].Append("• " + weight.label);
            }
            for (var i = 0; i < 4; i++) weightColumns[i].text = total > 0 ? columns[i].ToString() : "";
            if(oddsValues.Count!=weights.Count){oddsList.Clear();oddsValues.Clear();foreach(var w in weights){var row=E(oddsList,"row between odds-row");L(row,w.label.Replace("Vast Surge","Surge"));oddsValues.Add(L(row,"","strong"));}}for(int i=0;i<weights.Count;i++)oddsValues[i].text=total>0?(100*weights[i].current/total).ToString("0.0")+"%":"0%";
            pie.MarkDirtyRepaint();
        }
        private void DrawPie(MeshGenerationContext context)
        {
            if (weights == null) return;
            var total = weights.Sum(w => Mathf.Max(0, w.current)); if (total <= 0) return;
            var painter = context.painter2D; var center = new Vector2(20, 20); var angle = -90f;
            painter.fillColor = theme.textColor; painter.BeginPath(); painter.Arc(center, 20, 0, 360); painter.Fill();
            foreach (var weight in weights)
            {
                var sweep = Mathf.Max(0, weight.current) / total * 360; if (sweep <= 0) continue;
                painter.fillColor = weight.color; painter.BeginPath(); painter.MoveTo(center);
                painter.Arc(center, 19, new Angle(angle, AngleUnit.Degree), new Angle(angle + sweep, AngleUnit.Degree)); painter.ClosePath(); painter.Fill(); angle += sweep;
            }
        }
        private void Changed() => dirty = true;
        private void InventoryChanged() { dirty = true; membershipDirty = true; }
        private void Gained(string id, int amount) { collections.Gained(id, amount); dirty = true; }
        private void StatsChanged(CauldronManager.TastingStats _) => dirty = true;
        private void Started() { collections.ClearHighlights(); dirty = true; }
        private void Stopped() { collections.Refresh(); collections.ApplyCollectionsBonus(); dirty = true; }
        private void Loaded() { selectedA = selectedB = null; nextGreen = true; collections.Rebuild(); collections.ApplyCollectionsBonus(); membershipKey = collections.MembershipKey(); dirty = true; }
        private void QuestChanged(string _) { collections.Rebuild(); membershipKey = collections.MembershipKey(); dirty = true; }
        private void LocaleChanged(UnityEngine.Localization.Locale _) { collections.Rebuild(); dirty = true; }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout(){var area=ToolkitWindowLayout.SafeArea;windowLayout.Apply(root,new Rect(area.x+12,area.y+44,area.width-24,area.height-56));root.EnableInClassList("compact-width",area.width<650);bool narrow=area.width<350;root.EnableInClassList("narrow",narrow);wasNarrow=narrow;ApplyMobileTabs();}
        private void ApplyMobileTabs(){if(mobileTabs==null)return;mobileTabs.style.display=wasNarrow?DisplayStyle.Flex:DisplayStyle.None;ingredientsColumn.style.display=!wasNarrow||mobileTab=="Ingredients"?DisplayStyle.Flex:DisplayStyle.None;tastingColumn.style.display=!wasNarrow||mobileTab=="Tasting"?DisplayStyle.Flex:DisplayStyle.None;collectionColumn.style.display=!wasNarrow||mobileTab=="Collection"?DisplayStyle.Flex:DisplayStyle.None;foreach(var b in mobileTabs.Query<Button>().ToList())b.EnableInClassList("active",b.text==mobileTab);}
        private void Update()
        {
            if (!IsOpen) return; Layout();
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
            if (pie != null) pie.generateVisualContent -= DrawPie;
            CloseModal();root?.RemoveFromHierarchy(); root = null; collections = null; foodSlots.Clear();weightColumns.Clear();oddsValues.Clear(); dirty = membershipDirty = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
