using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ToolkitRunGraphView
    {
        public enum Metric { Resources, Tasks, Kills, Duration, Distance }
        private readonly Func<IReadOnlyList<GameData.RunRecord>> history;
        private readonly ToolkitStatisticsDefinition definition;
        private readonly List<Button> metrics = new(), maps = new();
        private readonly Button totals, rates;
        private readonly VisualElement plot, barHost, averageLine, details;
        private readonly Label summary, sample, empty, first, last, detailTitle, averageLabel;
        private readonly Label[] ticks = new Label[5];
        private List<GameData.RunRecord> visible = new();
        private Metric metric;
        private int map = -1, selectedRun = -1;
        private bool perMinute;
        public ToolkitRunGraphView(VisualElement body, VisualElement footer, ToolkitStatisticsDefinition definition, Func<IReadOnlyList<GameData.RunRecord>> history)
        {
            this.definition = definition; this.history = history;
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            footer.style.display = DisplayStyle.None;
            var controls = ToolkitGameplay.E(body, "graph-controls");
            foreach (Metric m in Enum.GetValues(typeof(Metric)))
            { var choice=m; metrics.Add(Choice(controls, "graph-"+m, m.ToString(),()=>SetMetric(choice))); }
            var units=ToolkitGameplay.E(controls,"graph-units");
            totals=Choice(units,"graph-totals","Totals",()=>{perMinute=false;Refresh();});
            rates=Choice(units,"graph-rates","Per minute",()=>{perMinute=true;Refresh();});
            var filters=ToolkitGameplay.E(body,"graph-map-filters");
            for(int i=-1;i<definition.maps.Length;i++) {var index=i;maps.Add(Choice(filters,"graph-map-"+(i+1),i<0?"All maps":definition.maps[i].label.Trim(),()=>{map=index;selectedRun=-1;Refresh();}));}
            var heading=ToolkitGameplay.E(body,"graph-summary-row");
            summary=Text(heading,"","graph-summary");sample=Text(heading,"","graph-sample");
            var chart=ToolkitGameplay.E(body,"graph-chart");
            var axis=ToolkitGameplay.E(chart,"graph-axis");
            for(int i=0;i<5;i++){ticks[i]=Text(axis,"","graph-tick");ticks[i].style.bottom=Length.Percent(i*25);}
            plot=ToolkitGameplay.E(chart,"graph-plot");
            for(int i=0;i<5;i++){var line=ToolkitGameplay.E(plot,"graph-grid-line");line.pickingMode=PickingMode.Ignore;line.style.bottom=Length.Percent(i*25);}
            barHost=ToolkitGameplay.E(plot,"graph-bars");
            averageLine=ToolkitGameplay.E(plot,"graph-average");averageLine.pickingMode=PickingMode.Ignore;
            averageLabel=Text(averageLine,"","graph-average-label");
            empty=Text(plot,"No saved runs for this selection.","graph-empty");
            var axisLabels=ToolkitGameplay.E(body,"graph-x-labels");first=Text(axisLabels,"","graph-first");Text(axisLabels,"Run number","graph-x-title");last=Text(axisLabels,"","graph-last");
            var legend=ToolkitGameplay.E(body,"graph-legend");
            Legend(legend,"Retreated",new Color(.72f,.66f,.61f));Legend(legend,"Reaped",new Color(.70f,.54f,.79f));Legend(legend,"Died",new Color(.82f,.40f,.40f));Legend(legend,"Abandoned",new Color(.43f,.42f,.43f));
            var detailHeader=ToolkitGameplay.E(body,"graph-detail-heading");detailTitle=Text(detailHeader,"","graph-detail-title");
            Text(detailHeader,"Hover to preview · click or tap to select","graph-detail-hint");
            details=ToolkitGameplay.E(body,"graph-details");
            Refresh();
        }
        private static Button Choice(VisualElement parent,string name,string text,Action click){var b=ToolkitGameplay.B(parent,text,click);b.name=name;b.AddToClassList("graph-choice");return b;}
        private static Label Text(VisualElement parent,string text,string classes)=>ToolkitGameplay.L(parent,text,classes);
        private static void Legend(VisualElement parent,string title,Color colour){var item=ToolkitGameplay.E(parent,"graph-legend-item");var swatch=ToolkitGameplay.E(item,"graph-legend-swatch");swatch.style.backgroundColor=colour;Text(item,title,"");}
        public void SetMetric(Metric value){metric=value;if(!SupportsRate)perMinute=false;Refresh();}
        private bool SupportsRate=>metric==Metric.Resources||metric==Metric.Tasks||metric==Metric.Kills;
        public static double Value(GameData.RunRecord run,Metric metric,bool rate)
        {
            double value=metric switch {Metric.Resources=>run.ResourcesCollected,Metric.Tasks=>run.TasksCompleted,Metric.Kills=>run.EnemiesKilled,Metric.Duration=>run.Duration,_=>run.Distance};
            return rate ? run.Duration>0?value*60/run.Duration:double.NaN : value;
        }
        private string Format(double value)=>metric==Metric.Duration?CalcUtils.FormatTime(value):ItemStatisticsPresentation.Number(value);
        public void Refresh()
        {
            var all=history();var key=map<0?null:definition.maps[map].config.name;
            var filtered=all.Where(r=>key==null||r.MapType==key).ToList();
            visible=filtered.Where(r=>!perMinute||r.Duration>0).ToList();
            var values=visible.Select(r=>Value(r,metric,perMinute)).ToArray();
            double max=values.Length>0?values.Max():0,avg=values.Length>0?values.Average():0;
            for(int i=0;i<metrics.Count;i++)metrics[i].EnableInClassList("active",i==(int)metric);
            for(int i=0;i<maps.Count;i++)maps[i].EnableInClassList("active",i==map+1);
            rates.SetEnabled(SupportsRate); rates.EnableInClassList("active",perMinute);totals.EnableInClassList("active",!perMinute);
            var suffix=perMinute?" / min":"";
            summary.text=values.Length==0?"No recent data":"Average "+Format(avg)+suffix+"   ·   "+(metric==Metric.Duration?"Longest ":"Best ")+Format(max)+suffix;
            sample.text=visible.Count+" of last "+all.Count+" saved runs"+(filtered.Count!=visible.Count?" · "+(filtered.Count-visible.Count)+" without duration omitted":"");
            var ceiling=max>0?NiceCeiling(max):1;
            for(int i=0;i<5;i++)ticks[i].text=Format(ceiling*i/4);
            barHost.Clear();var slots=Math.Max(10,visible.Count);
            for(int i=0;i<visible.Count;i++)
            {
                var run=visible[i];var value=values[i];var hit=new Button(()=>{selectedRun=run.RunNumber;ShowDetails(run);StyleSelection();}){name="graph-run-"+run.RunNumber};
                hit.AddToClassList("graph-run-bar");hit.style.width=Length.Percent(100f/slots);barHost.Add(hit);
                var fill=ToolkitGameplay.E(hit,"graph-run-fill");fill.pickingMode=PickingMode.Ignore;fill.style.height=Length.Percent((float)(value/ceiling*100));
                fill.style.backgroundColor=run.Abandoned?new Color(.43f,.42f,.43f):run.Reaped?new Color(.70f,.54f,.79f):run.Died?new Color(.82f,.40f,.40f):new Color(.72f,.66f,.61f);
                hit.RegisterCallback<PointerEnterEvent>(_=>ShowDetails(run));hit.RegisterCallback<PointerLeaveEvent>(_=>ShowSelected());
                hit.RegisterCallback<FocusInEvent>(_=>ShowDetails(run));hit.RegisterCallback<FocusOutEvent>(_=>ShowSelected());
                hit.userData=run.RunNumber;
            }
            averageLine.style.display=max>0?DisplayStyle.Flex:DisplayStyle.None;averageLine.style.bottom=Length.Percent((float)(avg/ceiling*100));averageLabel.text="Avg "+Format(avg);
            empty.style.display=visible.Count==0?DisplayStyle.Flex:DisplayStyle.None;
            first.text=visible.Count>0?"#"+visible[0].RunNumber:"";last.text=visible.Count>1?"#"+visible[visible.Count-1].RunNumber:"";
            if(!visible.Any(r=>r.RunNumber==selectedRun))selectedRun=visible.Count>0?visible[visible.Count-1].RunNumber:-1;
            StyleSelection();ShowSelected();
        }
        private static double NiceCeiling(double maximum){var power=Math.Pow(10,Math.Floor(Math.Log10(maximum)));return Math.Ceiling(maximum/power)*power;}
        private void StyleSelection(){foreach(var bar in barHost.Children())bar.EnableInClassList("selected",(int)bar.userData==selectedRun);}
        private void ShowSelected()=>ShowDetails(visible.FirstOrDefault(r=>r.RunNumber==selectedRun));
        private static string Outcome(GameData.RunRecord run)=>run.Abandoned?"Abandoned":run.Reaped?"Reaped":run.Died?"Died":"Retreated";
        private void ShowDetails(GameData.RunRecord run)
        {
            details.Clear();if(run==null){detailTitle.text="Run details";Text(details,"Select a map with saved runs to inspect its history.","graph-detail-hint");return;}
            var mapName=definition.maps.FirstOrDefault(m=>m.config&&m.config.name==run.MapType).label;
            detailTitle.text="Run #"+run.RunNumber+" · "+(string.IsNullOrWhiteSpace(mapName)?run.MapType:mapName.Trim())+" · "+Outcome(run);
            Detail("Duration",CalcUtils.FormatTime(run.Duration));Detail("Distance",ItemStatisticsPresentation.Number(run.Distance));Detail("Tasks",ItemStatisticsPresentation.Number(run.TasksCompleted));
            Detail("Resources",ItemStatisticsPresentation.Number(run.ResourcesCollected));Detail("Bonus resources",ItemStatisticsPresentation.Number(run.BonusResourcesCollected));Detail("Kills",ItemStatisticsPresentation.Number(run.EnemiesKilled));
            Detail("Damage dealt",ItemStatisticsPresentation.Number(run.DamageDealtAsDouble));Detail("Damage taken",ItemStatisticsPresentation.Number(run.DamageTakenAsDouble));
        }
        private void Detail(string title,string value){var pair=ToolkitGameplay.E(details,"graph-detail-pair");Text(pair,title,"graph-detail-key");Text(pair,value,"graph-detail-value");}
    }
}
