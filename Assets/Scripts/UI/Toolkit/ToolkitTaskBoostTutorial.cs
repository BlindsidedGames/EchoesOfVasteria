using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitStatisticsScreen
    {
        private void RefreshBoostGuidance()
        {
            var general = Blindsided.Oracle.oracle?.saveData?.General;
            var active = general?.TaskBoostTutorialStage == 1 && !MapStatisticsGuidance.Active && !GraphStatisticsGuidance.Active && (!GameManager.Instance || GameManager.Instance.IsInTown);
            if (boostHand == null && definition.tutorialHand)
            {
                boostHand = new Image { name = "task-boost-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
                boostHand.style.position = Position.Absolute; boostHand.style.width = boostHand.style.height = 24; root.Add(boostHand);
            }
            if (boostHand == null) return;
            VisualElement target = null;
            if (active)
            {
                if (selectedTab != 4) target = StatisticsSectionTarget(4);
                else
                {
                    var task = TaskBoostGuidance.FindTask();
                    if (task && taskPreferences != null)
                    {
                        if (Tasks.TaskWeightService.IsToggleEnabled(task))
                        { general.TaskBoostTutorialStage = 2; return; }
                        target = taskPreferences.ToggleFor(task);
                        if (!boostScrolled)
                        { target = taskPreferences.Reveal(task); boostScrolled = target?.worldBound.height > 0; }
                    }
                }
            }
            boostHand.style.display = target != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (target == null) return;
            PositionStatisticsHand(boostHand, target, selectedTab != 4 ? TabTitle(4) : null);
        }
    }
}
