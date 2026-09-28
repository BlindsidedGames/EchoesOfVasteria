using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TMPro;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine.UIElements;
using Button=UnityEngine.UI.Button;
using Image=UnityEngine.UI.Image;
using Slider=UnityEngine.UI.Slider;
namespace TimelessEchoes.EditorTools {
 public static class ToolkitOptionsMigration {
 [MenuItem("Tools/UI Toolkit/Import Options reference")]
 public static void Import(){
 if(Application.isPlaying)throw new InvalidOperationException("Main Edit mode required");
 const string path="Assets/UI/Toolkit/";
 var settings=UnityEngine.Object.FindAnyObjectByType<SettingsPanelUI>(FindObjectsInactive.Include);var source=new SerializedObject(settings);
 var window=Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.gameObject.scene.IsValid()&&t.name=="Options_Window");
 var d=AssetDatabase.LoadAssetAtPath<ToolkitOptionsDefinition>(path+"Options.asset");if(!d){d=ScriptableObject.CreateInstance<ToolkitOptionsDefinition>();AssetDatabase.CreateAsset(d,path+"Options.asset");}
 var all=window.GetComponentsInChildren<TMP_Text>(true);var texts=new List<ToolkitBookDefinition.Text>();
 void Add(string id,TMP_Text value){if(!value)throw new Exception("Missing reference text: "+id);texts.Add(ToolkitBookMigration.ImportText(value,id));}
 TMP_Text Field(string name)=>((Component)source.FindProperty(name).objectReferenceValue).GetComponentInChildren<TMP_Text>(true);
 TMP_Text Named(string text)=>all.First(t=>t.text==text);
 Add("master-label",Named("Master"));Add("music-label",Named("Music"));Add("sfx-label",Named("SFX"));Add("mute-label",Named("BG Mute"));
 Add("width-label",Named("Maximum interface width"));Add("floating-label",Named("Floating Texts"));Add("vsync-label",Named("Vsync"));
 Add("fullscreen-label",Field("fullscreenWindowButton"));Add("windowed-label",Field("windowButton"));Add("fps-label",Field("fpsButton"));
 Add("drops-label",Field("dropTextDurationText"));Add("player-damage-label",Field("playerDamageDurationText"));Add("enemy-damage-label",Field("enemyDamageDurationText"));
 Add("import-label",Field("openImportWindowButton"));Add("export-label",Field("openExportWindowButton"));Add("folder-label",Field("openSaveFolderButton"));
 Add("credits-label",window.Find("Image/ScrollViews/Options_SV/Viewport/Content/GameObject/Credits/Credits_Button/Text (TMP)").GetComponent<TMP_Text>());Add("language-label",window.Find("Image/ScrollViews/Options_SV/Viewport/Content/GameObject/Credits/Language/Text (TMP)").GetComponent<TMP_Text>());
 Add("version-label",((GameObject)source.FindProperty("VersionNumberObject").objectReferenceValue).GetComponent<TMP_Text>());
 Add("autosave-label",all.First(t=>t.text.StartsWith("Auto save happens")));
 for(int i=0;i<3;i++){var slot=(SaveSlotReferences)source.FindProperty("saveSlot"+(i+1)).objectReferenceValue;Add("slot-"+i+"-title",slot.fileNameText);Add("slot-"+i+"-playtime",slot.playtimeText);Add("slot-"+i+"-date",slot.lastPlayedText);Add("save-"+i+"-label",slot.saveButton.GetComponentInChildren<TMP_Text>(true));Add("load-"+i+"-label",slot.loadDeleteText);if(i==0)d.inset=slot.playtimeText.transform.parent.GetComponent<Image>().sprite;}
 var audio=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<AudioSettingsUI>(FindObjectsInactive.Include));var slider=(Slider)audio.FindProperty("masterSlider").objectReferenceValue;
 d.frame=slider.transform.parent.parent.parent.GetComponent<Image>().sprite;
 foreach(var safe in UnityEngine.Object.FindObjectsByType<Blindsided.Utilities.ScreenSafeArea>(FindObjectsInactive.Include)){var image=(Image)new SerializedObject(safe).FindProperty("previewImage").objectReferenceValue;if(image)d.widthPreview=image.sprite;}
 d.sliderTrack=slider.transform.Find("Background").GetComponent<Image>().sprite;d.sliderFill=slider.fillRect.GetComponent<Image>().sprite;d.sliderHandle=slider.handleRect.GetComponent<Image>().sprite;
 var importRoot=((GameObject)source.FindProperty("importPanel").objectReferenceValue).transform;
 var exportRoot=((GameObject)source.FindProperty("exportPanel").objectReferenceValue).transform;
 d.dialogFrame=importRoot.GetComponent<Image>().sprite;d.close=importRoot.Find("Horizontal/Close Button").GetComponent<Image>().sprite;
 Add("import-title",importRoot.Find("Horizontal/Text (TMP)").GetComponent<TMP_Text>());Add("export-title",exportRoot.Find("Horizontal/Text (TMP)").GetComponent<TMP_Text>());
 Add("paste-label",Field("pasteImportClipboardButton"));Add("copy-label",Field("copyExportButton"));Add("import-confirm-label",Field("importConfirmButton"));
 Add("input-placeholder",(TMP_Text)((TMP_InputField)source.FindProperty("importInput").objectReferenceValue).placeholder);
 var language=Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.gameObject.scene.IsValid()&&t.name=="Language Selector");
 Add("locale-en-label",language.GetComponentsInChildren<Button>(true).Single(b=>b.name=="English").GetComponentInChildren<TMP_Text>(true));
 Add("locale-ru-label",language.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Russian").GetComponentInChildren<TMP_Text>(true));
 var warning=Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.gameObject.scene.IsValid()&&t.name=="Russian Partial Translation warning");
 Add("translation-warning-label",warning.GetComponentsInChildren<TMP_Text>(true).First(t=>!string.IsNullOrEmpty(t.text)));
 d.button=((Button)source.FindProperty("fullscreenWindowButton").objectReferenceValue).GetComponent<Image>().sprite;d.toggleOn=(Sprite)source.FindProperty("onSprite").objectReferenceValue;d.toggleOff=(Sprite)source.FindProperty("offSprite").objectReferenceValue;d.texts=texts.ToArray();EditorUtility.SetDirty(d);
 var go=new GameObject("Toolkit Options");try{var screen=go.AddComponent<ToolkitOptionsScreen>();
 var dialogObject=new GameObject("Toolkit Options Dialogs");var dialogs=dialogObject.AddComponent<ToolkitOptionsDialogs>();
 var ds=new SerializedObject(dialogs);ds.FindProperty("definition").objectReferenceValue=d;ds.FindProperty("theme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path+"Theme.asset");ds.FindProperty("runtimeTheme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path+"Runtime.tss");ds.FindProperty("textSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path+"TextSettings.asset");ds.ApplyModifiedPropertiesWithoutUndo();var dialogPrefab=PrefabUtility.SaveAsPrefabAsset(dialogObject,path+"OptionsDialogs.prefab").GetComponent<ToolkitOptionsDialogs>();UnityEngine.Object.DestroyImmediate(dialogObject);
 var so=new SerializedObject(screen);so.FindProperty("dialogsPrefab").objectReferenceValue=dialogPrefab;so.FindProperty("definition").objectReferenceValue=d;so.FindProperty("theme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path+"Theme.asset");so.FindProperty("runtimeTheme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path+"Runtime.tss");so.FindProperty("textSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path+"TextSettings.asset");so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(go,path+"Options.prefab");AssetDatabase.SaveAssets();}finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 }
}

