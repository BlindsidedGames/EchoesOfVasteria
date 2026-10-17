#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
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
        public IEnumerator GuidanceSharedScrollingKeepsControlsIndependentAndNestedDragsLocal()
        {
            var owner = new GameObject("Protected shared scroll interaction", typeof(UIDocument));
            PanelSettings settings = null;
            try
            {
                settings = ToolkitPanel.CreateSettings(
                    AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"),
                    AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
                var document = owner.GetComponent<UIDocument>(); document.panelSettings = settings;
                var root = document.rootVisualElement;
                void Down(VisualElement target, Vector2 p)
                {
                    using var e = PointerDownEvent.GetPooled(new Event { type=EventType.MouseDown, button=0, mousePosition=p }); target.SendEvent(e);
                }
                void Move(VisualElement target, Vector2 p, bool held=true)
                {
                    using var e = PointerMoveEvent.GetPooled(new Event { type=held ? EventType.MouseDrag : EventType.MouseMove, button=0, mousePosition=p });
                    typeof(PointerMoveEvent).GetProperty(nameof(PointerMoveEvent.pressedButtons)).SetValue(e, held ? 1 : 0); target.SendEvent(e);
                }
                void Up(VisualElement target, Vector2 p)
                {
                    using var e = PointerUpEvent.GetPooled(new Event { type=EventType.MouseUp, button=0, mousePosition=p }); target.SendEvent(e);
                }
                foreach (var factory in new[] { false, true })
                {
                    var scroll = factory ? ToolkitGameplay.Scroll(root, "shared-scroll") : new ScrollView();
                    if (!factory) { ToolkitGameplay.StyleScroll(scroll); root.Add(scroll); }
                    // Run details restyle their view when switching to summary mode.
                    ToolkitGameplay.StyleScroll(scroll);
                    scroll.style.width=300; scroll.style.height=180; scroll.style.flexShrink=0;
                    var clicked=0;
                    var button = new Button(()=>clicked++) { text="Action" }; button.style.height=28; scroll.Add(button);
                    var slider = new Slider(0,1) { value=.1f }; slider.style.height=28; scroll.Add(slider);
                    var field = new TextField { value="Editable text" }; field.style.height=28; scroll.Add(field);
                    var content = new Label("Draggable content"); content.style.height=700; content.style.flexShrink=0; scroll.Add(content);
                    yield return null; yield return null;
                    scroll.scrollOffset = new Vector2(0,120); yield return null;
                    var start=scroll.contentViewport.worldBound.center;
                    Assert.True(content.worldBound.Contains(start));
                    Down(content,start); Move(content,start+new Vector2(0,-40)); Up(scroll.contentViewport,start+new Vector2(0,-40));
                    Assert.That(scroll.scrollOffset.y,Is.EqualTo(160).Within(1));
                    Assert.False(scroll.contentViewport.HasPointerCapture(PointerId.mousePointerId));
                    Move(scroll.contentViewport,start+new Vector2(0,-80),false);
                    Assert.That(scroll.scrollOffset.y,Is.EqualTo(160).Within(1));
                    scroll.scrollOffset=Vector2.zero; yield return null;
                    FieldsClick(button); Assert.AreEqual(1,clicked);
                    foreach(var control in new VisualElement[]{button,slider,field})
                    {
                        var p=control.worldBound.center;
                        Down(control,p); Move(scroll.contentViewport,p+new Vector2(0,-40)); Up(control,p+new Vector2(0,-40));
                        Assert.That(scroll.scrollOffset.y,Is.Zero.Within(1),control.GetType().Name+" must own its gesture.");
                        Assert.False(scroll.contentViewport.HasPointerCapture(PointerId.mousePointerId));
                    }
                    var tracker=slider.Q("unity-tracker"); var middle=tracker.worldBound.center;
                    Down(tracker,middle); Up(tracker,middle);
                    Assert.That(slider.value,Is.GreaterThan(.1f),"The slider must still respond to pointer input.");
                    // Closing between pointer-down and the drag threshold must cancel the pending drag.
                    scroll.scrollOffset=new Vector2(0,120); yield return null;
                    start=scroll.contentViewport.worldBound.center; Down(content,start);
                    scroll.RemoveFromHierarchy(); root.Add(scroll); yield return null; yield return null;
                    Move(content,start+new Vector2(0,-40));
                    Assert.That(scroll.scrollOffset.y,Is.EqualTo(120).Within(1));
                    Assert.False(scroll.contentViewport.HasPointerCapture(PointerId.mousePointerId));
                    Up(scroll.contentViewport,start);
                    scroll.RemoveFromHierarchy();
                }
                var outer=ToolkitGameplay.Scroll(root,"outer"); outer.style.width=300; outer.style.height=180; outer.style.flexShrink=0;
                var inner=ToolkitGameplay.Scroll(outer,"inner"); inner.style.height=100; inner.style.flexShrink=0;
                var innerText=new Label("Nested content"); innerText.style.height=700; innerText.style.flexShrink=0; inner.Add(innerText);
                var spacer=new VisualElement(); spacer.style.height=700; spacer.style.flexShrink=0; outer.Add(spacer);
                yield return null; yield return null;
                inner.scrollOffset=new Vector2(0,100); yield return null;
                var point=inner.contentViewport.worldBound.center;
                Down(innerText,point); Move(innerText,point+new Vector2(0,-40)); Up(inner.contentViewport,point+new Vector2(0,-40));
                Assert.That(inner.scrollOffset.y,Is.EqualTo(140).Within(1));
                Assert.That(outer.scrollOffset.y,Is.Zero.Within(1),"The outer viewport must not steal a nested drag.");
            }
            finally { Object.DestroyImmediate(owner); if(settings)Object.DestroyImmediate(settings); }
        }
    }
}
#endif
