#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator OrinHighlightsQuestsAndGillRewardsRenderDistinctStatIcons()
        {
            var owner = new GameObject("Isolated Orin quest owner");
            var windowsOwner = new GameObject("Isolated Orin windows");
            var locationObject = new GameObject("Isolated town with hidden field meetings");
            var fieldMeetings = new GameObject("Inactive field encounters");
            fieldMeetings.transform.SetParent(locationObject.transform, false);
            fieldMeetings.SetActive(false);
            GameObject intro = null, screenObject = null, navigation = null;
            try
            {
                var location = locationObject.AddComponent<TimelessEchoes.GameManager>(); location.enabled = false;
                typeof(TimelessEchoes.GameManager).GetField("meetingParent", Private).SetValue(location, fieldMeetings.transform);
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                var windows = windowsOwner.AddComponent<TownWindowManager>(); windows.enabled = false;
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var nav = navigation.GetComponent<ToolkitNavigationScreen>();
                var navUi = navigation.GetComponent<UIDocument>().rootVisualElement;
                nav.Refresh();
                foreach (var id in new[] { "options", "stats", "quests" })
                    Assert.AreEqual(DisplayStyle.Flex, navUi.Q(id + "-row").style.display.value, id);
                foreach (var id in new[] { "adventure", "skills", "inventory", "wiki", "townsfolk" })
                    Assert.AreEqual(DisplayStyle.None, navUi.Q(id + "-row").style.display.value, id);
                screenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
                var screen = screenObject.GetComponent<ToolkitQuestsScreen>();
                typeof(TownWindowManager).GetField("toolkitQuests", Private).SetValue(windows, screen);
                intro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab"));
                yield return null; yield return null; yield return null;
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                Assert.NotNull(meeting, "A new slot must receive Orin's introduction without a Meet click.");
                var ui = meeting.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreEqual(DisplayStyle.None, ui.Q("portrait-frame").style.display.value);
                Assert.IsNotEmpty(ui.Q<Label>("dialogue").text);
                Assert.False(screen.IsOpen);
                CaptureFeedback("orin-introduction.png"); yield return null; yield return null;
                for (int i = 0; i < 3; i++) meeting.BeginDialogue();
                yield return null;
                Assert.False(screen.IsOpen, "Orin must let the player choose to open Quests.");
                var questButton = navUi.Q<Button>("quests");
                var pointer = navUi.Q("quest-tutorial-pointer");
                Assert.NotNull(pointer);
                Assert.AreEqual(DisplayStyle.Flex, pointer.style.display.value);
                Assert.True(pointer.Query<Image>().ToList().All(image => image.sprite != null));
                Assert.AreEqual(PickingMode.Ignore, pointer.pickingMode);
                var minTop = float.MaxValue; var maxTop = float.MinValue;
                for (int i = 0; i < 7; i++)
                {
                    nav.Refresh();
                    minTop = Mathf.Min(minTop, pointer.style.top.value.value);
                    maxTop = Mathf.Max(maxTop, pointer.style.top.value.value);
                    Assert.AreEqual(1f, questButton.resolvedStyle.opacity, "The button must remain fully visible.");
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Assert.Greater(maxTop, minTop, "The pointing hand must move toward the button.");
                CaptureFeedback("quest-click-pointer.png"); yield return null; yield return null;
                Assert.False(screen.IsOpen);
                windows.OpenWindow(TownWindowManager.Window.Quests);
                nav.Refresh();
                Assert.True(screen.IsOpen);
                Assert.AreEqual(DisplayStyle.None, pointer.style.display.value, "Opening Quests dismisses the pointing hand.");

                var firstQuest = Resources.Load<QuestData>("Quests/Vast One/You get this one for free!");
                Assert.True(quests.TryTurnInQuest(firstQuest.questId));
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, navUi.Q("adventure-row").style.display.value);
                Assert.AreEqual(DisplayStyle.None, navUi.Q("skills-row").style.display.value);
                Assert.AreEqual(DisplayStyle.None, navUi.Q("wiki-row").style.display.value);
                var eva = Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Eva the Witch");
                var radishes = Resources.Load<QuestData>("Quests/SkillExperience/For The Jam");
                Assert.AreEqual(DisplayStyle.Flex, navUi.Q("tutorial-pointer-close").style.display.value);
                Assert.AreEqual(DisplayStyle.None, navUi.Q("tutorial-pointer-adventure").style.display.value);
                Assert.False(quests.IsQuestInProgress(radishes.questId), "Eva must introduce the request before it appears as a quest.");
                yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Eva must wait for Quests to close.");
                CaptureFeedback("first-quest-close-prompt.png"); yield return null; yield return null;
                FieldsClick(navUi.Q<Button>("close"));
                yield return null; yield return null; yield return null;
                meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                Assert.NotNull(meeting, "Closing Quests must start Eva's conversation.");
                var evaUi = meeting.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(evaUi.Q<Label>("dialogue").text, Does.Contain("Here, take my magic watering can.").And.Contain("radishes"));
                Assert.NotNull(evaUi.Q<Image>("portrait").sprite);
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, navUi.Q("tutorial-pointer-adventure").style.display.value);
                CaptureFeedback("eva-watering-can-introduction.png"); yield return null; yield return null;
                meeting.BeginDialogue(); yield return null; yield return null;
                nav.Refresh();
                Assert.True(eva.HasBeenCompleted);
                Assert.True(quests.IsQuestInProgress(radishes.questId));
                Assert.False(quests.IsQuestInProgress("Which Witch"));
                Assert.That(quests.GetNoticeboardEntries().Single(e => e.Quest == radishes).Progress, Is.EqualTo(0));
                Assert.AreEqual(DisplayStyle.Flex, navUi.Q("tutorial-pointer-adventure").style.display.value);
                yield return new WaitForSecondsRealtime(.6f);
                CaptureFeedback("adventure-hand-no-click-artifacts.png"); yield return null; yield return null;
                Object.Destroy(intro); yield return null;
                intro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab"));
                yield return null; yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The completed introduction must not replay on scene initialization.");
                Assert.IsNull(Resources.Load<QuestData>("Quests/Vast One/An intro to Trekking"), "The reaping introduction is no longer a playable quest.");
                Assert.False(quests.IsQuestInProgress("An Intro to Trekking"));
                var farmlands = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset");
                var navDefinition = AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset");
                Assert.True(ToolkitVisibilityRule.All(navDefinition.entries.Single(e => e.id == "map-0").visibility),
                    "Farmlands must be reachable before meeting the Reaper there.");
                var introduction = farmlands.entryIntroduction;
                Assert.NotNull(introduction);
                Assert.Greater(farmlands.taskGeneratorSettings.npcTasks.Single(n => n.id == "Farmers1").localX,
                    new GameData().General.MaxRunDistance, "The farmers must remain beyond the fresh save's distance limit.");
                oracle.saveData.Quests["An Intro to Trekking"] = new GameData.QuestRecord { Completed = true };
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, navUi.Q("skills-row").style.display.value);
                Assert.False(introduction.ShowAndWait(locationObject.transform).MoveNext(), "Older saves must not repeat their introduction.");
                oracle.saveData.Quests.Remove("An Intro to Trekking");
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, navUi.Q("skills-row").style.display.value);
                var cancelledMap = new GameObject("Map left before the Reaper finished speaking");
                var cancelledFlow = introduction.ShowAndWait(cancelledMap.transform);
                Assert.True(cancelledFlow.MoveNext());
                Object.DestroyImmediate(cancelledMap);
                Assert.False(cancelledFlow.MoveNext());
                Assert.False(oracle.saveData.CompletedNpcTasks.Contains(introduction.npcId));
                var flow = introduction.ShowAndWait(locationObject.transform);
                Assert.True(flow.MoveNext());
                var reaperMeeting = locationObject.GetComponentInChildren<ToolkitMeetingScreen>();
                Assert.NotNull(reaperMeeting);
                var carlPortrait = reaperMeeting.GetComponent<UIDocument>().rootVisualElement.Q<Image>("portrait");
                var flyInPose = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Reaper.prefab").GetComponentInChildren<SpriteRenderer>().sprite;
                Assert.AreNotEqual(flyInPose, carlPortrait.sprite, "Carl must greet the player in an idle pose.");
                var seenPortraits = new HashSet<Sprite>();
                for (int i = 0; i < 6; i++)
                {
                    seenPortraits.Add(carlPortrait.sprite);
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Assert.Greater(seenPortraits.Count, 1, "Carl's portrait must play his idle animation.");
                Assert.False(oracle.saveData.CompletedNpcTasks.Contains(introduction.npcId), "Starting the dialogue must not mark it as finished.");
                yield return null;
                CaptureFeedback("reaper-first-entry.png"); yield return null; yield return null;
                for (int i = 0; i < introduction.lines.Length; i++) reaperMeeting.BeginDialogue();
                Assert.False(flow.MoveNext());
                Assert.True(oracle.saveData.CompletedNpcTasks.Contains(introduction.npcId));
                Assert.False(quests.IsQuestInProgress("An Intro to Trekking"));
                yield return null;
                Assert.False(introduction.ShowAndWait(locationObject.transform).MoveNext(), "Subsequent visits must not repeat the meeting.");
                windows.OpenWindow(TownWindowManager.Window.Quests);
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, navUi.Q("skills-row").style.display.value);
                Assert.AreEqual(DisplayStyle.None, navUi.Q("wiki-row").style.display.value);

                var gill = Resources.Load<QuestData>("Quests/Gill/The names Gill");
                foreach (var prerequisite in gill.requiredQuests)
                    oracle.saveData.Quests[prerequisite.questId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.CompletedNpcTasks.Add("Gill1"); quests.OnNpcMet("Gill1");
                yield return null; yield return null;
                var stats = screenObject.GetComponent<UIDocument>().rootVisualElement.Q("equipment-reward-stats");
                Assert.NotNull(stats);
                var icons = stats.Query<Image>().ToList();
                Assert.AreEqual(4, icons.Count, "The boots' four stats must have real icons.");
                Assert.True(icons.All(icon => icon.sprite != null));
                Assert.AreEqual(4, icons.Select(icon => icon.sprite).Distinct().Count());
                CaptureFeedback("gill-reward-stat-icons.png"); yield return null; yield return null;
            }
            finally
            {
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (intro) Object.DestroyImmediate(intro);
                if (navigation) Object.DestroyImmediate(navigation);
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(windowsOwner); Object.DestroyImmediate(owner); Object.DestroyImmediate(locationObject);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SlimeTimeTrialWaitsForOldTimerMeeting(bool existingUnfinishedRecord)
        {
            var quest = Resources.Load<QuestData>("Quests/BuffS/CombatEnhancer25D");
            if (existingUnfinishedRecord)
                oracle.saveData.Quests[quest.questId] = new GameData.QuestRecord();
            var owner = new GameObject("Isolated Old Timer quest gate");
            try
            {
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                Assert.False(quests.IsQuestInProgress(quest.questId), "Slime Time Trial must stay inactive before meeting its quest giver.");
                oracle.saveData.CompletedNpcTasks.Add("Gill1"); quests.OnNpcMet("Gill1");
                Assert.False(quests.IsQuestInProgress(quest.questId), "Meeting another NPC must not unlock it.");
                oracle.saveData.CompletedNpcTasks.Add("OldTimer1"); quests.OnNpcMet("OldTimer1");
                Assert.True(quests.IsQuestInProgress(quest.questId));
                Blindsided.EventHandler.LoadData();
                Assert.True(quests.IsQuestInProgress(quest.questId), "Reloading a met Old Timer must retain access.");
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GillGeneratedAtShorelineKeepsFeetOnLandAndBobberInWater(bool asynchronous)
        {
            var authored = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset");
            var gill = authored.taskGeneratorSettings.npcTasks.Single(n => n.id == "Gill1");
            var random = Random.state;
            try
            {
                for (int seed = -1; seed < 4; seed++)
                {
                    var root = new GameObject("Isolated shoreline", typeof(Grid));
                    var waterTerrain = Object.Instantiate(authored.tilemapChunkSettings.bottomTerrain);
                    var landTerrain = Object.Instantiate(authored.tilemapChunkSettings.topTerrain);
                    try
                    {
                        var mapObject = new GameObject("Terrain", typeof(Tilemap)); mapObject.transform.SetParent(root.transform, false);
                        var map = mapObject.GetComponent<Tilemap>();
                        var water = new SerializedObject(waterTerrain).FindProperty("tile").objectReferenceValue as TileBase;
                        var land = new SerializedObject(landTerrain).FindProperty("tile").objectReferenceValue as TileBase;
                        for (int x = 0; x < 64; x++) for (int y = 0; y < 18; y++)
                            map.SetTile(new Vector3Int(x, y, 0), y < (seed < 0 ? 4 : 3 + ((x + seed * 3) / 4) % 3) ? water : land);
                        var generator = root.AddComponent<ProceduralTaskGenerator>();
                        root.GetComponent<TaskController>().enabled = false;
                        void Set(string field, object value) => typeof(ProceduralTaskGenerator).GetField(field, Private).SetValue(generator, value);
                        Set("terrainMap", map); Set("bottomTerrain", waterTerrain); Set("middleTerrain", landTerrain); Set("topTerrain", landTerrain);
                        Set("height", 18f); Set("enemyDensity", 0f);
                        Set("taskCategories", new List<ProceduralTaskGenerator.WeightedTaskCategory>());
                        Set("npcTasks", new List<MapGenerationConfig.ProceduralTaskSettings.NpcSpawnEntry> {
                            new() { prefab=gill.prefab, id=gill.id, localX=32, placeAtWaterEdge=gill.placeAtWaterEdge,
                                fishingWaterOffset=gill.fishingWaterOffset, spawnTerrains=new List<TerrainSettings>{landTerrain} }
                        });
                        if (asynchronous) { var work = generator.GenerateSegmentAsync(0, 64, root.transform); while (work.MoveNext()) { } }
                        else generator.GenerateSegment(0, 64, root.transform);
                        var spawned = root.GetComponentsInChildren<TalkToNpcTask>();
                        Assert.AreEqual(1, spawned.Length, "Gill must remain reachable on each stepped bank.");
                        var position = spawned[0].transform.position;
                        var cell = map.WorldToCell(position);
                        Assert.AreSame(land, map.GetTile(cell));
                        Assert.AreSame(water, map.GetTile(cell + Vector3Int.down));
                        Assert.AreSame(water, map.GetTile(map.WorldToCell(position + (Vector3)gill.fishingWaterOffset)));
                        Assert.AreSame(water, map.GetTile(map.WorldToCell(position + (Vector3)gill.fishingWaterOffset) + Vector3Int.up),
                            "The hook must clear the bank artwork in the first water row.");
                    }
                    finally { Object.DestroyImmediate(root); Object.DestroyImmediate(waterTerrain); Object.DestroyImmediate(landTerrain); }
                }
            }
            finally { Random.state = random; }
        }

    }
}
#endif
