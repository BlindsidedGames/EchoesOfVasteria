using System;using UnityEngine;using UnityEditor;using UnityEngine.UIElements;using TimelessEchoes.NpcGeneration;using TimelessEchoes.UI;using TimelessEchoes.UI.Toolkit;using References.UI;using Image=UnityEngine.UI.Image;
namespace TimelessEchoes.EditorTools
{
 public static class ToolkitAlterEchoesMigration
 {
  [MenuItem("Tools/UI Toolkit/Import Alter Echoes reference")]
  public static void Import()
  {
   if(Application.isPlaying)throw new InvalidOperationException("Main Edit mode required");const string path="Assets/UI/Toolkit/";
   var ui=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<AlterEchoGeneratorUIManager>(FindObjectsInactive.Include));
   var row=(AlterEchoGeneratorProgressUI)ui.FindProperty("progressUIPrefab").objectReferenceValue;var rs=new SerializedObject(row);
   var resource=(AlterEchoGeneratedResourceUIReferences)rs.FindProperty("generatedPrefab").objectReferenceValue;
   var wm=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include));var window=(GameObject)wm.FindProperty("alterEchoes").FindPropertyRelative("window").objectReferenceValue;
   var d=AssetDatabase.LoadAssetAtPath<ToolkitAlterEchoesDefinition>(path+"AlterEchoes.asset");if(!d){d=ScriptableObject.CreateInstance<ToolkitAlterEchoesDefinition>();AssetDatabase.CreateAsset(d,path+"AlterEchoes.asset");}
   d.row=row.GetComponent<Image>().sprite;d.inset=window.transform.Find("Image/ScrollViews").GetComponent<Image>().sprite;d.slot=resource.GetComponent<Image>().sprite;
   var button=(UnityEngine.UI.Button)rs.FindProperty("collectButton").objectReferenceValue;d.button=button.GetComponent<Image>().sprite;d.disabledColor=button.colors.disabledColor;
   var fill=(Blindsided.Utilities.SlicedFilledImage)rs.FindProperty("image").objectReferenceValue;d.fill=fill.sprite;d.fillColor=fill.color;d.track=fill.transform.parent.GetComponent<Image>().sprite;
   d.countColor=resource.awaitingCollectionText.color;d.preserveIconAspect=resource.iconImage.preserveAspect;EditorUtility.SetDirty(d);
   var go=new GameObject("Toolkit Alter Echoes");try{var native=new SerializedObject(go.AddComponent<ToolkitAlterEchoesScreen>());native.FindProperty("definition").objectReferenceValue=d;native.FindProperty("theme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path+"Theme.asset");native.FindProperty("runtimeTheme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path+"Runtime.tss");native.FindProperty("textSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path+"TextSettings.asset");native.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(go,path+"AlterEchoes.prefab");AssetDatabase.SaveAssets();}finally{UnityEngine.Object.DestroyImmediate(go);}
  }
 }
}
