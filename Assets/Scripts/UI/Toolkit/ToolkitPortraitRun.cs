using System.Linq;
using Blindsided.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitRunScreen
    {
        private VisualElement portraitResources;
        private ScrollView portraitResourceList;
        private bool portraitResourcesDirty = true;
        private void BuildPortraitResources()
        {
            portraitResourcesDirty = true;
            portraitResources = ToolkitGameplay.E(root, "portrait-run-resources");
            portraitResources.style.position = Position.Absolute;
            LocalizedLabel(portraitResources, "run.summary.resources", "Resources gathered", "heading");
            portraitResourceList = ToolkitGameplay.Scroll(portraitResources, "portrait-resource-list");
            portraitResourceList.style.flexGrow = 1;
            portraitResourceList.style.minHeight = 0;
        }
        private void UpdatePortraitResources(Rect area)
        {
            var visible = ToolkitResponsiveLayout.IsPortrait && GameManager.Instance && GameManager.Instance.CurrentMap &&
                !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) && !HasOpenDetails && !heroStatsOpen;
            SetVisible(portraitResources, visible);
            if (!visible) return;
            portraitResources.style.left = portraitResources.style.right = 12;
            portraitResources.style.top = Mathf.Max(portraitDashboard.worldBound.yMax, ToolkitPinnedGoals.PortraitBottom) - root.worldBound.yMin + 12;
            portraitResources.style.bottom = ToolkitResponsiveLayout.WindowBottom + 12;
            if (!portraitResourcesDirty) return;
            portraitResourcesDirty = false;
            portraitResourceList.Clear();
            foreach (var entry in runResources.OrderBy(e => e.Key.resourceID))
            {
                var row = ToolkitGameplay.E(portraitResourceList, "row portrait-resource-row");
                ToolkitGameplay.Icon(row, entry.Key.icon, 20);
                var name = ToolkitGameplay.L(row, ToolkitLocalization.Name(entry.Key));
                name.style.flexGrow = 1;
                ToolkitGameplay.L(row, CalcUtils.FormatNumber(entry.Value, true), "strong");
            }
            if (runResources.Count == 0) LocalizedLabel(portraitResourceList, "run.portrait.empty", "Nothing gathered yet", "muted");
        }
    }
}
