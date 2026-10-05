#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;

namespace Tests.EditMode
{
    public sealed class InventoryNavigationTests
    {
        [Test]
        public void AuthoredMenusOfferInventoryInHubAndFieldsInTownsfolk()
        {
            var navigation = AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset");
            var inventory = navigation.entries.Single(e => e.id == "inventory");
            Assert.AreEqual(ToolkitNavigationDefinition.Group.Hub, inventory.group);
            Assert.AreEqual(ToolkitNavigationDefinition.Action.Window, inventory.action);
            Assert.AreEqual(TownWindowManager.Window.Inventory, inventory.window);
            var fields = navigation.entries.Single(e => e.id == "farm");
            Assert.AreEqual(ToolkitNavigationDefinition.Group.Townsfolk, fields.group);
            Assert.AreEqual(TownWindowManager.Window.Farm, fields.window);
            Assert.IsTrue(fields.visibility.Any(), "Fields retains its authored discovery gate.");
        }
    }
}
#endif
