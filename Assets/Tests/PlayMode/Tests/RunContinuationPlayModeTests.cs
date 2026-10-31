#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.Audio;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Stats;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Events = Blindsided.EventHandler;

namespace Tests.PlayMode
{
    // Uses the existing offline fixture before Play starts, plus a disposable save root.
    [PrebuildSetup(typeof(RunCameraFrameOfflineSession))]
    [PostBuildCleanup(typeof(RunCameraFrameOfflineSession))]
    public sealed class RunContinuationPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string root, previousRoot;
        private GameObject owner;
        private Oracle oracle;
        private GameManager game;
        private GameplayStatTracker stats;
        private ResourceManager inventory;
        private BuffManager buffs;
        private MapGenerationConfig config;
        private uint width, height;
        private float timeScale;
        private bool prototypeOptionExisted;
        private int prototypeOptionValue;
        private readonly Dictionary<string, (bool existed, float number, string text)> slotMetadata = new();

        [SetUp]
        public void SetUp()
        {
            prototypeOptionExisted = PlayerPrefs.HasKey("Gameplay.BackgroundRunPrototype");
            prototypeOptionValue = PlayerPrefs.GetInt("Gameplay.BackgroundRunPrototype");
            Assert.IsNull(Oracle.oracle);
            Assert.IsTrue(TimelessEchoes.Platform.StartupDiagnostics.SkipOnlineServices);
            previousRoot = (string)typeof(SaveManager).GetField("rootPathOverride", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            root = Path.Combine(Application.temporaryCachePath, "RunContinuation-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            SaveManager.SetRootPathForTests(root);
            PlayModeWindow.GetRenderingResolution(out width, out height);
            timeScale = Time.timeScale;
            Time.timeScale = 1;
            slotMetadata.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroyRuntime();
            if (prototypeOptionExisted) PlayerPrefs.SetInt("Gameplay.BackgroundRunPrototype", prototypeOptionValue);
            else PlayerPrefs.DeleteKey("Gameplay.BackgroundRunPrototype");
            foreach (var pair in slotMetadata)
                if (!pair.Value.existed) PlayerPrefs.DeleteKey(pair.Key);
                else if (pair.Key.EndsWith("_Date", StringComparison.Ordinal)) PlayerPrefs.SetString(pair.Key, pair.Value.text);
                else PlayerPrefs.SetFloat(pair.Key, pair.Value.number);
            Time.timeScale = timeScale;
            PlayModeWindow.SetCustomRenderingResolution(width, height, "Restored viewport");
            SaveManager.SetRootPathForTests(previousRoot);
            var owned = Path.GetFullPath(root);
            var parent = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (owned.StartsWith(parent + "RunContinuation-", StringComparison.OrdinalIgnoreCase)) Directory.Delete(owned, true);
            yield return null;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);

        private void CreateRuntime(GameData data, string map)
        {
            owner = new GameObject("Isolated run continuation");
            oracle = owner.AddComponent<Oracle>();
            oracle.enabled = false; // Never run the normal scene/save bootstrap.
            for (var slot = 0; slot < 3; slot++)
                foreach (var suffix in new[] { "Completion", "Playtime", "Date" })
                {
                    var key = oracle.GetSlotPlayerPrefsKey(slot, suffix);
                    if (!slotMetadata.ContainsKey(key))
                        slotMetadata[key] = (PlayerPrefs.HasKey(key), suffix == "Date" ? 0 : PlayerPrefs.GetFloat(key), suffix == "Date" ? PlayerPrefs.GetString(key) : null);
                }
            oracle.saveData = data;
            Set(oracle, "loaded", true);
            Set(oracle, "_saveDataSlot", oracle.CurrentSlot);
            inventory = owner.AddComponent<ResourceManager>();
            stats = owner.AddComponent<GameplayStatTracker>();
            buffs = owner.AddComponent<BuffManager>();
            var loot = owner.AddComponent<RunResourceTrackerUI>();
            owner.AddComponent<AudioManager>();
            game = owner.AddComponent<GameManager>();
            game.enabled = false;
            config = Maps().Single(c => c.name == map);
            var entry = new MapGenerationButton();
            Set(entry, "config", config);
            Set(entry, "scalingMode", map == "Haloween" ? GameManager.MapScalingMode.KillBased : GameManager.MapScalingMode.DistanceBased);
            Set(game, "generationButtons", new List<MapGenerationButton> { entry });
            Set(game, "mapPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map.prefab"));
            Set(game, "echoPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Echo.prefab"));
            Set(game, "runResourceTracker", loot);
            Set(game, "warpDuration", 0f);
            Set(game, "enableStallAutoRestart", false);
        }

