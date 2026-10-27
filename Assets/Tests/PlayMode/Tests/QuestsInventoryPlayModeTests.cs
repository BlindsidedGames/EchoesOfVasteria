#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        // Resource links leave Quests for the regular Inventory in both layouts.
        // The ordinary inventory test separately owns durable seed-credit publication.
        [UnityTest]
        public IEnumerator QuestResourceLinksOpenStandaloneInventoryAcrossLayouts()
        {
            var seed = Resources.Load<Resource>("Resource Items/Radish Seed Pack");
            var radish = Resources.Load<Resource>("Resource Items/Radish");
            Assert.NotNull(radish); Assert.NotNull(seed);
            oracle.saveData.Resources[radish.name] = new GameData.ResourceEntry { Amount = 12500, Earned = true, Tier = 3 };
            oracle.saveData.Resources[seed.name] = new GameData.ResourceEntry { Amount = 14, Earned = true, Tier = 1 };
            var tierResources = AssetDatabase.LoadAssetAtPath<ToolkitResourceInventoryDefinition>("Assets/UI/Toolkit/ResourceInventory.asset")
                .resources.Where(r => r && r != seed && r != radish && !r.DisableAlterEcho).Take(8).ToArray();
            Assert.AreEqual(8, tierResources.Length);
            var authored = AssetDatabase.LoadAssetAtPath<ToolkitResourceInventoryDefinition>("Assets/UI/Toolkit/ResourceInventory.asset").resources;
            var discovered = new HashSet<Resource>(tierResources) { seed, radish };
            void AssertDiscoveryOrder(VisualElement view)
            {
                var cells = view.Q<ToolkitGrid>().Children().OrderBy(e => Mathf.Round(e.worldBound.yMin)).ThenBy(e => e.worldBound.xMin).ToArray();
                var expected = authored.Where(discovered.Contains).Concat(authored.Where(r => !discovered.Contains(r))).Select(r => "resource-" + r.name);
                CollectionAssert.AreEqual(expected, cells.Select(e => e.name), "Discovered items lead; both groups retain the authored order.");
            }
            for (var i = 0; i < tierResources.Length; i++)
                oracle.saveData.Resources[tierResources[i].name] = new GameData.ResourceEntry { Amount = 50 + i, Earned = true, Tier = i + 1 };
            var resourceOwner = new GameObject("Isolated quests inventory data");
            var questOwner = new GameObject("Isolated quests owner");
            var windowOwner = new GameObject("Isolated quests navigation");
            GameObject inventoryObject = null, questsObject = null;
            try
            {
                var resources = resourceOwner.AddComponent<ResourceManager>();
                questOwner.AddComponent<QuestManager>();
                inventoryObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/ResourceInventory.prefab"));
                questsObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
                var hub = inventoryObject.GetComponent<ToolkitResourceInventoryScreen>();
                var quests = questsObject.GetComponent<ToolkitQuestsScreen>();
                var windows = windowOwner.AddComponent<TownWindowManager>(); windows.enabled = false;
                typeof(TownWindowManager).GetField("toolkitResources", Private).SetValue(windows, hub);
                typeof(TownWindowManager).GetField("toolkitQuests", Private).SetValue(windows, quests);
                var questReference = typeof(TownWindowManager).GetField("quests", Private).GetValue(windows);
                questReference.GetType().GetField("openInventory").SetValue(questReference, true);
                windows.OpenWindow(TownWindowManager.Window.Quests);
                yield return null; yield return null;
                var ui = questsObject.GetComponent<UIDocument>().rootVisualElement;
                var hubUi = inventoryObject.GetComponent<UIDocument>().rootVisualElement;
                foreach (var portrait in new[] { false, true })
                {
                    PlayModeWindow.SetCustomRenderingResolution(portrait ? 1080u : 1920u, portrait ? 1920u : 1080u, "Quest resource navigation");
                    yield return null; yield return null;
                    if (!quests.IsOpen) windows.OpenWindow(TownWindowManager.Window.Quests);
                    yield return null; yield return null;
                    Assert.IsNull(ui.Q("quests-inventory"), "Quests must not retain a companion inventory.");
                    Assert.False(hub.IsOpen);
                    Assert.True(windows.TryHighlightNativeResource(seed, true));
                    yield return null; yield return null;
                    Assert.False(quests.IsOpen, "A resource link leaves Quests rather than embedding another panel.");
                    Assert.True(hub.IsOpen);
                    Assert.NotNull(hubUi.Q<Button>("inventory-close"));
                    Assert.AreEqual(ToolkitLocalization.Text("inventory.resource-tier", "{0} - Tier {1}", ToolkitLocalization.Name(seed), 1), hubUi.Q<Label>("selected-resource").text);
                }
                PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Inventory quality review");
                yield return null; yield return null;
                AssertDiscoveryOrder(hubUi);
                var newlyFound = authored.Last(r => !discovered.Contains(r)); discovered.Add(newlyFound);
                resources.Add(newlyFound, 1, trackStats: false, eligibleForTierRoll: false);
                yield return null; yield return null; AssertDiscoveryOrder(hubUi);
                resources.Spend(newlyFound, resources.GetAmount(newlyFound)); yield return null; AssertDiscoveryOrder(hubUi);
                resources.Add(seed, 2, trackStats: false, eligibleForTierRoll: false); yield return null;
                Assert.AreEqual("16", Regex.Replace(hubUi.Q<Button>("resource-Radish Seed Pack").Q<Label>().text, "<[^>]*>", ""));
                Assert.True(resources.Spend(seed, 1)); yield return null;
                Assert.AreEqual("15", Regex.Replace(hubUi.Q<Button>("resource-Radish Seed Pack").Q<Label>().text, "<[^>]*>", ""));
                var hubSeedButton = hubUi.Q<Button>("resource-Radish Seed Pack");
                var hubIconBounds = hubSeedButton.Q<Image>().worldBound;
                var hubCountBounds = hubSeedButton.Q<Label>().worldBound;
                hubSeedButton.Focus();
                using (var key = KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None)) hubSeedButton.SendEvent(key);
                using (var submit = NavigationSubmitEvent.GetPooled()) hubSeedButton.SendEvent(submit);
                yield return null;
                Assert.AreEqual(ToolkitLocalization.Text("inventory.resource-tier", "{0} - Tier {1}", ToolkitLocalization.Name(seed), 1), hubUi.Q<Label>("selected-resource").text,
                    "Keyboard submission must select the standalone inventory's resource.");
                Assert.AreSame(hubSeedButton, hubSeedButton.panel.focusController.focusedElement);
                Assert.AreEqual(hubIconBounds, hubSeedButton.Q<Image>().worldBound);
                Assert.AreEqual(hubCountBounds, hubSeedButton.Q<Label>().worldBound);
                CaptureFeedback("hub-inventory-keyboard-focus.png"); yield return null; yield return null;
                hubSeedButton.Blur(); yield return null;
                CaptureFeedback("hub-inventory-independent.png"); yield return null; yield return null;
                var qualityResource = tierResources[4];
                Assert.True(windows.TryHighlightNativeResource(qualityResource, true)); yield return null; yield return null;
                yield return CaptureQualityStates(hubUi.Q<Button>("resource-" + qualityResource.name), "hub");
                {
                    var qualityRoot = hubUi;
                    var qualityColours = new HashSet<Color>();
                    for (var i = 0; i < tierResources.Length; i++)
                    {
                        Assert.IsTrue(windows.TryHighlightNativeResource(tierResources[(i + 1) % tierResources.Length], true)); yield return null;
                        var slot = qualityRoot.Q<Button>("resource-" + tierResources[i].name);
                        var expected = InventoryQualityColour(slot);
                        Assert.IsTrue(qualityColours.Add(expected), "Each quality tier must retain its distinct colour.");
                        var marker = slot.Q(className: "eov-resource-quality");
                        if (marker != null)
                        {
                            Assert.Greater(marker.worldBound.width, 10);
                            Assert.GreaterOrEqual(marker.worldBound.height, .5f);
                            Assert.AreEqual(slot.LocalToWorld(slot.contentRect.min).x, marker.worldBound.xMin, .01f,
                                "Quality must reach the left inside edge of the slot frame.");
                            Assert.AreEqual(slot.LocalToWorld(slot.contentRect.max).x, marker.worldBound.xMax, .01f,
                                "Quality must reach the right inside edge without covering the frame.");
                            Assert.AreEqual(slot.LocalToWorld(slot.contentRect.max).y, marker.worldBound.yMax, .01f,
                                "Quality must meet the bottom inside edge without covering the frame.");
                        }
                        var iconBounds = slot.Q<Image>().layout;
                        var countBounds = slot.Q<Label>().layout;
                        Assert.AreEqual(i + 1, ResourceManager.Instance.GetTier(tierResources[i]));
                        slot.Focus();
                        using (var key = KeyDownEvent.GetPooled('\0', KeyCode.LeftShift, EventModifiers.Shift)) slot.SendEvent(key);
                        yield return null;
                        Assert.AreEqual(expected, InventoryQualityColour(slot), "Unselected keyboard focus must preserve quality.");
                        slot.Blur();
                        Assert.IsTrue(windows.TryHighlightNativeResource(tierResources[i], true)); yield return null;
                        using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = slot.worldBound.center })) slot.SendEvent(down);
                        using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = slot.worldBound.center })) slot.SendEvent(up);
                        yield return null;
                        Assert.AreEqual(expected, InventoryQualityColour(slot), "Selection and pointer hover must preserve tier " + (i + 1) + " colour.");
                        slot.Focus();
                        using (var key = KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None)) slot.SendEvent(key);
                        using (var submit = NavigationSubmitEvent.GetPooled()) slot.SendEvent(submit);
                        yield return null;
                        Assert.AreEqual(expected, InventoryQualityColour(slot), "Keyboard focus must preserve tier " + (i + 1) + " colour.");
                        AssertInventoryContentBounds(iconBounds, slot.Q<Image>().layout);
                        AssertInventoryContentBounds(countBounds, slot.Q<Label>().layout);
                        slot.Blur(); yield return null;
                    }
                }
            }
            finally
            {
                if (questsObject) Object.DestroyImmediate(questsObject);
                if (inventoryObject) Object.DestroyImmediate(inventoryObject);
                Object.DestroyImmediate(windowOwner); Object.DestroyImmediate(questOwner); Object.DestroyImmediate(resourceOwner);
            }
        }

        private static IEnumerator CaptureQualityStates(Button slot, string context)
        {
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = slot.worldBound.center })) slot.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = slot.worldBound.center })) slot.SendEvent(up);
            yield return null;
            // Allow the newly visible inventory sprites and atlas to finish rendering.
            yield return null; yield return null;
            slot.Focus();
            using (var key = KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None)) slot.SendEvent(key);
            using (var submit = NavigationSubmitEvent.GetPooled()) slot.SendEvent(submit);
            yield return null;
            CaptureFeedback(context + "-quality-focused.png"); yield return null; yield return null;
            slot.Blur();
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = slot.worldBound.center })) slot.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = slot.worldBound.center })) slot.SendEvent(up);
            yield return null; yield return null; yield return null;
            CaptureFeedback(context + "-quality-selected.png"); yield return null; yield return null;
        }

        private static Color InventoryQualityColour(Button slot) => slot.Q(className: "eov-resource-quality") is VisualElement marker
            ? marker.resolvedStyle.backgroundColor : slot.resolvedStyle.borderBottomColor;

        private static void AssertInventoryContentBounds(Rect before, Rect after)
        {
            // UI Toolkit rounds relayout to physical pixels. Allow one screen
            // pixel at the shared 432-unit reference height, not a visible slot shift.
            var pixelRoundingTolerance = 432f / Mathf.Max(1, Screen.height) + .001f;
            Assert.AreEqual(before.x, after.x, pixelRoundingTolerance);
            Assert.AreEqual(before.y, after.y, pixelRoundingTolerance);
            Assert.AreEqual(before.width, after.width, pixelRoundingTolerance);
            Assert.AreEqual(before.height, after.height, pixelRoundingTolerance);
        }
    }
}
#endif
