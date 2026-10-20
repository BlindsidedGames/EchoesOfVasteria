#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
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
  public IEnumerator GuidancePointerMeetingReceivesClicksAcrossOrientations()
  {
   var owner=new GameObject("Meeting input check");
   var events=owner.AddComponent<UnityEngine.EventSystems.EventSystem>();
   owner.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); GameObject nav=null, meeting=null, queued=null, run=null;
   try {
    owner.AddComponent<TownWindowManager>().enabled=false;
    nav=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
    run=CreateGuidanceRunView();
    meeting=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/CharacterMeeting.prefab"));
    var screen=meeting.GetComponent<ToolkitMeetingScreen>();
    var sprite=AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset").progressHero;
    screen.Init(sprite,new[]{"First line","Second line"},()=>{});
    queued=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/CharacterMeeting.prefab"));
    queued.GetComponent<ToolkitMeetingScreen>().Init(sprite,new[]{"Queued line"},()=>{});
    foreach(var size in new[]{new Vector2Int(5120,2054),new Vector2Int(1170,2532),new Vector2Int(1920,1080)}) {
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Meeting input check");
     yield return null; yield return null; yield return new WaitForSecondsRealtime(.15f);
     var doc=meeting.GetComponent<UIDocument>();var button=doc.rootVisualElement.Q<Button>("advance");
     Assert.AreEqual(DisplayStyle.None,queued.GetComponent<UIDocument>().rootVisualElement.Q(className:"eov-meeting").resolvedStyle.display,"The next conversation must not cover this action.");
     var point=button.worldBound.center;
     foreach(var other in Object.FindObjectsByType<UIDocument>()) {
      if(!other.panelSettings || other.panelSettings.sortingOrder <= doc.panelSettings.sortingOrder)continue;
      var hit=other.rootVisualElement.panel?.Pick(point);
      Assert.IsNull(hit,$"{size}: {other.name} intercepts Meet at {point}: {hit?.name} ({hit?.GetType().Name})");
     }
     var physical = point / ToolkitResponsiveLayout.Scale(size);
     var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
     events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position=new Vector2(physical.x,size.y-physical.y) },hits);
     Assert.IsNotEmpty(hits,$"{size}: no event-system raycast for Meet");
     var top=hits[0].module as UnityEngine.UIElements.PanelRaycaster;
     Assert.NotNull(top,$"{size}: another UI system intercepts Meet: {hits[0].gameObject}");
     Assert.AreSame(doc.rootVisualElement.panel,top.panel,$"{size}: another panel intercepts Meet: {hits[0].gameObject}");
     var ownHit=doc.rootVisualElement.panel.Pick(point);
     Assert.True(ownHit==button || button.Contains(ownHit),$"{size}: Meet does not own its clickable point: {ownHit?.name}");
     using(var down=PointerDownEvent.GetPooled(new Event {type=EventType.MouseDown,button=0,mousePosition=point})) ownHit.SendEvent(down);
     using(var up=PointerUpEvent.GetPooled(new Event {type=EventType.MouseUp,button=0,mousePosition=point})) ownHit.SendEvent(up);
     yield return null;
    }
    Assert.False(meeting, "Meet, Next and Close must advance the conversation through pointer events.");
    Assert.AreEqual(DisplayStyle.Flex,queued.GetComponent<UIDocument>().rootVisualElement.Q(className:"eov-meeting").resolvedStyle.display);
   } finally { if(queued)Object.DestroyImmediate(queued); if(meeting)Object.DestroyImmediate(meeting); if(nav)Object.DestroyImmediate(nav); if(run)Object.DestroyImmediate(run); Object.DestroyImmediate(owner); }
  }
  [UnityTest]
  public IEnumerator GuidancePointerEchoRetargetingLeavesFishingPose()
  {
   var owner=new GameObject("Echo fishing cleanup"); GameObject actor=null;
   try {
    CreateGuidancePathfinder(owner);
    actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Echo.prefab"),owner.transform);
    var echo=actor.GetComponent<TimelessEchoes.Hero.EchoController>(); echo.enabled=false;
    var fish=new GameObject("Current fish");fish.transform.SetParent(owner.transform);
    var task=fish.AddComponent<TimelessEchoes.Tasks.FishingTask>();
    echo.SetTask(task); task.OnArrival(echo); echo.Animator.Update(.1f);
    Assert.True(echo.Animator.GetCurrentAnimatorStateInfo(0).IsName("Fishing"));
    echo.SetTask(null); echo.Animator.Update(.1f);
    Assert.False(echo.Animator.GetCurrentAnimatorStateInfo(0).IsName("Fishing"),"An abandoned fishing task must not retain its cast pose while the echo moves elsewhere.");
    Assert.IsNull(task.ClaimedBy);
    yield return null;
   } finally {if(actor)Object.DestroyImmediate(actor);Object.DestroyImmediate(owner);}
  }
  [UnityTest]
  public IEnumerator GuidancePointerLandscapeKeepsUtilityIconsWithNavigation()
  {
   var nav=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
   try {
    foreach(var size in new[]{new Vector2Int(1170,2532),new Vector2Int(5120,2054)}) {
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Navigation resize check");
     yield return null;yield return null;yield return new WaitForSecondsRealtime(.15f);
    }
    var root=nav.GetComponent<UIDocument>().rootVisualElement;
    Assert.AreSame(root.Q("navigation-buttons"),root.Q<Button>("stats").parent.parent);
    Assert.AreSame(root.Q("navigation-buttons"),root.Q<Button>("options").parent.parent);
   } finally {Object.DestroyImmediate(nav);}
  }
 }
}
#endif
