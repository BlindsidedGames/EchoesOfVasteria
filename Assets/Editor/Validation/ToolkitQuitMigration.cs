using System;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitQuitMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Quit reference")]
        public static void Import()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit mode required.");
            const string path = "Assets/UI/Toolkit/";
            var original = UnityEngine.Object.FindAnyObjectByType<QuitGameButton>(FindObjectsInactive.Include);
            if (!original) throw new InvalidOperationException("Main scene required.");
            var source = new SerializedObject(original);
            var quit = (Button)source.FindProperty("quitButton").objectReferenceValue;
            var confirm = (Button)source.FindProperty("exitButton").objectReferenceValue;
            var cancel = confirm.transform.parent.Find("Supply_Button (1)").GetComponent<Button>();
            var definition = AssetDatabase.LoadAssetAtPath<ToolkitQuitDefinition>(path + "Quit.asset");
            if (!definition) { definition = ScriptableObject.CreateInstance<ToolkitQuitDefinition>(); AssetDatabase.CreateAsset(definition, path + "Quit.asset"); }
            definition.quit = quit.GetComponent<Image>().sprite;
            definition.confirm = confirm.GetComponent<Image>().sprite;
            definition.cancel = cancel.GetComponent<Image>().sprite;
            definition.confirmText = ToolkitBookMigration.ImportText(confirm.GetComponentInChildren<TMP_Text>(), "quit.confirm");
            definition.cancelText = ToolkitBookMigration.ImportText(cancel.GetComponentInChildren<TMP_Text>(), "quit.cancel");
            EditorUtility.SetDirty(definition);
            var go = new GameObject("Toolkit Quit");
            try
            {
                var screen = go.AddComponent<ToolkitQuitScreen>();
                var data = new SerializedObject(screen);
                data.FindProperty("definition").objectReferenceValue = definition;
                data.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(path + "Theme.asset");
                data.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path + "Runtime.tss");
                data.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(path + "TextSettings.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, path + "Quit.prefab"); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
