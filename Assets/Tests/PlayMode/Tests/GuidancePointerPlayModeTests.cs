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
  public IEnumerator IvanReturnHandRendersAboveReturnOnDeathAcrossOrientations()
  {
   var owner=new GameObject("Ivan return guidance"); GameObject run=null;
   try {
    var manager=owner.AddComponent<TimelessEchoes.GameManager>(); manager.enabled=false;
    typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(manager,owner);
    typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(manager,false);
    Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Add("Ivan1");
    oracle.saveData.Quests.Remove("Meeting Ivan");
    run=CreateGuidanceRunView();
    var ui=run.GetComponent<UIDocument>().rootVisualElement;
    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1206,2622),new Vector2Int(2752,2064),new Vector2Int(1280,800),new Vector2Int(1024,768),new Vector2Int(1920,1080)}) {
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Ivan return guidance");
     yield return new WaitForSecondsRealtime(.3f);
     Assert.True(manager.IvanReturnPending);
     var hand=ui.Q<Image>("ivan-return-pointer"); var town=ui.Q<Button>("run-return"); var death=ui.Q<Button>("run-return-on-death");
     Assert.AreEqual(DisplayStyle.Flex,hand.resolvedStyle.display);
     if(size.x>size.y)
      Assert.That(town.worldBound.yMin,Is.LessThan(20),"Landscape return controls must use the actual screen safe area, not the former letterbox inset.");
     Assert.That(hand.worldBound.center.x,Is.EqualTo(town.worldBound.center.x).Within(1),"Keep the original hand position below Return To Town.");
     Assert.Greater(hand.worldBound.yMin,town.worldBound.yMax);
     Assert.False(hand.worldBound.Overlaps(town.worldBound),$"{size}: the hand obscures its target.");
     Assert.True(ui.worldBound.Contains(hand.worldBound.min) && ui.worldBound.Contains(hand.worldBound.max),$"{size}: the hand leaves the HUD.");
     Assert.False(hand.worldBound.Overlaps(ui.Q("run-health-strip").worldBound),$"{size}: the hand obscures health.");
     Assert.AreEqual(PickingMode.Ignore,hand.pickingMode);
     var hit=ui.panel.Pick(town.worldBound.center);
     Assert.True(hit==town || town.Contains(hit),$"{size}: Return To Town must remain clickable.");
     System.IO.Directory.CreateDirectory("Artifacts/ScreenshotStudio/ivan-guidance");
     var path=$"Artifacts/ScreenshotStudio/ivan-guidance/hand-{size.x}.png";
     ScreenCapture.CaptureScreenshot(path); yield return null;
     if(size.x>size.y) {
      var visible=new Texture2D(2,2); visible.LoadImage(System.IO.File.ReadAllBytes(path));
      hand.style.opacity=0; yield return null;
      var hiddenPath=$"Artifacts/ScreenshotStudio/ivan-guidance/hidden-{size.x}.png";
      ScreenCapture.CaptureScreenshot(hiddenPath); yield return null;
      var hidden=new Texture2D(2,2); hidden.LoadImage(System.IO.File.ReadAllBytes(hiddenPath));
      var overlap=Rect.MinMaxRect(Mathf.Max(hand.worldBound.xMin,death.worldBound.xMin),Mathf.Max(hand.worldBound.yMin,death.worldBound.yMin),Mathf.Min(hand.worldBound.xMax,death.worldBound.xMax),Mathf.Min(hand.worldBound.yMax,death.worldBound.yMax));
      var changed=0; var scale=ToolkitResponsiveLayout.Scale(size);
      for(var y=(int)(overlap.yMin/scale);y<(int)(overlap.yMax/scale);y++)
       for(var x=(int)(overlap.xMin/scale);x<(int)(overlap.xMax/scale);x++)
        if(Vector4.Distance(visible.GetPixel(x,size.y-1-y),hidden.GetPixel(x,size.y-1-y))>.1f) changed++;
      Object.DestroyImmediate(visible);Object.DestroyImmediate(hidden);hand.style.opacity=1;
      Assert.Greater(changed,10,"The hand must render in front of Return On Death, not be concealed by it.");
     }
    }
   } finally {if(run)Object.DestroyImmediate(run);Object.DestroyImmediate(owner);}
  }
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
    var hand=run.GetComponent<UIDocument>().rootVisualElement.Q<Image>("first-echo-hand");
    bool automaticObserved=false; buffs.OnBuffCast+=(_,automatic)=>automaticObserved|=automatic;
    foreach(var size in new[]{new Vector2Int(1170,2532),new Vector2Int(1920,1080)}) {
     buffs.enabled=false; buffs.ClearActiveBuffs(); automaticObserved=false;
     oracle.saveData.General.BuffTutorialStage=3;
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Autocast prompt check");
     yield return new WaitForSecondsRealtime(.2f);
     Assert.AreEqual(DisplayStyle.Flex,hand.style.display.value);
     var target=run.GetComponent<UIDocument>().rootVisualElement.Q<Button>("run-buff-0");
     var fingertip=new Vector2(hand.worldBound.xMin+hand.worldBound.width*6.5f/16,hand.worldBound.yMin+hand.worldBound.height*2f/16);
     Assert.That(fingertip.x,Is.EqualTo(target.worldBound.center.x).Within(1),"The fingertip, rather than the image box, must be centered on the ability.");
     var pointerScale=hand.worldBound.height/24;
     Assert.That(fingertip.y-target.worldBound.yMax,Is.InRange(-2*pointerScale,pointerScale),"The fingertip must touch the lower button edge, including throughout its gentle bob.");
     Assert.AreEqual(PickingMode.Ignore,hand.pickingMode,"The overlay must not intercept the ability or disabled controls beneath it.");
     buffs.enabled=true;
     yield return null; yield return null;
     Assert.True(automaticObserved,"The check must exercise the automatic casting path.");
     Assert.AreEqual(4,oracle.saveData.General.BuffTutorialStage);
     Assert.AreEqual(DisplayStyle.None,hand.style.display.value);
    }
   } finally {if(run)Object.DestroyImmediate(run);Object.DestroyImmediate(owner);}
  }
  [UnityTest]
  public IEnumerator GuidancePointerNavigationOpensStatisticsTabsAndHubQuestsAcrossLayouts()
  {
   void ClickControl(Button button) {
    using var move = PointerMoveEvent.GetPooled(new Event {type=EventType.MouseMove,mousePosition=button.worldBound.center});
    button.SendEvent(move); FieldsClick(button);
   }
   var owner = new GameObject("Protected navigation routes");
   GameObject nav = null, stats = null, quests = null, run = null;
   try {
    owner.AddComponent<TimelessEchoes.Quests.QuestManager>().enabled = false;
    owner.AddComponent<TimelessEchoes.Upgrades.ResourceManager>();
    owner.AddComponent<TimelessEchoes.Stats.GameplayStatTracker>().enabled = false;
    var manager = owner.AddComponent<TimelessEchoes.UI.TownWindowManager>(); manager.enabled = false;
    stats = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Statistics.prefab"));
    quests = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Quests.prefab"));
    typeof(TimelessEchoes.UI.TownWindowManager).GetField("toolkitStatistics",Private).SetValue(manager,stats.GetComponent<ToolkitStatisticsScreen>());
    typeof(TimelessEchoes.UI.TownWindowManager).GetField("toolkitQuests",Private).SetValue(manager,quests.GetComponent<ToolkitQuestsScreen>());
    nav=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Navigation.prefab"));
    var screen=nav.GetComponent<ToolkitNavigationScreen>();
    var root=nav.GetComponent<UIDocument>().rootVisualElement;
    foreach(var size in new[]{new Vector2Int(1170,2532),new Vector2Int(5120,2054),new Vector2Int(1170,2532)}) {
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Navigation resize check");
     yield return null;yield return null;yield return new WaitForSecondsRealtime(.15f);
     Assert.AreSame(root.Q("navigation-buttons"),root.Q<Button>("stats").parent.parent);
     Assert.AreSame(root.Q("navigation-buttons"),root.Q<Button>("options").parent.parent);
     Assert.AreEqual(DisplayStyle.None,root.Q("quests-row").resolvedStyle.display);
     foreach(var tab in new[]{0,2,3,4,5,6}) {
      ClickControl(root.Q<Button>("stats")); yield return new WaitForSecondsRealtime(.15f);
      Assert.AreEqual(ToolkitNavigationDefinition.Group.Statistics,screen.OpenGroup);
      var choice=root.Q<Button>("statistics-section-"+tab);
      Assert.AreEqual(DisplayStyle.Flex,choice.parent.resolvedStyle.display);
      ClickControl(choice); yield return new WaitForSecondsRealtime(.15f);
      var statistics=stats.GetComponent<ToolkitStatisticsScreen>();
      Assert.True(statistics.IsOpen); Assert.AreEqual(tab,statistics.SelectedTab);
      Assert.False(screen.HasOpenMenu);
      Assert.IsNull(stats.GetComponent<UIDocument>().rootVisualElement.Q("portrait-statistics-section"));
      manager.CloseAllWindows(); yield return null;
     }
     ClickControl(root.Q<Button>("hub")); yield return new WaitForSecondsRealtime(.15f);
     var questButton=root.Q<Button>("quests");
     Assert.AreEqual(DisplayStyle.Flex,questButton.parent.resolvedStyle.display);
     ClickControl(questButton); yield return new WaitForSecondsRealtime(.15f);
     Assert.True(quests.GetComponent<ToolkitQuestsScreen>().IsOpen,"A fresh slot can reach Quests under Hub.");
     manager.CloseAllWindows(); yield return null;
     Assert.True(screen.OpenHelpDestination("stats/tasks"));
     Assert.AreEqual(4,stats.GetComponent<ToolkitStatisticsScreen>().SelectedTab,"Library links still open the exact Statistics section.");
     manager.CloseAllWindows(); yield return null;
     Assert.True(screen.OpenHelpDestination("quests"));
     Assert.True(quests.GetComponent<ToolkitQuestsScreen>().IsOpen);
     manager.CloseAllWindows(); yield return null;
     foreach (var lessonTab in new[] {0,2,4}) {
      var general=oracle.saveData.General;
      general.MapStatisticsTutorialStage=lessonTab==0?3:9;
      general.GraphsViewed=lessonTab!=2; general.GraphsTutorialStage=lessonTab==2?1:2;
      general.TaskBoostTutorialStage=lessonTab==4?1:2;
      Assert.True(screen.OpenHelpDestination("stats/items")); yield return new WaitForSecondsRealtime(.15f);
      if(ToolkitResponsiveLayout.IsPortrait)
       Assert.AreEqual(DisplayStyle.Flex,root.Q("tutorial-pointer-stats").style.display.value,"Guide out of the wrong portrait Statistics page.");
      ClickControl(root.Q<Button>("stats")); yield return new WaitForSecondsRealtime(.15f);
      Assert.AreEqual(DisplayStyle.Flex,root.Q("tutorial-pointer-statistics-section-"+lessonTab).style.display.value,"The lesson must point at the correct popup entry in either layout.");
      ClickControl(root.Q<Button>("statistics-section-"+lessonTab)); yield return new WaitForSecondsRealtime(.15f);
      Assert.AreEqual(lessonTab,stats.GetComponent<ToolkitStatisticsScreen>().SelectedTab);
      manager.CloseAllWindows(); yield return null;
     }
     oracle.saveData.General.MapStatisticsTutorialStage=9; oracle.saveData.General.GraphsViewed=true;
     oracle.saveData.General.TaskBoostTutorialStage=2;
    }
    foreach(var entry in AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>("Assets/UI/Toolkit/Navigation.asset").entries)
     foreach(var rule in entry.visibility)
      if(rule.quest)oracle.saveData.Quests[rule.quest.questId]=new Blindsided.SaveData.GameData.QuestRecord{Completed=true};
    var gm=owner.AddComponent<TimelessEchoes.GameManager>(); gm.enabled=false;
    typeof(TimelessEchoes.GameManager).GetProperty("CurrentMap").SetValue(gm,owner);
    typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(gm,false);
    run=CreateGuidanceRunView();
    var expand=run.GetComponent<UIDocument>().rootVisualElement.Q<Button>("run-focus-toggle");
    foreach(var size in new[]{new Vector2Int(1170,2532),new Vector2Int(5120,2054),new Vector2Int(1170,2532)}) {
     PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Expand closes navigation state");
     yield return new WaitForSecondsRealtime(.2f);
     if(!ToolkitResponsiveLayout.IsPortrait) {
      Assert.False(ToolkitResponsiveLayout.FocusView);
      Assert.True(screen.OpenHelpDestination("quests")); yield return null;
      Assert.True(manager.HasOpenWindow,"Landscape window navigation remains available.");
      manager.CloseAllWindows(); yield return null; continue;
     }
     foreach(var route in new[]{"quests","stats/tasks"}) {
      Assert.True(screen.OpenHelpDestination(route)); yield return new WaitForSecondsRealtime(.15f);
      Assert.True(manager.HasOpenWindow);
      ClickControl(expand); yield return new WaitForSecondsRealtime(.2f);
      Assert.True(ToolkitResponsiveLayout.FocusView);
      Assert.False(manager.HasOpenWindow,"Expanding must close the window, not suspend an open selection.");
      Assert.False(screen.HasOpenMenu);
      var parent=root.Q<Button>(route=="quests"?"hub":"stats");
      Assert.False(parent.ClassListContains("active"),"The hidden window must not leave its navigation selected.");
      ClickControl(parent); yield return new WaitForSecondsRealtime(.15f);
      Assert.True(screen.HasOpenMenu,"The first tap after expanding must open the menu again.");
      ClickControl(root.Q<Button>(route=="quests"?"quests":"statistics-section-4")); yield return new WaitForSecondsRealtime(.15f);
      Assert.True(manager.HasOpenWindow,"The window must reopen on the first selection.");
      manager.CloseAllWindows(); yield return null;
     }
     foreach(var id in new[]{"adventure","hub","townsfolk","stats"}) {
      ClickControl(root.Q<Button>(id)); yield return new WaitForSecondsRealtime(.15f);
      Assert.True(screen.HasOpenMenu);
      ClickControl(expand); yield return new WaitForSecondsRealtime(.2f);
      Assert.True(ToolkitResponsiveLayout.FocusView);
      Assert.False(screen.HasOpenMenu,"Expanding must close the "+id+" popup.");
      Assert.False(root.Q<Button>(id).ClassListContains("active"));
      ClickControl(root.Q<Button>(id)); yield return new WaitForSecondsRealtime(.15f);
      Assert.True(screen.HasOpenMenu,"A single tap must reopen "+id+" after expansion.");
      ClickControl(root.Q<Button>(id)); ClickControl(expand); yield return new WaitForSecondsRealtime(.15f);
     }
    }
   } finally {
    ToolkitResponsiveLayout.SetFocusView(false);
    if(run)Object.DestroyImmediate(run); if(nav)Object.DestroyImmediate(nav); if(stats)Object.DestroyImmediate(stats); if(quests)Object.DestroyImmediate(quests); Object.DestroyImmediate(owner);
   }
  }
 }
}
#endif