        private void DestroyRuntime()
        {
            if (oracle)
            {
                typeof(Oracle).GetMethod("CompleteActiveSaveBlocking", Private).Invoke(oracle, null);
                Set(oracle, "loaded", false);
                oracle.StopAllCoroutines();
            }
            if (game && game.CurrentMap)
            {
                foreach (var pooled in game.CurrentMap.GetComponentsInChildren<Blindsided.Utilities.Pooling.PooledObject>(true))
                    if (pooled) Blindsided.Utilities.Pooling.PoolManager.Release(pooled.gameObject);
                Object.DestroyImmediate(game.CurrentMap);
            }
            if (owner) Object.DestroyImmediate(owner);
            StaticReferences.ActiveNpcMeetings.Clear();
        }

        private static GameData Progress()
        {
            var data = new GameData();
            data.General.MaxRunDistance = 10000;
            data.General.BuffTutorialStage = 4;
            data.General.FirstReapTownVisited = true;
            data.Quests["Meeting Ivan"] = new GameData.QuestRecord { Completed = true };
            foreach (var map in Maps())
            {
                if (map.entryIntroduction) data.CompletedNpcTasks.Add(map.entryIntroduction.npcId);
                foreach (var npc in map.taskGeneratorSettings.npcTasks) if (npc != null) data.CompletedNpcTasks.Add(npc.id);
            }
            return data;
        }

        private static IEnumerable<MapGenerationConfig> Maps() => AssetDatabase.FindAssets("t:MapGenerationConfig")
            .Select(guid => AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(AssetDatabase.GUIDToAssetPath(guid)));

