#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
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
            var gameObject = new GameObject("Isolated Fields adventure");
            var screenObject = new GameObject("Actual Fields screen", typeof(UIDocument));
            var resourceObject = new GameObject("Fields live resource owner");
            try
            {
                var game = gameObject.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, false);
                oracle.saveData.CompletedNpcTasks.Add("Farmers1");
                Build(FarmContent.IntroductionId);
                oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 10, Earned = true };
                oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 20, Earned = true };
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
                var buildPlot = ui.Q<Button>("fields-build-selected");
                Assert.True(buildPlot.enabledInHierarchy);
                FieldsClick(buildPlot);
                Assert.AreEqual(1, service.State.GardenCapacity, "The selected plot construction action must purchase the correct plot.");
                Assert.AreEqual(0, oracle.saveData.Resources["Log"].Amount);
                Assert.AreEqual(0, oracle.saveData.Resources["Stick"].Amount);
                FieldsClick(buildPlot);
                Assert.AreEqual(0, oracle.saveData.Resources["Log"].Amount, "Selecting a built plot must not repeat its construction payment.");
                FieldsClick(ui.Q<Button>("fields-seed-" + FarmCommands.RadishRecipeId));
                Assert.True(ui.Q<Button>("fields-seed-" + FarmCommands.RadishRecipeId).ClassListContains("primary"),
                    "Seed selection must repaint on its own click, before touching any bed.");
                yield return null;
                var locked = ui.Q<Button>("fields-select-" + FarmCommands.EastBedId);
                Assert.NotNull(locked); Assert.True(locked.enabledInHierarchy, "Locked plots remain inspectable.");
                FieldsClick(locked); yield return null;
                Assert.False(service.State.Beds.ContainsKey(FarmCommands.EastBedId));
                Assert.That(ui.Q<VisualElement>("fields-plot-" + FarmCommands.EastBedId).Q<Label>(className: "fields-level-requirement").text, Does.Contain("Requires lvl"));
                FieldsClick(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId));
                var plant = ui.Q<Button>("fields-plant-selected");
                Assert.False(plant.enabledInHierarchy, "Discovered seeds with zero owned packs cannot be planted.");
                resources.Add(seedResource, 1, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.True(plant.enabledInHierarchy, "One matching pack enables planting the entire plot.");
                resources.Add(seedResource, 1, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.True(plant.enabledInHierarchy, "An available empty crop bed is selected automatically.");
                Assert.LessOrEqual(ui.Q<ScrollView>("fields-workspace").verticalScroller.highValue, .5f, "Empty-bed actions fit without an outer scrollbar.");
                FieldsClick(plant);
                Assert.True(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                Assert.AreEqual(1, oracle.saveData.Resources[seedName].Amount);
                Assert.That(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId).Q<Label>("bed-status").text, Does.Contain("Radish"));
                Assert.NotNull(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId).Q("bed-progress"));
                Assert.False(ui.Q<Button>("fields-plant-selected").enabledInHierarchy);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q<Button>("fields-harvest-selected").resolvedStyle.display, "Harvest stays visible during growth.");
                Assert.False(ui.Q<Button>("fields-harvest-selected").enabledInHierarchy);
                yield return null;
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.AreEqual(1, oracle.saveData.Resources[seedName].Amount, "A second click cannot charge a growing bed.");
                Assert.True((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private).Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                var loaded = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(loaded.Succeeded, loaded.Diagnostic);
                Assert.AreEqual(1, loaded.Data.Resources[seedName].Amount);
                Assert.True(loaded.Data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
                FieldsClick(ui.Q<Button>("fields-water-selected"));
                Assert.True(service.State.Beds[FarmCommands.WestBedId].Watered);
                yield return null; yield return null;
                var viewport = ui.Q<ScrollView>("fields-workspace");
                Assert.LessOrEqual(viewport.verticalScroller.highValue, .5f, "The desktop Fields grid and controls fit without an unnecessary outer scrollbar.");
                Assert.IsNull(ui.Q<Foldout>("fields-construction-foldout"), "Construction lives on its plot, rather than in a separate quest list.");
                Assert.AreEqual(9, ui.Query<VisualElement>(className: "fields-selector-tile").ToList().Count,
                    "Six crop selectors and three grouped orchard selectors share one 3x3 grid.");
                Assert.IsNull(ui.Q<Button>(className: "fields-plot-action"), "Economic actions belong only to the selected plot panel.");
                var evidence = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fields-grid-review");
                System.IO.Directory.CreateDirectory(evidence);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-growing-watered.png"));
                yield return null; yield return null;
                FarmCommands.TickFields(service.State, content.baseDurationSeconds);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.True(ui.Q<Button>("fields-harvest-selected").enabledInHierarchy);
                FieldsClick(ui.Q<Button>("fields-harvest-selected"));
                Assert.False(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                yield return null;
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy, ui.Q<Label>("fields-selected-status").text);
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.AreEqual(0, oracle.saveData.Resources[seedName].Amount);
                Assert.True((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private).Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
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
                resources.Add(otherRecipe.paidInput, 1, trackStats: false, eligibleForTierRoll: false);
                yield return new WaitForSecondsRealtime(.3f);
                FieldsClick(ui.Q<Button>("fields-seed-" + otherRecipe.id));
                Assert.False(ui.Q<Button>("fields-seed-" + otherRecipe.id).enabledInHierarchy, "Occupied plots lock seed selection.");
                Assert.True(ui.Q<Button>("fields-seed-" + FarmCommands.RadishRecipeId).ClassListContains("primary"));
                Assert.False(ui.Q<Button>("fields-plant-selected").enabledInHierarchy, "Changing crops cannot plant into the occupied bed or a locked one.");
                Assert.AreEqual(1, resources.GetAmount(otherRecipe.paidInput, oracle.saveData));
                var nextBuild = content.builds[1];
                oracle.saveData.Farm.TwinsLevel = 20;
                oracle.saveData.General.MaxRunDistance = 100000;
                foreach (var task in nextBuild.sources)
                    oracle.saveData.SkillData[task.associatedSkill.name] = new GameData.SkillProgress { Level = task.requiredSkillLevel };
                foreach (var cost in nextBuild.costs)
                    resources.Add(cost.resource, cost.amount, trackStats: false, eligibleForTierRoll: false);
                Build(nextBuild.questId);
                yield return new WaitForSecondsRealtime(.3f);
                FieldsClick(ui.Q<Button>("fields-select-" + FarmCommands.EastBedId));
                FieldsClick(ui.Q<Button>("fields-seed-" + otherRecipe.id));
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy, ui.Q<Label>("fields-selected-status").text);
                yield return null; // The newly revealed planting action needs its rendered bounds before the next pointer click.
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
                var orchardButton = ui.Q<Button>("fields-select-" + orchardId);
                ui.Q<ScrollView>("fields-workspace").ScrollTo(orchardButton);
                yield return null; yield return null;
                FieldsClick(orchardButton);
                yield return null; yield return null;
                ui.Q<ScrollView>("fields-seeds").ScrollTo(appleCard);
                yield return null; yield return null;
                FieldsClick(appleCard);
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy, ui.Q<Label>("fields-selected-status").text);
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                Assert.True(service.State.Beds[orchardId].IsPlanted);
                Assert.AreEqual(0, oracle.saveData.Resources[apple.paidInput.name].Amount, "Orchards still use one matching sapling.");
                yield return null; yield return null;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-and-orchard-growing.png"));
                yield return null; yield return null;
                FarmCommands.TickFields(service.State, service.State.Beds[orchardId].DurationSeconds);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(ui.Q<Button>("fields-select-" + orchardId).Q<Label>("bed-status").text, Does.Contain("Ready"));
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-and-orchard-ready.png"));
                yield return null; yield return null;
                // Controlled end-game bank checks the full collection's rendered capacity;
                // normal unlock/payment behaviour is exercised above, not bypassed by this setup.
                service.State.GardenCapacity = service.State.OrchardCapacity = 6;
                foreach (var bedId in FarmCommands.GardenBeds.Concat(FarmCommands.OrchardBeds))
                    if (!service.State.Beds.ContainsKey(bedId)) service.State.Beds[bedId] = new FarmBedState { Unlocked = true };
                service.State.TwinsLevel = 5000;
                foreach (var recipe in content.recipes)
                {
                    oracle.saveData.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = 500 };
                    resources.Add(recipe.paidInput, 1000000, trackStats: false, eligibleForTierRoll: false);
                }
                resources.Add(seedResource, 4, trackStats: false, eligibleForTierRoll: false);
                foreach (var bedId in FarmCommands.GardenBeds.Skip(2)) Assert.True(service.Plant(bedId, FarmCommands.RadishRecipeId));
                FieldsClick(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId));
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-whole-farm-layout.png"));
                yield return null; yield return null;
                Assert.AreEqual(17, ui.Query<Button>(className: "fields-seed-card").ToList().Count(b => b.resolvedStyle.display != DisplayStyle.None));
                Assert.LessOrEqual(ui.Q<ScrollView>("fields-workspace").verticalScroller.highValue, .5f, "The full twelve-plot overview and 17 seed choices must fit the desktop viewport.");
                Assert.LessOrEqual(ui.Q<ScrollView>("fields-seeds").verticalScroller.highValue, .5f, "Both seed rows must remain visible.");
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-whole-farm.png"));
                yield return null; yield return null;
                FieldsClick(ui.Q<Button>("fields-select-" + orchardId));
                yield return null; yield return null;
                Assert.AreEqual(7, ui.Query<Button>(className: "fields-seed-card").ToList().Count(b => b.resolvedStyle.display != DisplayStyle.None));
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-whole-orchard.png"));
                yield return null; yield return null;
                screen.CompanionWidth = 400;
                yield return null; yield return null;
                Assert.AreEqual(9, ui.Query<VisualElement>(className: "fields-selector-tile").ToList().Count);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(960, 2078, "Portrait Fields selector");
                yield return null; yield return null; yield return null;
                screen.CompanionWidth = 0;
                FieldsClick(ui.Q<Button>("fields-select-" + FarmCommands.WestBedId));
                yield return null;
                Assert.True(ui.Q<VisualElement>("fields-plot-" + FarmCommands.WestBedId).ClassListContains("selected"));
                Assert.AreEqual(9, ui.Query<VisualElement>(className: "fields-selector-tile").ToList().Count);
                var selectedHarvest = ui.Q<Button>("fields-harvest-selected");
                var autoHarvest = ui.Q<Button>("fields-repeat-selected");
                Assert.IsNull(ui.Q<Button>("farm-harvest-ready"), "Bulk harvesting is no longer shown.");
                Assert.That(Mathf.Abs(selectedHarvest.worldBound.yMin - autoHarvest.worldBound.yMin), Is.LessThan(1), "Auto and Harvest share one row.");
                Assert.That(autoHarvest.worldBound.width, Is.LessThan(selectedHarvest.worldBound.width), "Auto is the smaller action.");
                var selectedPreview = ui.Q<VisualElement>("fields-selected-preview");
                var progressStatus = selectedPreview.Q<Label>(className: "fields-progress-status");
                Assert.AreEqual("Radish", selectedPreview.Q<Label>("bed-status").text, "The crop label does not also contain its timer.");
                Assert.AreEqual("Ready", progressStatus.text);
                var previewTrack = selectedPreview.Q<VisualElement>(className: "track");
                Assert.That(progressStatus.worldBound.yMax, Is.LessThanOrEqualTo(previewTrack.worldBound.yMin), "Ready state sits above the progress bar.");
                Assert.That(Mathf.Abs(progressStatus.worldBound.xMax - previewTrack.worldBound.xMax), Is.LessThan(1), "Ready state aligns with the right edge of the bar.");
                var yieldRow = selectedPreview.Q<VisualElement>(className: "fields-yield");
                Assert.That(yieldRow.worldBound.yMin, Is.GreaterThanOrEqualTo(previewTrack.worldBound.yMax), "Harvest output sits below the progress bar.");
                Assert.That(Mathf.Abs(yieldRow.worldBound.xMin - previewTrack.worldBound.xMin), Is.LessThan(1), "Crop amount aligns with the left edge of the bar.");
                Assert.That(Mathf.Abs(yieldRow.Q<Label>(className: "fields-yield-xp").worldBound.xMax - progressStatus.worldBound.xMax), Is.LessThan(1), "XP aligns under the timer.");
                Assert.That(Mathf.Abs(autoHarvest.worldBound.xMin - selectedPreview.worldBound.xMin), Is.LessThan(1), "Auto starts at the detail panel edge.");
                Assert.That(Mathf.Abs(selectedHarvest.worldBound.xMax - selectedPreview.worldBound.xMax), Is.LessThan(1), "Harvest fills the rest of the detail row.");
                FieldsClick(ui.Q<Button>("fields-repeat-selected"));
                Assert.True(service.State.Beds[FarmCommands.WestBedId].Repeat, "The Auto toggle enables repeating.");
                Assert.That(autoHarvest.text, Does.EndWith("On"));
                FieldsClick(ui.Q<Button>("fields-repeat-selected"));
                Assert.False(service.State.Beds[FarmCommands.WestBedId].Repeat);
                Assert.That(autoHarvest.text, Does.EndWith("Off"));
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-selector-portrait.png"));
                yield return null;
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Desktop Fields selector");
                yield return null; yield return null;
                screen.CompanionWidth = 0;
                yield return null; yield return null;
                FarmCommands.TickFields(service.State, service.State.Beds.Values.Max(b => b.DurationSeconds));
                yield return new WaitForSecondsRealtime(.3f);
                FieldsClick(ui.Q<Button>("fields-harvest-selected"));
                Assert.False(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                Assert.True(service.State.Beds[FarmCommands.EastBedId].IsPlanted, "Harvest affects only the selected plot.");
                FieldsClick(ui.Q<Button>("fields-select-" + FarmCommands.EastBedId));
                yield return null;
                FieldsClick(ui.Q<Button>("fields-harvest-selected"));
                FieldsClick(ui.Q<Button>("fields-select-" + orchardId));
                yield return null;
                FieldsClick(ui.Q<Button>("fields-harvest-selected"));
                Assert.True(service.State.Beds[orchardId].IsPlanted);
                Assert.False(service.State.Beds[orchardId].EstablishedTree);
                Assert.False(service.State.Beds[FarmCommands.WestBedId].IsPlanted);
                Assert.False(service.State.Beds[FarmCommands.EastBedId].IsPlanted);
                Assert.Greater(oracle.saveData.Resources[apple.output.name].Amount, 0);
                var pear = content.Recipe("recipe.pear.v1");
                oracle.saveData.SkillData[pear.source.associatedSkill.name] = new GameData.SkillProgress { Level = pear.source.requiredSkillLevel };
                yield return new WaitForSecondsRealtime(.3f);
                var pearQuantity = oracle.saveData.Resources[pear.paidInput.name].Amount;
                foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(960, 2078) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)resolution.x, (uint)resolution.y, "Tree replacement review");
                    yield return null; yield return null;
                    FieldsClick(ui.Q<Button>("fields-seed-" + pear.id));
                    yield return null;
                    Assert.That(ui.Q<Button>("fields-plant-selected").text, Is.EqualTo("Replace tree"));
                    FieldsClick(ui.Q<Button>("fields-plant-selected"));
                    yield return null;
                    var dialog = ui.Q<VisualElement>("fields-replace-confirmation");
                    Assert.NotNull(dialog);
                    var confirm = ui.Q<Button>("fields-replace-confirm");
                    Assert.True(confirm.enabledInHierarchy);
                    Assert.True(dialog.worldBound.Contains(confirm.worldBound.center), "Confirmation stays inside the visible modal in both orientations.");
                    FieldsClick(ui.Q<Button>("fields-replace-cancel"));
                    Assert.IsNull(ui.Q("fields-replace-confirmation"));
                    Assert.AreEqual(apple.id, service.State.Beds[orchardId].RecipeId);
                    Assert.AreEqual(pearQuantity, oracle.saveData.Resources[pear.paidInput.name].Amount, "Cancel must not spend the sapling.");
                }
                yield return null;
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                yield return null;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-replace-confirmation.png"));
                yield return null;
                FieldsClick(ui.Q<Button>("fields-replace-confirm"));
                Assert.AreEqual(pear.id, service.State.Beds[orchardId].RecipeId);
                Assert.AreEqual(pearQuantity - 1, oracle.saveData.Resources[pear.paidInput.name].Amount);
                Assert.IsNull(ui.Q("fields-replace-confirmation"));
                var oak = content.Recipe("recipe.oak.v1");
                yield return null; yield return null;
                FieldsClick(ui.Q<Button>("fields-seed-" + oak.id));
                yield return null;
                Assert.True(ui.Q<Button>("fields-plant-selected").enabledInHierarchy, "An owned eligible oak sapling can replace the fruit tree.");
                FieldsClick(ui.Q<Button>("fields-plant-selected"));
                yield return null;
                FieldsClick(ui.Q<Button>("fields-replace-confirm"));
                FarmCommands.TickFields(service.State, 100000);
                yield return new WaitForSecondsRealtime(.3f);
                foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(960, 2078) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)resolution.x, (uint)resolution.y, "Timber Fields review");
                    yield return null; yield return null;
                    Assert.AreEqual("Chop", ui.Q<Button>("fields-harvest-selected").text);
                    Assert.AreEqual(DisplayStyle.Flex, ui.Q("fields-secondary-reward").resolvedStyle.display);
                    Assert.LessOrEqual(ui.Q<ScrollView>("fields-workspace").verticalScroller.highValue, .5f);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence, "fields-timber-" + resolution.x + ".png"));
                    yield return null;
                }
                var saplingsBeforeChop = oracle.saveData.Resources[oak.paidInput.name].Amount;
                FieldsClick(ui.Q<Button>("fields-harvest-selected"));
                Assert.Greater(oracle.saveData.Resources[oak.output.name].Amount, 0);
                Assert.Greater(oracle.saveData.Resources[oak.secondaryOutput.name].Amount, 0);
                Assert.AreEqual(saplingsBeforeChop, oracle.saveData.Resources[oak.paidInput.name].Amount);
                Assert.False(service.State.Beds[orchardId].EstablishedTree);
                Assert.Less(service.State.Beds[orchardId].ElapsedSeconds, 1);
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
