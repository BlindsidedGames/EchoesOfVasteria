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
  public IEnumerator GuidancePointerEchoWalksAfterFastFishingCompletes()
  {
   var owner=new GameObject("Completed fishing locomotion"); GameObject actor=null;
   try {
    CreateGuidancePathfinder(owner);
    actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Echo.prefab"),owner.transform);
    var echo=actor.GetComponent<TimelessEchoes.Hero.EchoController>(); echo.enabled=false;
    var animator=echo.Animator;
    // A fast task can complete while the cast clip is still playing. The pulse
    // used to disappear before Fishing_Idle_Down was able to consume it.
    animator.Play("Fishing",0,0); animator.Update(.01f);
    animator.SetTrigger("CatchFish"); animator.Update(.01f); animator.ResetTrigger("CatchFish");
    TimelessEchoes.Utilities.AnimatorMovementHelper.SetMovement(animator,Vector2.right,5);
    animator.Update(.02f);
    Assert.True(animator.GetCurrentAnimatorStateInfo(0).IsName("Run"),"Completed fishing must yield to resumed movement even during the cast clip.");
    foreach(var pose in new[]{"Fishing_Idle_Down","Fishing_Catch_down"}) {
     animator.SetFloat("MoveMagnitude",0); animator.Play(pose,0,0); animator.Update(.01f);
     TimelessEchoes.Utilities.AnimatorMovementHelper.SetMovement(animator,Vector2.right,5);
     animator.Update(.02f);
     Assert.True(animator.GetCurrentAnimatorStateInfo(0).IsName("Run"),pose+" must release locomotion when the echo moves.");
    }
    yield return null;
   } finally {if(actor)Object.DestroyImmediate(actor);Object.DestroyImmediate(owner);}
  }
  [UnityTest]
  public IEnumerator GuidancePointerAutocastCompletesEchoPromptInBothLayouts()
  {
   var owner=new GameObject("Autocast tutorial check"); GameObject run=null;
   try {
    owner.AddComponent<TimelessEchoes.Upgrades.ResourceManager>();
    var tracker=owner.AddComponent<TimelessEchoes.Stats.GameplayStatTracker>(); tracker.enabled=false;
    var manager=owner.AddComponent<TimelessEchoes.GameManager>(); manager.enabled=false;
    var map=new GameObject("Protected run"); map.transform.SetParent(owner.transform);
    typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(manager,map);
    typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(manager,false);
    tracker.BeginRun(AssetDatabase.LoadAssetAtPath<TimelessEchoes.MapGeneration.MapGenerationConfig>("Assets/Scriptables/MapSettings/River.asset"),TimelessEchoes.GameManager.MapScalingMode.DistanceBased);
    var recipe=Resources.Load<TimelessEchoes.Buffs.BuffRecipe>("Buffs/Echo Combat");
    oracle.saveData.Quests[recipe.requiredQuest.questId]=new Blindsided.SaveData.GameData.QuestRecord {Completed=true};
    var buffs=owner.AddComponent<TimelessEchoes.Buffs.BuffManager>(); buffs.enabled=false;
    buffs.AssignBuff(0,recipe); buffs.UnlockAutoSlots(1);
    run=CreateGuidanceRunView();
    var arrow=run.GetComponent<UIDocument>().rootVisualElement.Q<Image>("first-echo-arrow");
    bool automaticObserved=false; buffs.OnBuffCast+=(_,automatic)=>automaticObserved|=automatic;
    foreach(var size in new[]{new Vector2Int(1170,2532),new Vector2Int(1920,1080)}) {
     buffs.enabled=false; buffs.ClearActiveBuffs(); automaticObserved=false;
     oracle.saveData.General.BuffTutorialStage=3;
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Autocast prompt check");
     yield return null; yield return null;
     Assert.AreEqual(DisplayStyle.Flex,arrow.style.display.value);
     buffs.enabled=true;
     yield return null; yield return null;
     Assert.True(automaticObserved,"The check must exercise the automatic casting path.");
     Assert.AreEqual(4,oracle.saveData.General.BuffTutorialStage);
     Assert.AreEqual(DisplayStyle.None,arrow.style.display.value);
    }
   } finally {if(run)Object.DestroyImmediate(run);Object.DestroyImmediate(owner);}
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
