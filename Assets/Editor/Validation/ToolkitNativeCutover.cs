using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Tasks;
using TimelessEchoes.Hero;
using TimelessEchoes.Enemies;
namespace TimelessEchoes.EditorTools {
public static class ToolkitNativeCutover {
 const string Base="Assets/UI/Toolkit/";
 static readonly HashSet<string> Legacy=new(){"BuffUIManager","ForgeWindowUI","ForgeStatsUIController","AlterEchoGeneratorProgressUI","AlterEchoGeneratorUIManager","PinnedQuestUIManager","QuestUIManager","QuestEntryUI","QuestPinUI","SkillUIManager","MilestoneBonusUI","AudioSettingsUI","ButtonClickSfx","ButtonToggleGroup","EnemyStatsPanelUI","GeneralStatsPanelUI","ItemStatsPanelUI","LeaderboardsPanelUI","LeaderboardEntryUI","MapUI","QuestButtonIndicator","QuitGameButton","RunBarUI","RunCalebUIManager","RunStatsPanelUI","SettingsPanelUI","StatSortButton","StatSortingManager","TabPanelController","TaskStatsPanelUI","WikiUIToggle","ResourceInventoryUI","RunBreakdownManager","ProspectorPicker","QuantumConsole","IntroScreen","ScreenSafeArea","OpenUrlButton","ShowAchievementsButton"};
 static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value){var so=new SerializedObject(obj);var p=so.FindProperty(field);if(p==null)throw new Exception(field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static T Host<T>(Transform parent,string name) where T:Component {var t=UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);if(!t){var g=new GameObject(name);g.transform.SetParent(parent,false);t=g.AddComponent<T>();}Set(t,"theme",AssetDatabase.LoadAssetAtPath<ToolkitTheme>(Base+"Theme.asset"));Set(t,"runtimeTheme",AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Base+"Runtime.tss"));Set(t,"textSettings",AssetDatabase.LoadAssetAtPath<PanelTextSettings>(Base+"TextSettings.asset"));return t;}
 static int ImportWorld(GameObject root){int count=0;foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)){
 if(canvas.renderMode!=RenderMode.WorldSpace)continue;
 bool world=canvas.GetComponentInParent<HealthBase>()||canvas.GetComponentInParent<ContinuousTask>()||canvas.GetComponentInParent<EchoController>()||canvas.name=="Reap Line"||canvas.name=="HealthBarCanvas";
 if(!world)continue;
 var a=canvas.GetComponent<ToolkitWorldAnchor>();var prior=a?a.elements.ToDictionary(e=>e.anchor,e=>e.enabled):new Dictionary<RectTransform,bool>();if(!a)a=canvas.gameObject.AddComponent<ToolkitWorldAnchor>();a.health=canvas.GetComponentInParent<HealthBase>();a.enemy=canvas.GetComponentInParent<Enemy>();a.task=canvas.GetComponentInParent<ContinuousTask>();a.echo=canvas.GetComponentInParent<EchoController>();
 var elements=new List<ToolkitWorldAnchor.Element>();foreach(var graphic in canvas.GetComponentsInChildren<Graphic>(true)){
 var e=new ToolkitWorldAnchor.Element{anchor=graphic.rectTransform,rowGroup=graphic.transform.parent.name=="Grid Layout"?graphic.transform.parent as RectTransform:null,color=graphic.color,enabled=prior.TryGetValue(graphic.rectTransform,out var on)?on:(graphic is Blindsided.Utilities.SlicedFilledImage || graphic.enabled)};
 if(graphic is Blindsided.Utilities.SlicedFilledImage fill){e.sprite=fill.sprite;e.sliced=true;
 if(fill.name=="EchoLifetime"||fill.transform.parent.name=="EchoLifetime")e.value=ToolkitWorldAnchor.Value.EchoFill;else if(canvas.name!="Reap Line")e.value=a.task?ToolkitWorldAnchor.Value.TaskFill:ToolkitWorldAnchor.Value.HealthFill;
 }else if(graphic is UnityEngine.UI.Image im){e.sprite=im.sprite;e.sliced=im.type==UnityEngine.UI.Image.Type.Sliced||im is Blindsided.Utilities.SlicedFilledImage;
 
 }else if(graphic is TMPro.TMP_Text text){e.text=text.text;e.fontSize=text.fontSize;if(text.name=="lvl")e.value=ToolkitWorldAnchor.Value.EnemyTitle;else if(text.name=="Hp"&&canvas.name!="Reap Line")e.value=ToolkitWorldAnchor.Value.EnemyHealth;}
 else continue;elements.Add(e);
 }a.elements=elements.ToArray();count++;
 }return count;}
 static int Disable(GameObject root){int count=0;foreach(var b in root.GetComponentsInChildren<Behaviour>(true)){
 bool old=(b is Animator && b.GetComponentInParent<Canvas>())||b is Canvas||b is Graphic||b is GraphicRaycaster||b is CanvasScaler||b is UnityEngine.UI.Selectable||b is UnityEngine.UI.LayoutGroup||b is ContentSizeFitter||b is AspectRatioFitter||b is UnityEngine.UI.Mask||b is RectMask2D||Legacy.Contains(b.GetType().Name);
 if(old && b.enabled){b.enabled=false;EditorUtility.SetDirty(b);count++;}
 }return count;}
 static void SaveSceneClean(UnityEngine.SceneManagement.Scene scene){var before=File.ReadAllText(scene.path);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);var after=File.ReadAllText(scene.path);
 // Unity 6 rewrites cached tile animation values on save. Preserve those unrelated values.
 var original=Regex.Matches(before,@"(?m)^--- !u!1839735485 &[\s\S]*?(?=^--- !u!|\z)").Cast<Match>().ToDictionary(m=>m.Value.Split('\n')[0],m=>m.Value);
 after=Regex.Replace(after,@"(?m)^--- !u!1839735485 &[\s\S]*?(?=^--- !u!|\z)",m=>{if(!original.TryGetValue(m.Value.Split('\n')[0],out var old))return m.Value;return Regex.Replace(m.Value,@"(?m)^  m_AnimationSpeed:.*$",Regex.Match(old,@"(?m)^  m_AnimationSpeed:.*$").Value);});
 if(after!=File.ReadAllText(scene.path)){File.WriteAllText(scene.path,after);AssetDatabase.ImportAsset(scene.path);EditorSceneManager.OpenScene(scene.path);}}
 public static string Install(){if(Application.isPlaying)throw new Exception("Exit Play mode");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="Main")throw new Exception("Main required");if(scene.isDirty)throw new Exception("Preserve unsaved scene changes first");Directory.CreateDirectory("Library/UITKCutoverBackup");if(!File.Exists("Library/UITKCutoverBackup/Main.unity"))File.Copy(scene.path,"Library/UITKCutoverBackup/Main.unity");
 var parent=UnityEngine.Object.FindAnyObjectByType<TownWindowManager>().transform;var notices=Host<ToolkitNotificationScreen>(parent,"Toolkit Notifications");foreach(var source in UnityEngine.Object.FindObjectsByType<ResourceTierUpPopupUI>(FindObjectsInactive.Include))Set(source,"nativeView",notices);Host<ToolkitLoadingScreen>(parent,"Toolkit Loading");Host<ToolkitWorldScreen>(parent,"Toolkit World Indicators");Host<ToolkitConsoleScreen>(parent,"Toolkit Console");var backdrop=Host<ToolkitBackdropScreen>(parent,"Toolkit Window Backdrop");var background=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include).First(i=>AnimationUtility.CalculateTransformPath(i.transform,null)=="Canvas/SafeArea/UnifiedWindow/BG");Set(backdrop,"background",background.sprite);
 int world=0,disabled=0,prefabs=0;var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
 // Author all world bindings before disabling the source graphics, including nested prefabs.
 foreach(var path in paths){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset.GetComponentsInChildren<Canvas>(true).Any(c=>c.renderMode==RenderMode.WorldSpace))continue;var g=PrefabUtility.LoadPrefabContents(path);try{int n=ImportWorld(g);if(n>0){world+=n;PrefabUtility.SaveAsPrefabAsset(g,path);}}finally{PrefabUtility.UnloadPrefabContents(g);}}
 foreach(var path in paths){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset.GetComponentInChildren<Graphic>(true) && !asset.GetComponentInChildren<Canvas>(true))continue;var g=PrefabUtility.LoadPrefabContents(path);try{int n=Disable(g);if(n>0){disabled+=n;prefabs++;PrefabUtility.SaveAsPrefabAsset(g,path);}}finally{PrefabUtility.UnloadPrefabContents(g);}}
 foreach(var g in scene.GetRootGameObjects()){world+=ImportWorld(g);disabled+=Disable(g);}SaveSceneClean(scene);
 var loading=EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity");if(!File.Exists("Library/UITKCutoverBackup/Loading.unity"))File.Copy(loading.path,"Library/UITKCutoverBackup/Loading.unity");Host<ToolkitLoadingScreen>(null,"Toolkit Loading");foreach(var g in loading.GetRootGameObjects())disabled+=Disable(g);SaveSceneClean(loading);EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");AssetDatabase.SaveAssets();return $"Native cutover: {world} world bindings; {disabled} legacy components disabled; {prefabs} prefabs updated; native loading/notifications installed.";
 }
}}
