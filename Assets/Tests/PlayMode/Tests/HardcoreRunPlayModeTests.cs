#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.Enemies;
using TimelessEchoes.Farming;
using TimelessEchoes.Hero;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.TestTools;
using Events = Blindsided.EventHandler;

namespace Tests.PlayMode
{
    public sealed partial class RunContinuationPlayModeTests
    {
        [UnityTest]
        public IEnumerator HardcoreEnemyDeathWhileCarlApproachesIsDurableAndCannotResume()
        {
            var data = Progress(); data.Hardcore = true; data.PlayTime = 1234;
            CreateRuntime(data, "Farmlands");
            yield return null;
            Assert.True(game.BeginAdventure(config)); yield return WaitForRun();
            var hero = HeroController.Instance; hero.SetActiveState(false);
            BackgroundRunPrototype.OptedIn = true;
            var originalMap = game.CurrentMap;
            Events.ApplicationBackground();
            Assert.AreEqual(0, Time.timeScale, "Hardcore backgrounds by pausing actual gameplay.");
            Assert.NotNull(data.ActiveRun); Assert.IsNull(data.HardcoreDeath);
            yield return new WaitForSecondsRealtime(.1f);
            Events.ApplicationForeground();
            Assert.Greater(Time.timeScale, 0); Assert.AreSame(originalMap, game.CurrentMap);
            Assert.False(BackgroundRunPrototype.Enabled);
            var health = hero.GetComponent<HeroHealth>(); health.Init(10);
            typeof(HeroBase).GetProperty("ReaperSpawnedByDistance").SetValue(hero, true);
            health.TakeDamage(1000000);
            Assert.True(data.HardcoreFallen, "Being approached by Carl must not turn a lethal enemy hit into a safe reap.");
            Assert.IsNull(data.ActiveRun); Assert.False(game.DeathPromptVisible); Assert.AreEqual(0, Time.timeScale);
            var firstDeath = data.HardcoreDeath;
            game.RequestRestartAfterDeath(); game.RequestReturnAfterDeath();
            Assert.False(game.BeginAdventure(config)); Assert.AreSame(firstDeath, data.HardcoreDeath);
            Assert.False(oracle.FinishHardcoreRun("Other", 999), "The terminal record is one-off.");
            var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
            Assert.True(saved.Succeeded, saved.Diagnostic);
            Assert.True(saved.Data.HardcoreFallen); Assert.AreEqual("Farmlands", saved.Data.HardcoreDeath.Map);
            Assert.AreEqual(1234, saved.Data.PlayTime); Assert.IsNull(saved.Data.ActiveRun);
            DestroyRuntime(); CreateRuntime(saved.Data, "Farmlands");
            var farm = owner.AddComponent<FarmService>();
            Events.LoadData(); yield return null;
            Assert.False(game.BeginAdventure(config)); Assert.False(farm.Ready);
            Assert.AreEqual(0, Time.timeScale); Assert.IsNull(oracle.saveData.ActiveRun);
            buffs.SetBaseTimeScale(2);
            Assert.AreEqual(0, Time.timeScale, "Refreshing buff-derived speed cannot unfreeze a fallen save.");
            BackgroundRunPrototype.OptedIn = true;
            Assert.False(BackgroundRunPrototype.Enabled, "A global preference must not enable estimated hardcore deaths.");
            var playTime = oracle.saveData.PlayTime;
            typeof(Blindsided.Oracle).GetMethod("AccumulatePlayTime", Private).Invoke(oracle, new object[] { 10f });
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(playTime, oracle.saveData.PlayTime);
        }

        [UnityTest]
        public IEnumerator DistanceReapsRemainSafeForBothModesAndNormalEnemyDeathsRemainRestartable()
        {
            foreach (var hardcore in new[] { false, true })
            {
                var data = Progress(); data.Hardcore = hardcore;
                CreateRuntime(data, "Farmlands"); yield return null;
                Assert.True(game.BeginAdventure(config)); yield return WaitForRun();
                var hero = HeroController.Instance; hero.SetActiveState(false);
                hero.GetComponent<HeroHealth>().Init(100);
                typeof(HeroBase).GetProperty("ReaperSpawnedByDistance").SetValue(hero, true);
                var carl = new GameObject("Actual Carl kill event").AddComponent<ReaperManager>(); carl.transform.SetParent(owner.transform);
                carl.Init(hero.gameObject); carl.KillTarget();
                Assert.False(data.HardcoreFallen); Assert.True(game.HeroIsDead);
                Assert.True(hero.GetComponent<HeroHealth>().ReapedAtDistanceLimit);
                Assert.AreEqual(1, stats.TimesReaped); Assert.AreEqual(0, stats.Deaths);
                game.RequestRestartAfterDeath(); yield return WaitForRun();
                Assert.False(game.HeroIsDead); Assert.False(data.HardcoreFallen);
                DestroyRuntime();
            }
            var normal = Progress(); CreateRuntime(normal, "Farmlands"); yield return null;
            Assert.True(game.BeginAdventure(config)); yield return WaitForRun();
            var normalHero = HeroController.Instance; normalHero.SetActiveState(false);
            normalHero.GetComponent<HeroHealth>().KillImmediately();
            Assert.False(normal.HardcoreFallen); Assert.True(game.HeroIsDead);
            game.RequestRestartAfterDeath(); yield return WaitForRun();
            Assert.False(game.HeroIsDead);
        }
    }
}
#endif
