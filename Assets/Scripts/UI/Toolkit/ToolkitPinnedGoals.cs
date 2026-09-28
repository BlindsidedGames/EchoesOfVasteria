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
  public bool IsConfigured=>definition&&theme&&runtimeTheme&&textSettings;
  private void OnEnable(){if(IsConfigured)StartCoroutine(Initialize());}
  IEnumerator Initialize(){while(!QuestManager.Instance||!GameManager.Instance||oracle==null)yield return null;manager=QuestManager.Instance;if(!settings){settings=ToolkitPanel.CreateSettings(runtimeTheme,textSettings);settings.sortingOrder=50;}var document=GetComponent<UIDocument>();document.panelSettings=settings;document.rootVisualElement.pickingMode=PickingMode.Ignore;root=new VisualElement{name="pinned-goals",pickingMode=PickingMode.Ignore};root.AddToClassList("eov-pinned-goals");theme.Apply(root);ToolkitGameplay.Apply(root,theme);document.rootVisualElement.Add(root);backdrop=new VisualElement{pickingMode=PickingMode.Ignore};backdrop.AddToClassList("eov-pinned-background");backdrop.style.backgroundColor=definition.backgroundColor;root.Add(backdrop);entries=new VisualElement{name="pinned-entries",pickingMode=PickingMode.Ignore};root.Add(entries);toggle=new Button(()=>{ShowPinnedQuests=!ShowPinnedQuests;dirty=true;Audio.AudioManager.Instance?.PlayUIButtonClick();}){name="toggle-pinned-goals"};toggle.AddToClassList("eov-pinned-toggle");root.Add(toggle);state=new Image{pickingMode=PickingMode.Ignore,scaleMode=ScaleMode.ScaleToFit};state.AddToClassList("eov-pinned-state");toggle.Add(state);manager.NoticeboardChanged+=Changed;manager.QuestProgressChanged+=ProgressChanged;Blindsided.EventHandler.OnLoadData+=Changed;LocalizationSettings.SelectedLocaleChanged+=LocaleChanged;dirty=true;}
  void Changed()=>dirty=true;void ProgressChanged(QuestData _,float __)=>dirty=true;void LocaleChanged(UnityEngine.Localization.Locale _)=>dirty=true;
  void Refresh(){entries.Clear();float width=0;foreach(var id in oracle.saveData.PinnedQuests){if(string.IsNullOrEmpty(id))continue;var data=manager.GetQuestData(id);if(data&&data.requirements.Any(r=>r!=null&&r.type==QuestData.RequirementType.Instant))continue;oracle.saveData.Quests.TryGetValue(id,out var record);var display=data?PinnedQuestPresentation.Build(data,record):(id,false);var row=new VisualElement{pickingMode=PickingMode.Ignore};row.AddToClassList("eov-pinned-row");var text=new Label(display.Item1.Replace("\r\n","\n").TrimEnd('\r','\n')){name="pinned-"+id,pickingMode=PickingMode.Ignore,enableRichText=true};text.AddToClassList("eov-pinned-text");text.style.fontSize=definition.textSize;text.style.color=definition.textColor;text.style.letterSpacing=definition.textSize*.02f;text.RegisterCallback<GeometryChangedEvent>(_=>FitText());row.Add(text);var check=new Image{sprite=definition.ready,pickingMode=PickingMode.Ignore,scaleMode=ScaleMode.ScaleToFit};check.AddToClassList("eov-pinned-ready");check.style.display=display.Item2?DisplayStyle.Flex:DisplayStyle.None;row.Add(check);entries.Add(row);width=Mathf.Max(width,text.MeasureTextSize(text.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).x);}root.schedule.Execute(FitText);entries.style.display=ShowPinnedQuests?DisplayStyle.Flex:DisplayStyle.None;state.sprite=ShowPinnedQuests?definition.expanded:definition.collapsed;backdrop.style.display=ShowPinnedQuests?DisplayStyle.Flex:DisplayStyle.None;}
  void FitText()
  {
   if(root==null)return;
   float width=0;
   foreach(var label in entries.Query<Label>().ToList())
   {
    var lines=label.text.Split('\n');float height=0, previousSize=definition.textSize;
    for(int i=0;i<lines.Length;i++)
    {
     var line=lines[i];var match=System.Text.RegularExpressions.Regex.Match(line,@"<size=(\d+(?:\.\d+)?)%>");
     var size=match.Success?definition.textSize*float.Parse(match.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)/100:definition.textSize;
     var plain=System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(line,@"<sprite[^>]*>","X"),@"<[^>]+>","");
     // MeasureTextSize omits letter spacing from its preferred width.
     var measured=label.MeasureTextSize(line,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).x;
     width=Mathf.Max(width,measured+Mathf.Max(0,plain.Length-1)*definition.textSize*.02f);
     height+=i==0?definition.ascentRatio*size:definition.lineHeightRatio*previousSize;
     if(i==lines.Length-1)height+=definition.descentRatio*size;
     previousSize=size;
    }
    label.style.height=height;
   }
   root.style.width=width;
  }
  void Update(){if(root==null)return;var visible=GameManager.Instance&&GameManager.Instance.IsMapUIVisible&&oracle!=null&&oracle.saveData.PinnedQuests.Count>0;root.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;if(!visible)return;if(dirty){dirty=false;Refresh();}var safe=ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width,Screen.height),Screen.safeArea,Application.isMobilePlatform?1:SafeAreaRatio);root.style.left=safe.xMax;root.style.top=safe.y+65;}
  void OnDisable(){StopAllCoroutines();if(manager){manager.NoticeboardChanged-=Changed;manager.QuestProgressChanged-=ProgressChanged;}Blindsided.EventHandler.OnLoadData-=Changed;LocalizationSettings.SelectedLocaleChanged-=LocaleChanged;root?.RemoveFromHierarchy();root=null;}
  void OnDestroy(){if(settings)Destroy(settings);}
 }
}
