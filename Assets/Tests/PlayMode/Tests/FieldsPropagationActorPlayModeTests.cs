#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.TestTools;
namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroAndEchoCropCompletionsShareTheSameSeedRollAndSingleCredit()
        {
            var random = Random.state;
            var owner = new GameObject("Shared propagation services");
            var heroObject = new GameObject("Propagation hero");
            var echoObject = new GameObject("Propagation echo");
            GameObject taskObject = null;
            try
            {
                var game = owner.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, false);
                var inventory = owner.AddComponent<ResourceManager>(); inventory.enabled = false;
                var hero = heroObject.AddComponent<HeroController>(); hero.enabled = false;
                var echo = echoObject.AddComponent<EchoController>(); echo.enabled = false;
                oracle.saveData.Quests[FarmContent.IntroductionId] = new GameData.QuestRecord { Completed = true };
                var recipe = content.Recipe(FarmCommands.RadishRecipeId);
                var chance = content.SeedDropChance(recipe);
                var rolls = new Random.State[2];
                for (var outcome = 0; outcome < 2; outcome++)
                    for (var seed = 0; ; seed++)
                    {
                        Random.InitState(seed); var state = Random.state; var roll = Random.value;
                        // The losing roll lies below the former flat 10% sapling chance.
                        if (outcome == 1 ? roll < chance : roll > chance && roll < .1f)
                        { rolls[outcome] = state; break; }
                    }
                yield return null;
                foreach (HeroBase actor in new HeroBase[] { hero, echo })
                    for (var outcome = 0; outcome < 2; outcome++)
                    {
                        taskObject = Object.Instantiate(recipe.source.taskPrefab).gameObject;
                        var task = taskObject.GetComponent<FarmingTask>(); task.enabled = false;
                        Assert.True(task.Claim(actor)); task.StartTask();
                        var before = inventory.GetAmount(recipe.paidInput); Random.state = rolls[outcome];
                        for (var tick = 0; !task.IsComplete() && tick < 10000; tick++) task.Tick(actor);
                        Assert.True(task.IsComplete());
                        Assert.AreEqual(before + outcome, inventory.GetAmount(recipe.paidInput), actor.GetType().Name + " must use the same seed chance.");
                        task.Tick(actor);
                        Assert.AreEqual(before + outcome, inventory.GetAmount(recipe.paidInput), "A completed task cannot credit twice.");
                        Object.DestroyImmediate(taskObject); taskObject = null;
                    }
            }
            finally
            {
                Random.state = random;
                if (taskObject) Object.DestroyImmediate(taskObject);
                Object.DestroyImmediate(echoObject); Object.DestroyImmediate(heroObject); Object.DestroyImmediate(owner);
            }
        }
    }
}
#endif
