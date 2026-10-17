#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TimelessEchoes.Gear;
using TimelessEchoes.Quests;
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
        public IEnumerator InventoryLayoutFitsWholeColumnsAndSelectedIconsCanDrag()
        {
            var owner = new GameObject("Protected inventory layout and icon gestures");
            GameObject view = null;
            try
            {
                var resources=owner.AddComponent<ResourceManager>();
                var equipment=owner.AddComponent<EquipmentController>();
                equipment.Equip(Resources.Load<QuestEquipmentReward>("Quest Rewards/Gill's Old Boots").CreateItem());
                var radish=Resources.Load<Resource>("Resource Items/Radish");
                var seed=Resources.Load<Resource>("Resource Items/Radish Seed Pack");
                resources.Add(radish,10,trackStats:false,eligibleForTierRoll:false);
                resources.Add(seed,2,trackStats:false,eligibleForTierRoll:false);
                view=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/ResourceInventory.prefab"));
                var screen=view.GetComponent<ToolkitResourceInventoryScreen>();
                screen.Bounds=new Rect(32,44,640,330); Assert.True(screen.Show());
                var ui=view.GetComponent<UIDocument>().rootVisualElement;
                var scroll=ui.Q<ScrollView>("resource-scroll");
                var title=ui.Q(className:"eov-resource-title");
                var grid=ui.Q<ToolkitGrid>();
                var selected=ui.Q<Label>("selected-resource");
                var heading=title.Q(className:"heading");
                var height=0f; var wideEquipment=0f;
                foreach(var width in new[]{640,518,420})
                {
                    screen.Bounds=new Rect(32,44,width,330);
                    for(var i=0;i<5;i++)yield return null;
                    Assert.That(title.worldBound.height,Is.EqualTo(28).Within(1));
                    Assert.That(selected.worldBound.xMin,Is.EqualTo(heading.worldBound.xMin).Within(1));
                    Assert.GreaterOrEqual(selected.worldBound.yMin,heading.worldBound.yMax-1);
                    Assert.That(scroll.worldBound.yMin-title.worldBound.yMax,Is.InRange(1f,5f));
                    Assert.That(scroll.contentViewport.contentRect.width-grid.layout.width,Is.EqualTo(0).Within(1),"The scrollbar must sit next to the whole item columns.");
                    if(height==0)height=scroll.worldBound.height;
                    else Assert.That(scroll.worldBound.height,Is.EqualTo(height).Within(1),"The name must not take height from the items.");
                    var column=ui.Q("inventory-equipment");
                    if(width==640)wideEquipment=column.layout.width;
                    else Assert.Less(column.layout.width,wideEquipment);
                }
                var radishButton=ui.Q<Button>("resource-Radish");
                FieldsClick(radishButton); yield return null;
                Assert.That(selected.text,Does.Contain(ToolkitLocalization.Name(radish)));
                var selection=selected.text;
                Assert.Greater(scroll.verticalScroller.highValue,35,"There must be enough content to exercise dragging.");
                var point=radishButton.worldBound.center;
                Assert.True(scroll.contentViewport.worldBound.Contains(point));
                using(var down=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=0,mousePosition=point}))radishButton.SendEvent(down);
                using(var move=PointerMoveEvent.GetPooled(new Event{type=EventType.MouseDrag,button=0,mousePosition=point+new Vector2(0,-35)}))
                {
                    typeof(PointerMoveEvent).GetProperty(nameof(PointerMoveEvent.pressedButtons)).SetValue(move,1);
                    radishButton.SendEvent(move);
                }
                Assert.That(scroll.scrollOffset.y,Is.EqualTo(35).Within(1),"The icon gesture must scroll before release.");
                using(var up=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=0,mousePosition=point+new Vector2(0,-35)}))radishButton.SendEvent(up);
                Assert.That(scroll.scrollOffset.y,Is.EqualTo(35).Within(1));
                Assert.AreEqual(selection,selected.text,"Dragging a selected icon must retain selection.");
                Assert.False(scroll.contentViewport.HasPointerCapture(PointerId.mousePointerId));
                Assert.False(radishButton.HasPointerCapture(PointerId.mousePointerId));
                scroll.scrollOffset=Vector2.zero; yield return null;
                FieldsClick(ui.Q<Button>("resource-Radish Seed Pack")); yield return null;
                Assert.That(selected.text,Does.Contain(ToolkitLocalization.Name(seed)),"A normal icon click must still select after a drag.");
                CaptureFeedback("inventory-fitted-columns.png");yield return null;yield return null;
            }
            finally {if(view)Object.DestroyImmediate(view);Object.DestroyImmediate(owner);}
        }
    }
}
#endif
