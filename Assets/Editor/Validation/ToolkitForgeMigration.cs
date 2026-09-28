using System;using System.Collections.Generic;using System.Linq;using UnityEngine;using UnityEditor;using TimelessEchoes.Gear;using TimelessEchoes.Gear.UI;using TimelessEchoes.UI;using TimelessEchoes.UI.Toolkit;using TimelessEchoes.Upgrades;using Image=UnityEngine.UI.Image;
namespace TimelessEchoes.EditorTools
{
 public static class ToolkitForgeMigration
 {
  [MenuItem("Tools/UI Toolkit/Import Forge reference")]
  public static void Import()
  {
   if(Application.isPlaying)throw new InvalidOperationException("Main Edit mode required");const string path="Assets/UI/Toolkit/";
   var forge=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<ForgeWindowUI>(FindObjectsInactive.Include));
   var d=AssetDatabase.LoadAssetAtPath<ToolkitForgeDefinition>(path+"Forge.asset");if(!d){d=ScriptableObject.CreateInstance<ToolkitForgeDefinition>();AssetDatabase.CreateAsset(d,path+"Forge.asset");}
   var catalog=AssetDatabase.LoadAssetAtPath<ForgeCatalog>(path+"ForgeCatalog.asset");if(!catalog){catalog=ScriptableObject.CreateInstance<ForgeCatalog>();AssetDatabase.CreateAsset(catalog,path+"ForgeCatalog.asset");}
   var coreArray=forge.FindProperty("coreSlots");var bindings=new List<ForgeCatalog.CoreBinding>();var icons=new List<Sprite>();var discovered=Blindsided.Utilities.AssetCache.GetAll<CoreSO>().Where(x=>x).OrderBy(x=>x.tierIndex).ToArray();
   for(int i=0;i<coreArray.arraySize;i++){var slot=(CoreSlotUIReferences)coreArray.GetArrayElementAtIndex(i).objectReferenceValue;if(!slot)continue;bindings.Add(new(){core=slot.Core?slot.Core:discovered[i],coreResource=slot.CoreResource,ingotResource=slot.IngotResource});icons.Add(slot.CoreImage.sprite);d.slot=slot.SelectSlotButton.GetComponent<Image>().sprite;d.selector=slot.SelectionImage.sprite;}
   catalog.cores=bindings.ToArray();catalog.slime=(Resource)forge.FindProperty("slimeResource").objectReferenceValue;catalog.stone=(Resource)forge.FindProperty("stoneResource").objectReferenceValue;d.catalog=catalog;d.coreIcons=icons.ToArray();
   var slots=forge.FindProperty("gearSlots");var unknown=forge.FindProperty("unknownGearSprites");var gear=new List<ToolkitForgeDefinition.GearArt>();string[] names={"Weapon","Helmet","Chest","Boots"};
   for(int i=0;i<slots.arraySize;i++){var slot=(GearSlotUIReferences)slots.GetArrayElementAtIndex(i).objectReferenceValue;if(!slot)continue;var art=new SerializedObject(slot).FindProperty("spritesByRarity");var sprites=new Sprite[art.arraySize];for(int j=0;j<art.arraySize;j++)sprites[j]=(Sprite)art.GetArrayElementAtIndex(j).objectReferenceValue;gear.Add(new(){slot=string.IsNullOrEmpty(slot.SlotName)?names[i]:slot.SlotName,frame=slot.SelectSlotButton.GetComponent<Image>().sprite,unknown=(Sprite)unknown.GetArrayElementAtIndex(i).objectReferenceValue,rarities=sprites});}d.gear=gear.ToArray();
   var wm=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include));var root=((GameObject)wm.FindProperty("forge").FindPropertyRelative("window").objectReferenceValue).transform;
   d.panel=root.Find("Image/Mock/Forgework/Hori").GetComponent<Image>().sprite;d.inset=root.Find("Image/Mock").GetComponent<Image>().sprite;d.portrait=root.Find("Image/Mock/Ivan/Image/Image").GetComponent<Image>().sprite;
   d.button=((UnityEngine.UI.Button)forge.FindProperty("craftButton").objectReferenceValue).GetComponent<Image>().sprite;var section=(CraftSection2x1UIReferences)forge.FindProperty("craftSection").objectReferenceValue;d.arrowValid=section.validArrow;d.arrowInvalid=section.invalidArrow;
   var xp=(Blindsided.Utilities.SlicedFilledImage)forge.FindProperty("ivanXpBar").objectReferenceValue;d.xpFill=xp.sprite;d.xpTrack=xp.transform.parent.GetComponent<Image>().sprite;d.migratedHelmet=(Sprite)forge.FindProperty("migratedHelmetSprite").objectReferenceValue;
   var stats=AssetDatabase.LoadAssetAtPath<ToolkitStatisticsDefinition>(path+"Statistics.asset");d.toggleOn=stats.toggleOn;d.toggleOff=stats.toggleOff;
   EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(d);
   var go=new GameObject("Toolkit Forge");try{var native=new SerializedObject(go.AddComponent<ToolkitForgeScreen>());native.FindProperty("definition").objectReferenceValue=d;native.FindProperty("theme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path+"Theme.asset");native.FindProperty("runtimeTheme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(path+"Runtime.tss");native.FindProperty("textSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelTextSettings>(path+"TextSettings.asset");native.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(go,path+"Forge.prefab");AssetDatabase.SaveAssets();}finally{UnityEngine.Object.DestroyImmediate(go);}

  }
 }
}
