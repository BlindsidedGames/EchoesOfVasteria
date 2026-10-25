using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitRunScreen
    {
        private ToolkitRunResourcesView portraitResources;
        private void BuildPortraitResources()
        {
            portraitResources = new ToolkitRunResourcesView(definition);
            portraitResources.AddToClassList("portrait-run-resources");
            portraitResources.style.position = Position.Absolute;
            root.Add(portraitResources);
        }
        private void UpdatePortraitResources(Rect area)
        {
            var visible = ToolkitResponsiveLayout.IsPortrait && GameManager.Instance && GameManager.Instance.CurrentMap &&
                !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) && !HasOpenDetails && !heroStatsOpen;
            SetVisible(portraitResources, visible);
            if (!visible) return;
            portraitResources.style.left = portraitResources.style.right = 12;
            portraitResources.style.top = Mathf.Max(portraitDashboard.worldBound.yMax, ToolkitPinnedGoals.PortraitBottom) - root.worldBound.yMin + 12;
            portraitResources.style.bottom = 0;
            portraitResources.SetBottomClearance(ToolkitResponsiveLayout.WindowBottom + 12);
            portraitResources.SetColumns(1);
        }
        private void RefreshResourceView(ToolkitRunResourcesView view)
        {
            var seconds = tracker ? tracker.SessionDuration + (tracker.RunInProgress ? tracker.CurrentRunElapsedSeconds : 0) : 0;
            var kills = tracker ? tracker.SessionKills : 0;
            view.Refresh(runResources, seconds, GameManager.Instance ? GameManager.Instance.ResourceBonusPercent : 0,
                tracker ? tracker.SessionSteps : 0, tracker ? tracker.SessionDamageDealt : 0, kills);
        }
    }
}
