#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.Characters;
using TimelessEchoes.Gear;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator HardcoreChoiceCommitsOnlyAtCreationAndMemorialKeepsReadOnlyNavigationReachable()
        {
            var owner = new GameObject("Hardcore character UI"); GameObject intro = null, options = null, statistics = null;
            var prefs = new[] { "Completion", "Playtime", "Date" }.ToDictionary(s => oracle.GetSlotPlayerPrefsKey(oracle.CurrentSlot, s),
                key => (PlayerPrefs.HasKey(key), key.EndsWith("_Date") ? PlayerPrefs.GetString(key) : null, PlayerPrefs.GetFloat(key)));
            try
            {
                oracle.saveData.Character = CharacterProfile.Fresh();
                var game = owner.AddComponent<GameManager>(); game.enabled = false;
                owner.AddComponent<ResourceManager>();
                var equipment = owner.AddComponent<EquipmentController>();
                owner.AddComponent<TimelessEchoes.Stats.GameplayStatTracker>().enabled = false;
                Blindsided.EventHandler.LoadData();
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                intro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Intro.prefab"));
                yield return new WaitForSecondsRealtime(.2f);
                var creator = intro.GetComponentInChildren<ToolkitCharacterCreator>();
                Assert.True(creator.IsOpen);
                var ui = GameObject.Find("Character creation panel").GetComponent<UIDocument>().rootVisualElement;
                var toggle = ui.Q<Button>("character-hardcore"); Assert.NotNull(toggle);
                // First-time sprite imports can leave the panel awaiting its initial layout.
                for (var frame = 0; frame < 60 && !(toggle.worldBound.width > 0); frame++) yield return null;
                Assert.Greater(toggle.worldBound.width, 0, "Wait for a real hit target before sending pointer input.");
                Assert.False(oracle.saveData.Hardcore);
                FieldsClick(toggle); Assert.False(oracle.saveData.Hardcore, "The choice is still a draft until Start game succeeds.");
                ui.Q<TextField>("character-name").value = "Aster";
                var evidence = Path.GetFullPath("Artifacts/Hardcore20261101/StatsRevision5"); Directory.CreateDirectory(evidence);
                foreach (var portrait in new[] { false, true })
                {
                    PlayModeWindow.SetCustomRenderingResolution(portrait ? 1080u : 1920u, portrait ? 1920u : 1080u, "Hardcore creation");
                    yield return new WaitForSecondsRealtime(.3f);
                    var bounds = ui.Q(className: "character-sheet").worldBound;
                    foreach (var button in new[] { toggle, ui.Q<Button>("character-confirm") })
                        Assert.True(bounds.Contains(button.worldBound.center), "The opt-in and start controls stay reachable.");
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, portrait ? "creation-portrait.png" : "creation-landscape.png"));
                    yield return null; yield return null;
                }
                FieldsClick(ui.Q<Button>("character-confirm")); yield return null;
                Assert.True(oracle.saveData.Hardcore); Assert.True(oracle.saveData.Character.Created);
                var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(saved.Succeeded, saved.Diagnostic); Assert.True(saved.Data.Hardcore);
                Assert.True(creator.Show(true));
                ui = GameObject.Find("Character creation panel").GetComponent<UIDocument>().rootVisualElement;
                Assert.IsNull(ui.Q<Button>("character-hardcore"), "Editing appearance cannot change the save mode."); creator.Hide();
                Assert.True(creator.Show(false));
                yield return null;
                ui = GameObject.Find("Character creation panel").GetComponent<UIDocument>().rootVisualElement;
                Assert.IsNull(ui.Q<Button>("character-hardcore"), "The non-cancellable appearance entry point cannot change it either."); creator.Hide();
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                var rarity = Resources.LoadAll<RaritySO>("").OrderBy(r => r.tierIndex).ElementAt(2);
                var stat = Resources.LoadAll<StatDefSO>("").First(s => s.heroMapping == HeroStatMapping.MaxHealth);
                var regen = Resources.LoadAll<StatDefSO>("").First(s => s.heroMapping == HeroStatMapping.HealthRegen);
                var speed = Resources.LoadAll<StatDefSO>("").First(s => s.heroMapping == HeroStatMapping.MoveSpeed);
                foreach (var slot in new[] { "Weapon", "Helmet", "Chest", "Boots" }) Assert.True(equipment.Equip(new GearItem { slot = slot, rarity = rarity, affixes = { new GearAffix { stat = stat, value = 12 }, new GearAffix { stat = regen, value = 3 }, new GearAffix { stat = speed, value = 5 } } }));
                Blindsided.EventHandler.SaveData();
                oracle.saveData.PlayTime = 4321;
                oracle.saveData.General.TotalKills = 12345;
                oracle.saveData.General.TotalResourcesGathered = 1234567;
                oracle.saveData.General.TasksCompleted = 45678;
                oracle.saveData.General.LongestRun = 6789;
                oracle.saveData.General.TimesReaped = 42;
                oracle.saveData.Quests.Clear();
                foreach (var id in new[] { "The names Gill", "Meeting Ivan", ForgeFirstLesson.QuestId, "retired-quest-not-shipped" })
                    oracle.saveData.Quests[id] = new GameData.QuestRecord { Completed = true };
                oracle.saveData.HardcoreDeath = new GameData.HardcoreDeathRecord { Map = "River", Distance = 321, DiedUtc = "2026-11-01T01:00:00Z" };
                owner.AddComponent<TimelessEchoes.Quests.QuestManager>().enabled = false;
                Blindsided.EventHandler.LoadData(); yield return new WaitForSecondsRealtime(.2f);
                Assert.IsNull(Object.FindAnyObjectByType<ToolkitMeetingScreen>(), "Loading a fallen save must not start another tutorial conversation.");
                options = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Options.prefab"));
                typeof(TownWindowManager).GetField("toolkitOptions", Private).SetValue(windows, options.GetComponent<ToolkitOptionsScreen>());
                statistics = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
                typeof(TownWindowManager).GetField("toolkitStatistics", Private).SetValue(windows, statistics.GetComponent<ToolkitStatisticsScreen>());
                ui = GameObject.Find("Character creation panel").GetComponent<UIDocument>().rootVisualElement;
                Assert.NotNull(ui.Q("hardcore-memorial")); Assert.IsNull(ui.Q<Button>("character-confirm"));
                Assert.AreEqual("3", ui.Q<Label>("hardcore-value-quests").text, "Only authored quests count, not retired save records.");
                Assert.AreEqual(Blindsided.Utilities.CalcUtils.FormatNumber(12345, true), ui.Q<Label>("hardcore-value-kills").text);
                Assert.AreEqual(Blindsided.Utilities.CalcUtils.FormatNumber(1234567, true), ui.Q<Label>("hardcore-value-loot").text, "Lifetime loot is independent of current inventory and recent runs.");
                Assert.AreEqual(Blindsided.Utilities.CalcUtils.FormatNumber(45678, true), ui.Q<Label>("hardcore-value-tasks").text);
                Assert.AreEqual(Blindsided.Utilities.CalcUtils.FormatNumber(6789, true) + " m", ui.Q<Label>("hardcore-value-distance").text, "Best distance is distinct from the fatal run's distance.");
                Assert.AreEqual("42", ui.Q<Label>("hardcore-value-reaps").text);
                foreach (var slot in equipment.Slots) Assert.NotNull(ui.Q<Image>("hardcore-gear-" + slot).sprite, "Ordinary crafted equipment retains its real icon.");
                Assert.False(oracle.CanBeginSaveMutation); Assert.True(oracle.CanManageSave);
                Assert.False(windows.CanOpenWindow(TownWindowManager.Window.Forge)); Assert.False(windows.CanOpenWindow(TownWindowManager.Window.Quests));
                foreach (var portrait in new[] { false, true })
                {
                    PlayModeWindow.SetCustomRenderingResolution(portrait ? 1080u : 1920u, portrait ? 1920u : 1080u, "Hardcore memorial");
                    yield return new WaitForSecondsRealtime(.3f);
                    var bounds = ui.Q(className: "character-sheet").worldBound;
                    var cells = ui.Query<VisualElement>(className: "memorial-stat").ToList();
                    Assert.AreEqual(6, cells.Count);
                    foreach (var cell in cells)
                    {
                        Assert.NotNull(cell.Q<Image>().sprite, "Every lifetime total uses existing artwork.");
                        Assert.LessOrEqual(cell.Q<Label>(className: "memorial-stat-label").worldBound.yMax, cell.Q(className: "memorial-stat-value").worldBound.y, "Headings sit above their icons and values.");
                    }
                    Assert.AreEqual(cells[0].worldBound.y, cells[portrait ? 1 : 2].worldBound.y, .5f);
                    Assert.Greater(cells[portrait ? 2 : 3].worldBound.y, cells[0].worldBound.y + 1);
                    foreach (var cell in cells) Assert.True(bounds.Contains(cell.worldBound.center), "All six totals are visible without scrolling.");
                    var gear = ui.Query<VisualElement>(className: "memorial-gear").ToList();
                    Assert.AreEqual(4, gear.Count);
                    foreach (var entry in gear)
                    {
                        var affixes = entry.Query<VisualElement>(className: "memorial-gear-affix").ToList();
                        Assert.AreEqual(3, affixes.Count);
                        foreach (var affix in affixes)
                        {
                            Assert.NotNull(affix.Q<Image>().sprite, "Equipment bonuses use the canonical stat icons.");
                            Assert.False(affix.Q<Label>().text.Contains(":"), "Values replace the repeated stat-name labels.");
                            Assert.AreEqual(affixes[0].Q<Label>().worldBound.xMax, affix.Q<Label>().worldBound.xMax, .5f, "Bonus values share an aligned column.");
                        }
                    }
                    if (portrait)
                    {
                        Assert.AreEqual(gear[0].worldBound.y, gear[1].worldBound.y, .5f, "Portrait equipment uses two columns.");
                        Assert.AreEqual(gear[2].worldBound.y, gear[3].worldBound.y, .5f);
                        Assert.GreaterOrEqual(gear[2].worldBound.y, gear[0].worldBound.yMax - .5f);
                    }
                    else
                    {
                        Assert.AreEqual(gear[0].worldBound.y, gear[3].worldBound.y, .5f, "Landscape equipment shares one horizontal row.");
                        Assert.GreaterOrEqual(gear[1].worldBound.x, gear[0].worldBound.xMax - .5f);
                        foreach (var entry in gear) Assert.True(bounds.Contains(entry.worldBound.max), "All four equipment columns fit on screen.");
                    }
                    foreach (var name in new[] { "hardcore-statistics", "hardcore-save-files" }) Assert.True(bounds.Contains(ui.Q<Button>(name).worldBound.center));
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, portrait ? "memorial-portrait.png" : "memorial-landscape.png")); yield return null; yield return null;
                }
                FieldsClick(ui.Q<Button>("hardcore-save-files")); yield return null; yield return null;
                Assert.True(options.GetComponent<ToolkitOptionsScreen>().IsOpen);
                Assert.AreEqual(DisplayStyle.None, ui.Q("hardcore-memorial").style.display.value);
                var optionsUi = options.GetComponent<UIDocument>().rootVisualElement;
                Assert.True(optionsUi.Q<Button>("saves-tab").ClassListContains("active"));
                windows.CloseAllWindows(); yield return new WaitForSecondsRealtime(.25f);
                Assert.AreEqual(DisplayStyle.Flex, ui.Q("hardcore-memorial").style.display.value);
                Assert.True(ui.Q<Button>("hardcore-statistics").enabledInHierarchy);
                FieldsClick(ui.Q<Button>("hardcore-statistics")); yield return null; yield return null;
                Assert.True(statistics.GetComponent<ToolkitStatisticsScreen>().IsOpen);
                Assert.True(oracle.saveData.HardcoreFallen);
            }
            finally
            {
                if (statistics) Object.DestroyImmediate(statistics); if (options) Object.DestroyImmediate(options);
                if (intro) Object.DestroyImmediate(intro);
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                Object.DestroyImmediate(owner);
                foreach (var pref in prefs)
                    if (!pref.Value.Item1) PlayerPrefs.DeleteKey(pref.Key);
                    else if (pref.Key.EndsWith("_Date")) PlayerPrefs.SetString(pref.Key, pref.Value.Item2); else PlayerPrefs.SetFloat(pref.Key, pref.Value.Item3);
            }
        }
    }
}
#endif