        private IEnumerator WaitForRun()
        {
            var until = Time.realtimeSinceStartup + 40;
            while (game.IsInTown || game.RunLoadingActive)
            {
                Assert.Less(Time.realtimeSinceStartup, until, "The real run loader did not finish.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CompanionSnapshotReadsRealOwnersWithoutAwardingProgress()
        {
            var data = Progress();
            data.General.RecentRuns = Enumerable.Range(1, 18).Select(i => new GameData.RunRecord
            {
                RunNumber = i, MapType = i % 2 == 0 ? "River" : "Farmlands", Duration = 100,
                Distance = i * 10, EnemiesKilled = i, Estimated = i == 18, Reaped = true
            }).ToList();
            var skill = AssetDatabase.LoadAssetAtPath<Skill>("Assets/Scriptables/Skills/Mining.asset");
            data.SkillData[skill.name] = new GameData.SkillProgress { Level = 29, CurrentXP = 12 };
            CreateRuntime(data, "Farmlands");
            var controller = owner.AddComponent<SkillController>();
            Set(controller, "skills", new List<Skill> { skill });
            Events.LoadData();
            yield return null;
            var checkpoint = data.ActiveRun;
            var town = JsonUtility.FromJson<CompanionContract>(game.CaptureWatchCompanionSnapshot());
            Assert.That(town.schemaVersion, Is.EqualTo(1));
            Assert.That(town.state, Is.EqualTo("town"));
            Assert.That(town.generatedAt, Is.GreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 5));
            Assert.That(town.skills.Single().level, Is.EqualTo(29));
            Assert.That(town.recentRuns.Select(r => r.runNumber), Is.EqualTo(Enumerable.Range(9, 10).Reverse()));
            Assert.IsTrue(town.recentRuns[0].estimated);
            Assert.AreSame(checkpoint, data.ActiveRun, "Publishing must not create a save checkpoint.");

            Assert.IsTrue(game.BeginAdventure(config));
            yield return WaitForRun();
            HeroController.Instance.SetActiveState(false);
            HeroController.Instance.transform.position = new Vector3(77, 0, 0);
            stats.RecordHeroPosition(HeroController.Instance.transform.position);
            for (var i = 0; i < 7; i++) stats.AddKill(null);
            stats.AddResources(321);
            var kills = stats.TotalKills;
            var resources = stats.TotalResourcesGathered;
            var currentCheckpoint = data.ActiveRun;
            var json = game.CaptureWatchCompanionSnapshot();
            var run = JsonUtility.FromJson<CompanionContract>(json);
            Assert.That(run.state, Is.EqualTo("run"));
            Assert.That(run.run.mapId, Is.EqualTo("Farmlands"));
            Assert.That(run.run.distance, Is.EqualTo(77).Within(.01));
            Assert.That(run.run.kills, Is.EqualTo(7));
            Assert.That(run.run.resources, Is.EqualTo(321));
            Assert.IsFalse(run.phoneInBackground);
            game.CaptureWatchCompanionSnapshot();
            Assert.That(stats.TotalKills, Is.EqualTo(kills));
            Assert.That(stats.TotalResourcesGathered, Is.EqualTo(resources));
            Assert.AreSame(currentCheckpoint, data.ActiveRun);
            Assert.That(data.SkillData[skill.name].CurrentXP, Is.EqualTo(12));
        }

        [Serializable] private class CompanionContract
        {
            public int schemaVersion;
            public long generatedAt;
            public string state;
            public bool phoneInBackground;
            public bool backgroundProgressEnabled;
            public CompanionRun run;
            public CompanionSkill[] skills;
            public CompanionHistory[] recentRuns;
        }
        [Serializable] private class CompanionRun { public string mapId; public double distance, resources; public int kills; }
        [Serializable] private class CompanionSkill { public int level; }
        [Serializable] private class CompanionHistory { public int runNumber; public bool estimated; }

        [UnityTest]
        public IEnumerator BackgroundSettlementCommitsOnceAndWarmResumeRebuildsTheWorld()
        {
            var key = "Gameplay.BackgroundRunPrototype";
            var existed = PlayerPrefs.HasKey(key); var old = PlayerPrefs.GetInt(key);
            PlayerPrefs.SetInt(key, 1);
            try
            {
                var data = Progress();
                data.BackgroundSamples["River"] = Enumerable.Range(0, 10).Select(_ => new BackgroundRunSample
                    { Seconds = 100, Distance = 50, EndDistance = 500, Kills = 10 }).ToList();
                data.ActiveRun = new RunSaveData { Map = "River", Distance = 80,
                    CheckpointUtcTicks = DateTime.UtcNow.AddSeconds(-60).Ticks,
                    Stats = new RunSaveData.Statistics { Distance = 80, Duration = 100 } };
                CreateRuntime(data, "River");
                owner.AddComponent<EnemyKillTracker>();
                Events.LoadData();
                yield return WaitForRun();
                HeroController.Instance.SetActiveState(false);
                Assert.That(HeroController.Instance.transform.position.x, Is.InRange(109f, 113f));
                Assert.That(stats.TotalKills, Is.InRange(6, 7));
                Assert.That(inventory.GetAmount(AssetCache.GetAll<Resource>().Single(r => r.name == "Slime")), Is.GreaterThan(0));
                var amount = stats.TotalResourcesGathered; var totalKills = stats.TotalKills;
                Events.ApplicationForeground(); Events.ApplicationForeground();
                Assert.That(stats.TotalResourcesGathered, Is.EqualTo(amount));
                Assert.That(stats.TotalKills, Is.EqualTo(totalKills));
                Assert.IsTrue(oracle.PrepareForQuit());
                var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult().Data;
                Assert.That(saved.ActiveRun.Stats.EstimatedSeconds, Is.GreaterThan(59));
                Assert.That(saved.ActiveRun.CheckpointUtcTicks, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-10).Ticks));
                Assert.That(saved.BackgroundSamples["River"].Count, Is.EqualTo(10));
                // Exercise the real warm lifecycle, not just cold loading.
                Events.ApplicationBackground();
                Assert.That(Time.timeScale, Is.Zero);
                Assert.IsTrue(JsonUtility.FromJson<CompanionContract>(game.CaptureWatchCompanionSnapshot()).phoneInBackground);
                var before = game.CurrentMap;
                oracle.saveData.ActiveRun.CheckpointUtcTicks = DateTime.UtcNow.AddSeconds(-20).Ticks;
                Events.ApplicationForeground();
                Assert.IsFalse(JsonUtility.FromJson<CompanionContract>(game.CaptureWatchCompanionSnapshot()).phoneInBackground);
                yield return null;
                yield return WaitForRun();
                HeroController.Instance.SetActiveState(false);
                Assert.That(game.CurrentMap, Is.Not.EqualTo(before));
                var resumedWatch = JsonUtility.FromJson<CompanionContract>(game.CaptureWatchCompanionSnapshot());
                Assert.IsFalse(resumedWatch.phoneInBackground);
                Assert.That(resumedWatch.state, Is.EqualTo("run"));
                Assert.That(HeroController.Instance.transform.position.x, Is.GreaterThan(119));
                Assert.That(stats.TotalKills, Is.GreaterThan(totalKills));
                // Opting out preserves run continuation without estimating away-time work.
                BackgroundRunPrototype.OptedIn = false;
                var foregroundMap = game.CurrentMap;
                var foregroundKills = stats.TotalKills;
                var foregroundResources = stats.TotalResourcesGathered;
                Events.ApplicationBackground();
                Assert.That(Time.timeScale, Is.GreaterThan(0));
                var optedOutWatch = JsonUtility.FromJson<CompanionContract>(game.CaptureWatchCompanionSnapshot());
                Assert.IsTrue(optedOutWatch.phoneInBackground);
                Assert.IsFalse(optedOutWatch.backgroundProgressEnabled);
                Events.ApplicationForeground();
                yield return null;
                Assert.AreSame(foregroundMap, game.CurrentMap);
                Assert.That(stats.TotalKills, Is.EqualTo(foregroundKills));
                Assert.That(stats.TotalResourcesGathered, Is.EqualTo(foregroundResources));
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(key, old); else PlayerPrefs.DeleteKey(key);
            }
        }

