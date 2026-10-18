#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Buffs;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
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
        public IEnumerator GuidanceFirstReapingWaitsForDialogueBeforeOfferingRunActions()
        {
            var owner = new GameObject("Protected first reaping");
            GameObject runView = null, speakerObject = null;
            try
            {
                owner.AddComponent<ResourceManager>();
                owner.AddComponent<GameplayStatTracker>().enabled = false;
                owner.AddComponent<QuestManager>().enabled = false;
                owner.AddComponent<TownWindowManager>().enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                var map = new GameObject("Reaped map"); map.transform.SetParent(owner.transform);
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm, map);
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, false);
                typeof(TimelessEchoes.GameManager).GetProperty("DeathPromptVisible").SetValue(gm, true);
                typeof(TimelessEchoes.GameManager).GetField("heroDead", Private).SetValue(gm, true);
                typeof(TimelessEchoes.GameManager).GetField("runEndedByReaper", Private).SetValue(gm, true);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab").GetComponent<ToolkitOrinIntroduction>();
                var first = (QuestData)typeof(ToolkitOrinIntroduction).GetField("firstQuest", Private).GetValue(source);
                oracle.saveData.Quests[first.questId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.CompletedNpcTasks.Add(Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Eva the Witch").npcId);
                runView = CreateGuidanceRunView();
                speakerObject = new GameObject("Protected reaping speaker");
                var speaker = speakerObject.AddComponent<ToolkitOrinIntroduction>();
                foreach (var field in typeof(ToolkitOrinIntroduction).GetFields(Private)) field.SetValue(speaker, field.GetValue(source));
                yield return new WaitForSecondsRealtime(.15f);
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>(); Assert.NotNull(meeting);
                var ui = runView.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreEqual(DisplayStyle.None, ui.Q(className: "eov-run-death").style.display.value);
                Assert.AreEqual(DisplayStyle.None, ui.Q("ivan-return-pointer").style.display.value);
                gm.RequestRestartAfterDeath(); gm.RequestReturnAfterDeath();
                yield return null;
                Assert.True(gm.HeroIsDead); Assert.False(gm.IsInTown);
                FieldsClick(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance"));
                yield return new WaitForSecondsRealtime(.15f);
                Assert.True(oracle.saveData.CompletedNpcTasks.Contains("FirstReapIntroduction"));
                Assert.AreEqual(DisplayStyle.Flex, ui.Q(className: "eov-run-death").style.display.value);
                Assert.True(ui.Q<Button>("run-death-return").enabledInHierarchy);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("ivan-return-pointer").style.display.value);
                gm.RequestReturnAfterDeath(); yield return null; yield return null; yield return null;
                Assert.True(gm.IsInTown);
            }
            finally
            {
                if (speakerObject) Object.DestroyImmediate(speakerObject);
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (runView) Object.DestroyImmediate(runView);
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator GuidanceDistanceBarAnimatesCarlAndTheMovingPlayer()
        {
            var owner = new GameObject("Protected animated distance bar"); GameObject navigation = null;
            try
            {
                owner.AddComponent<ResourceManager>(); owner.AddComponent<GameplayStatTracker>().enabled = false;
                CreateGuidancePathfinder(owner);
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm, owner);
                var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Caleb.prefab"), owner.transform);
                var hero = actor.GetComponent<TimelessEchoes.Hero.HeroController>(); hero.enabled = false;
                hero.MovementController.SetActiveState(false, hero.Animator);
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var ui = navigation.GetComponent<UIDocument>().rootVisualElement;
                var definition = AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset");
                var carlFrames = new HashSet<Sprite>(); var idleFrames = new HashSet<Sprite>(); var runFrames = new HashSet<Sprite>();
                for (var i = 0; i < 8; i++)
                {
                    yield return new WaitForSecondsRealtime(.09f);
                    carlFrames.Add(ui.Q<Image>("progress-reaper").sprite);
                    idleFrames.Add(ui.Q<Image>("progress-hero").sprite);
                }
                Assert.Greater(carlFrames.Count, 1); Assert.Greater(idleFrames.Count, 1);
                Assert.True(carlFrames.All(s => definition.progressReaperFrames.Contains(s)));
                for (var i = 0; i < 30; i++)
                {
                    actor.transform.position += Vector3.right * .02f;
                    yield return null;
                    runFrames.Add(ui.Q<Image>("progress-hero").sprite);
                }
                Assert.Greater(runFrames.Count, 1);
                Assert.True(runFrames.All(s => definition.progressHeroRunFrames.Contains(s)));
                yield return new WaitForSecondsRealtime(.1f);
                Assert.Contains(ui.Q<Image>("progress-hero").sprite, definition.progressHeroIdleFrames);
            }
            finally { if (navigation) Object.DestroyImmediate(navigation); Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator GuidanceFiveFishUnlocksAnExplicitEchoChoice()
        {
            var owner=new GameObject("Early Echo lesson"); GameObject view=null, runView=null;
            try
            {
                Assert.That(oracle.saveData.BuffSlots.All(string.IsNullOrEmpty));
                var quest=Resources.Load<QuestData>("Quests/Mildred/BuffSlot1");
                foreach(var prerequisite in quest.requiredQuests) oracle.saveData.Quests[prerequisite.questId]=new GameData.QuestRecord {Completed=true};
                var resources=owner.AddComponent<ResourceManager>();
                var tracker=owner.AddComponent<GameplayStatTracker>();tracker.enabled=false;
                var quests=owner.AddComponent<QuestManager>();quests.enabled=false;
                var buffs=owner.AddComponent<BuffManager>();
                resources.Add(quest.requirements[0].resource,4,eligibleForTierRoll:false);
                Assert.False(quests.TryTurnInQuest(quest.questId));
                resources.Add(quest.requirements[0].resource,1,eligibleForTierRoll:false);
                var olderPins = Resources.LoadAll<QuestData>("Quests")
                    .Where(q => q.questId != quest.questId && q.questId != "Echo Casting" && !q.autoPin)
                    .Take(PinnedQuestUIManager.MaxPins).Select(q => q.questId).ToArray();
                oracle.saveData.PinnedQuests.Clear();
                oracle.saveData.PinnedQuests.AddRange(olderPins);
                Assert.True(quests.TryTurnInQuest(quest.questId));
                Assert.AreEqual("Echo Casting", oracle.saveData.PinnedQuests[0], "The autocast lesson takes priority even when tracking is full.");
                Assert.AreEqual(PinnedQuestUIManager.MaxPins, oracle.saveData.PinnedQuests.Count);
                Assert.True(buffs.HasUnlockedAbilities);
                var experience = Resources.Load<BuffRecipe>("Buffs/Bonus Experience");
                Assert.False(buffs.CanActivate(experience), "Bonus Experience now waits for the casting lesson.");
                Assert.False(quests.GetNoticeboardEntries().Any(e => e.Quest.questId == "Into The Multiverse"));
                Assert.True(BuffIntroductionGuidance.Eligible);
                Assert.IsNull(buffs.GetAssigned(0));
                oracle.saveData.General.BuffTutorialStage=1;
                view=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Buffs.prefab"));
                var screen=view.GetComponent<ToolkitBuffsScreen>();Assert.True(screen.Show());
                yield return null;yield return null;
                var ui=view.GetComponent<UIDocument>().rootVisualElement;
                Assert.NotNull(ui.Q<Button>("assign-Echo Tasks"));Assert.NotNull(ui.Q<Button>("assign-Echo Combat"));
                Assert.IsNull(ui.Q<Button>("assign-Bonus Experience"));
                FieldsClick(ui.Q<Button>("assign-Echo Combat"));yield return null;
                Assert.True(BuffIntroductionGuidance.Active,"Choosing the recipe is not yet an assignment.");
                FieldsClick(ui.Q<Button>("assign-slot-0"));yield return null;
                Assert.AreEqual("Echo Combat",buffs.GetAssigned(0).name);
                Assert.False(BuffIntroductionGuidance.Active);
                Assert.False(buffs.IsAutoSlotUnlocked(0));
                CaptureFeedback("first-echo-choice.png");yield return null;yield return null;
                Blindsided.EventHandler.LoadData();yield return null;
                Assert.AreEqual("Echo Combat",buffs.GetAssigned(0).name);
                Assert.AreEqual(3,oracle.saveData.General.BuffTutorialStage);
                screen.Hide();
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                var map = new GameObject("First Echo adventure"); map.transform.SetParent(owner.transform);
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm, map);
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, false);
                tracker.BeginRun(AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/River.asset"), TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
                runView = CreateGuidanceRunView(); yield return new WaitForSecondsRealtime(.15f);
                var runUi = runView.GetComponent<UIDocument>().rootVisualElement;
                var arrow = runUi.Q<Image>("first-echo-arrow"); var cast = runUi.Q<Button>("run-buff-0");
                Assert.AreEqual(DisplayStyle.Flex, arrow.style.display.value);
                Assert.That(arrow.worldBound.center.x, Is.EqualTo(cast.worldBound.center.x).Within(1));
                Assert.Less(arrow.worldBound.yMax, cast.worldBound.yMin);
                var positions = new HashSet<float>();
                for (var i = 0; i < 4; i++) { yield return new WaitForSecondsRealtime(.08f); positions.Add(arrow.style.top.value.value); }
                Assert.Greater(positions.Count, 1, "The green arrow bounces above the equipped ability.");
                FieldsClick(cast); yield return null;
                Assert.AreEqual(1, tracker.BuffsCast);
                Assert.AreEqual(4, oracle.saveData.General.BuffTutorialStage);
                Assert.AreEqual(DisplayStyle.None, arrow.style.display.value);
                Blindsided.EventHandler.LoadData(); yield return null;
                Assert.AreEqual(DisplayStyle.None, arrow.style.display.value, "The casting prompt stays completed after reload.");
                for (var i = tracker.BuffsCast; i < 9; i++) { buffs.ClearActiveBuffs(); Assert.True(buffs.ActivateSlot(0)); }
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, true);
                Assert.False(quests.TryTurnInQuest("Echo Casting"));
                buffs.ClearActiveBuffs(); Assert.True(buffs.ActivateSlot(0));
                Assert.True(quests.TryTurnInQuest("Echo Casting"));
                Assert.True(buffs.IsAutoSlotUnlocked(0));
                Assert.True(buffs.CanActivate(experience));
            }
            finally {if(runView)Object.DestroyImmediate(runView);if(view)Object.DestroyImmediate(view);Object.DestroyImmediate(owner);}
        }

        [UnityTest]
        public IEnumerator GuidanceRetiredMultiverseKeepsPreviouslyEarnedExperienceBuff()
        {
            var owner = new GameObject("Protected legacy Echo rewards"); GameObject view = null;
            try
            {
                oracle.saveData.Quests["Into The Multiverse"] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.PinnedQuests.Add("Into The Multiverse");
                owner.AddComponent<ResourceManager>();
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                var buffs = owner.AddComponent<BuffManager>();
                var experience = Resources.Load<BuffRecipe>("Buffs/Bonus Experience");
                Assert.True(buffs.CanActivate(experience));
                Assert.False(QuestUtils.QuestCompleted("Echo Casting"), "Keeping an earned buff must not grant the new quest's autocasting reward.");
                Assert.False(buffs.IsAutoSlotUnlocked(0));
                Assert.False(oracle.saveData.PinnedQuests.Contains("Into The Multiverse"));
                Assert.False(quests.GetNoticeboardEntries().Any(e => e.Quest.questId == "Into The Multiverse"));
                view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Buffs.prefab"));
                Assert.True(view.GetComponent<ToolkitBuffsScreen>().Show()); yield return null;
                Assert.NotNull(view.GetComponent<UIDocument>().rootVisualElement.Q<Button>("assign-Bonus Experience"));
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData(); yield return null;
                Assert.True(buffs.CanActivate(experience));
            }
            finally { if (view) Object.DestroyImmediate(view); Object.DestroyImmediate(owner); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GuidanceOpeningRiverReservesFiveFishBeforeBarkley(bool asynchronous)
        {
            var river=AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/River.asset");
            var random=Random.state;
            try
            {
                for(var seed=0;seed<6;seed++)
                {
                    var root=new GameObject("Opening River",typeof(Grid));
                    try
                    {
                        var mapObject=new GameObject("Terrain",typeof(Tilemap));mapObject.transform.SetParent(root.transform,false);
                        var map=mapObject.GetComponent<Tilemap>();
                        var water=river.tilemapChunkSettings.middleTerrain;var land=river.tilemapChunkSettings.topTerrain;
                        var waterTile=new SerializedObject(water).FindProperty("tile").objectReferenceValue as TileBase;
                        var landTile=new SerializedObject(land).FindProperty("tile").objectReferenceValue as TileBase;
                        for(var x=0;x<64;x++)for(var y=0;y<18;y++) map.SetTile(new Vector3Int(x,y),y<5+(x+seed)%3?waterTile:landTile);
                        var generator=root.AddComponent<ProceduralTaskGenerator>();root.GetComponent<TaskController>().enabled=false;
                        typeof(ProceduralTaskGenerator).GetMethod("ApplyConfig",Private).Invoke(generator,new object[]{river});
                        void Set(string field,object value)=>typeof(ProceduralTaskGenerator).GetField(field,Private).SetValue(generator,value);
                        Set("terrainMap",map);Set("enemyDensity",0f);Set("taskCategories",new List<ProceduralTaskGenerator.WeightedTaskCategory>());
                        Set("bottomTerrain",river.tilemapChunkSettings.bottomTerrain);Set("middleTerrain",water);Set("topTerrain",land);
                        Set("npcTasks",new List<MapGenerationConfig.ProceduralTaskSettings.NpcSpawnEntry>());
                        if(asynchronous){var work=generator.GenerateSegmentAsync(0,64,root.transform);while(work.MoveNext()){} }
                        else generator.GenerateSegment(0,64,root.transform);
                        var fish=root.GetComponentsInChildren<FishingTask>();
                        Assert.AreEqual(5,fish.Length,"The five opening spots must exist even with no random tasks enabled.");
                        Assert.True(fish.All(f=>f.transform.position.x<40));
                        Assert.AreEqual(5,root.GetComponent<TaskController>().tasks.Count);
                        Assert.True(fish.All(f=>f.taskData.resourceDrops[0].dropRange.x>=1));
                    }
                    finally{Object.DestroyImmediate(root);}
                }
            }
            finally{Random.state=random;}
        }

        [UnityTest]
        public IEnumerator GuidanceLibraryReaderKeepsSelectionAndProtectsLockedTopics()
        {
            var owner = new GameObject("Library help lifecycle", typeof(UIDocument));
            try
            {
                var screen = owner.AddComponent<ToolkitBookScreen>();
                void Assign(string field, Object value) => typeof(ToolkitBookScreen).GetField(field, Private).SetValue(screen, value);
                var book = AssetDatabase.LoadAssetAtPath<ToolkitBookDefinition>("Assets/UI/Toolkit/Library.asset");
                Assign("definition", book);
                Assign("theme", AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"));
                Assign("template", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Toolkit/Book.uxml"));
                Assign("runtimeTheme", AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"));
                Assign("textSettings", AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
                Assert.True(screen.Show()); yield return new WaitForSecondsRealtime(.1f);
                var ui = owner.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("help-forge").style.display.value);
                FieldsClick(ui.Q<Button>("help-combat")); yield return null;
                Assert.IsNull(ui.Q("library-reference-table"), "Overview keeps the numeric reference separate.");
                Assert.IsNotEmpty(ui.Q<Label>("library-article-body").text);
                Assert.True(ui.Q("library-illustrations").Query<Image>().ToList().All(i => i.sprite), "Supporting art uses actual game sprites.");
                FieldsClick(ui.Q<Button>("library-illustration-1")); yield return null;
                Assert.True(ui.Q<Button>("library-illustration-1").ClassListContains("active"));
                Assert.That(ui.Q<Label>("library-illustration-description").text, Does.Contain("Defense"));
                Assert.IsNotEmpty(ui.Q<Label>("library-article-body").text, "Icon explanations retain the article's main instructions.");
                CaptureFeedback("library-icon-tabs.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("library-reference")); yield return null;
                Assert.NotNull(ui.Q("library-reference-table"));
                CaptureFeedback("library-combat-reference.png"); yield return null; yield return null;
                screen.Hide(); Assert.True(screen.Show()); yield return new WaitForSecondsRealtime(.1f);
                Assert.True(ui.Q<Button>("help-combat").ClassListContains("active"));
                Assert.True(ui.Q<Button>("library-reference").ClassListContains("active"));
                Assert.NotNull(ui.Q("library-reference-table"));
                var forgeQuest = book.sections.Single(s => s.id == "help-forge").visibility[0].quest;
                FieldsClick(ui.Q<Button>("library-overview")); yield return null;
                Assert.True(ui.Q<Button>("library-illustration-1").ClassListContains("active"));
                Assert.That(ui.Q<Label>("library-illustration-description").text, Does.Contain("Defense"));
                oracle.saveData.Quests[forgeQuest.questId] = new GameData.QuestRecord { Completed = true };
                screen.Hide(); Assert.True(screen.Show()); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q<Button>("help-forge").style.display.value);
                FieldsClick(ui.Q<Button>("help-forge")); yield return null;
                Assert.True(ui.Q<Button>("library-overview").ClassListContains("active"));
                Assert.AreEqual(3, ui.Q("library-illustrations").Query<Image>().ToList().Count);
                CaptureFeedback("library-forge-overview.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("library-reference")); yield return null;
                CaptureFeedback("library-forge-reference.png"); yield return null; yield return null;
                foreach (var size in new[] { new Vector2Int(844, 390), new Vector2Int(3840, 1080) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Library layout check");
                    yield return null; yield return null;
                    var reader = ui.Q(className: "library-reader");
                    var article = ui.Q<ScrollView>("library-article-scroll");
                    Assert.Greater(article.worldBound.width, 0);
                    Assert.LessOrEqual(article.worldBound.xMax, reader.worldBound.xMax + 1);
                    Assert.LessOrEqual(ui.Q("library-reference-table").worldBound.xMax, article.worldBound.xMax + 1);
                    CaptureFeedback("library-" + size.x + ".png"); yield return null; yield return null;
                }
                oracle.saveData.Quests[forgeQuest.questId].Completed = false;
                screen.Hide(); Assert.True(screen.Show()); yield return new WaitForSecondsRealtime(.1f);
                Assert.False(ui.Q<Button>("help-forge").ClassListContains("active"), "A now-locked topic cannot be restored into the reader.");
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("help-forge").style.display.value);
                screen.Hide();
                Assign("definition", AssetDatabase.LoadAssetAtPath<ToolkitBookDefinition>("Assets/UI/Toolkit/Credits.asset"));
                Assert.True(screen.Show()); yield return null;
                Assert.NotNull(ui.Q(className: "eov-credit"));
                Assert.IsNull(ui.Q("library-topics"), "Credits retains its independent layout.");
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static void CreateGuidancePathfinder(GameObject owner)
        {
            var pathfinder = owner.AddComponent<AstarPath>();
            var grid = (Pathfinding.GridGraph)pathfinder.data.AddGraph(typeof(Pathfinding.GridGraph));
            grid.SetDimensions(32, 32, 1); grid.rotation = new Vector3(-90, 0, 0);
            grid.collision.use2D = true; grid.collision.collisionCheck = false; grid.collision.heightCheck = false;
            pathfinder.Scan();
            owner.AddComponent<Pathfinding.RVO.RVOSimulator>().movementPlane = Pathfinding.RVO.MovementPlane.XY;
        }

        [UnityTest]
        public IEnumerator GuidanceNpcMeetingsKeepTheWorldRunningUntilFinishedOrCancelled()
        {
            var owner = new GameObject("Protected NPC conversation lifecycle");
            GameObject npc = null;
            var scale = Time.timeScale;
            try
            {
                var buffs = owner.AddComponent<BuffManager>();
                buffs.SetBaseTimeScale(1.5f);
                foreach (var name in new[] { "Gill", "Barkley", "Ivan" })
                {
                    npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/NPC/" + name + "MeetingTask.prefab"));
                    var task = npc.GetComponent<TalkToNpcTask>();
                    var id = (string)typeof(TalkToNpcTask).GetField("npcId", Private).GetValue(task);
                    task.StartTask(); task.OnArrival(null);
                    yield return new WaitForSecondsRealtime(.1f);
                    Assert.False(task.IsComplete(), "Arrival must not complete a conversation.");
                    Assert.True(StaticReferences.ActiveNpcMeetings.Contains(id));
                    Assert.False(StaticReferences.CompletedNpcTasks.Contains(id));
                    var clock = Time.time;
                    buffs.SetBaseTimeScale(2f);
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.That(Time.time, Is.GreaterThan(clock + .1f), "The world clock must continue during NPC conversations.");
                    Assert.That(Time.timeScale, Is.EqualTo(2f), "Conversation UI must not override the selected game speed.");
                    var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                    Assert.NotNull(meeting);
                    var ui = meeting.GetComponent<UIDocument>().rootVisualElement;
                    Assert.AreEqual(DisplayStyle.Flex, ui.Q("meeting-hand").style.display.value);
                    for (var i = 0; meeting && i < 12; i++)
                    { FieldsClick(ui.Q<Button>("advance")); yield return new WaitForSecondsRealtime(.05f); }
                    Assert.False(meeting);
                    Assert.True(StaticReferences.CompletedNpcTasks.Contains(id));
                    Assert.False(StaticReferences.ActiveNpcMeetings.Contains(id));
                    Assert.True(task.IsComplete());
                    Assert.That(Time.timeScale, Is.EqualTo(2f));
                    Object.DestroyImmediate(npc); npc = null;
                }
                // Abandoning a route cancels an unfinished encounter without awarding it.
                StaticReferences.CompletedNpcTasks.Remove("Gill1");
                npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/NPC/GillMeetingTask.prefab"));
                npc.GetComponent<TalkToNpcTask>().StartTask(); npc.GetComponent<TalkToNpcTask>().OnArrival(null);
                yield return null;
                npc.SetActive(false); yield return null;
                Assert.False(StaticReferences.CompletedNpcTasks.Contains("Gill1"));
                Assert.False(StaticReferences.ActiveNpcMeetings.Contains("Gill1"));
            }
            finally
            {
                if (npc) Object.DestroyImmediate(npc);
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                Object.DestroyImmediate(owner); Time.timeScale = scale;
            }
        }

        // Real actor/pathfinding/combat boundary: combat may interrupt the wait,
        // but must return to the same spot and cannot select another objective.
        [UnityTest]
        public IEnumerator GuidanceConversationAllowsCombatThenReturnsToNpcBeforeContinuing()
        {
            var owner = new GameObject("Protected conversation combat map");
            GameObject actor = null, enemyObject = null, npcObject = null, cropObject = null, cameraObject = null;
            try
            {
                owner.AddComponent<ResourceManager>(); owner.AddComponent<GameplayStatTracker>().enabled = false;
                CreateGuidancePathfinder(owner);
                var tasks = owner.AddComponent<TaskController>(); tasks.enabled = false;
                actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Caleb.prefab"), owner.transform);
                actor.transform.position = Vector3.zero;
                var hero = actor.GetComponent<TimelessEchoes.Hero.HeroController>();
                hero.MovementController.AI.Teleport(Vector3.zero); tasks.hero = hero;
                // Carl is an entry introduction rather than a TalkToNpcTask. It
                // uses the same animated wait without disabling the actor.
                var carl = Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Carl the Reaper");
                hero.StartCoroutine(carl.ShowAndWait(owner.transform));
                yield return null;
                var carlAnimation = hero.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.True(hero.IsInNpcConversation);
                Assert.True(hero.MovementController.LogicActive);
                Assert.That(hero.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(carlAnimation));
                var carlMeeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                for (var i = 0; carlMeeting && i < 8; i++)
                { carlMeeting.BeginDialogue(); yield return new WaitForSecondsRealtime(.05f); }
                Assert.True(carl.HasBeenCompleted);
                Assert.False(hero.IsInNpcConversation);
                hero.MovementController.AI.Teleport(Vector3.zero);

                npcObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/NPC/GillMeetingTask.prefab"), owner.transform);
                npcObject.transform.position = actor.transform.position;
                var npc = npcObject.GetComponent<TalkToNpcTask>(); tasks.AddRuntimeTaskObject(npc);
                hero.SetTask(npc); npc.StartTask(); npc.OnArrival(hero);
                cropObject = Object.Instantiate(Resources.Load<TaskData>("Tasks/Farming/Radish").taskPrefab.gameObject, owner.transform);
                cropObject.transform.position = new Vector3(8, 0);
                var crop = cropObject.GetComponent<BaseTask>(); tasks.AddRuntimeTaskObject(crop);
                tasks.SelectEarliestTask(hero);
                Assert.AreSame(npc, hero.CurrentTask, "New objectives cannot replace the NPC conversation.");
                var standingSpot = hero.transform.position;
                var time = Time.time; var animation = hero.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                yield return new WaitForSecondsRealtime(.35f);
                Assert.True(hero.IsInNpcConversation);
                Assert.That(Time.time, Is.GreaterThan(time + .1f));
                Assert.That(Vector2.Distance(hero.transform.position, standingSpot), Is.LessThan(.2f));
                Assert.AreNotEqual(animation, hero.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, "Animation must advance, including transitions from walking to idle.");
                cameraObject = new GameObject("Protected combat visibility", typeof(Camera));
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 10;
                camera.transform.position = new Vector3(0,0,-10);
                cameraObject.AddComponent<TimelessEchoes.Enemies.EnemyActivator>().enabled = false;
                enemyObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/Enemies/Slime/Slime_Green.prefab"), owner.transform);
                enemyObject.transform.position = new Vector3(3, 0);
                var enemy = enemyObject.GetComponent<TimelessEchoes.Enemies.Enemy>(); enemy.InitForSpawn();
                enemy.Health.Init(100);
                var deadline = Time.unscaledTime + 5;
                while (!hero.InCombat && Time.unscaledTime < deadline) yield return null;
                Assert.True(hero.InCombat, "Nearby enemies must still engage the conversing player.");
                var hp = enemy.Health.CurrentHealth;
                deadline = Time.unscaledTime + 5;
                while (enemy && enemy.Health.CurrentHealth >= hp && Time.unscaledTime < deadline) yield return null;
                Assert.That(enemy.Health.CurrentHealth, Is.LessThan(hp), "The player must fight during dialogue.");
                enemy.Health.KillImmediately();
                deadline = Time.unscaledTime + 5;
                while ((hero.InCombat || Vector2.Distance(hero.transform.position, standingSpot) > .3f) && Time.unscaledTime < deadline) yield return null;
                Assert.False(hero.InCombat);
                Assert.That(Vector2.Distance(hero.transform.position, standingSpot), Is.LessThan(.3f), "After fighting, return to the NPC's standing spot.");
                Assert.True(hero.IsInNpcConversation);
                Assert.False(crop.IsComplete());
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(Vector2.Distance(hero.transform.position, standingSpot), Is.LessThan(.3f), "Keep waiting instead of taking the next task.");
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                for (var i = 0; meeting && i < 12; i++)
                { meeting.BeginDialogue(); yield return new WaitForSecondsRealtime(.05f); }
                Assert.False(hero.IsInNpcConversation);
                deadline = Time.unscaledTime + 3;
                while (hero.transform.position.x <= standingSpot.x + 1 && Time.unscaledTime < deadline) yield return null;
                Assert.Greater(hero.transform.position.x, standingSpot.x + 1, "Closing the conversation resumes the route.");
            }
            finally
            {
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (enemyObject) Object.DestroyImmediate(enemyObject);
                if (actor) Object.DestroyImmediate(actor);
                if (cameraObject) Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(owner);
            }
        }

        // Owns unreachable task targets: the real path ends on the bank, while
        // the fish remains in water outside the hero's normal interaction radius.
        [UnityTest]
        public IEnumerator GuidanceFishingStartsAtTheReachableBankAndAwardsTheCatch()
        {
            var owner = new GameObject("Protected fishing shoreline");
            GameObject actor = null, fishObject = null;
            try
            {
                var resources = owner.AddComponent<ResourceManager>();
                owner.AddComponent<GameplayStatTracker>().enabled = false;
                CreateGuidancePathfinder(owner);
                var pathfinder = owner.GetComponent<AstarPath>();
                pathfinder.UpdateGraphs(new Pathfinding.GraphUpdateObject(new Bounds(new Vector3(0, -8, 0), new Vector3(32, 16, 2)))
                    { modifyWalkability = true, setWalkability = false, updatePhysics = false });
                pathfinder.FlushGraphUpdates();
                actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Caleb.prefab"), owner.transform);
                var hero = actor.GetComponent<TimelessEchoes.Hero.HeroController>();
                hero.MovementController.AI.Teleport(new Vector3(0, 3));
                var data = Resources.Load<TaskData>("Tasks/Fishing/Flippy Floppy");
                fishObject = Object.Instantiate(data.taskPrefab.gameObject, owner.transform);
                fishObject.transform.position = new Vector3(6, -4);
                var fishing = fishObject.GetComponent<FishingTask>();
                var caught = false; var cast = false; var bank = Vector3.zero;
                var repathed = false; var castPosition = Vector3.zero;
                fishing.TaskCompleted += _ => { caught = true; bank = hero.transform.position; };
                fishing.StartTask(); hero.SetTask(fishing);
                var deadline = Time.unscaledTime + 8;
                while (!caught && Time.unscaledTime < deadline)
                {
                    if (fishing.ProgressRemaining < 1)
                    {
                        Assert.Greater(Vector2.Distance(hero.transform.position, fishing.Target.position), 1.5f);
                        Assert.GreaterOrEqual(hero.transform.position.y, 0, "The player must stay on the reachable bank.");
                        cast |= hero.Animator.GetCurrentAnimatorStateInfo(0).IsName("Fishing");
                        if (!repathed)
                        {
                            castPosition = hero.transform.position;
                            // A graph refresh can leave no current path while its replacement is pending.
                            hero.MovementController.AI.canSearch = false;
                            hero.MovementController.AI.SetPath(null);
                            repathed = true;
                        }
                        Assert.False(hero.MovementController.AI.simulateMovement, "Repathing during a cast must not restart walking.");
                        Assert.Less(Vector2.Distance(castPosition, hero.transform.position), .02f, "The hero must stay at the bank throughout the cast.");
                    }
                    yield return null;
                }
                Assert.True(caught, "Reaching the bank must start and finish fishing even though the fish target is in water.");
                Assert.True(cast, "Fishing must animate during the catch.");
                Assert.GreaterOrEqual(bank.y, 0);
                Assert.Greater(resources.GetAmount(data.resourceDrops[0].resource), 0, "The catch must award its real resource.");
            }
            finally
            {
                if (actor) Object.DestroyImmediate(actor);
                if (fishObject) Object.DestroyImmediate(fishObject);
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator GuidanceConversationDuringCropApproachDoesNotWaterUntilArrival()
        {
            var owner = new GameObject("Protected interrupted crop approach");
            GameObject actor = null, cropObject = null;
            System.IDisposable conversation = null;
            try
            {
                owner.AddComponent<ResourceManager>(); owner.AddComponent<GameplayStatTracker>().enabled = false;
                CreateGuidancePathfinder(owner);
                actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Caleb.prefab"), owner.transform);
                var hero = actor.GetComponent<TimelessEchoes.Hero.HeroController>();
                hero.MovementController.AI.Teleport(Vector3.zero);
                cropObject = Object.Instantiate(Resources.Load<TaskData>("Tasks/Farming/Radish").taskPrefab.gameObject, owner.transform);
                cropObject.transform.position = new Vector3(9, 0);
                var crop = cropObject.GetComponent<ContinuousTask>(); crop.StartTask(); hero.SetTask(crop);
                var deadline = Time.unscaledTime + 2;
                while (hero.transform.position.x < .5f && Time.unscaledTime < deadline) yield return null;
                Assert.Greater(hero.transform.position.x, .4f, "Begin dialogue while actually approaching the patch.");
                conversation = hero.BeginNpcConversation();
                yield return new WaitForSecondsRealtime(1f);
                Assert.That(crop.ProgressRemaining, Is.EqualTo(1f));
                conversation.Dispose(); conversation = null;
                deadline = Time.unscaledTime + 4;
                while (Vector2.Distance(hero.transform.position, crop.Target.position) > 1.05f && Time.unscaledTime < deadline)
                {
                    Assert.False(hero.Animator.GetCurrentAnimatorStateInfo(0).IsName("Water"), "A previous path endpoint must not start watering in the air.");
                    Assert.That(crop.ProgressRemaining, Is.EqualTo(1f), "Travel cannot advance crop work.");
                    yield return null;
                }
                Assert.LessOrEqual(Vector2.Distance(hero.transform.position, crop.Target.position), 1.05f);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.True(hero.Animator.GetCurrentAnimatorStateInfo(0).IsName("Water"));
                Assert.Less(crop.ProgressRemaining, 1f);
                // A conversation that interrupts watering must return to idle, too.
                conversation = hero.BeginNpcConversation(); yield return new WaitForSecondsRealtime(.2f);
                Assert.False(hero.Animator.GetCurrentAnimatorStateInfo(0).IsName("Water"));
            }
            finally
            {
                conversation?.Dispose();
                if (actor) Object.DestroyImmediate(actor);
                if (cropObject) Object.DestroyImmediate(cropObject);
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GuidanceTrackingTwinsRequiresMaximumDistanceOver80(bool existingRecord)
        {
            var quest = Resources.Load<QuestData>("Quests/Eva/Tracking Twins");
            foreach (var prerequisite in quest.requiredQuests)
                oracle.saveData.Quests[prerequisite.questId] = new GameData.QuestRecord { Completed = true };
            oracle.saveData.General.MaxRunDistance = 80;
            if (existingRecord) oracle.saveData.Quests[quest.questId] = new GameData.QuestRecord();
            var owner = new GameObject("Protected distance progression");
            try
            {
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                Assert.False(quests.IsQuestInProgress(quest.questId), "At exactly 80 the quest must stay hidden.");
                Assert.False(quests.GetNoticeboardEntries().Any(e => e.Quest == quest));
                tracker.IncreaseMaxRunDistance(.01f);
                Assert.True(quests.IsQuestInProgress(quest.questId), "A live maximum-distance increase must reveal the quest immediately.");
                Assert.True(quests.GetNoticeboardEntries().Any(e => e.Quest == quest));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator GuidanceQuestActionsAndContentDragging()
        {
            var barkley = Resources.Load<QuestData>("Quests/Barkley/Another Magicool Gift");
            var crops = Resources.Load<QuestData>("Quests/SkillExperience/For The Jam");
            oracle.saveData.CompletedNpcTasks.Add(crops.npcId);
            foreach (var q in barkley.requiredQuests.Concat(crops.requiredQuests))
                oracle.saveData.Quests[q.questId] = new GameData.QuestRecord { Completed = true };
            var owner = new GameObject("Protected quest actions"); GameObject viewObject = null;
            try
            {
                var resources = owner.AddComponent<ResourceManager>();
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                viewObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
                Assert.True(viewObject.GetComponent<ToolkitQuestsScreen>().Show());
                yield return null; yield return null;
                var ui = viewObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(ui.Q<Button>("turn-in-" + barkley.questId).Q<Label>().text, Does.Contain("Incomplete"));
                Assert.That(ui.Q<Button>("turn-in-" + crops.questId).Q<Label>().text, Does.Contain("Incomplete"));
                CaptureFeedback("quest-incomplete-actions.png"); yield return null; yield return null;
                var scroll = ui.Q<ScrollView>("quest-scroll");
                Assert.LessOrEqual(scroll.mouseWheelScrollSize, 8);
                scroll.contentContainer.style.minHeight = scroll.contentViewport.layout.height + 600;
                yield return null; yield return null;
                var text = ui.Q("quest-" + crops.questId).Q(className: "eov-quest-texts");
                scroll.ScrollTo(text); yield return null;
                var before = scroll.scrollOffset.y;
                var start = text.worldBound.center;
                var pins = oracle.saveData.PinnedQuests.ToArray();
                var pin = ui.Q<Button>("pin-" + crops.questId);
                var buttonStart = pin.worldBound.center;
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = buttonStart })) pin.SendEvent(down);
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = buttonStart + Vector2.down * 40 }))
                {
                    // Pooled IMGUI events do not populate the hardware mouse button state.
                    typeof(PointerMoveEvent).GetProperty(nameof(PointerMoveEvent.pressedButtons)).SetValue(move, 1);
                    scroll.contentViewport.SendEvent(move);
                }
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(before).Within(1), "Pressing a button must not begin content dragging.");
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = buttonStart + Vector2.down * 40 })) pin.SendEvent(up);
                using (var hover = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = buttonStart + Vector2.down * 80 })) scroll.contentViewport.SendEvent(hover);
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(before).Within(1), "Moving after a button release must not grab the content.");
                Assert.False(scroll.contentViewport.HasPointerCapture(PointerId.mousePointerId));
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start })) text.SendEvent(down);
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = start + Vector2.down * 40 }))
                {
                    typeof(PointerMoveEvent).GetProperty(nameof(PointerMoveEvent.pressedButtons)).SetValue(move, 1);
                    text.SendEvent(move);
                }
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = start + Vector2.down * 40 })) scroll.contentViewport.SendEvent(up);
                Assert.That(scroll.scrollOffset.y - before, Is.EqualTo(40).Within(1), "Dragging content must track the pointer precisely.");
                CollectionAssert.AreEqual(pins, oracle.saveData.PinnedQuests, "Dragging a quest must not pin it.");
                FieldsClick(ui.Q<Button>("pin-" + crops.questId));
                Assert.AreNotEqual(pins.Contains(crops.questId), oracle.saveData.PinnedQuests.Contains(crops.questId), "Ordinary clicks must still work after dragging.");
                oracle.saveData.CompletedNpcTasks.Add("Barkley1"); quests.OnNpcMet("Barkley1");
                resources.Add(crops.requirements.First().resource, crops.requirements.First().amount - 1, trackStats: false, eligibleForTierRoll: false);
                yield return null; yield return null;
                Assert.That(ui.Q<Button>("turn-in-" + crops.questId).Q<Label>().text, Does.Contain("Incomplete"));
                resources.Add(crops.requirements.First().resource, 1, trackStats: false, eligibleForTierRoll: false);
                yield return null; yield return null;
                Assert.That(ui.Q<Button>("turn-in-" + barkley.questId).Q<Label>().text, Does.Contain("Done"));
                Assert.That(ui.Q<Button>("turn-in-" + crops.questId).Q<Label>().text, Does.Contain("Turn In"));
                CaptureFeedback("quest-actions-and-drag.png"); yield return null; yield return null;
            }
            finally { if (viewObject) Object.DestroyImmediate(viewObject); Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator GuidanceAdventureThenFarmlandsAndForgeClickPrompts()
        {
            var owner = new GameObject("Protected guidance windows"); GameObject navigation = null, forge = null;
            try
            {
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                owner.AddComponent<ResourceManager>();
                owner.AddComponent<TimelessEchoes.Gear.CraftingService>();
                owner.AddComponent<TimelessEchoes.Gear.EquipmentController>();
                forge = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Forge.prefab"));
                typeof(TownWindowManager).GetField("toolkitForge", Private).SetValue(windows, forge.GetComponent<ToolkitForgeScreen>());
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var nav = navigation.GetComponent<ToolkitNavigationScreen>();
                var ui = navigation.GetComponent<UIDocument>().rootVisualElement;
                oracle.saveData.Quests["You get this one for free!"] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.CompletedNpcTasks.Add(Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Eva the Witch").npcId);
                yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-adventure").style.display.value);
                CaptureFeedback("adventure-click-prompt.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("adventure")); yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-map-0").style.display.value);
                Assert.AreEqual(DisplayStyle.None, ui.Q("tutorial-pointer-adventure").style.display.value);
                yield return null; yield return null; CaptureFeedback("farmlands-click-prompt.png"); yield return null; yield return null;
                Assert.That(ui.Q("tutorial-pointer-map-0").worldBound.center.x, Is.EqualTo(ui.Q<Button>("map-0").worldBound.center.x).Within(2));
                Assert.That(ui.Q<Button>("map-0").worldBound.xMin, Is.EqualTo(ui.Q<Button>("adventure").worldBound.xMin).Within(1));
                nav.CloseMenu();
                oracle.saveData.CompletedNpcTasks.Add("Reaper1");
                oracle.saveData.Quests["The names Gill"] = new GameData.QuestRecord { Completed = true };
                nav.Refresh(); FieldsClick(ui.Q<Button>("adventure")); yield return new WaitForSecondsRealtime(.1f); nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-map-3").style.display.value);
                nav.CloseMenu(); oracle.saveData.CompletedNpcTasks.Add("Barkley1");
                oracle.saveData.Quests["Another Magicool Gift"] = new GameData.QuestRecord { Completed = true };
                nav.Refresh(); FieldsClick(ui.Q<Button>("adventure")); yield return new WaitForSecondsRealtime(.1f); nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-map-1").style.display.value);
                nav.CloseMenu();
                var ivan = Resources.Load<QuestData>("Quests/Vast One/Meeting Ivan");
                oracle.saveData.Quests[ivan.questId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.Quests[TimelessEchoes.Gear.ForgeFirstLesson.QuestId] = new GameData.QuestRecord { Completed = true };
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-townsfolk").style.display.value);
                yield return null; yield return null;
                FieldsClick(ui.Q<Button>("townsfolk")); yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-forge").style.display.value);
                yield return null; yield return null; CaptureFeedback("forge-click-prompt.png"); yield return null; yield return null;
                Assert.That(ui.Q("tutorial-pointer-forge").worldBound.center.x, Is.EqualTo(ui.Q<Button>("forge").worldBound.center.x).Within(2));
                Assert.That(ui.Q<Button>("forge").worldBound.xMin, Is.EqualTo(ui.Q<Button>("townsfolk").worldBound.xMin).Within(1));
                FieldsClick(ui.Q<Button>("forge")); yield return null; nav.Refresh();
                Assert.True(forge.GetComponent<ToolkitForgeScreen>().IsOpen);
                Assert.True(oracle.saveData.General.ForgeIntroductionSeen);
                Assert.AreEqual(DisplayStyle.None, ui.Q("tutorial-pointer-forge").style.display.value);
                windows.CloseAllWindows(); nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-townsfolk").style.display.value, "Closing an unfinished lesson should retain the forge guide.");
            }
            finally
            {
                if (navigation) Object.DestroyImmediate(navigation);
                if (forge) Object.DestroyImmediate(forge);
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator GuidanceFirstSkillLessonWaitsForGillsRewardAndInventory()
        {
            var owner = new GameObject("Protected first skill level-up");
            var skillOwner = new GameObject("Protected skill controller"); skillOwner.SetActive(false);
            GameObject navigation = null, speakerObject = null, skillsView = null;
            try
            {
                var skillsDefinition = AssetDatabase.LoadAssetAtPath<ToolkitSkillsDefinition>("Assets/UI/Toolkit/Skills.asset");
                var farming = skillsDefinition.skills.Single(s => s.name == "Farming");
                var controller = skillOwner.AddComponent<TimelessEchoes.Skills.SkillController>();
                typeof(TimelessEchoes.Skills.SkillController).GetField("skills", Private).SetValue(controller, new List<TimelessEchoes.Skills.Skill>(skillsDefinition.skills));
                skillOwner.SetActive(true);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab").GetComponent<ToolkitOrinIntroduction>();
                var firstQuest = (QuestData)typeof(ToolkitOrinIntroduction).GetField("firstQuest", Private).GetValue(source);
                oracle.saveData.Quests[firstQuest.questId] = new GameData.QuestRecord { Completed = true };
                var eva = Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Eva the Witch");
                oracle.saveData.CompletedNpcTasks.Add(eva.npcId);
                var hub = Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Orin Hub");
                Assert.NotNull(hub);
                owner.AddComponent<QuestManager>().enabled = false;
                owner.AddComponent<ResourceManager>();
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, false);
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                skillsView = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Skills.prefab"));
                typeof(TownWindowManager).GetField("toolkitSkills", Private).SetValue(windows, skillsView.GetComponent<ToolkitSkillsScreen>());
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var nav = navigation.GetComponent<ToolkitNavigationScreen>();
                var ui = navigation.GetComponent<UIDocument>().rootVisualElement;
                void StartSpeaker()
                {
                    speakerObject = new GameObject("Protected Orin hub introduction");
                    var speaker = speakerObject.AddComponent<ToolkitOrinIntroduction>();
                    foreach (var field in typeof(ToolkitOrinIntroduction).GetFields(Private)) field.SetValue(speaker, field.GetValue(source));
                }
                StartSpeaker(); yield return null; yield return null;
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, ui.Q("hub-row").style.display.value);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>());
                controller.AddExperience(farming, farming.xpForFirstLevel * .5f); yield return null; nav.Refresh();
                Assert.AreEqual(1, controller.GetLevel(farming));
                Assert.AreEqual(DisplayStyle.None, ui.Q("hub-row").style.display.value, "XP below the first level-up must keep Hub hidden.");
                controller.AddExperience(farming, farming.xpForFirstLevel); yield return null; yield return null; nav.Refresh();
                Assert.Greater(controller.GetLevel(farming), 1);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("hub-row").style.display.value);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The Hub tour must wait until returning to town.");
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, true);
                yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Claim Gill's reward before introducing Skills.");
                oracle.saveData.Quests["The names Gill"] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.General.ReturnedFromFarmlands = true;
                oracle.saveData.General.InventoryTutorialStage = 3;
                windows.CloseAllWindows();
                for (var i = 0; i < 60 && !Object.FindAnyObjectByType<ToolkitMeetingScreen>(); i++) yield return null;
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>(); Assert.NotNull(meeting);
                var meetingUi = meeting.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(meetingUi.Q<Label>("dialogue").text, Does.Contain("Skills").And.Not.Contain("Inventory"));
                CaptureFeedback("first-skill-hub-introduction.png"); yield return null; yield return null;
                FieldsClick(meetingUi.Q<Button>("advance")); yield return null; yield return null;
                Assert.True(hub.HasBeenCompleted);
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-hub").style.display.value);
                FieldsClick(ui.Q<Button>("hub")); yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("inventory-row").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("skills-row").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-skills").style.display.value);
                FieldsClick(ui.Q<Button>("skills")); yield return new WaitForSecondsRealtime(.1f); nav.Refresh();
                Assert.True(skillsView.GetComponent<ToolkitSkillsScreen>().IsOpen);
                var skillUi = skillsView.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(skillUi.Q<Label>("skill-level").text, Does.Contain("Farming"));
                Assert.NotNull(skillUi.Q("skills-tutorial-hint"));
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-close").style.display.value);
                FieldsClick(ui.Q<Button>("close")); yield return new WaitForSecondsRealtime(.1f); nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, ui.Q("tutorial-pointer-inventory").style.display.value);
                Assert.AreEqual(3, oracle.saveData.General.InventoryTutorialStage);
                Assert.AreEqual(3, oracle.saveData.General.HubTutorialStage);
                Assert.AreEqual(DisplayStyle.None, ui.Q("tutorial-pointer-skills").style.display.value);
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData();
                nav.Refresh(); Assert.AreEqual(DisplayStyle.Flex, ui.Q("hub-row").style.display.value);
                Object.DestroyImmediate(speakerObject); StartSpeaker(); yield return null; yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Reloading must not repeat Orin's Hub explanation.");
            }
            finally
            {
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (speakerObject) Object.DestroyImmediate(speakerObject);
                if (navigation) Object.DestroyImmediate(navigation);
                if (skillsView) Object.DestroyImmediate(skillsView);
                Object.DestroyImmediate(skillOwner); Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator GuidanceGillsBootsIntroduceInventoryIndependentlyOfSkills()
        {
            var owner = new GameObject("Protected return inventory lesson");
            GameObject navigation = null, inventoryView = null, speakerObject = null, summaryView = null;
            var previousConfig = TimelessEchoes.GameManager.CurrentGenerationConfig;
            try
            {
                var resources = owner.AddComponent<ResourceManager>();
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, false);
                var farmlands = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset");
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentGenerationConfig").SetValue(null, farmlands);
                tracker.BeginRun(farmlands, TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                var first = Resources.Load<QuestData>("Quests/Vast One/You get this one for free!");
                oracle.saveData.Quests[first.questId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.CompletedNpcTasks.Add(Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Eva the Witch").npcId);
                var gear = owner.AddComponent<TimelessEchoes.Gear.EquipmentController>();
                var boots = Resources.Load<QuestData>("Quests/Gill/The names Gill").rewards.Single(r => r.type == QuestData.RewardType.Equipment).equipment.CreateItem();
                Assert.True(gear.Equip(boots));
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                inventoryView = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/ResourceInventory.prefab"));
                inventoryView.GetComponent<ToolkitResourceInventoryScreen>().Bounds = new Rect(64, 44, 640, 376);
                typeof(TownWindowManager).GetField("toolkitResources", Private).SetValue(windows, inventoryView.GetComponent<ToolkitResourceInventoryScreen>());
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var nav = navigation.GetComponent<ToolkitNavigationScreen>();
                var ui = navigation.GetComponent<UIDocument>().rootVisualElement;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab").GetComponent<ToolkitOrinIntroduction>();
                speakerObject = new GameObject("Protected inventory introduction");
                var speaker = speakerObject.AddComponent<ToolkitOrinIntroduction>();
                foreach (var field in typeof(ToolkitOrinIntroduction).GetFields(Private)) field.SetValue(speaker, field.GetValue(source));
                yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, ui.Q("inventory-row").style.display.value);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>());
                gm.RequestRetreat();
                yield return new WaitForSecondsRealtime(.15f); nav.Refresh();
                Assert.True(gm.IsInTown);
                Assert.True(oracle.saveData.General.ReturnedFromFarmlands);
                Assert.AreEqual(DisplayStyle.None, ui.Q("inventory-row").style.display.value);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>());
                summaryView = CreateGuidanceRunView();
                var run = summaryView.GetComponent<ToolkitRunScreen>();
                run.ShowSummary(new RunSummaryData { Map = "Farmlands" }, new Dictionary<Resource, double>(), new Dictionary<Resource, double>());
                oracle.saveData.Quests["The names Gill"] = new GameData.QuestRecord { Completed = true };
                yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The summary must close before the Inventory introduction.");
                var summaryUi = summaryView.GetComponent<UIDocument>().rootVisualElement;
                FieldsClick(summaryUi.Q<Button>("run-details-close"));
                for (var i = 0; i < 60 && !Object.FindAnyObjectByType<ToolkitMeetingScreen>(); i++) yield return null;
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.None, ui.Q("skills-row").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("hub-row").style.display.value);
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>(); Assert.NotNull(meeting);
                Assert.That(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Label>("dialogue").text, Does.Contain("Gill's boots").And.Contain("Inventory"));
                FieldsClick(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance"));
                yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-hub").style.display.value);
                FieldsClick(ui.Q<Button>("hub")); yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-inventory").style.display.value);
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData(); nav.Refresh();
                Assert.AreEqual(1, oracle.saveData.General.InventoryTutorialStage);
                FieldsClick(ui.Q<Button>("inventory")); yield return new WaitForSecondsRealtime(.1f); nav.Refresh();
                Assert.True(inventoryView.GetComponent<ToolkitResourceInventoryScreen>().IsOpen);
                var inventoryUi = inventoryView.GetComponent<UIDocument>().rootVisualElement;
                var equipmentUi = inventoryUi.Q("inventory-equipment"); Assert.NotNull(equipmentUi);
                Assert.AreEqual(4, equipmentUi.Query<Button>().ToList().Count);
                var gearArt = AssetDatabase.LoadAssetAtPath<ToolkitForgeDefinition>("Assets/UI/Toolkit/Forge.asset");
                Assert.AreSame(gearArt.GearSprite(boots, "Boots"), equipmentUi.Q<Button>("inventory-gear-Boots").Q<Image>().sprite);
                Assert.AreSame(gearArt.GearSprite(null, "Weapon"), equipmentUi.Q<Button>("inventory-gear-Weapon").Q<Image>().sprite);
                Assert.That(string.Join(" ", equipmentUi.Query<Label>().ToList().Select(l => l.text)), Does.Contain("25").And.Contain("10").And.Contain("5"));
                Assert.That(equipmentUi.Query<Image>().ToList().Count, Is.GreaterThan(4), "Equipment bonuses use stat icons.");
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("tutorial-pointer-close").style.display.value);
                CaptureFeedback("return-farmlands-inventory-lesson.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("close")); yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(3, oracle.saveData.General.InventoryTutorialStage);
                Assert.AreEqual(0, oracle.saveData.General.HubTutorialStage, "Inventory must not advance the independent Skills lesson.");
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("hub-row").style.display.value);
                oracle.saveData.General.BuffTutorialStage = 3; // This test owns Inventory, not the subsequent Echo lesson.
                oracle.saveData.Quests["Mildred"] = new GameData.QuestRecord { Completed = true };
                nav.Refresh(); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("hub")); yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("buffs-row").style.display.value, "Unlocked Buffs must have a reachable Hub menu.");
                Assert.AreEqual(DisplayStyle.None, ui.Q("tutorial-pointer-inventory").style.display.value);
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData(); yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>());
            }
            finally
            {
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (speakerObject) Object.DestroyImmediate(speakerObject);
                if (summaryView) Object.DestroyImmediate(summaryView);
                if (navigation) Object.DestroyImmediate(navigation);
                if (inventoryView) Object.DestroyImmediate(inventoryView);
                Object.DestroyImmediate(owner);
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentGenerationConfig").SetValue(null, previousConfig);
            }
        }

        // Owns map discovery privacy and the post-forge click lesson through the real UI.
        [UnityTest]
        public IEnumerator GuidanceForgeCompletionIntroducesAccessibleMapsWithoutSpoilingLockedOnes()
        {
            var owner = new GameObject("Protected map lesson");
            GameObject stats = null, navigation = null, speakerObject = null;
            try
            {
                owner.AddComponent<QuestManager>().enabled = false;
                owner.AddComponent<ResourceManager>();
                owner.AddComponent<GameplayStatTracker>().enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, true);
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                stats = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                var screen = stats.GetComponent<ToolkitStatisticsScreen>();
                typeof(TownWindowManager).GetField("toolkitStatistics", Private).SetValue(windows, screen);
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var nav = navigation.GetComponent<ToolkitNavigationScreen>();
                var navUi = navigation.GetComponent<UIDocument>().rootVisualElement;
                Assert.True(screen.Show()); yield return new WaitForSecondsRealtime(.1f);
                var ui = stats.GetComponent<UIDocument>().rootVisualElement;
                FieldsClick(ui.Q<Button>("journal-map-4")); yield return null;
                Assert.That(ui.Q<Label>("journal-locked-hint").text, Does.Contain("in the Woods"));
                Assert.IsNull(ui.Q(className: "journal-collection"));
                Assert.IsNull(ui.Q("journal-run-scroll"));
                Assert.IsNull(ui.Q(className: "journal-lifetime"));
                FieldsClick(ui.Q<Button>("stats-tab-4")); yield return null;
                var definition = AssetDatabase.LoadAssetAtPath<ToolkitStatisticsDefinition>("Assets/UI/Toolkit/Statistics.asset");
                foreach (var entry in definition.maps.Take(3))
                    foreach (var rule in entry.access)
                        oracle.saveData.Quests[rule.quest.questId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.Quests[TimelessEchoes.Gear.ForgeFirstLesson.QuestId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.General.ForgeFirstLessonStage = 5;
                oracle.saveData.General.HubTutorialStage = 3;
                oracle.saveData.CompletedNpcTasks.Add(Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Eva the Witch").npcId);
                oracle.saveData.CompletedNpcTasks.Add(Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/Orin Hub").npcId);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab").GetComponent<ToolkitOrinIntroduction>();
                speakerObject = new GameObject("Protected post-forge speaker");
                var speaker = speakerObject.AddComponent<ToolkitOrinIntroduction>();
                foreach (var field in typeof(ToolkitOrinIntroduction).GetFields(Private)) field.SetValue(speaker, field.GetValue(source));
                yield return null; yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(1, oracle.saveData.General.MapStatisticsTutorialStage);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Wait for the open window to close before speaking.");
                Assert.AreEqual(DisplayStyle.Flex, navUi.Q("tutorial-pointer-close").style.display.value);
                FieldsClick(navUi.Q<Button>("close")); yield return null; yield return null;
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>(); Assert.NotNull(meeting);
                Assert.AreEqual("Different maps give different resources.", System.Text.RegularExpressions.Regex.Replace(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Label>("dialogue").text, "<[^>]+>", ""));
                FieldsClick(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance"));
                yield return null; yield return null; nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, navUi.Q("tutorial-pointer-stats").style.display.value);
                FieldsClick(navUi.Q<Button>("stats")); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(3, oracle.saveData.General.MapStatisticsTutorialStage);
                Assert.That(ui.Q("map-statistics-tutorial").Q<Label>().text, Does.Contain("General"));
                FieldsClick(ui.Q<Button>("stats-tab-0")); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(4, oracle.saveData.General.MapStatisticsTutorialStage);
                FieldsClick(ui.Q<Button>("journal-map-1")); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(5, oracle.saveData.General.MapStatisticsTutorialStage);
                Assert.NotNull(ui.Q(className: "journal-collection"));
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData();
                Assert.AreEqual(5, oracle.saveData.General.MapStatisticsTutorialStage);
                FieldsClick(ui.Q<Button>("journal-map-2")); yield return new WaitForSecondsRealtime(.1f);
                var art = ui.Q<Button>("journal-map-2").Q<Image>();
                Assert.AreEqual(ScaleMode.ScaleToFit, art.scaleMode);
                Assert.LessOrEqual(art.worldBound.width, art.parent.worldBound.width + 1);
                CaptureFeedback("map-statistics-river.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("journal-map-3")); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(7, oracle.saveData.General.MapStatisticsTutorialStage);
                Assert.AreEqual(DisplayStyle.None, ui.Q("map-statistics-hand").style.display.value);
                nav.Refresh();
                Assert.AreEqual(DisplayStyle.Flex, navUi.Q("tutorial-pointer-close").style.display.value);
                Assert.That(ui.Q("map-statistics-tutorial").Q<Label>().text, Does.Contain("Close Statistics"));
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The closing message waits until Statistics is closed.");
                FieldsClick(ui.Q<Button>("journal-map-4")); yield return null;
                Assert.NotNull(ui.Q("journal-locked-hint"), "The lesson must not reveal Mines prematurely.");
                oracle.saveData.Quests[definition.maps[3].access[0].quest.questId] = new GameData.QuestRecord { Completed = true };
                yield return null; yield return null;
                Assert.IsNull(ui.Q("journal-locked-hint"));
                Assert.NotNull(ui.Q(className: "journal-collection"), "Unlocking a selected map should reveal its details immediately.");
                FieldsClick(navUi.Q<Button>("close")); yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The farewell waits for task weights and graphs, not just maps.");
                Assert.AreEqual(8, oracle.saveData.General.MapStatisticsTutorialStage);
                Assert.False(MapStatisticsGuidance.Active);
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData();
                yield return null; yield return null;
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The closing message must not repeat after loading.");
            }
            finally
            {
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (speakerObject) Object.DestroyImmediate(speakerObject);
                if (navigation) Object.DestroyImmediate(navigation);
                if (stats) Object.DestroyImmediate(stats);
                Object.DestroyImmediate(owner);
            }
        }

        // Owns the first usable boost interaction: no lesson for a one-task category,
        // then a real Statistics toggle changes weights and dismisses its guidance.
        [UnityTest]
        public IEnumerator GuidanceTaskBoostWaitsForAChoiceAndTeachesTheActualToggle()
        {
            var owner = new GameObject("Protected task boost lesson");
            var skillOwner = new GameObject("Protected boost skill owner"); skillOwner.SetActive(false);
            GameObject stats = null;
            TaskData task = null; bool oldToggle = false;
            try
            {
                var definition = AssetDatabase.LoadAssetAtPath<ToolkitSkillsDefinition>("Assets/UI/Toolkit/Skills.asset");
                var farming = definition.skills.Single(s => s.name == "Farming");
                var skills = skillOwner.AddComponent<TimelessEchoes.Skills.SkillController>();
                typeof(TimelessEchoes.Skills.SkillController).GetField("skills", Private).SetValue(skills, new List<TimelessEchoes.Skills.Skill>(definition.skills));
                skillOwner.SetActive(true);
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                var radish = Resources.LoadAll<TaskData>("Tasks").Single(t => t.name == "Radish");
                tracker.RegisterTaskComplete(radish, 1, 1);
                Assert.IsNull(TaskBoostGuidance.FindTask(), "Radish alone provides no meaningful spawn choice.");
                skills.GetProgress(farming).Level = 10;
                task = TaskBoostGuidance.FindTask(); Assert.AreSame(radish, task);
                oldToggle = TaskWeightService.IsToggleEnabled(task); TaskWeightService.SetToggle(task, false);
                oracle.saveData.General.TaskBoostTutorialStage = 1;
                stats = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                var screen = stats.GetComponent<ToolkitStatisticsScreen>(); Assert.True(screen.Show());
                yield return new WaitForSecondsRealtime(.1f);
                var ui = stats.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("task-boost-hand").style.display.value);
                FieldsClick(ui.Q<Button>("stats-tab-4")); yield return new WaitForSecondsRealtime(.1f);
                Assert.That(ui.Q<Label>("task-boost-lesson").text, Does.Contain("relative").And.Contain("milestones"));
                var weight = TaskWeightService.GetEffectiveWeight(task, tracker.MaxRunDistance);
                var toggle = ui.Q<Button>("toggle-task-stat-" + task.name);
                Assert.True(toggle.enabledInHierarchy);
                Assert.True(ui.Q<ScrollView>("task-statistics").contentViewport.worldBound.Contains(toggle.worldBound.center));
                FieldsClick(toggle); yield return new WaitForSecondsRealtime(.1f);
                Assert.True(TaskWeightService.IsToggleEnabled(task));
                Assert.Greater(TaskWeightService.GetEffectiveWeight(task, tracker.MaxRunDistance), weight);
                Assert.AreEqual(2, oracle.saveData.General.TaskBoostTutorialStage);
                Assert.AreEqual(DisplayStyle.None, ui.Q("task-boost-hand").style.display.value);
                FieldsClick(toggle); yield return null;
                Assert.False(TaskWeightService.IsToggleEnabled(task), "The player can undo the demonstrated boost.");
                CaptureFeedback("task-boost-first-lesson.png"); yield return null; yield return null;
            }
            finally
            {
                if (stats) Object.DestroyImmediate(stats);
                if (task) TaskWeightService.SetToggle(task, oldToggle);
                Object.DestroyImmediate(skillOwner); Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator GuidanceAbilityBarWaitsForAnUnlockedRecipe()
        {
            var owner = new GameObject("Protected run abilities");
            var map = new GameObject("Protected run map");
            GameObject viewObject = null;
            try
            {
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm, map);
                var buffs = owner.AddComponent<BuffManager>(); buffs.enabled = false;
                viewObject = CreateGuidanceRunView(); yield return new WaitForSecondsRealtime(.2f);
                var ui = viewObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.False(buffs.HasUnlockedAbilities);
                Assert.AreEqual(DisplayStyle.None, ui.Q("run-abilities").style.display.value);
                var recipe = buffs.Recipes.First(r => r && !r.IsRetired && r.requiredQuest);
                oracle.saveData.Quests[recipe.requiredQuest.questId] = new GameData.QuestRecord { Completed = true };
                yield return new WaitForSecondsRealtime(.2f);
                Assert.True(buffs.HasUnlockedAbilities);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("run-abilities").style.display.value);
            }
            finally { Object.DestroyImmediate(viewObject); Object.DestroyImmediate(map); Object.DestroyImmediate(owner); }
        }

        // Owns the real outing-to-summary path, including retries before town,
        // explicit dismissal, and resource totals that already include bonuses.
        [UnityTest]
        public IEnumerator GuidanceRunSummarySeparatesOutingTotalsAndClosesExplicitly()
        {
            var owner = new GameObject("Protected outing summary");
            GameObject viewObject = null;
            try
            {
                var resources = owner.AddComponent<ResourceManager>();
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                viewObject = CreateGuidanceRunView();
                var screen = viewObject.GetComponent<ToolkitRunScreen>();
                var summary = owner.AddComponent<RunResourceTrackerUI>();
                typeof(RunResourceTrackerUI).GetField("nativeView", Private).SetValue(summary, screen);
                var map = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset");
                var radish = Resources.Load<Resource>("Resource Items/Radish");
                summary.BeginRun();
                tracker.BeginRun(map, TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
                tracker.RecordHeroPosition(new Vector3(1, 0)); tracker.RecordHeroPosition(new Vector3(11, 0));
                resources.Add(radish, 10, eligibleForTierRoll: false);
                tracker.AddDeath(); tracker.AddTimesReaped();
                yield return null; tracker.EndRun(true, true);
                tracker.BeginRun(map, TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
                tracker.RecordHeroPosition(new Vector3(1, 0)); tracker.RecordHeroPosition(new Vector3(16, 0));
                resources.Add(radish, 2, bonus: true, eligibleForTierRoll: false);
                tracker.AddDeath(); tracker.IncreaseMaxRunDistance(1);
                yield return null; tracker.EndRun(true);
                summary.ShowWindow(); yield return null; yield return null;
                var ui = viewObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.True(screen.HasOpenDetails);
                Assert.AreEqual("2", ui.Q("summary-deaths").Q<Label>(className: "summary-fact-value").text);
                Assert.AreEqual("1", ui.Q("summary-reaps").Q<Label>(className: "summary-fact-value").text);
                Assert.AreEqual("25", ui.Q("summary-steps").Q<Label>(className: "summary-fact-value").text);
                Assert.That(ui.Q<Label>("summary-distance-value").text, Does.Contain("100").And.Contain("101").And.Not.Contain("sprite"));
                var resource = ui.Q("summary-resource-" + radish.name); Assert.NotNull(resource);
                Assert.AreEqual(ToolkitLocalization.Name(radish), resource.Q<Label>("resource-name").text);
                Assert.AreEqual("12", resource.Q<Label>("resource-total").text, "The total must include the bonus once.");
                Assert.That(resource.Q<Label>(className: "summary-resource-bonus").text, Does.Contain("+2"));
                Assert.That(ui.Q<Label>("summary-guide-text").text, Does.Contain("keep the resources"));
                Assert.AreEqual("summary-resources-title", ui.Q("summary-guide-arrow").userData);
                FieldsClick(ui.Q<Button>("summary-guide-next")); yield return null;
                Assert.That(ui.Q<Label>("summary-guide-text").text, Does.Contain("Farmlands gives no increase").And.Contain("Spooky uses kills"));
                Assert.AreEqual("summary-guide-next", ui.Q("summary-guide-hand").userData);
                Assert.AreEqual("summary-distance-value", ui.Q("summary-guide-arrow").userData);
                Assert.Less(ui.Q("summary-guide-arrow").worldBound.center.x, ui.Q("summary-distance-value").worldBound.xMin);
                Assert.That(ui.Q("summary-guide-arrow").worldBound.center.y, Is.InRange(ui.Q("summary-distance-value").worldBound.yMin, ui.Q("summary-distance-value").worldBound.yMax));
                Assert.LessOrEqual(ui.Q<Label>("summary-guide-text").worldBound.xMax, ui.Q<Button>("summary-guide-next").worldBound.xMin);
                Assert.That(ui.Q<Button>("summary-guide-next").worldBound.center.y, Is.InRange(ui.Q<Label>("summary-guide-text").worldBound.yMin, ui.Q<Label>("summary-guide-text").worldBound.yMax));
                CaptureFeedback("first-summary-distance-guide.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("summary-guide-next")); yield return null;
                Assert.AreEqual("run-details-close", ui.Q("summary-guide-hand").userData);
                var overlay = ui.Q(className: "eov-run-overlay");
                Assert.AreEqual(0f, overlay.resolvedStyle.backgroundColor.a);
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = overlay.worldBound.min + Vector2.one })) overlay.SendEvent(down);
                Assert.True(screen.HasOpenDetails, "An outside tap must not dismiss the summary.");
                var sheet = ui.Q(className: "summary-sheet");
                Assert.Greater(ui.Q<Label>(className: "breakdown-title").worldBound.yMin - sheet.worldBound.yMin, 6);
                FieldsClick(ui.Q<Button>("run-details-close")); yield return null;
                Assert.False(screen.HasOpenDetails);
                Assert.True(oracle.saveData.General.RunSummaryIntroductionSeen);
                Assert.True(oracle.SaveToSlot(oracle.CurrentSlot));
                var saved = Blindsided.SaveData.SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(saved.Succeeded);
                Assert.True(saved.Data.General.RunSummaryIntroductionSeen);
                summary.BeginRun(); summary.ShowWindow(); yield return null;
                Assert.NotNull(ui.Q<Label>(className: "summary-empty"));
                Assert.AreEqual(DisplayStyle.None, ui.Q("summary-guide").style.display.value);
                FieldsClick(ui.Q<Button>("run-details-close"));
                screen.ShowBreakdown(); yield return null;
                Assert.AreEqual(DisplayStyle.None, ui.Q("run-summary-overview").style.display.value);
                Assert.IsNull(ui.Q(className: "summary-sheet"));
                FieldsClick(ui.Q<Button>("run-details-close")); Assert.False(screen.HasOpenDetails);
            }
            finally
            {
                if (viewObject) Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(owner);
            }
        }

        // Real run endings, native navigation and save/load own this lesson's timing.
        [UnityTest]
        public IEnumerator GuidanceGraphsLessonStartsAfterFiveAdventuresAndSurvivesReload() => ExerciseGraphsLesson(false);

        [UnityTest]
        public IEnumerator GuidanceGraphsEarlyVisitSuppressesTheLessonAfterReload() => ExerciseGraphsLesson(true);

        private IEnumerator ExerciseGraphsLesson(bool earlyVisit)
        {
            var owner = new GameObject("Protected graph introduction");
            GameObject stats = null, navigation = null, speakerObject = null, runView = null;
            try
            {
                owner.AddComponent<ResourceManager>();
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                owner.AddComponent<QuestManager>().enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                void Town(bool value) => typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, value);
                Town(false);
                var general = oracle.saveData.General;
                general.HubTutorialStage = 3; general.InventoryTutorialStage = 3;
                general.BuffTutorialStage = 3;
                general.ReturnedFromFarmlands = true; general.MapStatisticsTutorialStage = 9; general.TaskBoostTutorialStage = 2;
                if (!earlyVisit) { general.MapStatisticsTutorialStage = 7; general.TaskBoostTutorialStage = 0; }
                var first = Resources.Load<QuestData>("Quests/Vast One/You get this one for free!");
                oracle.saveData.Quests[first.questId] = new GameData.QuestRecord { Completed = true };
                foreach (var intro in new[] { "Eva the Witch", "Orin Hub" })
                    oracle.saveData.CompletedNpcTasks.Add(Resources.Load<ToolkitNpcIntroduction>("NPC Introductions/" + intro).npcId);
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                stats = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                var screen = stats.GetComponent<ToolkitStatisticsScreen>();
                typeof(TownWindowManager).GetField("toolkitStatistics", Private).SetValue(windows, screen);
                navigation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var nav = navigation.GetComponent<ToolkitNavigationScreen>();
                var navUi = navigation.GetComponent<UIDocument>().rootVisualElement;
                runView = CreateGuidanceRunView();
                var run = runView.GetComponent<ToolkitRunScreen>();
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab").GetComponent<ToolkitOrinIntroduction>();
                speakerObject = new GameObject("Protected graph speaker");
                var speaker = speakerObject.AddComponent<ToolkitOrinIntroduction>();
                foreach (var field in typeof(ToolkitOrinIntroduction).GetFields(Private)) field.SetValue(speaker, field.GetValue(source));
                var map = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset");
                void Adventure(bool retreat = false)
                {
                    tracker.BeginRun(map, TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
                    tracker.RecordHeroPosition(new Vector3(1, 0)); tracker.RecordHeroPosition(new Vector3(11, 0));
                    if (retreat) tracker.AbandonRun(); else tracker.EndRun(true);
                }
                if (earlyVisit)
                {
                    Assert.True(screen.Show()); yield return new WaitForSecondsRealtime(.1f);
                    FieldsClick(stats.GetComponent<UIDocument>().rootVisualElement.Q<Button>("stats-tab-2"));
                    yield return null;
                    Assert.True(general.GraphsViewed);
                    windows.CloseAllWindows();
                    Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData();
                    Assert.True(oracle.saveData.General.GraphsViewed);
                }
                for (var i = 0; i < 4; i++) Adventure(i == 3);
                Town(true); yield return new WaitForSecondsRealtime(.1f);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Four adventures must not introduce graphs.");
                if (!earlyVisit)
                    Assert.AreEqual(8, general.MapStatisticsTutorialStage, "The maps tour ends without a premature farewell or blocking the later lessons.");
                Town(false);
                tracker.BeginRun(map, TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
                tracker.RecordHeroPosition(new Vector3(1, 0)); tracker.RecordHeroPosition(new Vector3(11, 0));
                Assert.AreEqual(4, tracker.CompletedAdventures, "Starting an adventure is not completing it.");
                tracker.EndRun(true);
                Assert.AreEqual(5, tracker.CompletedAdventures);
                run.ShowSummary(new RunSummaryData(), new Dictionary<Resource, double>(), new Dictionary<Resource, double>());
                Town(true); yield return new WaitForSecondsRealtime(.1f);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Wait until the run summary closes.");
                FieldsClick(runView.GetComponent<UIDocument>().rootVisualElement.Q<Button>("run-details-close"));
                yield return new WaitForSecondsRealtime(.1f);
                if (!earlyVisit)
                {
                    var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>(); Assert.NotNull(meeting);
                    Assert.That(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Label>("dialogue").text, Does.Contain("statistics panel"));
                    FieldsClick(meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance"));
                    yield return null; yield return null; nav.Refresh();
                    Assert.AreEqual(DisplayStyle.Flex, navUi.Q("tutorial-pointer-stats").style.display.value);
                    Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData();
                    yield return null; yield return null; nav.Refresh();
                    Assert.AreEqual(5, tracker.CompletedAdventures);
                    Assert.AreEqual(1, oracle.saveData.General.GraphsTutorialStage);
                    Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Reload resumes the hand prompt without repeating dialogue.");
                    FieldsClick(navUi.Q<Button>("stats")); yield return new WaitForSecondsRealtime(.1f);
                    var ui = stats.GetComponent<UIDocument>().rootVisualElement;
                    Assert.AreEqual(5, ui.Query<Button>(className: "eov-stat-tab").ToList().Count);
                    Assert.IsNull(ui.Q("stats-tab-1"), "The ranking tab is removed.");
                    Assert.AreEqual(DisplayStyle.Flex, ui.Q("graphs-tutorial-hand").style.display.value);
                    FieldsClick(ui.Q<Button>("stats-tab-2")); yield return new WaitForSecondsRealtime(.1f);
                    Assert.AreEqual(DisplayStyle.None, ui.Q("graphs-tutorial-hand").style.display.value);
                    Assert.NotNull(ui.Q(className: "graph-chart"));
                }
                Assert.True(oracle.saveData.General.GraphsViewed);
                Assert.AreEqual(2, oracle.saveData.General.GraphsTutorialStage);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>());
                windows.CloseAllWindows();
                if (!earlyVisit)
                {
                    yield return new WaitForSecondsRealtime(.1f);
                    Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "The farewell still waits for task weights after graphs are viewed.");
                    oracle.saveData.General.TaskBoostTutorialStage = 2;
                    yield return new WaitForSecondsRealtime(.1f);
                    var farewell = Object.FindAnyObjectByType<ToolkitMeetingScreen>(); Assert.NotNull(farewell);
                    Assert.That(farewell.GetComponent<UIDocument>().rootVisualElement.Q<Label>("dialogue").text, Does.Contain("explore on your own"));
                    FieldsClick(farewell.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance"));
                    yield return null; yield return null;
                    Assert.AreEqual(9, oracle.saveData.General.MapStatisticsTutorialStage);
                }
                Blindsided.EventHandler.SaveData(); Blindsided.EventHandler.LoadData();
                Town(false); Adventure(); Town(true);
                yield return new WaitForSecondsRealtime(.1f); nav.Refresh();
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Viewing Graphs permanently suppresses the lesson.");
                Assert.AreEqual(6, tracker.CompletedAdventures);
            }
            finally
            {
                if (speakerObject) Object.DestroyImmediate(speakerObject);
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                if (navigation) Object.DestroyImmediate(navigation);
                if (stats) Object.DestroyImmediate(stats);
                if (runView) Object.DestroyImmediate(runView);
                Object.DestroyImmediate(owner);
            }
        }

        private static GameObject CreateGuidanceRunView()
        {
            var viewObject = new GameObject("Protected run HUD", typeof(UIDocument)); viewObject.SetActive(false);
            var screen = viewObject.AddComponent<ToolkitRunScreen>();
            void Set(string field, Object value) => typeof(ToolkitRunScreen).GetField(field, Private).SetValue(screen, value);
            Set("definition", AssetDatabase.LoadAssetAtPath<ToolkitRunDefinition>("Assets/UI/Toolkit/Run.asset"));
            Set("theme", AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"));
            Set("runtimeTheme", AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"));
            Set("textSettings", AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
            viewObject.SetActive(true); return viewObject;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GuidanceRiverWoodcuttingWaitsForBarkley(bool asynchronous)
        {
            var river = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/River.asset");
            var root = new GameObject("Protected river tasks", typeof(Grid));
            var land = Object.Instantiate(river.tilemapChunkSettings.topTerrain);
            var task = Object.Instantiate(river.taskGeneratorSettings.woodcutting.tasks.First());
            var random = Random.state;
            try
            {
                land.taskSettings = new TerrainSettings.TaskSettings { taskDensity = .5f };
                task.requiredSkillLevel = 0; task.spawnTerrains.Clear(); task.terrainWeights.Clear();
                var terrainObject = new GameObject("River bank", typeof(Tilemap)); terrainObject.transform.SetParent(root.transform, false);
                var terrain = terrainObject.GetComponent<Tilemap>();
                var tile = new SerializedObject(land).FindProperty("tile").objectReferenceValue as TileBase;
                for (int x = 0; x < 64; x++) for (int y = 0; y < 18; y++) terrain.SetTile(new Vector3Int(x, y), tile);
                var generator = root.AddComponent<ProceduralTaskGenerator>(); root.GetComponent<TaskController>().enabled = false;
                void Set(string field, object value) => typeof(ProceduralTaskGenerator).GetField(field, Private).SetValue(generator, value);
                Set("terrainMap", terrain); Set("topTerrain", land); Set("bottomTerrain", null); Set("middleTerrain", null);
                Set("height", 18f); Set("enemyDensity", 0f); Set("otherTaskEdgeOffset", .5f);
                Set("taskCategories", new List<ProceduralTaskGenerator.WeightedTaskCategory> { new() {
                    requiredNpcId = river.taskGeneratorSettings.woodcutting.requiredNpcId, tasks = new List<TaskData> { task }
                }});
                Set("npcTasks", new List<MapGenerationConfig.ProceduralTaskSettings.NpcSpawnEntry>());
                void Generate() { if (asynchronous) { var work = generator.GenerateSegmentAsync(0, 64, root.transform); while (work.MoveNext()) { } } else generator.GenerateSegment(0, 64, root.transform); }
                Generate(); Assert.AreEqual(0, root.GetComponentsInChildren<WoodcuttingTask>().Length);
                oracle.saveData.CompletedNpcTasks.Add("Barkley1");
                Generate(); Assert.Greater(root.GetComponentsInChildren<WoodcuttingTask>().Length, 0);
            }
            finally { Random.state = random; Object.DestroyImmediate(root); Object.DestroyImmediate(land); Object.DestroyImmediate(task); }
        }
    }
}
#endif
