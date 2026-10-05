#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        // Pointer clicks left selection stale while the old screen deferred rebuilding.
        // Existing command tests cannot detect selection, clipping or rendered bed state.
        [UnityTest]
        public IEnumerator FieldsPointerSelectionPlantAndReloadStayCoherent()
        {
            var gameObject = new GameObject("Isolated Fields town");
            var screenObject = new GameObject("Actual Fields screen", typeof(UIDocument));
            var resourceObject = new GameObject("Fields live resource owner");
            try
            {
                var game = gameObject.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, true);
                oracle.saveData.CompletedNpcTasks.Add("Farmers1");
                Build(FarmContent.IntroductionId);
                oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 10, Earned = true };
                oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 20, Earned = true };
                Build("Farm.Garden.Build01.v1");
                var seedName = FarmCommands.SeedResourceName(FarmCommands.RadishSeedId);
                oracle.saveData.Resources[seedName] = new GameData.ResourceEntry { Amount = 0, Earned = true };
                var resources = resourceObject.AddComponent<TimelessEchoes.Upgrades.ResourceManager>(); resources.enabled = false;
                var seedResource = Resources.Load<TimelessEchoes.Upgrades.Resource>("Resource Items/Radish Seed Pack");
                var appearance = Resources.Load<FieldsWorldAppearance>("Farming/FieldsWorldAppearance");
                var screen = screenObject.AddComponent<ToolkitFarmScreen>();
                screen.Configure(service, appearance.theme, appearance.runtimeTheme, appearance.textSettings, null, null);
                Assert.True(screen.Show());
                var ui = screenObject.GetComponent<UIDocument>().rootVisualElement;
                yield return null; yield return null;
                FieldsClick(ui.Q<Button>("fields-seed-" + FarmCommands.RadishRecipeId));
                Assert.True(ui.Q<Button>("fields-seed-" + FarmCommands.RadishRecipeId).ClassListContains("primary"),
                    "Seed selection must repaint on its own click, before touching any bed.");
                yield return null;
                var locked = ui.Q<Button>("fields-select-" + FarmCommands.EastBedId);
                Assert.NotNull(locked); Assert.False(locked.enabledInHierarchy);
                FieldsClick(locked); yield return null;
                Assert.False(service.State.Beds.ContainsKey(FarmCommands.EastBedId));
                var plant = ui.Q<Button>("fields-plant-selected");
                Assert.False(plant.enabledInHierarchy, "Discovered seeds with zero owned packs cannot be planted.");
                resources.Add(seedResource, 8, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.False(plant.enabledInHierarchy, "Eight packs cannot enable a nine-pack planting.");
                resources.Add(seedResource, 10, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.True(plant.enabledInHierarchy, "An available empty crop bed is selected automatically.");
                Assert.LessOrEqual(ui.Q<ScrollView>("fields-workspace").verticalScroller.highValue, .5f, "Empty-bed actions fit without an outer scrollbar.");
                FieldsClick(plant);
                Assert.True(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                Assert.AreEqual(9, oracle.saveData.Resources[seedName].Amount);
                Assert.That(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId).Q<Label>("bed-status").text, Does.Contain("Radish"));
                Assert.NotNull(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId).Q("bed-progress"));
                Assert.False(ui.Q<Button>("fields-plant-selected").enabledInHierarchy);
                yield return null;
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.AreEqual(9, oracle.saveData.Resources[seedName].Amount, "A second click cannot charge a growing bed.");
                var loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(loaded.Succeeded, loaded.Diagnostic);
                Assert.AreEqual(9, loaded.Data.Resources[seedName].Amount);
                Assert.True(loaded.Data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
                FieldsClick(ui.Q<Button>("fields-water-selected"));
                Assert.True(service.State.Beds[FarmCommands.WestBedId].Watered);
                yield return null; yield return null;
                var viewport = ui.Q<ScrollView>("fields-workspace");
                Assert.LessOrEqual(viewport.verticalScroller.highValue, .5f, "The desktop Fields grid and controls fit without an unnecessary outer scrollbar.");
                var construction = ui.Q<Foldout>("fields-construction-foldout");
                Assert.LessOrEqual(construction.worldBound.yMax, viewport.contentViewport.worldBound.yMax + 1, "Construction remains visible below the actions.");
                var evidence = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fields-grid-review");
                System.IO.Directory.CreateDirectory(evidence);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-growing-watered.png"));
                yield return null; yield return null;
                FarmCommands.TickFields(service.State, content.baseDurationSeconds);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.True(ui.Q<Button>("farm-harvest-ready").enabledInHierarchy);
                FieldsClick(ui.Q<Button>("farm-harvest-ready"));
                Assert.False(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                yield return null;
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy);
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.AreEqual(0, oracle.saveData.Resources[seedName].Amount);
                loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(loaded.Succeeded, loaded.Diagnostic);
                Assert.AreEqual(0, loaded.Data.Resources[seedName].Amount);
                oracle.saveData = loaded.Data;
                Blindsided.EventHandler.LoadData(); // The ordinary load boundary rebinds live resource caches.
                screen.Hide(); Assert.True(screen.Show());
                yield return null; yield return null;
                Assert.That(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId).Q<Label>("bed-status").text, Does.Contain("Radish"));
                var otherRecipe = content.Recipe("recipe.wheat.v1");
                oracle.saveData.SkillData[otherRecipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = otherRecipe.source.requiredSkillLevel };
                resources.Add(otherRecipe.paidInput, 9, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                FieldsClick(ui.Q<Button>("fields-seed-" + otherRecipe.id));
                Assert.True(ui.Q<Button>("fields-seed-" + otherRecipe.id).ClassListContains("primary"));
                Assert.False(ui.Q<Button>("fields-seed-" + FarmCommands.RadishRecipeId).ClassListContains("primary"));
                Assert.False(ui.Q<Button>("fields-plant-selected").enabledInHierarchy, "Changing crops cannot plant into the occupied bed or a locked one.");
                Assert.AreEqual(9, resources.GetAmount(otherRecipe.paidInput, oracle.saveData));
                Assert.That(ui.Q<Button>("fields-seed-" + otherRecipe.id).Q<Label>("seed-caption").text, Does.Contain("Wheat"));
                var nextBuild = content.builds[1];
                oracle.saveData.Farm.TwinsLevel = 20;
                oracle.saveData.General.MaxRunDistance = 100000;
                foreach (var task in nextBuild.sources)
                    oracle.saveData.SkillData[task.associatedSkill.name] = new GameData.SkillProgress { Level = task.requiredSkillLevel };
                foreach (var cost in nextBuild.costs)
                    resources.Add(cost.resource, cost.amount, trackStats: false, eligibleForTierRoll: false);
                Build(nextBuild.questId);
                yield return new WaitForSecondsRealtime(.3f);
                FieldsClick(ui.Q<Button>("fields-seed-" + otherRecipe.id));
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy);
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.True(service.State.Beds[FarmCommands.EastBedId].IsPlanted);
                Assert.AreEqual(0, oracle.saveData.Resources[otherRecipe.paidInput.name].Amount);
                Assert.AreEqual(0, oracle.saveData.Resources[seedName].Amount);
                // Controlled orchard availability separates UI rendering/spending from
                // the construction-chain owner tests; no claim of naturally earned unlocks.
                var apple = content.Recipe("recipe.apple.v1");
                var orchardId = FarmCommands.OrchardBeds[0];
                oracle.saveData.Farm.OrchardCapacity = 1;
                oracle.saveData.Farm.Beds[orchardId] = new FarmBedState { Unlocked = true };
                oracle.saveData.SkillData[apple.source.associatedSkill.name] = new GameData.SkillProgress { Level = apple.source.requiredSkillLevel };
                resources.Add(apple.paidInput, 1, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                var appleCard = ui.Q<Button>("fields-seed-" + apple.id);
                ui.Q<ScrollView>("fields-seeds").ScrollTo(appleCard);
                yield return null; yield return null;
                FieldsClick(appleCard);
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy);
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.True(service.State.Beds[orchardId].IsPlanted);
                Assert.AreEqual(0, oracle.saveData.Resources[apple.paidInput.name].Amount, "Orchards still use one matching sapling.");
                yield return null; yield return null;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-and-orchard-growing.png"));
                yield return null; yield return null;
                FarmCommands.TickFields(service.State, content.baseDurationSeconds);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(ui.Q<Button>("fields-select-" + orchardId).Q<Label>("bed-status").text, Does.Contain("Ready"));
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-and-orchard-ready.png"));
                yield return null; yield return null;
                screen.CompanionWidth = 400;
                yield return null; yield return null;
                Assert.GreaterOrEqual(ui.Q("fields-orchard-grid").worldBound.y, ui.Q("fields-bed-grid").worldBound.yMax - 1, "Narrow layouts stack both grids without overlap.");
                Assert.Greater(ui.Q("fields-orchard-grid").worldBound.height, 100);
                screen.CompanionWidth = 0;
                yield return null; yield return null;
                FieldsClick(ui.Q<Button>("farm-harvest-ready"));
                Assert.False(service.State.Beds[orchardId].IsPlanted);
                Assert.False(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                Assert.False(service.State.Beds[FarmCommands.EastBedId].IsPlanted);
                Assert.Greater(oracle.saveData.Resources[apple.output.name].Amount, 0);
            }
            finally { Object.DestroyImmediate(screenObject); Object.DestroyImmediate(resourceObject); Object.DestroyImmediate(gameObject); }
        }
        private static void FieldsClick(Button button)
        {
            Assert.NotNull(button);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = button.worldBound.center })) button.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = button.worldBound.center })) button.SendEvent(up);
        }
    }
}
#endif
