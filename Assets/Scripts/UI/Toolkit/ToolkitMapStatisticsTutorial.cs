using Blindsided;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public static class MapStatisticsGuidance
    {
        public static int Stage => Oracle.oracle?.saveData?.General.MapStatisticsTutorialStage ?? 0;
        public static bool Active => Stage > 0 && Stage < 8;
        public static string TargetMap => Stage == 4 ? "Farmlands" : null;
        public static void Viewed(string map)
        {
            if (map == TargetMap && TargetMap != null) Oracle.oracle.saveData.General.MapStatisticsTutorialStage = 7;
        }
    }

    public sealed partial class ToolkitStatisticsScreen
    {
        private VisualElement mapTutorial;
        private Label mapTutorialText;
        private Image mapTutorialHand;

        private void BuildMapTutorial()
        {
            if (MapStatisticsGuidance.Stage is >= 2 and <= 4) journalMap = -1;
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
            // Saves on the old comparison steps have already viewed their first map.
            if (general?.MapStatisticsTutorialStage is 5 or 6) general.MapStatisticsTutorialStage = 7;
            var active = general != null && MapStatisticsGuidance.Stage >= 2 && MapStatisticsGuidance.Active &&
                (!GameManager.Instance || GameManager.Instance.IsInTown);
            mapTutorial.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            mapTutorialHand.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active) return;
            if (general.MapStatisticsTutorialStage == 2) general.MapStatisticsTutorialStage = 3;
            if (general.MapStatisticsTutorialStage == 3 && selectedTab == 0) general.MapStatisticsTutorialStage = 4;
            VisualElement target = null;
            string option = null;
            if (general.MapStatisticsTutorialStage >= 7)
            {
                mapTutorialText.text = ToolkitLocalization.Text("journal.tutorial.close", "Close Statistics to finish the tutorial.");
            }
            else if (selectedTab != 0)
            {
                mapTutorialText.text = ToolkitLocalization.Text("journal.tutorial.general", "Open General to view the maps.");
                target = StatisticsSectionTarget(0);
                option = TabTitle(0);
            }
            else
            {
                var map = MapStatisticsGuidance.TargetMap;
                var index = System.Array.FindIndex(definition.maps, m => m.config && m.config.name == map);
                if (index >= 0 && journalSelectors.Count > index + 1)
                {
                    option = ToolkitLocalization.Text("map." + map, definition.maps[index].label.Trim());
                    mapTutorialText.text = ToolkitLocalization.Text("journal.tutorial." + map, "Choose Farmlands to see its resources.");
                    target = ToolkitResponsiveLayout.IsPortrait ? journalArea : journalSelectors[index + 1];
                }
            }
            mapTutorialHand.style.display = target != null && definition.tutorialHand ? DisplayStyle.Flex : DisplayStyle.None;
            if (target == null) return;
            PositionStatisticsHand(mapTutorialHand, target, option);
        }
    }
}
