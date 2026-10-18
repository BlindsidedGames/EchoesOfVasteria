using Blindsided;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public static class GraphStatisticsGuidance
    {
        public static bool Eligible
        {
            get
            {
                var data = Oracle.oracle?.saveData?.General;
                var completed = GameplayStatTracker.Instance ? GameplayStatTracker.Instance.CompletedAdventures : Mathf.Max(0, (data?.NextRunNumber ?? 1) - 1);
                return data != null && !data.GraphsViewed && data.GraphsTutorialStage == 0 && completed >= 5;
            }
        }
        public static bool Active => Oracle.oracle?.saveData?.General is { GraphsViewed: false, GraphsTutorialStage: 1 };
        public static void MarkViewed()
        {
            var general = Oracle.oracle?.saveData?.General;
            if (general == null) return;
            general.GraphsViewed = true; general.GraphsTutorialStage = 2;
        }
    }

    public sealed partial class ToolkitStatisticsScreen
    {
        private Image graphHand;
        private void RefreshGraphGuidance()
        {
            var active = GraphStatisticsGuidance.Active && !MapStatisticsGuidance.Active && (!GameManager.Instance || GameManager.Instance.IsInTown);
            if (graphHand == null && active && definition.tutorialHand)
            {
                graphHand = new Image { name = "graphs-tutorial-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
                graphHand.style.position = Position.Absolute; graphHand.style.width = graphHand.style.height = 24;
                root.Add(graphHand);
            }
            if (graphHand == null) return;
            graphHand.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active) return;
            var target = StatisticsSectionTarget(2);
            PositionStatisticsHand(graphHand, target);
        }
    }
}
