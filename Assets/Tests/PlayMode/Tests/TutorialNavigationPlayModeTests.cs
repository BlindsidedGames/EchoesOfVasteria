#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator TutorialDestinationButtonsAdvanceOnceAndOpenTheNamedPageAcrossLayouts()
        {
            var owner = new GameObject("Tutorial navigation services");
            GameObject nav = null, stats = null, quests = null, meeting = null;
            try
            {
                owner.AddComponent<ResourceManager>();
                owner.AddComponent<GameplayStatTracker>().enabled = false;
                owner.AddComponent<QuestManager>().enabled = false;
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                var general = oracle.saveData.General;
                general.HubTutorialStage = 3; general.InventoryTutorialStage = 3;
                general.MapStatisticsTutorialStage = 9; general.TaskBoostTutorialStage = 2;
                general.GraphsTutorialStage = 2; general.GraphsViewed = true; general.BuffTutorialStage = 4;
                foreach (var entry in AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset").entries)
                    foreach (var rule in entry.visibility)
                        if (rule.quest) oracle.saveData.Quests[rule.quest.questId] = new GameData.QuestRecord { Completed = true };
                stats = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                quests = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
                typeof(TownWindowManager).GetField("toolkitStatistics", Private).SetValue(windows, stats.GetComponent<ToolkitStatisticsScreen>());
                typeof(TownWindowManager).GetField("toolkitQuests", Private).SetValue(windows, quests.GetComponent<ToolkitQuestsScreen>());
                nav = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var navigation = nav.GetComponent<ToolkitNavigationScreen>();
                var ui = nav.GetComponent<UIDocument>().rootVisualElement;
                var introduction = Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Orin Hub");
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1080, 1920) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Tutorial destination buttons");
                    yield return new WaitForSecondsRealtime(.15f);
                    foreach (var route in new[] { "statistics/tasks", "statistics/graphs", "quests" })
                    {
                        var completed = 0;
                        meeting = Object.Instantiate(introduction.meetingPrefab);
                        var dialogue = meeting.GetComponent<ToolkitMeetingScreen>();
                        dialogue.Init(null, new[] { "Open <link=\"help:" + route + "\">the named page</link>." }, () => completed++);
                        dialogue.BeginDialogue();
                        yield return new WaitForSecondsRealtime(.15f); navigation.Refresh();
                        var target = ui.Q<Button>(route == "quests" ? "hub" : "stats");
                        Assert.True(target.enabledInHierarchy, "A tutorial must leave its destination clickable: " + route);
                        Assert.False(ui.Q<Button>("options").enabledInHierarchy, "Unrelated navigation stays locked.");
                        FieldsClick(target); FieldsClick(target);
                        yield return new WaitForSecondsRealtime(.2f);
                        Assert.AreEqual(1, completed, "The destination click completes the same dialogue action exactly once.");
                        Assert.False(ToolkitMeetingScreen.HasOpenDialogue);
                        if (route == "quests") Assert.True(quests.GetComponent<ToolkitQuestsScreen>().IsOpen);
                        else
                        {
                            Assert.True(stats.GetComponent<ToolkitStatisticsScreen>().IsOpen);
                            Assert.AreEqual(route.EndsWith("tasks") ? 4 : 2, stats.GetComponent<ToolkitStatisticsScreen>().SelectedTab);
                        }
                        windows.CloseAllWindows(); yield return null;
                    }
                    meeting = Object.Instantiate(introduction.meetingPrefab);
                    meeting.GetComponent<ToolkitMeetingScreen>().Init(null, new[] { "Ordinary conversation." }, () => { });
                    meeting.GetComponent<ToolkitMeetingScreen>().BeginDialogue();
                    yield return null; navigation.Refresh();
                    Assert.False(ui.Q<Button>("stats").enabledInHierarchy, "A conversation without a destination must remain modal.");
                    FieldsClick(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance"));
                    yield return null;

                    general.MapStatisticsTutorialStage = 7;
                    Assert.True(navigation.OpenHelpDestination("statistics/general"));
                    yield return new WaitForSecondsRealtime(.15f); navigation.Refresh();
                    Assert.True(ui.Q<Button>("stats").enabledInHierarchy);
                    FieldsClick(ui.Q<Button>("stats")); yield return new WaitForSecondsRealtime(.15f);
                    Assert.False(stats.GetComponent<ToolkitStatisticsScreen>().IsOpen, "The Stats button must also satisfy Close Statistics.");
                    Assert.AreEqual(8, general.MapStatisticsTutorialStage);
                    general.MapStatisticsTutorialStage = 9;
                }
            }
            finally
            {
                if (meeting) Object.DestroyImmediate(meeting);
                if (nav) Object.DestroyImmediate(nav);
                if (stats) Object.DestroyImmediate(stats);
                if (quests) Object.DestroyImmediate(quests);
                Object.DestroyImmediate(owner);
            }
        }
    }
}
#endif
