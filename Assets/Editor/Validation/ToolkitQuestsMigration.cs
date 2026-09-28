using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using TimelessEchoes.Quests;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using Image = UnityEngine.UI.Image;
namespace TimelessEchoes.EditorTools
{
 public static class ToolkitQuestsMigration
 {
  [MenuItem("Tools/UI Toolkit/Import Quests reference")]
  public static void Import()
  {
   if(Application.isPlaying) throw new InvalidOperationException("Main Edit mode required");
   const string path="Assets/UI/Toolkit/";
   var ui=UnityEngine.Object.FindAnyObjectByType<QuestUIManager>(FindObjectsInactive.Include);var source=new SerializedObject(ui);
   var wm=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include));
   var window=((GameObject)wm.FindProperty("quests").FindPropertyRelative("window").objectReferenceValue).transform;
   var d=AssetDatabase.LoadAssetAtPath<ToolkitQuestsDefinition>(path+"Quests.asset");
   if(!d){d=ScriptableObject.CreateInstance<ToolkitQuestsDefinition>();AssetDatabase.CreateAsset(d,path+"Quests.asset");}
   var row=(QuestEntryUI)source.FindProperty("questEntryPrefab").objectReferenceValue;
   d.inset=window.Find("Image/ScrollViews").GetComponent<Image>().sprite;
   d.category=ui.questCategoryPrefab.transform.Find("vert/Title").GetComponent<Image>().sprite;
   d.row=row.questImage.sprite;d.button=row.turnInButton.image.sprite;
   d.progressFill=row.requirementSlotPrefab.progressBar.sprite;
   d.progressTrack=row.requirementSlotPrefab.progressBar.transform.parent.GetComponent<Image>()?.sprite;
   var divider=(GameObject)source.FindProperty("dividerPrefab").objectReferenceValue;
   d.instructions=ToolkitBookMigration.ImportText(divider.GetComponentInChildren<TMPro.TMP_Text>(true),"quest-pinning-instructions");
   EditorUtility.SetDirty(d);var go=new GameObject("Toolkit Quests");
   try {
    var screen=go.AddComponent<ToolkitQuestsScreen>();var so=new SerializedObject(screen);
    so.FindProperty("definition").objectReferenceValue=d;so.FindProperty("theme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path+"Theme.asset");
    so.FindProperty("runtimeTheme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path+"Runtime.tss");
    so.FindProperty("textSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path+"TextSettings.asset");so.ApplyModifiedPropertiesWithoutUndo();
    PrefabUtility.SaveAsPrefabAsset(go,path+"Quests.prefab");AssetDatabase.SaveAssets();
   } finally {UnityEngine.Object.DestroyImmediate(go);}
  }
 }
}
