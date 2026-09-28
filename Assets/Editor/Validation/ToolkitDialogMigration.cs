using System.Linq;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitDialogMigration
    {
        [MenuItem("Tools/UI Toolkit/Author Recovery dialog")]
        public static void ImportRecovery()
        {
            const string root = "Assets/UI/Toolkit/";
            var red = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Packs/Cute_Fantasy_UI/UI_Buttons.png")
                .OfType<Sprite>().Single(s => s.name == "UI_Buttons_1569");
            var go = new GameObject("Toolkit Save Recovery");
            try
            {
                var view = go.AddComponent<ToolkitDialogScreen>();
                var so = new SerializedObject(view);
                so.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(root + "Theme.asset");
                so.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(root + "Dialog.uxml");
                so.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "Runtime.tss");
                so.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(root + "TextSettings.asset");
                so.FindProperty("frame").objectReferenceValue = red;
                so.FindProperty("primarySprite").objectReferenceValue = red;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, root + "Recovery.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
