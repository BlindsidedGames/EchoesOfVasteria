#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Conversations;
using TimelessEchoes.NPC;
using TimelessEchoes.Shop;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator CompanionChoicesSwitchAllFiveDogsAndRestoreCatsAcrossLayouts()
        {
            var shop = CreateCompanionShop();
            shop.SetOriginalOwner(true);
            var previous = MildredAppearance.Selected;
            var host = new GameObject("Companion and conversation UI", typeof(UIDocument));
            var simulator = host.AddComponent<Pathfinding.RVO.RVOSimulator>();
            var companion = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mildred.prefab"));
            companion.GetComponent<MildredMovementController>().enabled = false;
            var screen = host.AddComponent<ToolkitConversationScreen>();
            screen.Configure(AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"),
                AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"),
                AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
            var originalController = companion.GetComponentInChildren<Animator>().runtimeAnimatorController;
            try
            {
                oracle.saveData.CompletedNpcTasks.Add("Witch1");
                oracle.saveData.CompletedNpcTasks.Add("EvaWateringCanIntroduction");
                oracle.saveData.Quests["Mildred"] = new GameData.QuestRecord { Completed = true };
                screen.Show("Eva");
                var ui = host.GetComponent<UIDocument>().rootVisualElement;
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1080, 1920), new Vector2Int(1920, 1080) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Companion options");
                    yield return new WaitForSecondsRealtime(.3f);
                    for (var i = 5; i < 10; i++)
                    {
                        var button = ui.Q<Button>("companion-choice-" + i);
                        Assert.NotNull(button);
                        Assert.Greater(button.worldBound.width, 0);
                        FieldsClick(button);
                        yield return null; yield return null;
                        var animator = companion.GetComponentInChildren<Animator>();
                        Assert.AreSame(MildredAppearance.Colours.Controller(i), animator.runtimeAnimatorController);
                        Assert.True(MildredAppearance.Colours.PreviewFrames(i).Contains(companion.GetComponentInChildren<SpriteRenderer>().sprite));
                        animator.SetFloat("MoveMagnitude", 1);
                        animator.Update(.5f); animator.Update(.5f);
                        StringAssert.Contains("_run_", companion.GetComponentInChildren<SpriteRenderer>().sprite.name);
                        animator.SetFloat("MoveMagnitude", 0);
                        animator.Update(.5f); animator.Update(.5f);
                        if (i > 5) Assert.LessOrEqual(ui.Q<Button>("companion-choice-" + (i - 1)).worldBound.xMax, button.worldBound.xMin + 1);
                    }
                    Assert.Greater(ui.Q<Button>("companion-choice-5").worldBound.yMin, ui.Q<Button>("companion-choice-0").worldBound.yMax);
                    SaveConversationScreenshot("eva-" + size.x + ".png");
                    yield return null; yield return null;
                    FieldsClick(ui.Q<Button>("companion-choice-0"));
                    yield return null; yield return null;
                    Assert.AreSame(originalController, companion.GetComponentInChildren<Animator>().runtimeAnimatorController);
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(companion);
                MildredAppearance.Select(previous);
                Object.DestroyImmediate(shop.gameObject);
            }
        }

        private ShopService CreateCompanionShop()
        {
            if (ShopService.Instance) Object.DestroyImmediate(ShopService.Instance.gameObject);
            var shop = new GameObject("Isolated companion shop").AddComponent<ShopService>();
            shop.enabled = false;
            typeof(ShopService).GetField("ledger", Private).SetValue(shop, new ShopLedger(Path.Combine(root, "cosmetics.json")));
            return shop;
        }

        [UnityTest]
        public IEnumerator CompanionCosmeticsRequirePurchaseOrOriginalOwnership()
        {
            var shop = CreateCompanionShop();
            var previous = MildredAppearance.Selected;
            GameObject host = null;
            try
            {
                MildredAppearance.Select(0);
                foreach (var choice in new[] { 0, 1, 5, 6 }) Assert.True(MildredAppearance.IsUnlocked(choice));
                foreach (var choice in new[] { 2, 3, 4, 7, 8, 9 }) Assert.False(MildredAppearance.IsUnlocked(choice));
                Assert.True(shop.Fulfill(shop.Settings.supporterProductIds[0], "support-1"));
                Assert.True(shop.Fulfill(shop.Settings.permanentProductId, "gathering-1"));
                Assert.False(shop.CosmeticsOwned);
                MildredAppearance.Select(7);
                Assert.AreEqual(0, MildredAppearance.Selected);
                host = new GameObject("Locked companion choices", typeof(UIDocument));
                var picker = host.AddComponent<ToolkitMildredPicker>();
                picker.Configure(AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"),
                    AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"),
                    AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
                picker.Show();
                yield return new WaitForSecondsRealtime(.3f);
                shop.Purchases.Connect();
                var deadline = Time.realtimeSinceStartup + 5;
                while (!shop.Purchases.Available(shop.Settings.cosmeticProductId) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual("US$1.99", shop.Purchases.Price(shop.Settings.cosmeticProductId));
                var ui = host.GetComponent<UIDocument>().rootVisualElement;
                Assert.IsNull(ui.Q("companion-purchase"), "Companion selection links to the Shop instead of selling in the picker.");
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1080, 1920) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Companion cosmetic pricing");
                    yield return new WaitForSecondsRealtime(.3f);
                    var unlock = ui.Q<Button>("companion-unlock");
                    var heading = unlock.parent.Q<Label>(className: "heading");
                    Assert.AreEqual("Unlock all · US$1.99", unlock.text);
                    Assert.LessOrEqual(heading.worldBound.xMax, unlock.worldBound.xMin);
                    Assert.Less(Mathf.Abs(heading.worldBound.center.y - unlock.worldBound.center.y), 1);
                    Assert.AreEqual(1, ui.Query<TextElement>().ToList().Count(element => element.text?.Contains("US$1.99") == true),
                        "The pack price appears once, rather than on each companion.");
                    Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("companion-choice-5").Q(className: "companion-lock").resolvedStyle.display);
                    SaveConversationScreenshot("cosmetics-locked-" + size.x + ".png");
                    yield return null; yield return null;
                }
                Assert.True(shop.Fulfill(shop.Settings.cosmeticProductId, "cosmetics-1"));
                yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("companion-unlock").resolvedStyle.display);
                MildredAppearance.Select(7);
                Assert.AreEqual(7, MildredAppearance.Selected);
                Assert.True(shop.SetPurchasedEntitlement(shop.Settings.cosmeticProductId, false));
                Assert.AreEqual(5, MildredAppearance.Selected, "A locked dog falls back to the base dog.");
                Assert.True(shop.SetPurchasedEntitlement(shop.Settings.cosmeticProductId, true));
                Assert.AreEqual(7, MildredAppearance.Selected, "Restoration restores the requested appearance.");
                Assert.True(shop.SetPurchasedEntitlement(shop.Settings.cosmeticProductId, false));
                shop.SetOriginalOwner(true);
                Assert.True(shop.CosmeticsOwned);
                for (var i = 0; i < 10; i++) Assert.True(MildredAppearance.IsUnlocked(i));
            }
            finally
            {
                if (host) Object.DestroyImmediate(host);
                shop.SetOriginalOwner(true);
                MildredAppearance.Select(previous);
                Object.DestroyImmediate(shop.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CompanionConversationReplayPreservesProgressAndKeepsUnfinishedScenesLocked()
        {
            var catalog = ConversationCatalog.Load();
            var entry = catalog.entries.Single(e => e.id == "meeting:Barkley1");
            oracle.saveData.CompletedNpcTasks.Add("Barkley1");
            oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 123, Earned = true };
            oracle.saveData.Quests["Farm.Saplings.Birch.v1"] = new GameData.QuestRecord { Accepted = true };
            ConversationCatalog.EnsureHistory(oracle.saveData);
            Assert.True(oracle.saveData.HeardConversations.ContainsKey(entry.id));
            Assert.False(oracle.saveData.HeardConversations.ContainsKey("story:Farm.Saplings.Birch.v1"));
            var host = new GameObject("Replay picker", typeof(UIDocument));
            var picker = host.AddComponent<ToolkitConversationScreen>();
            picker.Configure(AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"),
                AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"),
                AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
            try
            {
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1080, 1920) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Conversations");
                    picker.Show("Barkley");
                    yield return new WaitForSecondsRealtime(.3f);
                    var ui = host.GetComponent<UIDocument>().rootVisualElement;
                    Assert.False(ui.Q<Button>("conversation-story:Farm.Saplings.Birch.v1").enabledSelf);
                    SaveConversationScreenshot("barkley-list-" + size.x + ".png");
                    yield return null; yield return null;
                    FieldsClick(ui.Q<Button>("conversation-" + entry.id));
                    yield return new WaitForSecondsRealtime(.3f);
                    var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                    Assert.NotNull(meeting);
                    SaveConversationScreenshot("barkley-replay-" + size.x + ".png");
                    yield return null; yield return null;
                    var advance = meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance");
                    for (var i = 0; i < entry.task.Lines.Count; i++) { FieldsClick(advance); yield return null; }
                    yield return null;
                    Assert.True(picker.IsOpen);
                    Assert.AreEqual(123, oracle.saveData.Resources["Log"].Amount);
                    Assert.False(oracle.saveData.Quests["Farm.Saplings.Birch.v1"].Completed);
                    Assert.AreEqual(1, oracle.saveData.CompletedNpcTasks.Count);
                }
                var write = SaveManager.Instance.SaveDetailedAsync(oracle.saveData, oracle.GetSlotDirectoryName(oracle.CurrentSlot));
                while (!write.IsCompleted) yield return null;
                Assert.True(write.GetAwaiter().GetResult().Succeeded);
                var read = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot));
                while (!read.IsCompleted) yield return null;
                Assert.True(read.Result.Data.HeardConversations.ContainsKey(entry.id));
                Assert.False(read.Result.Data.HeardConversations.ContainsKey("story:Farm.Saplings.Birch.v1"));
            }
            finally
            {
                Object.DestroyImmediate(host);
                var presentation = Object.FindAnyObjectByType<TimelessEchoes.WorldPresentation>();
                if (presentation) Object.DestroyImmediate(presentation.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CompanionConversationOnlyUnlocksAfterOriginalMeetingFinishes()
        {
            var entry = ConversationCatalog.Load().entries.Single(e => e.id == "meeting:Gill1");
            TimelessEchoes.Tasks.TalkToNpcTask task = null;
            try
            {
                task = Object.Instantiate(entry.task);
                // This fixture has no hero for the fishing animation to face.
                foreach (var performance in task.GetComponentsInChildren<TimelessEchoes.WorldSpeakerPerformance>())
                    Object.DestroyImmediate(performance);
                task.OnArrival(null);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.False(oracle.saveData.HeardConversations.ContainsKey(entry.id));
                Object.DestroyImmediate(task.gameObject);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.False(oracle.saveData.HeardConversations.ContainsKey(entry.id), "Interrupted scenes stay locked.");
                task = Object.Instantiate(entry.task);
                foreach (var performance in task.GetComponentsInChildren<TimelessEchoes.WorldSpeakerPerformance>())
                    Object.DestroyImmediate(performance);
                task.OnArrival(null);
                yield return new WaitForSecondsRealtime(.3f);
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                var advance = meeting.GetComponent<UIDocument>().rootVisualElement.Q<Button>("advance");
                Assert.Greater(advance.worldBound.width, 0);
                Assert.True(advance.enabledInHierarchy);
                FieldsClick(advance); // Meet.
                yield return new WaitForSecondsRealtime(.2f);
                for (var i = 0; i < entry.task.Lines.Count; i++) { FieldsClick(advance); yield return new WaitForSecondsRealtime(.2f); }
                for (var i = 0; i < 120 && meeting; i++) yield return null;
                Assert.True(oracle.saveData.HeardConversations.ContainsKey(entry.id));
                Assert.True(oracle.saveData.CompletedNpcTasks.Contains("Gill1"));
            }
            finally
            {
                if (task) Object.DestroyImmediate(task.gameObject);
                var presentation = Object.FindAnyObjectByType<TimelessEchoes.WorldPresentation>();
                if (presentation) Object.DestroyImmediate(presentation.gameObject);
            }
        }

        private static void SaveConversationScreenshot(string name)
        {
            var output = Path.GetFullPath("Artifacts/CompanionConversations20261106");
            Directory.CreateDirectory(output);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
        }
    }
}
#endif
