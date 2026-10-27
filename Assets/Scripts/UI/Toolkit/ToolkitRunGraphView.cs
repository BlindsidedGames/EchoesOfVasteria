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
        private readonly Button totals, rates, zoomIn, zoomOut, resetZoom;
        private readonly VisualElement plot, axis, axisLabels, barHost, averageLine, details;
        private readonly Label summary, sample, empty, first, last, detailTitle, averageLabel, overflowValue;
        private readonly Label[] ticks = new Label[5];
        private List<GameData.RunRecord> visible = new();
        private Metric metric;
        private int map = -1, selectedRun = -1, zoomSteps;
        private const int MaximumZoomSteps = 18;
        private double ceiling;
        private bool perMinute;
        private readonly DropdownField portraitMetric, portraitMap;
        public ToolkitRunGraphView(VisualElement body, VisualElement footer, ToolkitStatisticsDefinition definition, Func<IReadOnlyList<GameData.RunRecord>> history)
        {
            this.definition = definition; this.history = history;
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            footer.style.display = DisplayStyle.None;
            var scroll = ToolkitGameplay.Scroll(body, "run-graph-scroll");
            scroll.style.flexGrow = 1; scroll.style.minHeight = 0;
            scroll.contentContainer.style.flexGrow = 1;
            scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(e => scroll.contentContainer.style.minHeight = e.newRect.height);
            body = scroll.contentContainer;
            body.AddToClassList("run-graph-content");
            var phoneFilters = ToolkitGameplay.E(body, "portrait-graph-pickers");
            portraitMetric = new DropdownField(Enum.GetValues(typeof(Metric)).Cast<Metric>().Select(MetricLabel).ToList(), 0);
            portraitMetric.AddToClassList("portrait-picker"); phoneFilters.Add(portraitMetric);
            portraitMetric.RegisterValueChangedCallback(_ => { if (portraitMetric.index >= 0) SetMetric((Metric)portraitMetric.index); });
            var mapNames = new List<string> { ToolkitLocalization.Text("graph.all-maps", "All maps") };
            mapNames.AddRange(definition.maps.Select(m => ToolkitLocalization.Text("map." + m.config.name, m.label.Trim())));
            portraitMap = new DropdownField(mapNames, 0); portraitMap.AddToClassList("portrait-picker"); portraitMap.AddToClassList("graph-last-picker"); phoneFilters.Add(portraitMap);
            portraitMap.RegisterValueChangedCallback(_ => { map = portraitMap.index - 1; selectedRun = -1; zoomSteps = 0; Refresh(); });
            var controls = ToolkitGameplay.E(body, "graph-controls");
            foreach (Metric m in Enum.GetValues(typeof(Metric)))
            { var choice=m; metrics.Add(Choice(controls, "graph-"+m, MetricLabel(m),()=>SetMetric(choice))); }
            var units=ToolkitGameplay.E(controls,"graph-units");
            totals=Choice(units,"graph-totals",ToolkitLocalization.Text("graph.totals", "Totals"),()=>SetRate(false));
            rates=Choice(units,"graph-rates",ToolkitLocalization.Text("graph.per-minute", "Per minute"),()=>SetRate(true));
            rates.AddToClassList("graph-last-choice");
            var filters=ToolkitGameplay.E(body,"graph-map-filters");
            for(int i=-1;i<definition.maps.Length;i++) {var index=i;maps.Add(Choice(filters,"graph-map-"+(i+1),i<0?ToolkitLocalization.Text("graph.all-maps", "All maps"):ToolkitLocalization.Text("map." + definition.maps[i].config.name, definition.maps[i].label.Trim()),()=>{map=index;selectedRun=-1;zoomSteps=0;Refresh();}));}
            maps[maps.Count - 1].AddToClassList("graph-last-choice");
            var heading=ToolkitGameplay.E(body,"graph-summary-row");
            var copy=ToolkitGameplay.E(heading,"graph-summary-copy");
            summary=Text(copy,"","graph-summary");sample=Text(copy,"","graph-sample");
            var zoom=ToolkitGameplay.E(heading,"graph-zoom-controls");
            zoomOut=Choice(zoom,"graph-zoom-out","−",()=>{zoomSteps=Math.Max(0,zoomSteps-1);Refresh();});
            zoomIn=Choice(zoom,"graph-zoom-in","+",()=>{zoomSteps=Math.Min(MaximumZoomSteps,zoomSteps+1);Refresh();});
            resetZoom=Choice(zoom,"graph-reset-zoom",ToolkitLocalization.Text("graph.reset-zoom", "Reset"),()=>{zoomSteps=0;Refresh();});
            ToolkitLocalization.BindTooltip(zoomOut,"graph.zoom-out","Zoom out");
            ToolkitLocalization.BindTooltip(zoomIn,"graph.zoom-in","Zoom in");
            ToolkitLocalization.BindTooltip(resetZoom,"graph.reset-zoom-help","Show the full range");
            var chart=ToolkitGameplay.E(body,"graph-chart");
            axis=ToolkitGameplay.E(chart,"graph-axis");
            for(int i=0;i<5;i++){ticks[i]=Text(axis,"","graph-tick");ticks[i].style.bottom=Length.Percent(i*25);}
            plot=ToolkitGameplay.E(chart,"graph-plot");
            for(int i=0;i<5;i++){var line=ToolkitGameplay.E(plot,"graph-grid-line");line.pickingMode=PickingMode.Ignore;line.style.bottom=Length.Percent(i*25);}
            barHost=ToolkitGameplay.E(plot,"graph-bars");
            averageLine=ToolkitGameplay.E(plot,"graph-average");averageLine.pickingMode=PickingMode.Ignore;
            averageLabel=Text(averageLine,"","graph-average-label");
            empty=Text(plot,ToolkitLocalization.Text("graph.empty", "No saved runs for this selection."),"graph-empty");
            overflowValue=Text(plot,"","graph-overflow-value");overflowValue.pickingMode=PickingMode.Ignore;
            plot.RegisterCallback<GeometryChangedEvent>(_=>{SizeAxis();PositionOverflowValue();});
            overflowValue.RegisterCallback<GeometryChangedEvent>(_=>PositionOverflowValue());
            axisLabels=ToolkitGameplay.E(body,"graph-x-labels");first=Text(axisLabels,"","graph-first");Text(axisLabels,ToolkitLocalization.Text("graph.run-number", "Run number"),"graph-x-title");last=Text(axisLabels,"","graph-last");
            var legend=ToolkitGameplay.E(body,"graph-legend");
            Legend(legend,ToolkitLocalization.Text("run.outcome-retreated", "Retreated"),"retreated");Legend(legend,ToolkitLocalization.Text("run.outcome-reaped", "Reaped"),"reaped");Legend(legend,ToolkitLocalization.Text("run.outcome-died", "Died"),"died");Legend(legend,ToolkitLocalization.Text("run.outcome-abandoned", "Abandoned"),"abandoned");
            var detailHeader=ToolkitGameplay.E(body,"graph-detail-heading");detailTitle=Text(detailHeader,"","graph-detail-title");
            Text(detailHeader,ToolkitLocalization.Text("graph.select-bar", "Select a bar for details"),"graph-detail-hint");
            details=ToolkitGameplay.E(body,"graph-details");
            Refresh();
        }
        private static string MetricLabel(Metric metric) => metric switch
        {
            Metric.Resources => ToolkitLocalization.Text("graph.resources", "Resources"),
            Metric.Tasks => ToolkitLocalization.Text("graph.tasks", "Tasks"),
            Metric.Kills => ToolkitLocalization.Text("graph.kills", "Kills"),
            Metric.Duration => ToolkitLocalization.Text("graph.duration", "Duration"),
            _ => ToolkitLocalization.Text("graph.distance", "Distance")
        };
        private static Button Choice(VisualElement parent,string name,string text,Action click){var b=ToolkitGameplay.B(parent,text,click);b.name=name;b.AddToClassList("graph-choice");return b;}
        private static Label Text(VisualElement parent,string text,string classes)=>ToolkitGameplay.L(parent,text,classes);
        private static void Legend(VisualElement parent,string title,string outcome){var item=ToolkitGameplay.E(parent,"graph-legend-item");var swatch=ToolkitGameplay.E(item,"graph-legend-swatch");swatch.AddToClassList("graph-outcome-"+outcome);Text(item,title,"");}
        public (Metric metric, int map, int selectedRun, bool perMinute, int zoomSteps) ViewState => (metric, map, selectedRun, perMinute, zoomSteps);
        public void RestoreViewState((Metric metric, int map, int selectedRun, bool perMinute, int zoomSteps) state)
        {
            metric = state.metric; map = Mathf.Clamp(state.map, -1, definition.maps.Length - 1);
            selectedRun = state.selectedRun; perMinute = state.perMinute && SupportsRate;
            zoomSteps = Mathf.Clamp(state.zoomSteps, 0, MaximumZoomSteps); Refresh();
        }
        public void SetMetric(Metric value){if(metric!=value)zoomSteps=0;metric=value;if(!SupportsRate)perMinute=false;Refresh();}
        private void SetRate(bool value){if(perMinute!=value)zoomSteps=0;perMinute=value;Refresh();}
        private bool SupportsRate=>metric==Metric.Resources||metric==Metric.Tasks||metric==Metric.Kills;
        public static double Value(GameData.RunRecord run,Metric metric,bool rate)
        {
            double value=metric switch {Metric.Resources=>run.ResourcesCollected,Metric.Tasks=>run.TasksCompleted,Metric.Kills=>run.EnemiesKilled,Metric.Duration=>run.Duration,_=>run.Distance};
            return rate ? run.Duration>0?value*60/run.Duration:double.NaN : value;
        }
        private string Format(double value)=>metric==Metric.Duration?CalcUtils.FormatTime(value):ItemStatisticsPresentation.Number(value);
        public void Refresh()
        {
            portraitMetric.SetValueWithoutNotify(MetricLabel(metric));
            portraitMap.SetValueWithoutNotify(portraitMap.choices[Mathf.Clamp(map + 1, 0, portraitMap.choices.Count - 1)]);
            var all=history();var key=map<0?null:definition.maps[map].config.name;
            var filtered=all.Where(r=>key==null||r.MapType==key).ToList();
            visible=filtered.Where(r=>!perMinute||r.Duration>0).ToList();
            var omitted = filtered.Count - visible.Count;
            if (ToolkitResponsiveLayout.IsPortrait && visible.Count > 10)
                visible = visible.Skip(visible.Count - 10).ToList();
            var values=visible.Select(r=>Value(r,metric,perMinute)).ToArray();
            double max=values.Length>0?values.Max():0,avg=values.Length>0?values.Average():0;
            for(int i=0;i<metrics.Count;i++)metrics[i].EnableInClassList("active",i==(int)metric);
            for(int i=0;i<maps.Count;i++)maps[i].EnableInClassList("active",i==map+1);
            rates.SetEnabled(SupportsRate); rates.EnableInClassList("active",perMinute);totals.EnableInClassList("active",!perMinute);
            var suffix=perMinute?ToolkitLocalization.Text("graph.rate-suffix", " / min"):"";
            summary.text=values.Length==0?ToolkitLocalization.Text("graph.no-data", "No recent data"):ToolkitLocalization.Text("graph.average-best", "Average {0}{1}   ·   {2} {3}{1}", Format(avg), suffix, metric==Metric.Duration?ToolkitLocalization.Text("graph.longest", "Longest"):ToolkitLocalization.Text("graph.best", "Best"), Format(max));
            var omittedText = omitted > 0 ? ToolkitLocalization.Text("graph.omitted", " · {0} without duration omitted", omitted) : "";
            sample.text = ToolkitResponsiveLayout.IsPortrait
                ? ToolkitLocalization.Text("graph.recent-sample", "Latest {0} runs{1}", visible.Count, omittedText)
                : ToolkitLocalization.Text("graph.sample", "{0} of last {1} saved runs{2}", visible.Count, all.Count, omittedText);
            ceiling=max>0?NiceCeiling(max):1;
            for(var step=0;step<zoomSteps;step++)ceiling=LowerCeiling(ceiling);
            if(!perMinute)ceiling=Math.Max(1,ceiling);
            zoomIn.SetEnabled(max>0&&zoomSteps<MaximumZoomSteps&&(perMinute||LowerCeiling(ceiling)>=1));
            zoomOut.SetEnabled(zoomSteps>0);resetZoom.SetEnabled(zoomSteps>0);
            for(int i=0;i<5;i++)ticks[i].text=Format(ceiling*i/4);
            SizeAxis();
            barHost.Clear();var slots=Math.Max(10,visible.Count);
            for(int i=0;i<visible.Count;i++)
            {
                var run=visible[i];var value=values[i];var hit=new Button(()=>{selectedRun=run.RunNumber;ShowDetails(run);StyleSelection();}){name="graph-run-"+run.RunNumber};
                hit.AddToClassList("graph-run-bar");hit.style.width=Length.Percent(100f/slots);barHost.Add(hit);
                var fill=ToolkitGameplay.E(hit,"graph-run-fill");fill.pickingMode=PickingMode.Ignore;fill.style.height=Length.Percent((float)(Math.Min(value/ceiling,1)*100));
                fill.AddToClassList("graph-outcome-"+(run.Abandoned?"abandoned":run.Reaped?"reaped":run.Died?"died":"retreated"));
                hit.tooltip=Format(value)+suffix;
                if(value>ceiling){var overflow=Text(hit,"↑","graph-overflow-marker");overflow.pickingMode=PickingMode.Ignore;}
                hit.RegisterCallback<PointerEnterEvent>(_=>ShowDetails(run));hit.RegisterCallback<PointerLeaveEvent>(_=>ShowSelected());
                hit.RegisterCallback<FocusInEvent>(_=>ShowDetails(run));hit.RegisterCallback<FocusOutEvent>(_=>ShowSelected());
                hit.userData=run.RunNumber;
            }
            averageLine.style.display=max>0&&avg<=ceiling?DisplayStyle.Flex:DisplayStyle.None;averageLine.style.bottom=Length.Percent((float)(avg/ceiling*100));averageLabel.text=ToolkitLocalization.Text("graph.average-marker", "Avg {0}", Format(avg));
            empty.style.display=visible.Count==0?DisplayStyle.Flex:DisplayStyle.None;
            first.text=visible.Count>0?"#"+visible[0].RunNumber:"";last.text=visible.Count>1?"#"+visible[visible.Count-1].RunNumber:"";
            if(!visible.Any(r=>r.RunNumber==selectedRun))selectedRun=visible.Count>0?visible[visible.Count-1].RunNumber:-1;
            StyleSelection();ShowSelected();
        }
        private static double NiceCeiling(double maximum){var power=Math.Pow(10,Math.Floor(Math.Log10(maximum)));return Math.Ceiling(maximum/power)*power;}
        private static double LowerCeiling(double value)
        {
            var power=Math.Pow(10,Math.Floor(Math.Log10(value)));
            var fraction=value/power;
            return fraction>5.000001?5*power:fraction>2.000001?2*power:fraction>1.000001?power:power/2;
        }
        private void StyleSelection(){foreach(var bar in barHost.Children())bar.EnableInClassList("selected",(int)bar.userData==selectedRun);}
        private void ShowSelected()=>ShowDetails(visible.FirstOrDefault(r=>r.RunNumber==selectedRun));
        private static string Outcome(GameData.RunRecord run)=>run.Abandoned?ToolkitLocalization.Text("run.outcome-abandoned", "Abandoned"):run.Reaped?ToolkitLocalization.Text("run.outcome-reaped", "Reaped"):run.Died?ToolkitLocalization.Text("run.outcome-died", "Died"):ToolkitLocalization.Text("run.outcome-retreated", "Retreated");
        private void ShowDetails(GameData.RunRecord run)
        {
            overflowValue.text=run!=null&&Value(run,metric,perMinute)>ceiling?Format(Value(run,metric,perMinute)):"";
            overflowValue.style.display=string.IsNullOrEmpty(overflowValue.text)?DisplayStyle.None:DisplayStyle.Flex;
            overflowValue.userData=run?.RunNumber;PositionOverflowValue();
            details.Clear();if(run==null){detailTitle.text=ToolkitLocalization.Text("graph.details", "Run details");Text(details,ToolkitLocalization.Text("graph.select-map", "Select a map with saved runs to inspect its history."),"graph-detail-hint");return;}
            var mapName=definition.maps.FirstOrDefault(m=>m.config&&m.config.name==run.MapType).label;
            detailTitle.text=ToolkitLocalization.Text("graph.detail-title", "Run #{0} · {1} · {2}", run.RunNumber, ToolkitLocalization.Text("map." + run.MapType, string.IsNullOrWhiteSpace(mapName)?run.MapType:mapName.Trim()), Outcome(run));
            Detail(ToolkitLocalization.Text("graph.duration", "Duration"),CalcUtils.FormatTime(run.Duration));Detail(ToolkitLocalization.Text("graph.distance", "Distance"),ItemStatisticsPresentation.Number(run.Distance));Detail(ToolkitLocalization.Text("graph.tasks", "Tasks"),ItemStatisticsPresentation.Number(run.TasksCompleted));
            Detail(ToolkitLocalization.Text("graph.resources", "Resources"),ItemStatisticsPresentation.Number(run.ResourcesCollected));Detail(ToolkitLocalization.Text("graph.bonus-resources", "Bonus resources"),ItemStatisticsPresentation.Number(run.BonusResourcesCollected));Detail(ToolkitLocalization.Text("graph.kills", "Kills"),ItemStatisticsPresentation.Number(run.EnemiesKilled));
            Detail(ToolkitLocalization.Text("graph.damage-dealt", "Damage dealt"),ItemStatisticsPresentation.Number(run.DamageDealtAsDouble));Detail(ToolkitLocalization.Text("graph.damage-taken", "Damage taken"),ItemStatisticsPresentation.Number(run.DamageTakenAsDouble));
        }
        private void PositionOverflowValue()
        {
            if(overflowValue?.userData is not int number||string.IsNullOrEmpty(overflowValue.text))return;
            var bar=barHost.Children().FirstOrDefault(b=>b.userData is int id&&id==number);
            if(bar==null||float.IsNaN(plot.contentRect.width)||float.IsNaN(overflowValue.resolvedStyle.width))return;
            overflowValue.style.left=Mathf.Clamp(bar.layout.center.x-overflowValue.resolvedStyle.width/2,0,Mathf.Max(0,plot.contentRect.width-overflowValue.resolvedStyle.width));
        }
        private void SizeAxis()
        {
            var width=42f;
            foreach(var tick in ticks)
            {
                var measured=tick.MeasureTextSize(tick.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).x;
                if(!float.IsNaN(measured)&&!float.IsInfinity(measured))width=Mathf.Max(width,Mathf.Ceil(measured)+8);
            }
            axis.style.width=width;
            if(axisLabels!=null)axisLabels.style.marginLeft=width;
        }
        private void Detail(string title,string value){var pair=ToolkitGameplay.E(details,"graph-detail-pair");Text(pair,title,"graph-detail-key");Text(pair,value,"graph-detail-value");}
    }
}
