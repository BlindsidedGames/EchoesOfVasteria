#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
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
            var previousTimeScale = Time.timeScale;
            var owner = new GameObject("Shared propagation services");
            var heroObject = new GameObject("Propagation hero");
            var echoObject = new GameObject("Propagation echo");
            GameObject taskObject = null;
            try
            {
                Time.timeScale = 1;
                var game = owner.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, false);
                var inventory = owner.AddComponent<ResourceManager>(); inventory.enabled = false;
                var hero = heroObject.AddComponent<HeroController>(); hero.enabled = false;
                var echo = echoObject.AddComponent<EchoController>(); echo.enabled = false;
                oracle.saveData.Quests[FarmContent.IntroductionId] = new GameData.QuestRecord { Completed = true };
                var recipe = content.Recipe(FarmCommands.RadishRecipeId);
                yield return null;
                foreach (HeroBase actor in new HeroBase[] { hero, echo })
                {
                    Random.InitState(104729);
                    var initial = inventory.GetAmount(recipe.paidInput);
                    for (var attempt = 0; attempt < 128; attempt++)
                    {
                        taskObject = Object.Instantiate(recipe.source.taskPrefab).gameObject;
                        var task = taskObject.GetComponent<FarmingTask>(); task.enabled = false;
                        Assert.True(task.Claim(actor)); task.StartTask();
                        var before = inventory.GetAmount(recipe.paidInput);
                        for (var tick = 0; !task.IsComplete() && tick < 10000; tick++) task.Tick(actor);
                        Assert.True(task.IsComplete());
                        var credited = inventory.GetAmount(recipe.paidInput);
                        Assert.That(credited - before, Is.InRange(0, 1));
                        task.Tick(actor);
                        Assert.AreEqual(credited, inventory.GetAmount(recipe.paidInput), "A completed task cannot credit twice.");
                        Object.DestroyImmediate(taskObject); taskObject = null;
                    }
                    Assert.That(inventory.GetAmount(recipe.paidInput) - initial, Is.InRange(1, 30), actor.GetType().Name);
                }
            }
            finally
            {
                Random.state = random;
                Time.timeScale = previousTimeScale;
                if (taskObject) Object.DestroyImmediate(taskObject);
                Object.DestroyImmediate(echoObject); Object.DestroyImmediate(heroObject); Object.DestroyImmediate(owner);
            }
        }
    }
}
#endif
