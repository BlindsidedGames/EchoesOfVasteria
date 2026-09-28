using System;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.EditorTools
{
    /// <summary>Explicit authoring step; never switches production screens on script reload.</summary>
    public static class ToolkitBookInstallation
    {
        [MenuItem("Tools/UI Toolkit/Install Library and Credits in current scene")]
        public static void Install()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Install in Edit mode.");
            var manager = UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include);
            if (!manager) throw new InvalidOperationException("No town window manager in the current scene.");
            var target = new SerializedObject(manager);
            InstallBook(manager.transform, target, "toolkitLibrary", "Library");
            InstallBook(manager.transform, target, "toolkitCredits", "Credits");
            target.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        private static void InstallBook(Transform parent, SerializedObject manager, string field, string name)
        {
            const string root = "Assets/UI/Toolkit/";
            var screen = manager.FindProperty(field).objectReferenceValue as ToolkitBookScreen;
            if (!screen)
            {
                var go = new GameObject("Toolkit " + name);
                Undo.RegisterCreatedObjectUndo(go, "Install native " + name);
                go.transform.SetParent(parent, false);
                screen = Undo.AddComponent<ToolkitBookScreen>(go);
            }
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitBookDefinition>(root + name + ".asset");
            serialized.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(root + "Theme.asset");
            serialized.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(root + "Book.uxml");
            serialized.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "Runtime.tss");
            serialized.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(root + "TextSettings.asset");
            serialized.ApplyModifiedProperties();
            if (!screen.IsConfigured) throw new InvalidOperationException("Missing native assets for " + name);
            manager.FindProperty(field).objectReferenceValue = screen;
        }
    }
}
