using Screen = UnityEngine.Device.Screen;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;
using TimelessEchoes.Quests;
using static Blindsided.Oracle;
using static Blindsided.SaveData.StaticReferences;
namespace TimelessEchoes.UI.Toolkit {
 [RequireComponent(typeof(UIDocument))]
 public sealed class ToolkitPinnedGoals : MonoBehaviour {
  [SerializeField] private ToolkitPinnedGoalsDefinition definition;
  [SerializeField] private ToolkitTheme theme;
  [SerializeField] private ThemeStyleSheet runtimeTheme;
  [SerializeField] private PanelTextSettings textSettings;
  PanelSettings settings;VisualElement root,entries,backdrop;Button toggle;Image state;QuestManager manager;bool dirty=true;
  public static float PortraitBottom { get; private set; }
  ToolkitRunScreen runScreen;
  VisualElement portraitCard, portraitText; Label portraitPage;
  readonly List<string> portraitIds = new();
  string selectedId; bool? previousPortrait; int pointerId = -1; Vector2 pointerStart;
  public bool IsConfigured=>definition&&theme&&runtimeTheme&&textSettings;
  private void OnEnable(){
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;if(IsConfigured)StartCoroutine(Initialize());}
  IEnumerator Initialize(){while(!QuestManager.Instance||!GameManager.Instance||oracle==null)yield return null;manager=QuestManager.Instance;if(!settings){settings=ToolkitPanel.CreateSettings(runtimeTheme,textSettings);settings.sortingOrder=50;}var document=GetComponent<UIDocument>();document.panelSettings=settings;document.rootVisualElement.pickingMode=PickingMode.Ignore;root=new VisualElement{name="pinned-goals",pickingMode=PickingMode.Ignore};root.AddToClassList("eov-pinned-goals");theme.Apply(root);ToolkitGameplay.Apply(root,theme);document.rootVisualElement.Add(root);backdrop=new VisualElement{pickingMode=PickingMode.Ignore};backdrop.AddToClassList("eov-pinned-background");root.Add(backdrop);entries=new VisualElement{name="pinned-entries",pickingMode=PickingMode.Ignore};root.Add(entries);toggle=new Button(()=>{ShowPinnedQuests=!ShowPinnedQuests;dirty=true;Audio.AudioManager.Instance?.PlayUIButtonClick();}){name="toggle-pinned-goals"};toggle.AddToClassList("eov-pinned-toggle");root.Add(toggle);state=new Image{pickingMode=PickingMode.Ignore,scaleMode=ScaleMode.ScaleToFit};state.AddToClassList("eov-pinned-state");toggle.Add(state);runScreen = FindAnyObjectByType<ToolkitRunScreen>();BuildPortraitCard();manager.NoticeboardChanged+=Changed;manager.QuestProgressChanged+=ProgressChanged;Blindsided.EventHandler.OnLoadData+=Changed;LocalizationSettings.SelectedLocaleChanged+=LocaleChanged;dirty=true;}
  void Changed()=>dirty=true;void ProgressChanged(QuestData _,float __)=>dirty=true;void LocaleChanged(UnityEngine.Localization.Locale _)=>dirty=true;
  void Refresh(){RefreshPortrait();if(ToolkitResponsiveLayout.IsPortrait)return;entries.Clear();foreach(var id in oracle.saveData.PinnedQuests){if(string.IsNullOrEmpty(id))continue;var data=manager.GetQuestData(id);if(data&&data.requirements.Any(r=>r!=null&&r.type==QuestData.RequirementType.Instant))continue;oracle.saveData.Quests.TryGetValue(id,out var record);var display=data?PinnedQuestPresentation.Build(data,record):(id,false);var row=new VisualElement{pickingMode=PickingMode.Ignore};row.AddToClassList("eov-pinned-row");var content=new VisualElement {pickingMode=PickingMode.Ignore};
    row.Add(content);
    if(data) PopulateQuestText(content,data,record,definition.textSize,true);
    else content.Add(new Label(id){pickingMode=PickingMode.Ignore});
    var check=new Image{sprite=definition.ready,pickingMode=PickingMode.Ignore,scaleMode=ScaleMode.ScaleToFit};check.AddToClassList("eov-pinned-ready");check.style.display=display.Item2?DisplayStyle.Flex:DisplayStyle.None;row.Add(check);entries.Add(row);}root.schedule.Execute(FitText);entries.style.display=ShowPinnedQuests?DisplayStyle.Flex:DisplayStyle.None;state.sprite=ShowPinnedQuests?definition.expanded:definition.collapsed;backdrop.style.display=ShowPinnedQuests?DisplayStyle.Flex:DisplayStyle.None;}
  void PopulateQuestText(VisualElement content, QuestData data, Blindsided.SaveData.GameData.QuestRecord record, float size, bool rightAligned)
  {
   content.Clear();
   var display = PinnedQuestPresentation.Build(data, record);
   var resources = new Queue<QuestData.Requirement>(data.requirements.Where(r => r != null && r.type == QuestData.RequirementType.Resource));
   foreach(var line in display.text.Replace("\r\n","\n").TrimEnd('\r','\n').Split('\n'))
   {
    if(line.Contains("<sprite") && resources.Count > 0)
    {
     var requirement = resources.Dequeue();
     var resourceRow = new ToolkitQuestRequirementRow(requirement.resource, size, rightAligned);
     resourceRow.SetText(line); content.Add(resourceRow);
    }
    else
    {
     var label = new Label(line) { pickingMode=PickingMode.Ignore, enableRichText=true };
     label.style.fontSize=size;
     label.style.unityTextAlign=rightAligned?TextAnchor.MiddleRight:TextAnchor.MiddleLeft;
     content.Add(label);
    }
   }
  }
  void FitText()
  {
   if(root==null||ToolkitResponsiveLayout.IsPortrait)return;
   float width=0;
   foreach(var label in entries.Query<Label>().ToList())
   {
    var measured=label.MeasureTextSize(label.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).x;
    var icon=label.parent.Q<Image>(className:"quest-resource-icon");
    if(icon!=null && icon.resolvedStyle.display!=DisplayStyle.None) measured+=icon.style.width.value.value+icon.style.marginRight.value.value;
    width=Mathf.Max(width,measured);
   }
   root.style.width=width;
  }
  void BuildPortraitCard()
  {
   portraitCard = new VisualElement { name = "portrait-pinned-quest" };
   portraitCard.AddToClassList("portrait-pinned-card");
   portraitText = new VisualElement { name = "portrait-pinned-text", pickingMode = PickingMode.Ignore };
   portraitText.AddToClassList("portrait-pinned-text");
   portraitPage = new Label { name = "portrait-pinned-page", pickingMode = PickingMode.Ignore };
   portraitPage.AddToClassList("portrait-pinned-page");
   portraitCard.Add(portraitText); portraitCard.Add(portraitPage); root.Add(portraitCard);
   portraitCard.RegisterCallback<PointerDownEvent>(evt => {
    if(evt.button != 0 || pointerId >= 0) return;
    pointerId = evt.pointerId; pointerStart = evt.position;
    portraitCard.CapturePointer(pointerId); evt.StopPropagation();
   });
   portraitCard.RegisterCallback<PointerUpEvent>(evt => {
    if(evt.pointerId != pointerId) return;
    var delta = (Vector2)evt.position - pointerStart;
    var id = pointerId; pointerId = -1; portraitCard.ReleasePointer(id);
    if(Mathf.Abs(delta.x) >= 32 && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
    {
     if(portraitIds.Count > 1) {
      var index = Mathf.Max(0, portraitIds.IndexOf(selectedId));
      selectedId = portraitIds[(index + (delta.x < 0 ? 1 : portraitIds.Count - 1)) % portraitIds.Count];
      RefreshPortrait();
     }
    }
    else if(delta.sqrMagnitude < 100)
     {
     TownWindowManager.Instance?.OpenQuest(selectedId);
    }
    evt.StopPropagation();
   });
   portraitCard.RegisterCallback<PointerCaptureOutEvent>(_ => pointerId = -1);
   portraitCard.RegisterCallback<PointerCancelEvent>(_ => pointerId = -1);
  }
  void RefreshPortrait()
  {
   portraitIds.Clear();
   foreach(var id in oracle.saveData.PinnedQuests) {
    if(string.IsNullOrEmpty(id))continue;
    var quest = manager.GetQuestData(id);
    if(quest && !quest.requirements.Any(r => r != null && r.type == QuestData.RequirementType.Instant)) portraitIds.Add(id);
   }
   if(!portraitIds.Contains(selectedId)) selectedId = portraitIds.FirstOrDefault();
   if(selectedId == null) { portraitText.Clear(); portraitPage.text = ""; return; }
   var data = manager.GetQuestData(selectedId);
   oracle.saveData.Quests.TryGetValue(selectedId, out var record);
   PopulateQuestText(portraitText, data, record, 11, false);
   portraitPage.text = portraitIds.Count > 1 ? $"{portraitIds.IndexOf(selectedId)+1} / {portraitIds.Count}" : "";
  }
  void Update()
  {
   PortraitBottom = 0;
   if(root == null)return;
   var portrait = ToolkitResponsiveLayout.IsPortrait;
   if(previousPortrait != portrait) { previousPortrait = portrait; dirty = true; }
   if(dirty && oracle != null) { dirty = false; Refresh(); }
   var visible = GameManager.Instance && GameManager.Instance.IsMapUIVisible && oracle != null && oracle.saveData.PinnedQuests.Count > 0;
   if(portrait) visible &= !ToolkitResponsiveLayout.FocusView && ShowPinnedQuests && portraitIds.Count > 0 &&
    !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) &&
    !ToolkitMeetingScreen.HasVisibleMeeting && !(runScreen && (runScreen.HasOpenDetails || runScreen.HasOpenHeroStats)) &&
    !GameManager.Instance.HeroIsDead && !GameManager.Instance.FirstReapDialoguePending;
   root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
   if(!visible)return;
   portraitCard.style.display = portrait ? DisplayStyle.Flex : DisplayStyle.None;
   entries.style.display = !portrait && ShowPinnedQuests ? DisplayStyle.Flex : DisplayStyle.None;
   toggle.style.display = portrait ? DisplayStyle.None : DisplayStyle.Flex;
   backdrop.style.display = !portrait && ShowPinnedQuests ? DisplayStyle.Flex : DisplayStyle.None;
   var safe = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width,Screen.height),Screen.safeArea,UnityEngine.Device.Application.isMobilePlatform?1:SafeAreaRatio);
   root.style.translate = portrait ? new Translate(0,0) : StyleKeyword.Null;
   root.style.left = portrait ? 12 : safe.xMax;
   root.style.top = portrait ? ToolkitResponsiveLayout.WorldArea.yMax + 92 : safe.y + 65;
   root.style.maxWidth = portrait ? ToolkitResponsiveLayout.Size.x - 24 : StyleKeyword.None;
   if(portrait) { root.style.width = ToolkitResponsiveLayout.Size.x - 24; PortraitBottom = root.worldBound.yMax; }
   else FitText();
  }
  void OnDisable(){PortraitBottom=0;StopAllCoroutines();if(manager){manager.NoticeboardChanged-=Changed;manager.QuestProgressChanged-=ProgressChanged;}Blindsided.EventHandler.OnLoadData-=Changed;LocalizationSettings.SelectedLocaleChanged-=LocaleChanged;root?.RemoveFromHierarchy();root=null;}
  void OnDestroy(){if(settings)Destroy(settings);}
 }
}