        [UnityTest]
        public IEnumerator RelaunchKeepsDistanceLootAndBonusWithoutDuplicatingProgress()
        {
            foreach (var portrait in new[] { false, true })
            {
                PlayModeWindow.SetCustomRenderingResolution(portrait ? 540u : 960u, portrait ? 960u : 540u, "Run continuation");
                CreateRuntime(Progress(), "Farmlands");
                yield return null; // Existing owners finish their normal Start/Load subscriptions.
                Assert.IsTrue(game.BeginAdventure(config));
                yield return WaitForRun();
                var hero = HeroController.Instance;
                hero.SetActiveState(false);
                var move = buffs.Recipes.Single(r => r.name == "MoveSpeed");
                var windfall = buffs.Recipes.Single(r => r.name == "Windfall");
                var echoTasks = buffs.Recipes.Single(r => r.name == "Echo Tasks");
                foreach (var recipe in new[] { move, windfall, echoTasks })
                    if (recipe.requiredQuest) oracle.saveData.Quests[recipe.requiredQuest.questId] = new GameData.QuestRecord { Completed = true };
                Assert.IsTrue(buffs.PurchaseBuff(move));
                buffs.Tick(move.GetDuration() - .1f);
                buffs.Tick(.2f);
                Assert.Greater(buffs.GetCooldownRemaining(move), 0);
                Assert.IsTrue(buffs.PurchaseBuff(windfall));
                Assert.IsTrue(buffs.PurchaseBuff(echoTasks));
                buffs.Pause();
                hero.transform.position = new Vector3(777, hero.transform.position.y);
                stats.RecordHeroPosition(hero.transform.position);
                for (var i = 0; i < 40; i++) stats.AddKill(null);
                var stone = AssetCache.GetAll<Resource>().Single(r => r.name == "Stone");
                buffs.ActiveBuffs.Single(b => b.recipe == windfall).gathering.AccumulateWindfall(new Dictionary<Resource, double> { [stone] = 20 });
                inventory.Add(stone, 100, eligibleForTierRoll: false, applyTierBonus: false);
                var health = hero.GetComponent<HeroHealth>();
                health.SetCurrentHealthSilently(health.MaxHealth * .6f);
                var totalKills = stats.TotalKills;
                var totalResources = stats.TotalResourcesGathered;
                var casts = stats.BuffsCast;
                var remaining = buffs.GetRemaining(echoTasks);
                Assert.IsTrue(oracle.PrepareForQuit());
                Assert.IsTrue(oracle.PrepareForQuit(), "Repeated close notifications must share the same completed save.");
                Assert.IsTrue(stats.RunInProgress, "Closing the game must preserve the run rather than abandon it.");
                var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult().Data;
                Assert.NotNull(saved.ActiveRun, "A pause checkpoint must include its active adventure.");
                DestroyRuntime();
                CreateRuntime(saved, "Farmlands");
                Events.LoadData();
                Events.SaveData(force: true); // Pausing again before generation must retain the checkpoint.
                Assert.NotNull(saved.ActiveRun);
                yield return null;
                yield return null;
                Assert.IsFalse(game.IsInTown && !game.RunLoadingActive, "Loading a saved adventure must not leave the game idle in town.");
                yield return WaitForRun();
                hero = HeroController.Instance;
                hero.SetActiveState(false);
                Assert.AreEqual(777, hero.transform.position.x, 2, "Generate and resume near the saved distance.");
                Assert.IsTrue(AstarPath.active.GetNearest(hero.transform.position, Pathfinding.NearestNodeConstraint.Walkable).node.Walkable);
                Assert.AreEqual(totalKills, stats.TotalKills);
                Assert.AreEqual(totalResources, stats.TotalResourcesGathered);
                Assert.AreEqual(casts, stats.BuffsCast, "Restoration must not cast the saved abilities again.");
                Assert.AreEqual(remaining, buffs.GetRemaining(echoTasks), .2f);
                Assert.Greater(buffs.GetCooldownRemaining(move), 0);
                Assert.AreEqual(1, buffs.ActiveBuffs.Single(b => b.recipe == windfall).gathering.completed);
                Assert.AreEqual(20, buffs.ActiveBuffs.Single(b => b.recipe == windfall).gathering.bundle[stone]);
                Assert.IsNotEmpty(game.CurrentMap.GetComponentsInChildren<EchoController>(), "Active Echoes must return with their remaining duration.");
                Assert.AreEqual(40, stats.CurrentRunKills);
                Assert.AreEqual(80, game.ResourceBonusPercent);
                Assert.AreEqual(100, inventory.GetAmount(stone));
                Assert.AreEqual(.6f, hero.GetComponent<HeroHealth>().CurrentHealth / hero.GetComponent<HeroHealth>().MaxHealth, .05f);
                game.RequestRetreat();
                for (var i = 0; i < 120 && !game.IsInTown; i++) yield return null;
                Assert.IsTrue(game.IsInTown);
                Assert.AreEqual(180, inventory.GetAmount(stone), 1e-6, "The existing return bonus is paid exactly once.");
                Assert.IsTrue(oracle.SaveToSlot(oracle.CurrentSlot));
                Assert.IsNull(oracle.saveData.ActiveRun, "An intentional town return must retire the adventure.");
                DestroyRuntime();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HollowRestoresKillScalingBeforeGeneratingEnemiesAndDeathDoesNotRevive()
        {
            PlayerPrefs.SetInt("Gameplay.BackgroundRunPrototype", 1);
            foreach (var reaped in new[] { false, true })
            {
                var data = Progress();
                data.ActiveRun = new RunSaveData
                {
                    Map = "Haloween", Distance = 777, HealthFraction = 0, Dead = true, Reaped = reaped,
                    CheckpointUtcTicks = DateTime.UtcNow.AddSeconds(-30).Ticks,
                    Stats = new RunSaveData.Statistics { Distance = 777, Kills = 75, SessionKills = 75 }
                };
                data.General.Deaths = 3;
                data.General.TimesReaped = 2;
                CreateRuntime(data, "Haloween");
                yield return null;
                Events.LoadData();
                yield return WaitForRun();
                Assert.IsTrue(game.HeroIsDead);
                Assert.IsTrue(game.DeathPromptVisible);
                Assert.AreEqual(3, stats.Deaths, "Restoring the notice must not record another death.");
                Assert.AreEqual(2, stats.TimesReaped);
                Assert.AreEqual(reaped, game.RunEndedByReaper);
                Assert.AreEqual(75, stats.CurrentRunKills);
                Assert.AreEqual(0, HeroController.Instance.GetComponent<HeroHealth>().CurrentHealth);
                var enemies = game.CurrentMap.GetComponentsInChildren<Enemy>(true);
                Assert.IsNotEmpty(enemies);
                foreach (var enemy in enemies)
                {
                    var expected = enemy.Stats.GetLevel(enemy.transform.position.x - config.taskGeneratorSettings.enemySpawnXOffset - enemy.Stats.minX) + 30;
                    Assert.AreEqual(expected, enemy.Level, "Hollow's saved kills must affect the first spawned enemies.");
                }
                game.RequestRestartAfterDeath();
                yield return WaitForRun();
                Assert.IsFalse(game.HeroIsDead);
                Assert.AreEqual(0, stats.CurrentRunKills, "An ordinary next run must reset its own difficulty.");
                Assert.Less(HeroController.Instance.transform.position.x, 5);
                Assert.AreEqual(3, stats.Deaths);
                Assert.AreEqual(reaped, stats.RecentRuns.Last().Reaped);
                DestroyRuntime();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator UnfinishedNpcCanBeMetAgainAndCompletedIvanStillWaitsForTown()
        {
            var data = Progress();
            data.CompletedNpcTasks.Remove("Ivan1");
            data.Quests.Remove("Meeting Ivan");
            CreateRuntime(data, "Woods");
            yield return null;
            Assert.IsTrue(game.BeginAdventure(config));
            yield return WaitForRun();
            var ivan = config.taskGeneratorSettings.npcTasks.Single(n => n.id == "Ivan1");
            var hero = HeroController.Instance;
            hero.SetActiveState(false);
            hero.transform.position = new Vector3(ivan.localX + 1, hero.transform.position.y);
            stats.RecordHeroPosition(hero.transform.position);
            StaticReferences.ActiveNpcMeetings.Add("Ivan1");
            Assert.IsTrue(oracle.SaveToSlot(oracle.CurrentSlot));
            var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult().Data;
            DestroyRuntime();
            CreateRuntime(saved, "Woods");
            Events.LoadData();
            yield return WaitForRun();
            hero = HeroController.Instance;
            hero.SetActiveState(false);
            var encounter = game.CurrentMap.GetComponentsInChildren<TimelessEchoes.Tasks.TalkToNpcTask>()
                .Single(task => Mathf.Abs(task.Target.position.x - ivan.localX) < 2);
            Assert.Greater(encounter.Target.position.x, hero.transform.position.x);
            Assert.IsFalse(encounter.IsComplete());

            // A completed meeting must resume its existing return wait, not replay rewards.
            saved.CompletedNpcTasks.Add("Ivan1");
            Assert.IsTrue(oracle.SaveToSlot(oracle.CurrentSlot));
            saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult().Data;
            DestroyRuntime();
            CreateRuntime(saved, "Woods");
            Events.LoadData();
            yield return WaitForRun();
            Assert.IsTrue(game.IvanReturnPending);
            Assert.IsTrue(HeroController.Instance.IsInNpcConversation, "The return prompt should hold the player in place.");
            game.RequestRetreat();
            for (var i = 0; i < 120 && !game.IsInTown; i++) yield return null;
            Assert.IsTrue(game.IsInTown, "Return To Town must remain reachable after restoring the wait.");
        }
    }
}
#endif
