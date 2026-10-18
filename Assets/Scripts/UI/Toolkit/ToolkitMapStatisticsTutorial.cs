using Blindsided;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public static class MapStatisticsGuidance
    {
        public static int Stage => Oracle.oracle?.saveData?.General.MapStatisticsTutorialStage ?? 0;
        public static bool Active => Stage > 0 && Stage < 8;
        public static string TargetMap => Stage switch { 4 => "Farmlands", 5 => "River", 6 => "Woods", _ => null };
        public static void Viewed(string map)
        {
            if (map == TargetMap && TargetMap != null) Oracle.oracle.saveData.General.MapStatisticsTutorialStage++;
        }
    }

    public sealed partial class ToolkitStatisticsScreen
    {
        private VisualElement mapTutorial;
        private Label mapTutorialText;
        private Image mapTutorialHand;

        private void BuildMapTutorial()
        {
            mapTutorial = new VisualElement { name = "map-statistics-tutorial" };
            mapTutorial.AddToClassList("journal-tutorial");
            mapTutorial.style.display = DisplayStyle.None;
            mapTutorialText = ToolkitControls.Text("", ToolkitControls.TextRole.Body);
            mapTutorialText.style.whiteSpace = WhiteSpace.Normal; mapTutorial.Add(mapTutorialText); root.Add(mapTutorial);
            mapTutorialHand = new Image { name = "map-statistics-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
            mapTutorialHand.style.position = Position.Absolute; mapTutorialHand.style.width = mapTutorialHand.style.height = 24;
            mapTutorialHand.style.display = DisplayStyle.None;
            root.Add(mapTutorialHand);
        }

        private void RefreshMapTutorial()
        {
            if (mapTutorial == null) return;
            var general = Oracle.oracle?.saveData?.General;
            var active = general != null && MapStatisticsGuidance.Stage >= 2 && MapStatisticsGuidance.Active &&
                (!GameManager.Instance || GameManager.Instance.IsInTown);
            mapTutorial.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            mapTutorialHand.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active) return;
            if (general.MapStatisticsTutorialStage == 2) general.MapStatisticsTutorialStage = 3;
            if (general.MapStatisticsTutorialStage == 3 && selectedTab == 0) general.MapStatisticsTutorialStage = 4;
            VisualElement target = null;
            if (general.MapStatisticsTutorialStage >= 7)
            {
                mapTutorialText.text = ToolkitLocalization.Text("journal.tutorial.close", "Close Statistics to finish the tutorial.");
            }
            else if (selectedTab != 0)
            {
                mapTutorialText.text = ToolkitLocalization.Text("journal.tutorial.general", "Open General to view the maps.");
                target = tabButtons[0];
            }
            else
            {
                var map = MapStatisticsGuidance.TargetMap;
                var index = System.Array.FindIndex(definition.maps, m => m.config && m.config.name == map);
                if (index >= 0 && journalSelectors.Count > index + 1)
                {
                    mapTutorialText.text = ToolkitLocalization.Text("journal.tutorial." + map,
                        map == "Farmlands" ? "Choose Farmlands to see its resources." : "Now choose {0} to compare its resources.",
                        ToolkitLocalization.Text("map." + map, definition.maps[index].label.Trim()));
                    target = journalSelectors[index + 1];
                }
            }
            mapTutorialHand.style.display = target != null && definition.tutorialHand ? DisplayStyle.Flex : DisplayStyle.None;
            if (target == null) return;
            var p = root.WorldToLocal(new Vector2(target.worldBound.center.x, target.worldBound.yMax));
            mapTutorialHand.style.left = Mathf.Round(p.x - 12);
            mapTutorialHand.style.top = Mathf.Round(p.y - 6 + 4 * (1 + Mathf.Cos(Time.unscaledTime * 5)));
            mapTutorialHand.BringToFront();
        }
    }
}
