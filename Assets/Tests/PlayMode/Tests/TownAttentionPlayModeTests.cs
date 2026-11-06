#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Shop;
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
        [UnityTest]
        public IEnumerator TownDropdownShowsDestinationAttentionAndShopOnlyRemindsBelowThirtyMinutes()
        {
            var owner = new GameObject("Town notification owner");
            if (ShopService.Instance) Object.DestroyImmediate(ShopService.Instance.gameObject);
            var shopObject = new GameObject("Town notification shop");
            GameObject navigationObject = null;
            try
            {
                var shop = shopObject.AddComponent<ShopService>(); shop.enabled = false;
                typeof(ShopService).GetField("ledger", Private).SetValue(shop, new ShopLedger(Path.Combine(root, "shop.json")));
                var windows = owner.AddComponent<TownWindowManager>(); windows.enabled = false;
                var general = oracle.saveData.General;
                general.ReturningPlayerIntroductionComplete = true;
                general.HubTutorialStage = 3; general.InventoryTutorialStage = 3;
                general.MapStatisticsTutorialStage = 9; general.TaskBoostTutorialStage = 2;
                general.GraphsTutorialStage = 2; general.GraphsViewed = true; general.BuffTutorialStage = 4;
                foreach (var entry in AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset").entries)
                    foreach (var rule in entry.visibility)
                    {
                        if (rule.quest) oracle.saveData.Quests[rule.quest.questId] = new GameData.QuestRecord { Completed = true };
                        if (!string.IsNullOrEmpty(rule.npcId)) oracle.saveData.CompletedNpcTasks.Add(rule.npcId);
                    }
                navigationObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
                var navigation = navigationObject.GetComponent<ToolkitNavigationScreen>();
                var ui = navigationObject.GetComponent<UIDocument>().rootVisualElement;
                TownWindowManager.ShowForgeAttention(); TownWindowManager.ShowCauldronAttention();
                service.State.Beds["notification-test"] = new FarmBedState { BatchId = "ready", DurationSeconds = 1, ElapsedSeconds = 1 };
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1080, 1920) })
                {
                    PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Town attention review");
                    yield return new WaitForSecondsRealtime(.3f);
                    navigation.Refresh(); yield return null;
                    if (!navigation.HasOpenMenu) FieldsClick(ui.Q<Button>("townsfolk"));
                    yield return new WaitForSecondsRealtime(.3f);
                    foreach (var id in new[] { "forge", "cauldron", "farm" })
                    {
                        var button = ui.Q<Button>(id);
                        var attention = button.Q<Image>(className: "eov-navigation-attention");
                        Assert.NotNull(attention, id);
                        Assert.Greater(button.worldBound.width, 0, id + " is unlocked and visible.");
                        Assert.AreEqual(DisplayStyle.Flex, attention.resolvedStyle.display, id);
                        Assert.True(button.worldBound.Contains(attention.worldBound.center), id + " icon stays within its button.");
                    }
                    CaptureFeedback("town-attention-" + size.x + ".png"); yield return null; yield return null;
                    FieldsClick(ui.Q<Button>("townsfolk"));
                }
                TownWindowManager.ClearForgeAttention(); TownWindowManager.ClearCauldronAttention();
                service.State.Beds.Clear(); navigation.Refresh(); yield return null;
                foreach (var id in new[] { "forge", "cauldron", "farm" })
                    Assert.AreEqual(DisplayStyle.None, ui.Q<Button>(id).Q<Image>(className: "eov-navigation-attention").resolvedStyle.display);
                foreach (var seconds in new[] { 1799d, 1800d, 5400d, 7199d })
                {
                    oracle.saveData.Shop.GatheringSeconds = seconds; navigation.Refresh(); yield return null;
                    Assert.AreEqual(seconds < 1800 ? DisplayStyle.Flex : DisplayStyle.None, ui.Q<Image>("hub-shop-attention").resolvedStyle.display);
                    Assert.True(ShopService.Instance.CanClaim);
                }
                oracle.saveData.Shop.GatheringSeconds = 7190;
                Assert.True(shop.Claim(), "The final claim fills even a small amount of free capacity.");
                Assert.AreEqual(7200, shop.Remaining);
                Assert.False(shop.Claim(), "A full bank cannot accept another claim.");
            }
            finally
            {
                if (navigationObject) Object.DestroyImmediate(navigationObject);
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(shopObject);
            }
        }
    }
}
#endif
