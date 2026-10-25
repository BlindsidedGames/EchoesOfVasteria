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
        private Label summaryResourceCount, summaryMap;
        private Image summaryDistanceIcon;
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
            summaryMap = L(detailTitle.parent, "", "summary-map muted"); summaryMap.name = "summary-map";
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
            summaryMap.text = map; summaryMap.tooltip = map; SetVisible(summaryMap, !string.IsNullOrEmpty(map));
            var facts = E(summaryOverview, "row summary-facts summary-outing-facts");
            SummaryFact(facts, "kills", "run.summary.label.kills", "Kills", summaryData.Kills.ToString("N0", LocaleNumbers.Culture), definition.killsIcon);
            SummaryFact(facts, "multiplier", "run.summary.label.bonus", "Bonus", "×" + (1f + summaryData.RetreatBonusPercent * .01f).ToString("0.##", LocaleNumbers.Culture), definition.bonusIcon);
            SummaryFact(facts, "time", "run.summary.label.time", "Time", CalcUtils.FormatTime(summaryData.Duration), definition.TimeIcon(summaryData.Duration));
            SummaryFact(facts, "steps", "run.summary.label.steps", "Steps", CalcUtils.FormatNumber(summaryData.Steps, true), definition.stepsIcon);
            SummaryFact(facts, "deaths", "run.summary.label.deaths", "Deaths", summaryData.Deaths.ToString("N0", LocaleNumbers.Culture), definition.graveIcon);
            SummaryFact(facts, "reaps", "run.summary.label.reaps", "Reaps", summaryData.Reaps.ToString("N0", LocaleNumbers.Culture), definition.scytheIcon);
            var reaping = E(summaryOverview, "row summary-reaping-gain"); reaping.name = "summary-reaping-gain";
            summaryDistanceIcon = SummaryIcon(reaping, definition.reaperIcon, 20, "distance");
            L(reaping, ToolkitLocalization.Text("run.summary.label.distance", "Distance"), "summary-fact-label muted");
            reaping.tooltip = ToolkitLocalization.Text("run.summary.limit", "Reaping distance");
            L(reaping, summaryData.StartingDistance.ToString("N0", LocaleNumbers.Culture) + " → " + summaryData.Distance.ToString("N0", LocaleNumbers.Culture), "summary-reaping-value");
            L(reaping, "+" + Mathf.Max(0, summaryData.Distance - summaryData.StartingDistance).ToString("N0", LocaleNumbers.Culture), "summary-distance-change");
            summaryResourceHeading.Q<Label>("summary-resources-title").text = ToolkitLocalization.Text("run.summary.resources", "Resources gathered");
            summaryResourceCount.text = ToolkitLocalization.Text("run.summary.resource-types", "Types: {0}", summaryAmounts.Count);
            detailRows.Clear();
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
                {
                    var bonusLine = E(text, "row summary-resource-bonus-line");
                    var bonusIcon = E(bonusLine, "summary-resource-bonus-icon");
                    ToolkitGameplay.Icon(bonusIcon, definition.bonusIcon, 12);
                    L(bonusLine, "+" + CalcUtils.FormatNumber(extra, true), "summary-resource-bonus");
                }
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
            summaryGuideStep = Mathf.Clamp(step, 0, 1);
            switch (summaryGuideStep)
            {
                case 0:
                    summaryGuideTarget = summaryResourceHeading.Q("summary-resources-title");
                    summaryGuideText.text = ToolkitLocalization.Text("run.summary.guide-resources", "You keep the resources gathered during your adventure.");
                    break;
                default:
                    summaryGuideTarget = detailsClose;
                    summaryGuideText.text = ToolkitLocalization.Text("run.summary.guide-close", "Close the summary to continue.");
                    detailsClose.Focus();
                    break;
            }
            SetVisible(summaryGuideNext, summaryGuideStep < 1);
            if (summaryGuideStep < 1) summaryGuideNext.Focus();
            summaryGuideHand.userData = summaryGuideStep < 1 ? summaryGuideNext.name : detailsClose.name;
            summaryGuideArrow.userData = summaryGuideTarget?.name;
            SetVisible(summaryGuideArrow, summaryGuideStep < 1);
            PositionSummaryGuide();
        }

        private void PositionSummaryGuide()
        {
            if (summaryGuideStep < 0 || summaryGuideTarget?.panel == null) return;
            var action = summaryGuideStep < 1 ? summaryGuideNext : detailsClose;
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

        private static void SummaryFact(VisualElement parent, string name, string key, string fallback, string value, Sprite icon = null)
        {
            var fact = E(parent, "summary-fact"); fact.name = "summary-" + name;
            if (icon)
            {
                fact.AddToClassList("summary-icon-fact");
                SummaryIcon(fact, icon, 16, name);
                fact.tooltip = ToolkitLocalization.Text(key, fallback);
            }
            var text = E(fact, "row summary-fact-text");
            L(text, ToolkitLocalization.Text(key, fallback), "summary-fact-label muted");
            L(text, value, "strong summary-fact-value");
        }

        private static Image SummaryIcon(VisualElement parent, Sprite sprite, float size, string name)
        {
            var slot = E(parent, "summary-icon-slot summary-icon-" + name);
            return ToolkitGameplay.Icon(slot, sprite, size);
        }

        private float PortraitDetailsTop()
        {
            if (!ToolkitResponsiveLayout.IsPortrait) return 0;
            if (GameManager.Instance && GameManager.Instance.IsInTown) return ToolkitResponsiveLayout.WindowTop;
            var bottom = portraitDashboard != null && portraitDashboard.resolvedStyle.display != DisplayStyle.None
                ? portraitDashboard.worldBound.yMax - root.worldBound.yMin + 8 : ToolkitResponsiveLayout.WindowTop;
            return Mathf.Max(ToolkitResponsiveLayout.WindowTop, float.IsNaN(bottom) ? 0 : bottom);
        }

        private void LayoutSummary(Rect area)
        {
            detail.EnableInClassList("summary-landscape", !ToolkitResponsiveLayout.IsPortrait);
            if (summaryDistanceIcon != null)
                summaryDistanceIcon.style.width = summaryDistanceIcon.style.height = ToolkitResponsiveLayout.IsPortrait ? 20 : 16;
            var width = ToolkitResponsiveLayout.IsPortrait ? area.width : showingBreakdown ? Mathf.Min(720, area.width - 32) : Mathf.Max(160, Mathf.Min(480, area.width - 24));
            detail.style.width = width;
            liveBreakdown?.SetColumns(ToolkitResponsiveLayout.IsPortrait ? 1 : width >= 620 ? 3 : 2);
            detailsClose.text = ToolkitResponsiveLayout.IsPortrait ? ToolkitLocalization.Text("common.continue", "Continue") : "×";
            var columns = width >= 620 ? 3 : 2;
            var resourceHeight = 88 + Mathf.CeilToInt(runResources.Count / (float)columns) * 42;
            var keepHudClear = !ToolkitResponsiveLayout.IsPortrait && (showingBreakdown || (GameManager.Instance && GameManager.Instance.CurrentMap));
            detail.style.position = keepHudClear || ToolkitResponsiveLayout.IsPortrait ? Position.Absolute : StyleKeyword.Null;
            detail.style.height = ToolkitResponsiveLayout.IsPortrait ? Mathf.Max(100, area.height - PortraitDetailsTop() - ToolkitResponsiveLayout.WindowBottom) : keepHudClear ? Mathf.Min(Mathf.Max(200, showingBreakdown ? resourceHeight : 350), area.height - ToolkitResponsiveLayout.WindowTop - 100) : Mathf.Max(220, Mathf.Min(350, area.height - 64));
            if (ToolkitResponsiveLayout.IsPortrait) { detail.style.top = 0; detail.style.left = 0; detail.style.translate = new Translate(0, 0); }
            else if (keepHudClear) { detail.style.top = area.y + ToolkitResponsiveLayout.WindowTop + 16; detail.style.left = area.x + (area.width - width) * .5f; detail.style.translate = new Translate(0, 0); }
            else { detail.style.top = StyleKeyword.Null; detail.style.left = StyleKeyword.Null; detail.style.translate = StyleKeyword.Null; }
            detail.EnableInClassList("summary-narrow", area.width < 500);
            detail.EnableInClassList("summary-compact", summaryData != null);
            PositionSummaryGuide();
        }

        private void ClearSummary()
        {
            EndSummaryGuide();
            summaryData = null; summaryDistanceIcon = null; summaryAmounts.Clear(); summaryBonuses.Clear();
            if (detail == null) return;
            detail.RemoveFromClassList("summary-sheet"); detail.RemoveFromClassList("summary-narrow"); detail.RemoveFromClassList("summary-compact");
            detail.style.width = detail.style.height = StyleKeyword.Null;
            SetVisible(summaryOverview, false); SetVisible(summaryResourceHeading, false); SetVisible(summaryMap, false);
            SetVisible(runTime, true); if (runRatesLeft != null) SetVisible(runRatesLeft.parent, true);
            detailRows?.contentContainer.RemoveFromClassList("summary-resource-grid");
        }
    }
}
