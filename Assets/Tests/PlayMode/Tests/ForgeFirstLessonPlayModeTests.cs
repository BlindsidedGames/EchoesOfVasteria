#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Gear;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator FirstForgeLessonUsesRealConversionsCraftAndEquipAcrossSaveReload()
        {
            var owner = new GameObject("Protected first forge lesson"); GameObject forge = null, board = null;
            try
            {
                var ivan = Resources.Load<QuestData>("Quests/Vast One/Meeting Ivan");
                foreach (var q in ivan.requiredQuests) oracle.saveData.Quests[q.questId] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.CompletedNpcTasks.Add("Ivan1");
                var resources = owner.AddComponent<ResourceManager>();
                owner.AddComponent<CraftingService>(); var equipment = owner.AddComponent<EquipmentController>();
                var quests = owner.AddComponent<QuestManager>(); quests.enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                Blindsided.EventHandler.LoadData();
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, false);
                Assert.True(quests.IsQuestInProgress("You get this one for free!"));
                Assert.False(quests.TryTurnInQuest("You get this one for free!"), "Quests without an individual town restriction must also be blocked during stages.");
                Assert.False(quests.TryTurnInQuest(ivan.questId), "Quest turn-ins must wait for town.");
                typeof(TimelessEchoes.GameManager).GetField("deathWindowDuration", Private).SetValue(gm, .05f);
                gm.ForceHandleHeroDeath(); yield return new WaitForSecondsRealtime(.2f);
                Assert.True(gm.HeroIsDead, "After meeting Ivan, automatic restart must wait for the town visit.");
                gm.RequestReturnAfterDeath(); yield return null; yield return null; yield return null; yield return null;
                Assert.True(gm.IsInTown);
                Assert.True(quests.TryTurnInQuest(ivan.questId));
                var lesson = Resources.LoadAll<QuestData>("Quests").Single(q => q.questId == ForgeFirstLesson.QuestId);
                board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
                Assert.True(board.GetComponent<ToolkitQuestsScreen>().Show()); yield return null; yield return null;
                var accept = board.GetComponent<UIDocument>().rootVisualElement.Q<Button>("turn-in-" + lesson.questId);
                Assert.That(accept.Q<Label>().text, Does.Contain("Accept"));
                var boardUi = board.GetComponent<UIDocument>().rootVisualElement;
                var viewport = boardUi.Q<ScrollView>("quest-scroll").contentViewport.worldBound;
                Assert.True(viewport.Contains(accept.worldBound.center), "Ivan's required lesson must be immediately visible above other quests.");
                Assert.AreEqual(DisplayStyle.Flex, boardUi.Q("quest-accept-hand").style.display.value);
                FieldsClick(accept);
                yield return null; yield return null;
                Assert.False(quests.IsQuestCompleted(lesson));
                Assert.True(ForgeFirstLesson.Accepted);
                foreach (var reward in lesson.acceptanceResources) Assert.GreaterOrEqual(resources.GetAmount(reward.resource), reward.amount);
                Assert.False(quests.TryTurnInQuest(lesson.questId), "Ivan's materials must be granted only once.");
                Object.DestroyImmediate(board); board = null;
                forge = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Forge.prefab"));
                Assert.True(forge.GetComponent<ToolkitForgeScreen>().Show()); yield return new WaitForSecondsRealtime(.2f);
                var ui = forge.GetComponent<UIDocument>().rootVisualElement;
                Assert.IsNull(ui.Q<Button>("forge-inventory"));
                Assert.IsNull(ui.Q<Button>("forge-info-tab"));
                Assert.IsNull(ui.Q<Button>("forge-auto"), "Repeating crafting shares the main Craft button.");
                Assert.AreEqual(1, ui.Query<Button>(className: "eov-forge-core").ToList().Count(b => b.name != "forge-locked-cores" && b.resolvedStyle.display != DisplayStyle.None));
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("forge-locked-cores").resolvedStyle.display);
                Assert.AreEqual("—", ui.Q<Label>("forge-locked-core-count").text);
                Assert.That(ui.Q("forge-locked-cores").Q(className: "eov-forge-core-frame").worldBound.y,
                    Is.EqualTo(ui.Query<Button>(className: "eov-forge-core").ToList().First(b => b.name != "forge-locked-cores" && b.resolvedStyle.display != DisplayStyle.None)
                        .Q(className: "eov-forge-core-frame").worldBound.y).Within(.5f), "Locked and discovered core frames should share a baseline.");
                Assert.IsNull(ui.Q("forge-odds-popup"));
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("forge-odds-legend").resolvedStyle.display,
                    "Rarity odds must be visible without hover or click.");
                Assert.That(ui.Query<Label>(className: "forge-odds-name").ToList().Count(l => l.text == "???"), Is.GreaterThan(0),
                    "Undiscovered rarities must not reveal their names.");
                var session = forge.GetComponent<ForgeSession>();
                Assert.IsNull(equipment.GetEquipped("Weapon"));
                CaptureFeedback("first-forge-smelt.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("smelt-Ingot")); yield return new WaitForSecondsRealtime(.2f);
                Assert.That(ui.Query<Label>(className: "forge-odds-name").ToList().Any(l => l.text == "Bronze"), Is.True,
                    "Discovering an ingot reveals its rarity in the odds legend.");
                Assert.AreEqual(1, ForgeFirstLesson.Stage);
                Assert.That(ui.Q("forge-first-lesson").Q<Label>().text, Does.Contain("rarity").And.Contain("higher tiers"));
                CaptureFeedback("first-forge-core-explanation.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("core-" + session.Core.name)); yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(2, ForgeFirstLesson.Stage);
                FieldsClick(ui.Q<Button>("gear-Weapon")); yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(3, ForgeFirstLesson.Stage);
                FieldsClick(ui.Q<Button>("forge-craft")); yield return new WaitForSecondsRealtime(.2f);
                var craftButton = ui.Q<Button>("forge-craft");
                Assert.AreEqual(4, ForgeFirstLesson.Stage,
                    $"Craft click: ready={session.CanCraft()}, enabled={craftButton.enabledInHierarchy}, " +
                    $"button={craftButton.worldBound}, viewport={ui.Q<ScrollView>().contentViewport.worldBound}, screen={Screen.width}x{Screen.height}");
                Assert.IsNull(equipment.GetEquipped("Weapon"), "The player must explicitly equip the tutorial sword.");
                Assert.AreEqual("Weapon", session.Pending.slot);
                var pendingRarity = session.Pending.rarity.name;
                var pendingStats = session.Pending.affixes.Select(a => a.value).ToArray();
                Assert.True(oracle.SaveToSlot(oracle.CurrentSlot));
                var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(saved.Succeeded, saved.Diagnostic);
                Object.DestroyImmediate(forge); forge = null;
                oracle.saveData = saved.Data; Blindsided.EventHandler.LoadData();
                forge = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Forge.prefab"));
                Assert.True(forge.GetComponent<ToolkitForgeScreen>().Show()); yield return new WaitForSecondsRealtime(.2f);
                ui = forge.GetComponent<UIDocument>().rootVisualElement; session = forge.GetComponent<ForgeSession>();
                Assert.AreEqual(4, ForgeFirstLesson.Stage);
                Assert.AreEqual(pendingRarity, session.Pending.rarity.name);
                CollectionAssert.AreEqual(pendingStats, session.Pending.affixes.Select(a => a.value).ToArray());
                Assert.That(ui.Q<Button>("forge-replace").text, Does.Contain("Equip"));
                CaptureFeedback("first-forge-equip-after-reload.png"); yield return null; yield return null;
                FieldsClick(ui.Q<Button>("forge-replace")); yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(5, ForgeFirstLesson.Stage); Assert.NotNull(equipment.GetEquipped("Weapon"));
                Assert.True(quests.IsQuestCompleted(lesson), "Equipping the sword completes the accepted lesson.");
                Assert.AreEqual(pendingRarity, equipment.GetEquipped("Weapon").rarity.name);
                Assert.AreEqual(DisplayStyle.None, ui.Q("forge-lesson-hand").style.display.value);
                Assert.True(oracle.SaveToSlot(oracle.CurrentSlot));
                var finished = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(finished.Succeeded, finished.Diagnostic);
                Assert.True(finished.Data.EquipmentBySlot.ContainsKey("Weapon"));
                Assert.AreEqual(5, finished.Data.General.ForgeFirstLessonStage);
                var binding = session.Catalog.Find(session.Core);
                oracle.saveData.CraftingMasteryLevel = 2;
                resources.Add(binding.coreResource, 10);
                resources.Add(binding.ingotResource, 100);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.True(session.CanCraft());
                var automationBounds = ui.Q("forge-automation-area").worldBound;
                var craftsBeforeRepeat = oracle.saveData.Forge.TotalCrafts;
                session.StartAutoCrafting();
                Assert.False(session.IsAutoCrafting);
                Assert.AreEqual(craftsBeforeRepeat, oracle.saveData.Forge.TotalCrafts);
                Assert.False(ui.Q<Button>("forge-repeat-craft").enabledInHierarchy);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("forge-automation-locked").style.display.value);
                CaptureFeedback("forge-automation-locked.png"); yield return null; yield return null;
                oracle.saveData.CraftingMasteryLevel = 3;
                resources.Add(binding.coreResource, 1);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.True(ui.Q<Button>("forge-repeat-craft").enabledInHierarchy);
                Assert.AreEqual(DisplayStyle.None, ui.Q("forge-automation-locked").style.display.value);
                Assert.AreEqual(automationBounds, ui.Q("forge-automation-area").worldBound, "Unlocking automation must preserve the work area layout.");
                FieldsClick(ui.Q<Button>("forge-repeat-craft"));
                Assert.AreEqual(craftsBeforeRepeat, oracle.saveData.Forge.TotalCrafts, "Choosing repeat must not craft before Craft is clicked.");
                FieldsClick(ui.Q<Button>("forge-craft"));
                Assert.Greater(oracle.saveData.Forge.TotalCrafts, craftsBeforeRepeat);
                FieldsClick(ui.Q<Button>("forge-repeat-craft"));
                Assert.False(session.IsAutoCrafting, "Unchecking repeat stops the active batch.");
                yield return null; yield return null;
                var conversions = ui.Q(className: "eov-forge-conversions").worldBound;
                var workViewport = ui.Q<ScrollView>().contentViewport.worldBound;
                Assert.That(ui.Query<Label>().ToList().Any(l => l.text == "New item"), Is.True);
                Assert.Greater(ui.Q<Button>("forge-repeat-craft").worldBound.xMin, ui.Q<Button>("forge-lock-stats").worldBound.xMin);
                foreach (var toggle in ui.Query<Button>(className: "eov-forge-toggle").ToList())
                    Assert.LessOrEqual(toggle.Q<Label>().worldBound.height, toggle.worldBound.height,
                        "Automation labels must fit on one line inside their checkbox controls.");
                Assert.That(conversions.yMax, Is.EqualTo(workViewport.yMax).Within(20), "Conversion cards should sit at the bottom of the forge viewport.");
                foreach (var field in ui.Query<TextField>(className: "eov-number-field").ToList()) {
                    Assert.True(field.enabledInHierarchy, "Conversion amounts remain editable even when there are not enough materials to convert.");
                    Assert.GreaterOrEqual(field.resolvedStyle.borderLeftWidth, 1, "Editable quantity fields need a visible boundary against their card.");
                }
                CaptureFeedback("forge-persistent-odds-bottom-cards.png"); yield return null; yield return null;
                ui.Q<TextField>("amount-Ingot").Focus(); yield return null;
                var focused = ui.panel.focusController.focusedElement as VisualElement;
                Assert.True(focused == ui.Q<TextField>("amount-Ingot") || ui.Q<TextField>("amount-Ingot").Contains(focused), "The editable quantity field must accept focus.");
                CaptureFeedback("forge-quantity-field-focused.png"); yield return null; yield return null;
                var lirium = session.Catalog.cores.Single(c => c.core.tierIndex == 4).ingotResource;
                resources.Add(lirium, 1); yield return new WaitForSecondsRealtime(.2f);
                Assert.True(ui.Query<Label>(className: "forge-odds-name").ToList().Any(l => l.text == "Lirium"));
                resources.Spend(lirium, 1); yield return new WaitForSecondsRealtime(.2f);
                Assert.True(ui.Query<Label>(className: "forge-odds-name").ToList().Any(l => l.text == "Lirium"), "Spending the last ingot must not hide a discovered rarity again.");
                forge.GetComponent<ToolkitForgeScreen>().Hide();
                var statistics = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"), owner.transform);
                var statisticsScreen = statistics.GetComponent<ToolkitStatisticsScreen>();
                Assert.True(statisticsScreen.Show());
                var statisticsUi = statistics.GetComponent<UIDocument>().rootVisualElement;
                yield return null; yield return null;
                FieldsClick(statisticsUi.Q<Button>("stats-tab-6")); yield return null; yield return null;
                var totalCrafts = statisticsUi.Q("town-statistics-crafted");
                Assert.AreEqual(oracle.saveData.Forge.TotalCrafts.ToString("N0"), totalCrafts.Q<Label>(className: "town-statistics-value").text,
                    "The Statistics tab must retain the actual crafting history from the completed forge lesson.");
                CaptureFeedback("town-statistics.png"); yield return null; yield return null;
            }
            finally { if (forge) Object.DestroyImmediate(forge); if (board) Object.DestroyImmediate(board); Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator FirstForgeRiverReturnUnlocksLoreOnlyAfterReachingTown()
        {
            var owner = new GameObject("Protected river return"); var map = new GameObject("Protected river map"); GameObject nav = null;
            var previousMap = TimelessEchoes.GameManager.CurrentGenerationConfig;
            try
            {
                var tracker = owner.AddComponent<GameplayStatTracker>(); tracker.enabled = false;
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm, map);
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm, false);
                typeof(TimelessEchoes.GameManager).GetProperty("CurrentGenerationConfig").SetValue(null, AssetDatabase.LoadAssetAtPath<TimelessEchoes.MapGeneration.MapGenerationConfig>("Assets/Scriptables/MapSettings/River.asset"));
                nav = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var screen = nav.GetComponent<ToolkitNavigationScreen>(); var ui = nav.GetComponent<UIDocument>().rootVisualElement;
                screen.Refresh(); Assert.AreEqual(DisplayStyle.None, ui.Q("wiki-row").style.display.value);
                gm.ForceHandleHeroDeath(); gm.RequestReturnAfterDeath();
                yield return null; yield return null; yield return null; yield return null; yield return null;
                Assert.True(gm.IsInTown); Assert.True(oracle.saveData.General.ReturnedFromRiver);
                screen.Refresh(); Assert.AreEqual(DisplayStyle.Flex, ui.Q("wiki-row").style.display.value);
                Assert.IsNull(ui.Q("tutorial-pointer-wiki"));
            }
            finally { typeof(TimelessEchoes.GameManager).GetProperty("CurrentGenerationConfig").SetValue(null, previousMap); if (nav) Object.DestroyImmediate(nav); if (map) Object.DestroyImmediate(map); Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator FirstForgeLoadingBackdropIsOpaqueAndCoversThePanelViewport()
        {
            var owner = new GameObject("Protected loading owner");
            var view = new GameObject("Protected loading screen", typeof(UIDocument)); view.SetActive(false);
            try
            {
                var gm = owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled = false;
                typeof(TimelessEchoes.GameManager).GetField("runLoadingActive", Private).SetValue(gm, true);
                var screen = view.AddComponent<ToolkitLoadingScreen>();
                void Set(string field, Object value) => typeof(ToolkitLoadingScreen).GetField(field, Private).SetValue(screen, value);
                Set("theme", AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"));
                Set("runtimeTheme", AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"));
                Set("textSettings", AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
                view.SetActive(true); yield return null; yield return null; yield return null;
                var document = view.GetComponent<UIDocument>().rootVisualElement;
                var backdrop = document.Q("loading-backdrop"); var viewport = document.panel.visualTree.worldBound;
                Assert.AreEqual(1, backdrop.resolvedStyle.backgroundColor.a);
                Assert.AreEqual(1, backdrop.resolvedStyle.opacity);
                Assert.That(backdrop.worldBound.xMin, Is.EqualTo(viewport.xMin).Within(.1f));
                Assert.That(backdrop.worldBound.yMin, Is.EqualTo(viewport.yMin).Within(.1f));
                Assert.That(backdrop.worldBound.width, Is.EqualTo(viewport.width).Within(.1f));
                Assert.That(backdrop.worldBound.height, Is.EqualTo(viewport.height).Within(.1f));
                CaptureFeedback("opaque-full-screen-loading.png"); yield return null; yield return null;
            }
            finally { Object.DestroyImmediate(view); Object.DestroyImmediate(owner); }
        }
    }
}
#endif
