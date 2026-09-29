using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using TimelessEchoes.Buffs;
using TimelessEchoes.UI.Toolkit;
using References.UI;
using Image = UnityEngine.UI.Image;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitBuffsMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Buffs reference")]
        public static void Import()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Main Edit mode required");
            const string path = "Assets/UI/Toolkit/";
            var source = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<BuffUIManager>(FindObjectsInactive.Include));
            var row = (BuffRecipeUIReferences)source.FindProperty("recipePrefab").objectReferenceValue;
            var slot = (BuffSlotUIReferences)source.FindProperty("assignSlotButtons").GetArrayElementAtIndex(0).objectReferenceValue;
            var window = ((GameObject)source.FindProperty("buffPurchaseWindow").objectReferenceValue).transform;
            var definition = AssetDatabase.LoadAssetAtPath<ToolkitBuffsDefinition>(path + "Buffs.asset");
            if (!definition) { definition = ScriptableObject.CreateInstance<ToolkitBuffsDefinition>(); AssetDatabase.CreateAsset(definition, path + "Buffs.asset"); }
            var windowImage = window.Find("Image").GetComponent<Image>(); definition.window = windowImage.enabled ? windowImage.sprite : null;
            definition.row = row.GetComponent<Image>().sprite; definition.inset = window.Find("Image/ScrollViews").GetComponent<Image>().sprite;
            definition.slot = slot.ActivateButton.image.sprite; definition.autoCast = slot.AutoCastImage.sprite; definition.autoCastTint = slot.AutoCastImage.color;
            definition.button = row.purchaseButton.image.sprite;
            definition.instructions = ToolkitBookMigration.ImportText(window.Find("Image/Horizontal/Divider/Text (TMP)").GetComponent<TMPro.TMP_Text>(), "buff-instructions");
            EditorUtility.SetDirty(definition);
            var screenObject = new GameObject("Toolkit Buffs");
            try
            {
                var screen = screenObject.AddComponent<ToolkitBuffsScreen>(); Configure(screen, definition);
                PrefabUtility.SaveAsPrefabAsset(screenObject, path + "Buffs.prefab"); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(screenObject); }
        }
        private static void Configure(Component component, ToolkitBuffsDefinition definition)
        {
            const string path = "Assets/UI/Toolkit/"; var so = new SerializedObject(component);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path + "Theme.asset");
            so.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path + "Runtime.tss");
            so.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path + "TextSettings.asset"); so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}