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
        // The real navigation boundary owns embedding and disposal; the existing
        // ordinary inventory test separately owns durable seed-credit publication.
        [UnityTest]
        public IEnumerator QuestsInventoryStaysEmbeddedAndHubOwnsAnIndependentView()
        {
            var seed = Resources.Load<Resource>("Resource Items/Radish Seed Pack");
            var radish = Resources.Load<Resource>("Resource Items/Radish");
            Assert.NotNull(radish); Assert.NotNull(seed);
            oracle.saveData.Resources[radish.name] = new GameData.ResourceEntry { Amount = 12500, Earned = true, Tier = 3 };
            oracle.saveData.Resources[seed.name] = new GameData.ResourceEntry { Amount = 14, Earned = true, Tier = 1 };
            var tierResources = AssetDatabase.LoadAssetAtPath<ToolkitResourceInventoryDefinition>("Assets/UI/Toolkit/ResourceInventory.asset")
                .resources.Where(r => r && r != seed && r != radish && !r.DisableAlterEcho).Take(8).ToArray();
            Assert.AreEqual(8, tierResources.Length);
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
                var host = ui.Q("quests-inventory");
                Assert.NotNull(host, "Quests must own its inventory inside the Quests layout, rather than open the Hub window beside/behind it.");
                Assert.IsFalse(hub.IsOpen, "Opening Quests must not open or move the standalone Hub view.");
                var embedded = host.Q("resource-inventory");
                Assert.NotNull(embedded); Assert.IsNull(embedded.Q<Button>("inventory-close"));
                var grid = embedded.Q<ToolkitGrid>();
                Assert.GreaterOrEqual(grid.Columns, 5, "The companion must retain a usable compact grid instead of two oversized standalone columns.");
                Assert.LessOrEqual(embedded.worldBound.xMax, ui.Q("quests").worldBound.xMax + 1);
                Assert.Greater(embedded.worldBound.height, 100);
                Assert.AreEqual(ToolkitLocalization.Text("inventory.resource-tier-tooltip", "{0} · Tier {1}", ToolkitLocalization.Name(radish), 3),
                    embedded.Q<Button>("resource-Radish").tooltip);
                Assert.IsTrue(windows.TryHighlightNativeResource(seed, true));
                Assert.IsFalse(hub.IsOpen, "Highlighting a Quests resource must select its embedded inventory, not open Hub.");
                Assert.AreEqual(ToolkitLocalization.Text("inventory.resource-tier", "{0} - Tier {1}", ToolkitLocalization.Name(seed), 1), embedded.Q<Label>("selected-resource").text);
                Assert.AreEqual(26f, embedded.Q<Button>("resource-Radish Seed Pack").resolvedStyle.width, .01f);
                Assert.AreEqual(32f, embedded.Q<Button>("resource-Radish Seed Pack").resolvedStyle.height, .01f);
                var count = embedded.Q<Button>("resource-Radish Seed Pack").Q<Label>();
                Assert.AreEqual("14", Regex.Replace(count.text, "<[^>]*>", ""));
                Assert.AreSame(seed.icon, embedded.Q<Button>("resource-Radish Seed Pack").Q<Image>().sprite);
                yield return null; // Apply the normal highlight scroll before visual capture.
                var seedButton = embedded.Q<Button>("resource-Radish Seed Pack");
                var originalBounds = seedButton.worldBound;
                var originalIconBounds = seedButton.Q<Image>().worldBound;
                var originalCountBounds = count.worldBound;
                var radishButton = embedded.Q<Button>("resource-Radish");
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = radishButton.worldBound.center })) radishButton.SendEvent(down);
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = radishButton.worldBound.center })) radishButton.SendEvent(up);
                Assert.AreEqual(ToolkitLocalization.Text("inventory.resource-tier", "{0} - Tier {1}", ToolkitLocalization.Name(radish), 3), embedded.Q<Label>("selected-resource").text,
                    "Pointer selection must update the embedded inventory's resource details.");
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = seedButton.worldBound.center })) seedButton.SendEvent(down);
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = seedButton.worldBound.center })) seedButton.SendEvent(up);
                yield return null;
                Assert.AreEqual(originalBounds, seedButton.worldBound, "Changing selection must not move or resize a slot.");
                Assert.AreEqual(originalIconBounds, seedButton.Q<Image>().worldBound, "Selecting a slot must not offset its icon.");
                Assert.AreEqual(originalCountBounds, count.worldBound, "Selecting a slot must not offset its quantity.");
                CaptureFeedback("quests-inventory-embedded.png"); yield return null; yield return null;

                // Concurrent views share resource changes, never their visual tree.
                hub.Bounds = new Rect(64, 44, 640, 376); Assert.IsTrue(hub.Show());
                yield return null;
                var hubUi = inventoryObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreNotSame(embedded, hubUi.Q("resource-inventory"));
                Assert.AreSame(host, embedded.parent);
                resources.Add(seed, 2, trackStats: false, eligibleForTierRoll: false); yield return null;
                Assert.AreEqual("16", Regex.Replace(count.text, "<[^>]*>", ""));
                Assert.AreEqual("16", Regex.Replace(hubUi.Q<Button>("resource-Radish Seed Pack").Q<Label>().text, "<[^>]*>", ""));
                Assert.IsTrue(resources.Spend(seed, 1)); yield return null;
                Assert.AreEqual("15", Regex.Replace(count.text, "<[^>]*>", ""));
                Assert.AreEqual("15", Regex.Replace(hubUi.Q<Button>("resource-Radish Seed Pack").Q<Label>().text, "<[^>]*>", ""));
                hub.Hide(); Assert.IsNotNull(host.Q("resource-inventory"));
                windows.OpenWindow(TownWindowManager.Window.Inventory); yield return null; yield return null;
                Assert.IsFalse(quests.IsOpen); Assert.IsTrue(hub.IsOpen);
                Assert.NotNull(hubUi.Q<Button>("inventory-close"));
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
                windows.CloseAllWindows(); Assert.IsFalse(hub.IsOpen);
                windows.OpenWindow(TownWindowManager.Window.Quests); yield return null;
                Assert.NotNull(ui.Q("quests-inventory").Q<Button>("resource-Radish Seed Pack"));
                Assert.IsFalse(hub.IsOpen);
                var qualityResource = tierResources[4];
                Assert.IsTrue(windows.TryHighlightNativeResource(qualityResource, true)); yield return null; yield return null;
                var qualitySlot = ui.Q("quests-inventory").Q<Button>("resource-" + qualityResource.name);
                yield return CaptureQualityStates(qualitySlot, "quests");
                windows.OpenWindow(TownWindowManager.Window.Inventory); yield return null; yield return null;
                Assert.IsTrue(windows.TryHighlightNativeResource(qualityResource, true)); yield return null; yield return null;
                yield return CaptureQualityStates(hubUi.Q<Button>("resource-" + qualityResource.name), "hub");
                for (var context = 0; context < 2; context++)
                {
                    if (context == 1) { windows.OpenWindow(TownWindowManager.Window.Quests); yield return null; yield return null; }
                    var qualityRoot = context == 0 ? hubUi : ui.Q("quests-inventory");
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
            // Scroll relayout can change floating-point rounding by far less than a pixel.
            Assert.AreEqual(before.x, after.x, .01f);
            Assert.AreEqual(before.y, after.y, .01f);
            Assert.AreEqual(before.width, after.width, .01f);
            Assert.AreEqual(before.height, after.height, .01f);
        }
    }
}
#endif
