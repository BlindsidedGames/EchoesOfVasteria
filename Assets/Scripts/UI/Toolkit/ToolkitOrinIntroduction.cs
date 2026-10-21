using System.Collections;
using Blindsided;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ToolkitOrinIntroduction : MonoBehaviour
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
            var returning = Oracle.oracle.saveData.General;
            if (returning.ReturningPlayerIntroductionComplete && !returning.ReturningPlayerWelcomeSeen)
            {
                yield return Explain("ReturningPlayerWelcome", "Welcome back! Your progress and equipment are ready. The opening lessons have been skipped. Explore the updated menus at your own pace; help is available in the Library.");
                returning.ReturningPlayerWelcomeSeen = true;
                Blindsided.EventHandler.SaveData();
            }
            // Field encounter cards are hidden in town. Keep this native panel at
            // the scene root, independent of field visibility and other UIDocuments.
            if (!QuestUtils.QuestCompleted(firstQuest))
            {
                var meeting = Instantiate(meetingPrefab).GetComponent<ToolkitMeetingScreen>();
                meeting.Init(portrait, lines, () => FindAnyObjectByType<ToolkitNavigationScreen>()?.HighlightQuests(), lineKeys);
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
                if (TimelessEchoes.Characters.CharacterCreation.Required || data == null || !windows || windows.HasOpenWindow ||
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
                    yield return Explain("BuffIntroduction", "You can now summon an Echo to help on your adventures. Choose one to gather resources or fight by your side.");
                    data.General.BuffTutorialStage = 1;
                    Blindsided.EventHandler.SaveData();
                }
                else if (gm && gm.FirstReapReturnPending && !Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains("FirstReapIntroduction"))
                    yield return Explain("FirstReapIntroduction", "Being reaped ends this run, but you keep your loot and skill progress. Return to town to turn in your quests.");
                else if ((!gm || gm.IsInTown) && data.General.ReturnedFromFarmlands && QuestUtils.QuestCompleted("The names Gill") && data.General.InventoryTutorialStage == 0)
                {
                    if (!Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains("InventoryIntroduction"))
                        yield return Explain("InventoryIntroduction", "Open Hub, then Inventory, to view your equipped items, their bonuses and the resources you gathered.");
                    data.General.InventoryTutorialStage = 1;
                }
                else if ((!gm || gm.IsInTown) && hubIntroduction && !hubIntroduction.HasBeenCompleted &&
                         data.General.HubTutorialStage < 3 &&
                         QuestUtils.QuestCompleted("The names Gill") && data.General.InventoryTutorialStage >= 3 && ToolkitVisibilityRule.HasLevelledSkill())
                    yield return hubIntroduction.ShowAndWait(windows.transform);
                else if ((!gm || gm.IsInTown) && data.General.MapStatisticsTutorialStage == 1)
                {
                    yield return Explain("MapStatisticsIntroduction", "Different maps give different resources.");
                    data.General.MapStatisticsTutorialStage = 2;
                }
                else if ((!gm || gm.IsInTown) && data.General.MapStatisticsTutorialStage == 8 &&
                         data.General.TaskBoostTutorialStage >= 2 && data.General.GraphsViewed)
                {
                    yield return Explain("OpeningTutorialComplete", "You’re ready to explore on your own! If you’d like to learn more, you’ll find helpful information in the Library.");
                    data.General.MapStatisticsTutorialStage = 9;
                }
                else if ((!gm || gm.IsInTown) && GraphStatisticsGuidance.Eligible &&
                         !Gear.ForgeFirstLesson.Active && !MapStatisticsGuidance.Active && data.General.TaskBoostTutorialStage != 1 &&
                         !(gm && gm.IvanReturnPending))
                {
                    yield return Explain("GraphsIntroduction", "You can find the history of your travels in the statistics panel!");
                    if (!data.General.GraphsViewed) data.General.GraphsTutorialStage = 1;
                }
                else if ((!gm || gm.IsInTown) && data.General.HubTutorialStage >= 3 && data.General.TaskBoostTutorialStage == 0 &&
                         !TimelessEchoes.Gear.ForgeFirstLesson.Active && !MapStatisticsGuidance.Active && !GraphStatisticsGuidance.Active && !(gm && gm.IvanReturnPending) &&
                         Time.unscaledTime >= nextBoostCheck)
                {
                    nextBoostCheck = Time.unscaledTime + 1;
                    if (!TaskBoostGuidance.FindTask()) { yield return null; continue; }
                    yield return Explain("TaskBoostIntroduction", "You now have a choice of tasks. Open Statistics, then Tasks, to adjust which ones appear more often.");
                    data.General.TaskBoostTutorialStage = 1;
                }
                yield return null;
            }
        }

        private IEnumerator Explain(string marker, string text)
        {
            var meeting = Instantiate(meetingPrefab).GetComponent<ToolkitMeetingScreen>();
            Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Add(marker);
            try
            {
                meeting.Init(null, new[] { ToolkitLocalization.Text("intro." + marker, text) }, () =>
                    Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Add(marker));
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
