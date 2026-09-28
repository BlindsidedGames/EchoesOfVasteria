using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using TimelessEchoes.Skills;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using References.UI;
using Image = UnityEngine.UI.Image;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitSkillsMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Skills reference")]
        public static void Import()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Main Edit mode required");
            const string path = "Assets/UI/Toolkit/";
            var ui = UnityEngine.Object.FindAnyObjectByType<SkillUIManager>(FindObjectsInactive.Include); var source = new SerializedObject(ui);
            var wm = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include));
            var window = ((GameObject)wm.FindProperty("skills").FindPropertyRelative("window").objectReferenceValue).transform;
            var d = AssetDatabase.LoadAssetAtPath<ToolkitSkillsDefinition>(path + "Skills.asset");
            if (!d) { d = ScriptableObject.CreateInstance<ToolkitSkillsDefinition>(); AssetDatabase.CreateAsset(d, path + "Skills.asset"); }
            var skills = source.FindProperty("skills"); d.skills = Enumerable.Range(0, skills.arraySize).Select(i => (Skill)skills.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var selectors = source.FindProperty("skillSelectors"); var slots = Enumerable.Range(0, selectors.arraySize).Select(i => (SkillUIReferences)selectors.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            d.icons = slots.Select(s => s.iconImage.sprite).ToArray(); d.slot = slots[0].selectButton.image.sprite; d.selection = slots[0].selectionImage.sprite; d.highlight = slots[0].highlightImage.sprite;
            d.frame = window.Find("LeftVertical/MilestoneList").GetComponent<Image>().sprite;
            d.inset = window.Find("LeftVertical/MilestoneList/SkillInfo/vert/Image").GetComponent<Image>().sprite;
            d.rightFrame = window.Find("Vertical").GetComponent<Image>().sprite;
            d.xpTrack = window.Find("LeftVertical/MilestoneList/SkillInfo/Image").GetComponent<Image>().sprite;
            d.xpFill = ((Blindsided.Utilities.SlicedFilledImage)source.FindProperty("experienceBar").objectReferenceValue).sprite;
            d.toggleOn = (Sprite)source.FindProperty("activeMilestoneToggleSprite").objectReferenceValue; d.toggleOff = (Sprite)source.FindProperty("inactiveMilestoneToggleSprite").objectReferenceValue;
            var bonus = new SerializedObject((MilestoneBonusUI)source.FindProperty("bonusUI").objectReferenceValue); var row = ((GameObject)bonus.FindProperty("entryPrefab").objectReferenceValue).GetComponent<MilestoneEntryUIReferences>();
            d.taskFrame = row.TaskImageObject.GetComponent<Image>().sprite; d.taskInset = row.TaskImage.transform.parent.GetComponent<Image>().sprite; d.lockedColor = bonus.FindProperty("lockedColor").colorValue;
            var title = window.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t => t.text == "Total skill increases"); d.totalsTitle = ToolkitBookMigration.ImportText(title, "skill-totals-title"); EditorUtility.SetDirty(d);
            var go = new GameObject("Toolkit Skills");
            try
            {
                var screen = go.AddComponent<ToolkitSkillsScreen>(); var so = new SerializedObject(screen);
                so.FindProperty("definition").objectReferenceValue = d; so.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path + "Theme.asset");
                so.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path + "Runtime.tss");
                so.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path + "TextSettings.asset"); so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, path + "Skills.prefab"); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
