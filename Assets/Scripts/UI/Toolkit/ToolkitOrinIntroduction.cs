using System.Collections;
using Blindsided;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitOrinIntroduction : MonoBehaviour
    {
        [SerializeField] private GameObject meetingPrefab;
        [SerializeField] private Sprite portrait;
        [SerializeField] private QuestData firstQuest;
        [SerializeField, TextArea] private string[] lines;
        [SerializeField] private string[] lineKeys;
        [SerializeField] private ToolkitNpcIntroduction evaIntroduction;
        [SerializeField] private ToolkitNpcIntroduction hubIntroduction;

        private IEnumerator Start()
        {
            while (Oracle.oracle == null || !Oracle.oracle.HasLoadedCurrentSlotData ||
                   QuestManager.Instance == null || TownWindowManager.Instance == null)
                yield return null;
            if (!firstQuest) yield break;
            var intro = FindAnyObjectByType<ToolkitIntroScreen>();
            // Let the existing first-launch notice finish before introducing Orin.
            yield return null;
            while (intro && intro.IsVisible) yield return null;
            while (TimelessEchoes.Characters.CharacterCreation.Required) yield return null;
            if (!meetingPrefab) yield break;
            yield return ReturningLessons();
            // Field encounter cards are hidden in town. Keep this native panel at
            // the scene root, independent of field visibility and other UIDocuments.
            if (!QuestUtils.QuestCompleted(firstQuest))
            {
                var meeting = Instantiate(meetingPrefab).GetComponent<ToolkitMeetingScreen>();
                meeting.Init(portrait, lines, () => FindAnyObjectByType<ToolkitNavigationScreen>()?.HighlightQuests(), lineKeys, npcId: "Orin");
                meeting.BeginDialogue();
                while (meeting) yield return null;
            }
            if (evaIntroduction && !evaIntroduction.HasBeenCompleted)
            {
                // Finish the first quest and close its panel before Eva speaks.
                while (!QuestUtils.QuestCompleted(firstQuest) || TownWindowManager.Instance.HasOpenWindow ||
                       (GameManager.Instance && !GameManager.Instance.IsInTown))
                    yield return null;
                yield return evaIntroduction.ShowAndWait(TownWindowManager.Instance.transform);
            }
            var nextBoostCheck = 0f;
            ToolkitRunScreen runScreen = null;
            while (true)
            {
                var gm = GameManager.Instance;
                var data = Oracle.oracle?.saveData;
                var windows = TownWindowManager.Instance;
                // Older saves have no explicit return flag; migrate only while in town.
                if (data != null && (!gm || gm.IsInTown) && !data.General.ReturnedFromFarmlands &&
                    data.General.RecentRuns?.Exists(r => r.MapType == "Farmlands") == true)
                    data.General.ReturnedFromFarmlands = true;
                if (data != null && data.General.MapStatisticsTutorialStage == 0 &&
                    Gear.ForgeFirstLesson.Stage >= 5 && QuestUtils.QuestCompleted(Gear.ForgeFirstLesson.QuestId))
                    data.General.MapStatisticsTutorialStage = 1;
                if (!runScreen) runScreen = FindAnyObjectByType<ToolkitRunScreen>();
                if (TimelessEchoes.Characters.CharacterCreation.Required || TownQuestReveal.IsPlaying || data == null || !windows || windows.HasOpenWindow ||
                    runScreen?.HasOpenDetails == true ||
                    Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Count > 0 || (gm && gm.RunLoadingActive))
                { yield return null; continue; }
                // Closing the maps tour releases later lessons; the farewell waits for all of them.
                if ((!gm || gm.IsInTown) && data.General.MapStatisticsTutorialStage == 7)
                    data.General.MapStatisticsTutorialStage = 8;
                // Finish one guided interaction before introducing the next system.
                if ((!gm || gm.IsInTown) && BuffIntroductionGuidance.Active)
                { yield return null; continue; }
                if ((!gm || gm.IsInTown) &&
                    ((hubIntroduction && hubIntroduction.HasBeenCompleted && data.General.InventoryTutorialStage >= 3 && ToolkitVisibilityRule.HasLevelledSkill() && data.General.HubTutorialStage < 3) ||
                     (data.General.InventoryTutorialStage > 0 && data.General.InventoryTutorialStage < 3) || GraphStatisticsGuidance.Active))
                { yield return null; continue; }
                if ((!gm || gm.IsInTown) && BuffIntroductionGuidance.Eligible)
                {
                    yield return Explain("BuffIntroduction", "You can now call an Echo to gather for you. Assign Task Echo in your first buff slot.", "buffs");
                    data.General.BuffTutorialStage = 1;
                    Blindsided.EventHandler.SaveData();
                }
                else if (gm && gm.FirstReapReturnPending && !Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains("FirstReapIntroduction"))
                    yield return Explain("FirstReapIntroduction", "Carl has ended this run. You keep your loot and skill progress. Return to town to turn in your quests.", "return-town");
                else if ((!gm || gm.IsInTown) && data.General.ReturnedFromFarmlands && QuestUtils.QuestCompleted("The names Gill") && data.General.InventoryTutorialStage == 0)
                {
                    if (!Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains("InventoryIntroduction"))
                        yield return Explain("InventoryIntroduction", "Open <link=\"help:inventory\"><u><color=#EFB889>Hub → Inventory</color></u></link> to inspect your equipment and resources.");
                    data.General.InventoryTutorialStage = 1;
                }
                else if ((!gm || gm.IsInTown) && hubIntroduction && !hubIntroduction.HasBeenCompleted &&
                         data.General.HubTutorialStage < 3 &&
                         QuestUtils.QuestCompleted("The names Gill") && data.General.InventoryTutorialStage >= 3 && ToolkitVisibilityRule.HasLevelledSkill())
                    yield return hubIntroduction.ShowAndWait(windows.transform);
                else if ((!gm || gm.IsInTown) && data.General.MapStatisticsTutorialStage == 1)
                {
                    yield return Explain("MapStatisticsIntroduction", "Each map has different resources. Let’s see what you’ve found.", "statistics/general");
                    data.General.MapStatisticsTutorialStage = 2;
                }
                else if ((!gm || gm.IsInTown) && data.General.MapStatisticsTutorialStage == 8 &&
                         (data.General.TaskBoostTutorialStage >= 2 || data.General.TaskBoostTutorialStage == 0 && !TaskBoostGuidance.FindTask()) &&
                         (data.General.GraphsViewed || !GraphStatisticsGuidance.Eligible && !GraphStatisticsGuidance.Active))
                {
                    yield return Explain("OpeningTutorialComplete", "You’re ready to explore. Open Quests to choose what to work on next.", "quests");
                    data.General.MapStatisticsTutorialStage = 9;
                    FindAnyObjectByType<ToolkitNavigationScreen>()?.HighlightQuests();
                }
                else if ((!gm || gm.IsInTown) && GraphStatisticsGuidance.Eligible &&
                         !Gear.ForgeFirstLesson.Active && !MapStatisticsGuidance.Active && data.General.TaskBoostTutorialStage != 1 &&
                         !(gm && gm.IvanReturnPending))
                {
                    yield return Explain("GraphsIntroduction", "Open <link=\"help:statistics/graphs\"><u><color=#EFB889><sprite=\"NavigationInlineSprites\" name=\"statistics\" tint=1> → Graphs</color></u></link> to see your recent runs.");
                    if (!data.General.GraphsViewed) data.General.GraphsTutorialStage = 1;
                }
                else if ((!gm || gm.IsInTown) && data.General.HubTutorialStage >= 3 && data.General.TaskBoostTutorialStage == 0 &&
                         !TimelessEchoes.Gear.ForgeFirstLesson.Active && !MapStatisticsGuidance.Active && !GraphStatisticsGuidance.Active && !(gm && gm.IvanReturnPending) &&
                         Time.unscaledTime >= nextBoostCheck)
                {
                    nextBoostCheck = Time.unscaledTime + 1;
                    if (!TaskBoostGuidance.FindTask()) { yield return null; continue; }
                    yield return Explain("TaskBoostIntroduction", "Open <link=\"help:statistics/tasks\"><u><color=#EFB889><sprite=\"NavigationInlineSprites\" name=\"statistics\" tint=1> → Tasks</color></u></link> to choose which tasks appear more often.");
                    data.General.TaskBoostTutorialStage = 1;
                }
                yield return null;
            }
        }

        public IEnumerator TeachFirstEchoCast()
        {
            var gm = GameManager.Instance;
            var hero = Hero.HeroController.Instance;
            if (BuffIntroductionGuidance.Stage != 3 || !gm || !gm.CurrentMap || !hero || !meetingPrefab)
                yield break;
            var map = gm.CurrentMap;
            // Keep the starting position after the dialogue closes, until the
            // player uses the highlighted ability. Existing saves keep their Echo.
            using (hero.BeginNpcConversation())
            {
                if (!Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains("FirstEchoCastIntroduction"))
                    yield return Explain("FirstEchoCastIntroduction",
                        "My power lets you reach through the ether and call an echo of your past self. Press the marked ability to summon it.");
                while (BuffIntroductionGuidance.Stage == 3 && gm.CurrentMap == map && !gm.HeroIsDead)
                    yield return null;
            }
        }

        private IEnumerator Explain(string marker, string text, string navigationRoute = null)
        {
            var meeting = Instantiate(meetingPrefab).GetComponent<ToolkitMeetingScreen>();
            Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Add(marker);
            try
            {
                meeting.Init(null, new[] { text }, () =>
                    Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Add(marker), new[] { "intro." + marker }, npcId: "Orin");
                meeting.SetNavigationShortcut(navigationRoute);
                meeting.BeginDialogue();
                while (meeting) yield return null;
            }
            finally
            {
                Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Remove(marker);
                if (meeting) Destroy(meeting.gameObject);
            }
        }
    }
}
