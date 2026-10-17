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
                if (selectedTab != 4) target = tabButtons[4];
                else
                {
                    var task = TaskBoostGuidance.FindTask();
                    if (task && taskRows.TryGetValue(task, out var row))
                    {
                        target = row.Toggle;
                        if (!boostScrolled && row.Root.layout.height > 0)
                        { body.Q<ScrollView>()?.ScrollTo(row.Root); boostScrolled = true; }
                    }
                }
            }
            boostHand.style.display = target != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (target == null) return;
            var p = root.WorldToLocal(new Vector2(target.worldBound.center.x, target.worldBound.yMax));
            boostHand.style.left = Mathf.Round(p.x - 12); boostHand.style.top = Mathf.Round(p.y + 2 + 4 * (1 + Mathf.Cos(Time.unscaledTime * 5)));
        }
    }
}
