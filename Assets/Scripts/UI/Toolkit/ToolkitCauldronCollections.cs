using System.Collections.Generic;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;
using System.Linq;
using TimelessEchoes.Buffs;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI.Cauldron;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.Oracle;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native collection cards, section tiers and the existing card details.</summary>
    internal sealed class ToolkitCauldronCollections
    {
        private readonly ToolkitCauldronDefinition definition;
        private readonly CauldronManager manager;
        private readonly ResourceManager resources;
        private readonly VisualElement content, tooltip;
        private readonly Label tooltipText;
        private readonly Dictionary<string, Resource> resourceById = new();
        private readonly Dictionary<string, BuffRecipe> buffById = new();
        private readonly Dictionary<string, InfinityCauldronStatSO> infinityById = new();
        private readonly Dictionary<string, Card> cards = new();
        private readonly List<Section> sections = new();
        private string tooltipId;private string activeTab="Resources";private VisualElement tabs,legend;private Label empty;
        private float nextRefresh;
        private sealed class Card
        {
            public string id;
            public VisualElement border, background, fill;
            public Label count, effect;public int lastTier=-1;
            public float highlightedUntil = -1;
        }
        private sealed class Section
        {
            public VisualElement border, background;public Label bonus;public int lastTier=-1;
            public CauldronManager.AEResourceGroup? group;
            public bool buffs, infinity;
            public readonly List<string> ids = new();
        }
        public ToolkitCauldronCollections(ToolkitCauldronDefinition definition, CauldronManager manager,
            ResourceManager resources, VisualElement content, VisualElement tooltip,VisualElement tabParent)
        {
            this.definition = definition; this.manager = manager; this.resources = resources;
            this.content = content; this.tooltip = tooltip;
            tooltipText = new Label { name = "collection-tooltip-text", pickingMode = PickingMode.Ignore };
            tooltipText.AddToClassList("eov-cauldron-tooltip-text"); tooltip.Add(tooltipText);
            tabs=E(tabParent,"row tabs");tabParent.Insert(1,tabs);foreach(var name in new[]{"Resources","Buffs","Eternal"}){string selected=name;ToolkitGameplay.B(tabs,name,()=>{activeTab=selected;SelectTab();},"tab"+(name=="Eternal"?" last-tab":""));}legend=E(tabParent,"row legend");tabParent.Insert(2,legend);for(int i=1;i<=8;i++)L(legend,"T"+i,"tier-swatch rarity-badge tier-"+i);HideTooltip();
        }
        private void SelectTab(){foreach(var section in sections)section.border.style.display=(activeTab=="Resources"?!section.buffs&&!section.infinity:activeTab=="Buffs"?section.buffs:section.infinity)?DisplayStyle.Flex:DisplayStyle.None;foreach(var button in tabs.Query<Button>().ToList())button.EnableInClassList("active",button.text==activeTab);legend.style.visibility=activeTab=="Eternal"?Visibility.Hidden:Visibility.Visible;if(empty!=null)empty.style.display=activeTab=="Eternal"&&!manager.IsInfinityActive()?DisplayStyle.Flex:DisplayStyle.None;}

        public string MembershipKey()
        {
            var qm = TimelessEchoes.Quests.QuestManager.Instance;
            return string.Join("|", Blindsided.Utilities.AssetCache.GetAll<Resource>("").Where(r => r && !r.DisableAlterEcho && resources.IsUnlocked(r, oracle?.saveData)).Select(r => r.name))
                + string.Join("|", BuffRecipe.LoadAvailable("").Where(b => b && (!b.requiredQuest || qm && qm.IsQuestCompleted(b.requiredQuest))).Select(b => b.name)) + manager.IsInfinityActive();
        }
        public void Rebuild()
        {
            HideTooltip(); content.Clear(); cards.Clear(); sections.Clear(); resourceById.Clear(); buffById.Clear(); infinityById.Clear();
            if (manager.IsInfinityActive())
            {
                var section = AddSection("Eternal Boons", null, false, true);
                foreach (var stat in Blindsided.Utilities.AssetCache.GetAll<InfinityCauldronStatSO>("Infinity"))
                    if (stat) {var id="INF:"+stat.Stat;infinityById[id]=stat;AddCard(section,id,stat.Icon);}
            }
            var qm = TimelessEchoes.Quests.QuestManager.Instance;
            var buffs = BuffRecipe.LoadAvailable("").Where(b => b && (!b.requiredQuest || qm && qm.IsQuestCompleted(b.requiredQuest))).ToList();
            if (buffs.Count > 0)
            {
                var section = AddSection("Buffs", null, true, false);
                foreach (var buff in buffs) { var id = "BUFF:" + buff.name; buffById[id] = buff; AddCard(section, id, buff.buffIcon); }
            }
            var groups = new Dictionary<CauldronManager.AEResourceGroup, Section>();
            foreach (var resource in Blindsided.Utilities.AssetCache.GetAll<Resource>("").Where(r => r && !r.DisableAlterEcho && resources.IsUnlocked(r, oracle?.saveData)).OrderBy(r => r.resourceID).ThenBy(r => r.name))
            {
                var group = manager.GetResourceGroup(resource);
                if (!groups.TryGetValue(group, out var section))
                {
                    section = AddSection("Resources — " + CauldronCollectionPresentation.FormatGroupName(group), group, false, false);
                    groups[group] = section;
                }
                var id = "RES:" + resource.name; resourceById[id] = resource; AddCard(section, id, resource.icon);
            }
            empty=L(content,"Max all collections to unlock Eternal Boons.","muted");SelectTab();Refresh();
        }
        private Section AddSection(string title, CauldronManager.AEResourceGroup? group, bool buffs, bool infinity)
        {
            var section = new Section { group = group, buffs = buffs, infinity = infinity };
            section.border=E(content,"collection-section");var head=E(section.border,"row between group-heading");L(head,title.Replace("Resources — ",""),"strong");var bonus=E(head,"row section-bonus");bonus.Add(new ToolkitRarityStar());section.bonus=L(bonus,"","small muted");section.background=E(section.border,"card-grid");
            sections.Add(section); return section;
        }
        private void AddCard(Section section, string id, Sprite sprite)
        {
            var card = new Card { id = id }; cards[id] = card; section.ids.Add(id);
            var button=ToolkitGameplay.B(section.background,"",()=>{if(tooltipId==id)HideTooltip();else ShowTooltip(id);},"card");button.name="card-"+id;card.border=button;card.background=button;ToolkitGameplay.Icon(button,sprite,18);
            string title=resourceById.TryGetValue(id,out var resource)?resource.name:buffById.TryGetValue(id,out var buff)?buff.GetDisplayName():infinityById.TryGetValue(id,out var infinity)?infinity.DisplayName:id.Substring(4);L(button,title,"small strong card-name");card.count=L(button,"","small muted");var progress=E(button,"row card-progress-row");progress.Add(new ToolkitRarityStar());card.fill=ToolkitGameplay.Bar(progress,"collection-progress");
            if(infinityById.ContainsKey(id)){progress.style.display=DisplayStyle.None;card.effect=L(button,"","small strong");}
            button.RegisterCallback<PointerEnterEvent>(e=>{if(e.pointerType=="mouse")ShowTooltip(id);});button.RegisterCallback<PointerLeaveEvent>(e=>{if(e.pointerType=="mouse"&&tooltipId==id)HideTooltip();});button.RegisterCallback<FocusInEvent>(_=>ShowTooltip(id));button.RegisterCallback<FocusOutEvent>(_=>HideTooltip());
        }
        private Sprite Tier(Sprite[] sprites, int tier) => sprites.Length == 0 ? null : sprites[Mathf.Clamp(tier - 1, 0, sprites.Length - 1)];
        private int CardTier(string id) => id.StartsWith("INF:") ? 7 : Mathf.Max(1, id.StartsWith("RES:") ? manager.GetResourceTier(id.Substring(4)) : manager.GetBuffTier(id.Substring(5)));
        private int SectionTier(Section section) => section.infinity ? 7 : section.buffs ? manager.GetBuffsGroupTier() : CauldronResourceYield.CategoryTier(oracle?.saveData, section.group.Value, definition.config);
        private int GroupTier(CauldronManager.AEResourceGroup group) => sections.FirstOrDefault(s => s.group == group) is { } section ? SectionTier(section) : 0;
        public void Refresh()
        {
            if (oracle?.saveData == null) return;
            foreach (var card in cards.Values)
            {
                var tier = CardTier(card.id); var infinity = card.id.StartsWith("INF:"); var fill = infinity ? 0 : manager.GetTierFill01(card.id);
                if(card.lastTier!=tier){card.border.RemoveFromClassList("tier-"+card.lastTier);card.border.AddToClassList("tier-"+tier);card.lastTier=tier;}
                oracle.saveData.CauldronCardCounts.TryGetValue(card.id, out var count);
                if(infinity && infinityById.TryGetValue(card.id,out var stat))
                {
                    card.count.text=count.ToString("N0")+" cards";
                    var value=manager.GetInfinityValueFor(stat.Stat,out var percent);
                    card.effect.text="+"+value.ToString("N"+Mathf.Clamp(stat.DecimalPlaces,0,6))+(percent?"%":"");
                }
                else
                {
                    var resource=card.id.StartsWith("RES:");
                    var thresholds=resource?definition.config.resourceTierThresholds:definition.config.buffTierThresholds;
                    int actual=resource?manager.GetResourceTier(card.id.Substring(4)):manager.GetBuffTier(card.id.Substring(5));
                    bool maxed=thresholds.Length==0||actual>=thresholds.Length;
                    card.count.text=maxed?"Max":count+" / "+thresholds[Mathf.Clamp(actual,0,thresholds.Length-1)];
                    card.fill.style.width=Length.Percent(maxed?100:Mathf.Clamp01(fill)*100);
                }
                card.border.EnableInClassList("reward-highlight", card.highlightedUntil >= Time.unscaledTime);
            }
            foreach (var section in sections)
            {
                var tier = SectionTier(section);
                if(section.lastTier!=tier){section.bonus.parent.RemoveFromClassList("tier-"+section.lastTier);section.bonus.parent.AddToClassList("tier-"+tier);section.lastTier=tier;}section.bonus.parent.style.display=section.infinity?DisplayStyle.None:DisplayStyle.Flex;section.bonus.text=section.infinity?"":"+"+(section.buffs ? tier * 2.5f : CauldronResourceYield.CategoryBonusPercent(tier, definition.config)).ToString("0.#")+"%";
            }
            if (tooltipId != null) ShowTooltip(tooltipId);
        }
        public void Gained(string id, int count)
        {
            if (cards.TryGetValue(id, out var card)) card.highlightedUntil = Time.unscaledTime + 1;
        }
        public void ClearHighlights() { foreach (var card in cards.Values) card.highlightedUntil = -1; Refresh(); }
        public void Tick()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f; Refresh();
        }
        public void ShowTooltip(string id)
        {
            tooltipId = id;
            tooltipText.text = CauldronCollectionPresentation.BuildTooltipText(id, manager, resourceById, buffById, GroupTier, out var tier, out var infinity);
            tooltipText.style.whiteSpace=WhiteSpace.Normal;
            tooltip.style.display = DisplayStyle.Flex;
        }
        public void HideTooltip() { tooltipId = null; tooltip.style.display = DisplayStyle.None; }
    }
}
