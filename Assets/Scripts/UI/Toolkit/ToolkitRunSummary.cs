using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitRunScreen
    {
        private VisualElement summaryOverview, summaryResourceHeading;
        private Label summaryResourceCount;
        private Button detailsClose;
        private VisualElement summaryGuide;
        private Label summaryGuideText;
        private Button summaryGuideNext;
        private Image summaryGuideHand, summaryGuideArrow;
        private VisualElement summaryGuideTarget;
        private int summaryGuideStep = -1;
        private RunSummaryData summaryData;
        private readonly Dictionary<Resource, double> summaryAmounts = new(), summaryBonuses = new();

        private void BuildSummaryOverview()
        {
            summaryOverview = E(detail, "summary-overview"); summaryOverview.name = "run-summary-overview";
            summaryResourceHeading = E(detail, "row summary-resource-heading"); summaryResourceHeading.name = "summary-resources";
            L(summaryResourceHeading, "", "strong").name = "summary-resources-title";
            summaryResourceCount = L(summaryResourceHeading, "", "small muted");
        }

        private void BuildDetailsClose()
        {
            detailsClose = Button(detail, "run-details-close", "×", HideDetails);
            detailsClose.AddToClassList("summary-close");
            ToolkitLocalization.BindTooltip(detailsClose, "navigation.close-tooltip", "Close window");
            BuildSummaryGuide();
        }

        public void ShowSummary(RunSummaryData summary, IReadOnlyDictionary<Resource, double> amounts, IReadOnlyDictionary<Resource, double> bonus)
        {
            ToolkitResponsiveLayout.SetFocusView(false);
            HideDetails();
            summaryData = summary ?? new RunSummaryData();
            foreach (var pair in amounts) if (pair.Key && pair.Value > 0) summaryAmounts[pair.Key] = pair.Value;
            foreach (var pair in bonus) if (pair.Key && pair.Value > 0) summaryBonuses[pair.Key] = pair.Value;
            detail.AddToClassList("summary-sheet");
            SetVisible(summaryOverview, true); SetVisible(summaryResourceHeading, true);
            SetVisible(runTime, false); SetVisible(runRatesLeft.parent, false);
            detailRows.contentContainer.AddToClassList("summary-resource-grid");
            detailRows.verticalScrollerVisibility = ScrollerVisibility.Auto;
            theme.StyleScroll(detailRows); ToolkitGameplay.StyleScroll(detailRows);
            RefreshSummary(); LayoutSummary(ToolkitWindowLayout.SafeArea);
            SetVisible(overlay, true); detailsClose.Focus();
            BeginSummaryGuide();
        }

        private void RefreshSummary()
        {
            detailTitle.text = ToolkitLocalization.Text("run.summary-title", "Run Summary");
            summaryOverview.Clear();
            var map = string.IsNullOrEmpty(summaryData.Map) ? "" : ToolkitLocalization.Text("map." + summaryData.Map, summaryData.Map);
            L(summaryOverview, string.IsNullOrEmpty(map) ? ToolkitLocalization.Text("run.summary.since-town", "Since leaving town") :
                ToolkitLocalization.Text("run.summary.outing", "{0} · Since leaving town", map), "summary-subtitle muted");
            var facts = E(summaryOverview, "row summary-facts");
            SummaryFact(facts, "time", "run.summary.time-away", "Time away", CalcUtils.FormatTime(summaryData.Duration));
            SummaryFact(facts, "steps", "run.summary.steps", "Steps walked", CalcUtils.FormatNumber(summaryData.Steps, true));
            SummaryFact(facts, "deaths", "run.summary.deaths-label", "Deaths", summaryData.Deaths.ToString("N0"));
            SummaryFact(facts, "reaps", "run.summary.reaps-label", "Reaped by Carl", summaryData.Reaps.ToString("N0"));
            var limit = E(summaryOverview, "row summary-distance"); limit.name = "summary-reaping-distance";
            L(limit, ToolkitLocalization.Text("run.summary.limit", "Reaping distance"), "summary-distance-label");
            var changed = summaryData.StartingDistance > 0 && !Mathf.Approximately(summaryData.StartingDistance, summaryData.Distance);
            var value = changed ? ToolkitLocalization.Text("run.summary.limit-change", "{0:N0} → {1:N0} steps", summaryData.StartingDistance, summaryData.Distance) :
                ToolkitLocalization.Text("run.summary.limit-value", "{0:N0} steps", summaryData.Distance);
            L(limit, value, "strong summary-distance-value").name = "summary-distance-value";
            if (changed)
                L(limit, (summaryData.Distance > summaryData.StartingDistance ? "+" : "") + (summaryData.Distance - summaryData.StartingDistance).ToString("N0"), "summary-distance-change");
            if (summaryData.Retreated && summaryData.RetreatBonusPercent > 0)
                L(summaryOverview, ToolkitLocalization.Text("run.summary.retreat-bonus", "Returned with {0} kills · +{1:0}% resource bonus", summaryData.Kills, summaryData.RetreatBonusPercent), "summary-return-bonus");
            summaryResourceHeading.Q<Label>("summary-resources-title").text = ToolkitLocalization.Text("run.summary.resources", "Resources gathered");
            summaryResourceCount.text = ToolkitLocalization.Text("run.summary.resource-types", "{0} types", summaryAmounts.Count);
            detailRows.Clear(); resourceRows.Clear();
            if (summaryAmounts.Count == 0)
                L(detailRows, ToolkitLocalization.Text("run.summary.empty", "No resources gathered this outing."), "summary-empty muted");
            var resourceIndex = 0;
            foreach (var pair in summaryAmounts.OrderBy(p => p.Key.resourceID))
            {
                var row = E(detailRows, "row summary-resource"); row.name = "summary-resource-" + pair.Key.name;
                if (resourceIndex++ % 2 == 1) row.AddToClassList("summary-resource-right");
                row.tooltip = ToolkitLocalization.Name(pair.Key);
                ToolkitGameplay.Icon(row, pair.Key.icon, 16);
                var text = E(row, "summary-resource-text");
                L(text, ToolkitLocalization.Name(pair.Key), "summary-resource-name").name = "resource-name";
                summaryBonuses.TryGetValue(pair.Key, out var extra);
                if (extra > 0)
                    L(text, ToolkitLocalization.Text("run.summary.resource-bonus", "Includes +{0} bonus", CalcUtils.FormatNumber(extra, true)), "summary-resource-bonus");
                L(row, CalcUtils.FormatNumber(pair.Value, true), "strong summary-resource-total").name = "resource-total";
            }
        }

        private void BuildSummaryGuide()
        {
            summaryGuide = E(detail, "surface summary-guide"); summaryGuide.name = "summary-guide";
            summaryGuide.style.flexDirection = FlexDirection.Row; summaryGuide.style.alignItems = Align.Center;
            summaryGuide.style.flexShrink = 0; summaryGuide.style.paddingTop = summaryGuide.style.paddingBottom = 5;
            summaryGuide.style.paddingLeft = summaryGuide.style.paddingRight = 6;
            summaryGuide.style.marginTop = 5;
            summaryGuideText = L(summaryGuide, ""); summaryGuideText.name = "summary-guide-text";
            summaryGuideText.style.whiteSpace = WhiteSpace.Normal; summaryGuideText.style.flexGrow = 1; summaryGuideText.style.flexShrink = 1; summaryGuideText.style.flexBasis = 0;
            summaryGuideNext = Button(summaryGuide, "summary-guide-next", ToolkitLocalization.Text("common.next", "Next"), () => SetSummaryGuideStep(summaryGuideStep + 1));
            summaryGuideNext.style.flexShrink = 0; summaryGuideNext.style.marginLeft = 8;
            summaryGuideHand = new Image { name = "summary-guide-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
            summaryGuideHand.style.position = Position.Absolute; summaryGuideHand.style.width = summaryGuideHand.style.height = 24;
            overlay.Add(summaryGuideHand);
            StatIconLookup.TryGetIcon(StatIconLookup.StatKey.UpArrow, out var arrow);
            summaryGuideArrow = new Image { name = "summary-guide-arrow", sprite = arrow, pickingMode = PickingMode.Ignore };
            summaryGuideArrow.style.position = Position.Absolute; summaryGuideArrow.style.width = summaryGuideArrow.style.height = 12;
            // Reuse the green arrow, pointing toward the current observation target.
            summaryGuideArrow.style.rotate = new Rotate(new Angle(90, AngleUnit.Degree));
            overlay.Add(summaryGuideArrow); SetVisible(summaryGuideArrow, false);
            SetVisible(summaryGuide, false); SetVisible(summaryGuideHand, false);
        }

        private void BeginSummaryGuide()
        {
            var general = Blindsided.Oracle.oracle?.saveData?.General;
            if (general == null || general.RunSummaryIntroductionSeen || (GameManager.Instance && !GameManager.Instance.IsInTown)) return;
            SetVisible(summaryGuide, true); SetVisible(summaryGuideHand, definition.tutorialHand);
            SetSummaryGuideStep(0);
        }

        private void SetSummaryGuideStep(int step)
        {
            summaryGuideStep = Mathf.Clamp(step, 0, 2);
            switch (summaryGuideStep)
            {
                case 0:
                    summaryGuideTarget = summaryResourceHeading.Q("summary-resources-title");
                    summaryGuideText.text = ToolkitLocalization.Text("run.summary.guide-resources", "You keep the resources gathered during your adventure.");
                    break;
                case 1:
                    summaryGuideTarget = summaryOverview.Q("summary-distance-value");
                    summaryGuideText.text = ToolkitLocalization.Text("run.summary.guide-distance", "<b>River, Woods and Mines:</b> being reaped at the distance limit increases your shared reaping distance by 1%.\n<b>Farmlands:</b> no distance increase from reaping.\n<b>Spooky:</b> enemy levels scale with kills, with no kill cap or distance reaping limit.");
                    break;
                default:
                    summaryGuideTarget = detailsClose;
                    summaryGuideText.text = ToolkitLocalization.Text("run.summary.guide-close", "Close the summary to continue.");
                    detailsClose.Focus();
                    break;
            }
            SetVisible(summaryGuideNext, summaryGuideStep < 2);
            if (summaryGuideStep < 2) summaryGuideNext.Focus();
            summaryGuideHand.userData = summaryGuideStep < 2 ? summaryGuideNext.name : detailsClose.name;
            summaryGuideArrow.userData = summaryGuideTarget?.name;
            SetVisible(summaryGuideArrow, summaryGuideStep < 2);
            PositionSummaryGuide();
        }

        private void PositionSummaryGuide()
        {
            if (summaryGuideStep < 0 || summaryGuideTarget?.panel == null) return;
            var action = summaryGuideStep < 2 ? summaryGuideNext : detailsClose;
            var button = action.worldBound;
            var above = ToolkitResponsiveLayout.IsPortrait;
            var point = overlay.WorldToLocal(new Vector2(button.center.x, above ? button.yMin : button.yMax));
            var bob = 2 * (1 + Mathf.Cos(Time.unscaledTime * 5));
            summaryGuideHand.style.left = Mathf.Round(point.x - 12);
            summaryGuideHand.style.top = Mathf.Round(above ? point.y - 25 - bob : point.y + 1 + bob);
            summaryGuideHand.style.rotate = new Rotate(Angle.Degrees(above ? 180 : 0));
            var target = summaryGuideTarget.worldBound;
            var observation = overlay.WorldToLocal(new Vector2(target.xMin, target.center.y));
            var fromRight = observation.x < 20;
            summaryGuideArrow.style.rotate = new Rotate(Angle.Degrees(fromRight ? 270 : 90));
            summaryGuideArrow.style.left = Mathf.Round(fromRight ? overlay.WorldToLocal(new Vector2(target.xMax, target.center.y)).x + 3 + bob : observation.x - 15 - bob);
            summaryGuideArrow.style.top = Mathf.Round(observation.y - 6);
        }

        private void EndSummaryGuide()
        {
            if (summaryGuideStep >= 0 && Blindsided.Oracle.oracle?.saveData?.General != null)
                Blindsided.Oracle.oracle.saveData.General.RunSummaryIntroductionSeen = true;
            summaryGuideStep = -1; summaryGuideTarget = null;
            SetVisible(summaryGuide, false); SetVisible(summaryGuideHand, false); SetVisible(summaryGuideArrow, false);
        }

        private static void SummaryFact(VisualElement parent, string name, string key, string fallback, string value)
        {
            var fact = E(parent, "summary-fact"); fact.name = "summary-" + name;
            L(fact, ToolkitLocalization.Text(key, fallback), "summary-fact-label muted");
            L(fact, value, "strong summary-fact-value");
        }

        private void LayoutSummary(Rect area)
        {
            detail.style.width = ToolkitResponsiveLayout.IsPortrait ? area.width : Mathf.Max(160, Mathf.Min(480, area.width - 24));
            detailsClose.text = ToolkitResponsiveLayout.IsPortrait ? ToolkitLocalization.Text("common.continue", "Continue") : "×";
            detail.style.height = ToolkitResponsiveLayout.IsPortrait ? Mathf.Max(100, area.height - ToolkitResponsiveLayout.WindowTop - ToolkitResponsiveLayout.WindowBottom) : Mathf.Max(220, Mathf.Min(350, area.height - 64));
            if (ToolkitResponsiveLayout.IsPortrait) { detail.style.top = ToolkitResponsiveLayout.WindowTop; detail.style.left = 0; detail.style.translate = new Translate(0, 0); }
            else { detail.style.top = StyleKeyword.Null; detail.style.left = StyleKeyword.Null; detail.style.translate = StyleKeyword.Null; }
            detail.EnableInClassList("summary-narrow", area.width < 500);
            PositionSummaryGuide();
        }

        private void ClearSummary()
        {
            EndSummaryGuide();
            summaryData = null; summaryAmounts.Clear(); summaryBonuses.Clear();
            if (detail == null) return;
            detail.RemoveFromClassList("summary-sheet"); detail.RemoveFromClassList("summary-narrow");
            detail.style.width = detail.style.height = StyleKeyword.Null;
            SetVisible(summaryOverview, false); SetVisible(summaryResourceHeading, false);
            SetVisible(runTime, true); if (runRatesLeft != null) SetVisible(runRatesLeft.parent, true);
            detailRows?.contentContainer.RemoveFromClassList("summary-resource-grid");
        }
    }
}
