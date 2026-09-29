using System.Collections.Generic;
using TimelessEchoes.Gear;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitForgeScreen
    {
        private VisualElement oddsPopup, oddsRows;
        private bool oddsPinned, oddsHovered, popupHovered;
        private readonly List<(VisualElement swatch,Label name,Label value)> oddsLabels=new();
        private void BuildOddsPopup()
        {
            oddsPinned=oddsHovered=popupHovered=false;oddsLabels.Clear();
            oddsPopup=new VisualElement{name="forge-odds-popup"};oddsPopup.AddToClassList("forge-odds-popup");root.Add(oddsPopup);
            Label(oddsPopup,"Rarity odds",ToolkitControls.TextRole.Heading);
            oddsRows=new VisualElement();oddsPopup.Add(oddsRows);oddsPopup.style.display=DisplayStyle.None;
            odds.focusable=true;odds.tabIndex=0;
            odds.RegisterCallback<PointerEnterEvent>(e=>{if(e.pointerType!="mouse")return;oddsHovered=true;ShowOddsPopup();});
            odds.RegisterCallback<PointerLeaveEvent>(_=>{oddsHovered=false;CloseOddsAfterHover();});
            oddsPopup.RegisterCallback<PointerEnterEvent>(_=>popupHovered=true);
            oddsPopup.RegisterCallback<PointerLeaveEvent>(_=>{popupHovered=false;CloseOddsAfterHover();});
            odds.RegisterCallback<ClickEvent>(e=>{ToggleOddsPopup();e.StopPropagation();});
            odds.RegisterCallback<NavigationSubmitEvent>(e=>{ToggleOddsPopup();e.StopPropagation();});
            root.RegisterCallback<PointerDownEvent>(e=>{var target=e.target as VisualElement;if(target!=odds&&!odds.Contains(target)&&target!=oddsPopup&&!(oddsPopup?.Contains(target)??false))CloseOddsPopup();},TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape&&oddsPopup.resolvedStyle.display!=DisplayStyle.None){CloseOddsPopup();e.StopPropagation();}},TrickleDown.TrickleDown);
        }
        private void ToggleOddsPopup(){if(oddsPinned){CloseOddsPopup();return;}oddsPinned=true;odds.Focus();ShowOddsPopup();}
        private void ShowOddsPopup(){if(oddsPopup==null)return;oddsPopup.style.display=DisplayStyle.Flex;oddsPopup.BringToFront();PositionOddsPopup();}
        private void CloseOddsPopup(){oddsPinned=false;if(oddsPopup!=null)oddsPopup.style.display=DisplayStyle.None;}
        private void CloseOddsAfterHover(){odds.schedule.Execute(()=>{if(!oddsPinned&&!oddsHovered&&!popupHovered)CloseOddsPopup();}).StartingIn(120);}
        private void PositionOddsPopup()
        {
            if(oddsPopup==null||oddsPopup.style.display.value==DisplayStyle.None)return;
            var chart=root.WorldToLocal(odds.worldBound.position);float width=168,height=32+oddsLabels.Count*17;
            var x=chart.x+odds.worldBound.width+6;
            if(x+width>root.contentRect.width)x=chart.x-width-6;
            oddsPopup.style.left=Mathf.Clamp(x,4,Mathf.Max(4,root.layout.width-width-4));
            oddsPopup.style.top=Mathf.Clamp(chart.y,4,Mathf.Max(4,root.layout.height-height-4));
        }
        private void RefreshOddsPopup(List<(RaritySO r,float w)> weights)
        {
            while(oddsLabels.Count<weights.Count){var row=new VisualElement();row.AddToClassList("forge-odds-row");oddsRows.Add(row);var swatch=new VisualElement();swatch.AddToClassList("forge-odds-swatch");row.Add(swatch);var name=Label(row,"");name.AddToClassList("forge-odds-name");var value=Label(row,"");value.AddToClassList("forge-odds-value");oddsLabels.Add((swatch,name,value));}
            float total=0;foreach(var entry in weights)total+=Mathf.Max(0,entry.w);
            for(int i=0;i<oddsLabels.Count;i++){var labels=oddsLabels[i];labels.name.parent.style.display=i<weights.Count?DisplayStyle.Flex:DisplayStyle.None;if(i>=weights.Count)continue;var entry=weights[i];labels.swatch.style.backgroundColor=entry.r?entry.r.color:Color.white;labels.name.text=entry.r?entry.r.GetName():"Unknown";labels.value.text=(total>0?Mathf.Max(0,entry.w)/total*100:0).ToString("0.000")+"%";}
        }
    }
}
