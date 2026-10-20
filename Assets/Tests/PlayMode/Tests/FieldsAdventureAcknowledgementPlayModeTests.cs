#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Hero;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        // The noticeboard is available on adventures, but acknowledgements and
        // the introduction waits until the player returns to town.
        [UnityTest]
        public IEnumerator AdventureIntroductionWaitsForTownThenDurablyUnlocksCropCompletion()
        {
            var random = Random.state;
            var gameObject = new GameObject("Isolated adventure location");
            var resourceObject = new GameObject("Isolated adventure resource owner");
            var heroObject = new GameObject("Isolated normal hero");
            var questObject = new GameObject("Isolated actual quest owner");
            GameObject screenObject = null, cropObject = null;
            try
            {
                var game = gameObject.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, false);
                var resources = resourceObject.AddComponent<ResourceManager>(); resources.enabled = false;
                var hero = heroObject.AddComponent<HeroController>(); hero.enabled = false;
                var recipe = content.Recipe(FarmCommands.RadishRecipeId);
                Assert.AreEqual(.1f, content.seedChance, "Exercise the shipped roll, not a guaranteed test chance.");
                Random.State winning = random;
                for (var seed = 0; ; seed++)
                {
                    Random.InitState(seed); winning = Random.state;
                    if (Random.value < content.seedChance) break;
                }
                cropObject = Object.Instantiate(recipe.source.taskPrefab).gameObject;
                var crop = cropObject.GetComponent<FarmingTask>();
                Assert.NotNull(crop); crop.enabled = false;
                Assert.AreSame(recipe.source, crop.taskData, "Use the authored adventure crop definition.");
                Assert.True(crop.Claim(hero));
                crop.StartTask(); Random.state = winning;
                for (var tick = 0; !crop.IsComplete() && tick < 10000; tick++) crop.Tick(hero);
                Assert.True(crop.IsComplete());
                Assert.AreEqual(0, resources.GetAmount(recipe.paidInput), "Meeting/acknowledgement gate blocks pre-introduction rolls.");
                oracle.saveData.CompletedNpcTasks.Add("Farmers1");
                var quests = questObject.AddComponent<QuestManager>(); quests.enabled = false;
                screenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
                var screen = screenObject.GetComponent<ToolkitQuestsScreen>();
                Assert.True(screen.Show());
                yield return null;
                var okay = screenObject.GetComponent<UIDocument>().rootVisualElement.Q<Button>("turn-in-" + FarmContent.IntroductionId);
                Assert.NotNull(okay); Assert.False(okay.enabledInHierarchy);
                using (var submit = NavigationSubmitEvent.GetPooled()) okay.SendEvent(submit);
                Assert.False(FarmContent.Completed(oracle.saveData, FarmContent.IntroductionId));
                Assert.False(quests.TryTurnInQuest(FarmContent.IntroductionId));
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, true);
                yield return new WaitForSecondsRealtime(.3f);
                okay = screenObject.GetComponent<UIDocument>().rootVisualElement.Q<Button>("turn-in-" + FarmContent.IntroductionId);
                Assert.True(okay.enabledInHierarchy);
                okay.Focus();
                // Existing transaction-owner barrier: rejected acknowledgement must
                // keep the quest available so the same UI can retry after contention.
                SetOracle("_economicTransactionActive", true);
                using (var submit = NavigationSubmitEvent.GetPooled()) okay.SendEvent(submit);
                Assert.False(FarmContent.Completed(oracle.saveData, FarmContent.IntroductionId));
                Assert.NotNull(service.LastError);
                SetOracle("_economicTransactionActive", false);
                Assert.True(okay.enabledInHierarchy);
                using (var submit = NavigationSubmitEvent.GetPooled()) okay.SendEvent(submit);
                Assert.True(FarmContent.Completed(oracle.saveData, FarmContent.IntroductionId),
                    "Returning to town must allow the introduction acknowledgement.");
                Assert.False(quests.TryTurnInQuest(FarmContent.IntroductionId), "Repeated acknowledgement cannot pay or complete twice.");
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, false);
                Assert.False(service.BuildQuest("Farm.Garden.Build01.v1"), "Construction still requires its materials while on an adventure.");
                Assert.AreEqual(0, service.State.GardenCapacity);
                Assert.True((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private)
                    .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                var loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(loaded.Succeeded, loaded.Diagnostic);
                Assert.True(FarmContent.Completed(loaded.Data, FarmContent.IntroductionId));
                // A live reward since the last snapshot must survive the task credit.
                resources.Add(recipe.paidInput, 2, trackStats: false, eligibleForTierRoll: false);
                crop.StartTask(); Random.state = winning;
                for (var tick = 0; !crop.IsComplete() && tick < 10000; tick++) crop.Tick(hero);
                Assert.True(crop.IsComplete());
                Assert.AreEqual(4, resources.GetAmount(recipe.paidInput), "The new pack must preserve the two unsaved live packs.");
                crop.Tick(hero);
                Assert.AreEqual(4, resources.GetAmount(recipe.paidInput));
                loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(loaded.Succeeded, loaded.Diagnostic);
                Assert.AreEqual(1, loaded.Data.Resources.TryGetValue(recipe.paidInput.name, out var unsaved) ? unsaved.Amount : 0,
                    "Task completion must not write a checkpoint of its own.");
                Assert.True((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private)
                    .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(loaded.Succeeded, loaded.Diagnostic);
                Assert.AreEqual(4, loaded.Data.Resources[recipe.paidInput.name].Amount);
            }
            finally
            {
                SetOracle("_economicTransactionActive", false);
                Random.state = random;
                if (cropObject) Object.DestroyImmediate(cropObject);
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(questObject); Object.DestroyImmediate(heroObject);
                Object.DestroyImmediate(resourceObject); Object.DestroyImmediate(gameObject);
            }
        }
    }
}
#endif
