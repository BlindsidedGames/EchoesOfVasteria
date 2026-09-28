using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitIntroMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Introduction reference")]
        public static void Import()
        {
            const string root = "Assets/UI/Toolkit/";
            var original = Object.FindAnyObjectByType<IntroScreenController>(FindObjectsInactive.Include);
            var source = new SerializedObject(original);
            var legacyRoot = (GameObject)source.FindProperty("introRoot").objectReferenceValue;
            var definition = AssetDatabase.LoadAssetAtPath<ToolkitIntroDefinition>(root + "Intro.asset");
            if (!definition)
            {
                definition = ScriptableObject.CreateInstance<ToolkitIntroDefinition>();
                AssetDatabase.CreateAsset(definition, root + "Intro.asset");
            }
            definition.width = legacyRoot.GetComponent<RectTransform>().rect.width;
            definition.countdownSeconds = source.FindProperty("countdownDurationSeconds").floatValue;
            definition.title = ToolkitBookMigration.ImportText(legacyRoot.transform.Find("Image/Text (TMP)").GetComponent<TMP_Text>(), "intro.title");
            definition.body = ToolkitBookMigration.ImportText(legacyRoot.transform.Find("Image/Text (TMP) (1)").GetComponent<TMP_Text>(), "intro.body");
            definition.close = ToolkitBookMigration.ImportText(((GameObject)source.FindProperty("clickToCloseContainer").objectReferenceValue).GetComponent<TMP_Text>(), "intro.close");
            definition.frame = legacyRoot.GetComponent<UnityEngine.UI.Image>().sprite;
            definition.progressTrack = ((GameObject)source.FindProperty("progressContainer").objectReferenceValue).GetComponent<UnityEngine.UI.Image>().sprite;
            definition.progressFill = (Sprite)new SerializedObject(source.FindProperty("progressFillImage").objectReferenceValue).FindProperty("m_Sprite").objectReferenceValue;
            EditorUtility.SetDirty(definition);
            var go = new GameObject("Toolkit Introduction");
            try
            {
                var screen = go.AddComponent<ToolkitIntroScreen>();
                var serialized = new SerializedObject(screen);
                serialized.FindProperty("definition").objectReferenceValue = definition;
                serialized.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(root + "Theme.asset");
                serialized.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(root + "Intro.uxml");
                serialized.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "Runtime.tss");
                serialized.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(root + "TextSettings.asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, root + "Intro.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
