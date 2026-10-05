using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using TimelessEchoes.Gear;
using TimelessEchoes.Gear.UI;
using TimelessEchoes.Upgrades;
using Blindsided.Utilities;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitForgeScreen
    {
        private string comparisonSignature, equipmentSignature;
        private static void NativeSize(Image image)
        {
            var sprite = image.sprite;
            if (!sprite) return;
            image.style.width = sprite.rect.width * 16 / sprite.pixelsPerUnit;
            image.style.height = sprite.rect.height * 16 / sprite.pixelsPerUnit;
            image.style.flexShrink = 0;
        }
        private static Image FramedIcon(VisualElement parent, Sprite sprite)
        {
            var mask = new VisualElement { pickingMode = PickingMode.Ignore }; mask.AddToClassList("forge-icon-mask"); parent.Add(mask);
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; mask.Add(image); NativeSize(image); return image;
        }
        private static List<GearAffix> Affixes(GearItem item) => item?.affixes?.Where(a => a?.stat != null).ToList() ?? new List<GearAffix>();
        private static string Amount(float value, bool percent) => CalcUtils.FormatNumber(value) + (percent ? "%" : "");
        private VisualElement Metric(VisualElement parent, string title, Sprite icon, string first, string second = null, int change = 0)
        {
            var row = new VisualElement { tooltip = title }; row.AddToClassList("forge-metric"); parent.Add(row);
            if (icon) { var well = new VisualElement(); well.AddToClassList("forge-stat-icon"); row.Add(well); FramedIcon(well, icon); }
            var name = Label(row, title); name.AddToClassList("forge-metric-name");
            var a = Label(row, first); a.AddToClassList("forge-metric-value");
            a.EnableInClassList("forge-gain", change > 0); a.EnableInClassList("forge-loss", change < 0);
            if (second != null) { var b = Label(row, second); b.AddToClassList("forge-metric-value"); }
            return row;
        }
        private void RefreshComparison(GearItem pending, GearItem equipped)
        {
            var p = Affixes(pending); var e = Affixes(equipped);
            var pq = pending == null ? 0 : UpgradeEvaluator.ComputeQualityPercent(crafting, pending, session.Slot);
            var eq = equipped == null ? 0 : UpgradeEvaluator.ComputeQualityPercent(crafting, equipped, session.Slot);
            var signature = session.Slot + ":" + (pending != null) + ":" + (equipped != null) + ":" + pq + ":" + eq + string.Join(";", p.Concat(e).Select(a => a.stat.name + ":" + a.value));
            if (signature == comparisonSignature) return;
            comparisonSignature = signature; comparisonRows.Clear();
            Metric(comparisonRows, session.Slot, null, ToolkitLocalization.Text("forge.pending", "Pending"), ToolkitLocalization.Text("forge.equipped", "Equipped")).AddToClassList("forge-metric-header");
            StatIconLookup.TryGetIcon(StatIconLookup.StatKey.Quality, out var quality);
            Metric(comparisonRows, ToolkitLocalization.Text("forge.quality", "Quality"), quality, pending == null ? "—" : pq.ToString("0.#")+"%", equipped == null ? "—" : eq.ToString("0.#")+"%", pending == null ? 0 : Math.Sign(pq-eq));
            var defs = p.Concat(e).Select(a=>a.stat).GroupBy(s=>s.heroMapping).Select(g=>g.First()).ToList();
            defs.Sort((a,b)=>StatSortOrder.Compare(a.heroMapping,b.heroMapping));
            foreach(var def in defs)
            {
                var pa=p.Where(a=>a.stat.heroMapping==def.heroMapping).Sum(a=>a.value); var ea=e.Where(a=>a.stat.heroMapping==def.heroMapping).Sum(a=>a.value);
                StatIconLookup.TryGetIcon(def.heroMapping,out var icon);
                Metric(comparisonRows,def.GetName(),icon,pending == null ? "—" : Amount(pa,def.isPercent),equipped == null ? "—" : Amount(ea,def.isPercent),pending == null ? 0 : Math.Sign(pa-ea));
            }
            if(pending==null) Label(comparisonRows,ToolkitLocalization.Text("forge.compare-hint", "Craft an item to compare it with your equipment."),ToolkitControls.TextRole.Caption).AddToClassList("forge-empty-result");
        }
        private void RefreshEquipmentTotals()
        {
            var pieces=equipment.Slots.Select(slot=>(slot,item:equipment.GetEquipped(slot))).ToList();
            var all=pieces.SelectMany(x=>Affixes(x.item)).ToList();var selected=Affixes(equipment.GetEquipped(session.Slot));
            float quality=pieces.Sum(x=>x.item==null?0:UpgradeEvaluator.ComputeQualityPercent(crafting,x.item,x.slot));
            var item=equipment.GetEquipped(session.Slot);float selectedQuality=item==null?0:UpgradeEvaluator.ComputeQualityPercent(crafting,item,session.Slot);
            var sig=session.Slot+":"+quality+":"+selectedQuality+string.Join(";",all.Concat(selected).Select(a=>a.stat.name+":"+a.value));
            if(sig==equipmentSignature)return;equipmentSignature=sig;equipmentRows.Clear();
            Metric(equipmentRows,ToolkitLocalization.Text("forge.stat", "Stat"),null,ToolkitLocalization.Text("forge.total", "Total"),session.Slot).AddToClassList("forge-metric-header");
            StatIconLookup.TryGetIcon(StatIconLookup.StatKey.Quality,out var q);Metric(equipmentRows,ToolkitLocalization.Text("forge.quality", "Quality"),q,quality.ToString("0.#")+"%",selectedQuality.ToString("0.#")+"%");
            var defs=all.Select(a=>a.stat).GroupBy(s=>s.heroMapping).Select(g=>g.First()).ToList();defs.Sort((a,b)=>StatSortOrder.Compare(a.heroMapping,b.heroMapping));
            foreach(var def in defs){StatIconLookup.TryGetIcon(def.heroMapping,out var icon);Metric(equipmentRows,def.GetName(),icon,Amount(all.Where(a=>a.stat.heroMapping==def.heroMapping).Sum(a=>a.value),def.isPercent),Amount(selected.Where(a=>a.stat.heroMapping==def.heroMapping).Sum(a=>a.value),def.isPercent));}
        }
        private static string HistoryTitle(string key) => ToolkitLocalization.Text("forge.stats.section-" + key.ToLowerInvariant().Replace(" ", "-"), key);
        // Preserve the legacy presenter's complete metrics and calculations. Only its
        // section/row presentation changes; values are not recomputed or discarded.
        private void RefreshHistory(string text)
        {
            if(text==lastHistory)return;lastHistory=text;
            var sections=Regex.Split(text,@"<size=105%><b>(.*?)</b></size>");
            for(int i=1;i+1<sections.Length;i+=2)
            {
                var key=sections[i];
                if(!historySections.TryGetValue(key,out var section))
                {
                    var title=key=="Totals"?ToolkitLocalization.Text("forge.crafting-history", "Crafting history"):key=="Autocraft"?ToolkitLocalization.Text("forge.automation", "Automation"):key=="Quality"?ToolkitLocalization.Text("forge.best-rolls", "Best rolls"):HistoryTitle(key);
                    section=new ToolkitDisclosure(title,theme,key=="Totals");section.AddToClassList("forge-history-section");historySections.Add(key,section);historyHost.Add(section);
                }
                var headerTitle=key=="Totals"?ToolkitLocalization.Text("forge.crafting-history", "Crafting history"):key=="Autocraft"?ToolkitLocalization.Text("forge.automation", "Automation"):key=="Quality"?ToolkitLocalization.Text("forge.best-rolls", "Best rolls"):HistoryTitle(key);
                section.Q<Label>().text = "<b><smallcaps>" + headerTitle + "</smallcaps></b>";
                section.Content.Clear();
                var content=Regex.Replace(sections[i+1],@"</?(?:size|b|mspace)(?:=[^>]*)?>","");
                foreach(var line in content.Split('\n'))
                foreach(var part in line.Split('•'))
                {
                    var value=part.Trim();if(string.IsNullOrEmpty(value))continue;
                    var split=value.IndexOf(": ",StringComparison.Ordinal);
                    if(split>0) Metric(section.Content,value.Substring(0,split),null,value.Substring(split+2));
                    else Label(section.Content,value.TrimEnd(':'),ToolkitControls.TextRole.Caption).AddToClassList("forge-history-subtitle");
                }
            }
        }
    }
}
