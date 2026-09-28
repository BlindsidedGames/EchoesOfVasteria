using System.Linq;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitMeetingMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Meeting reference")]
        public static void Import()
        {
            const string root = "Assets/UI/Toolkit/";
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CharacterMeeting.prefab");
            var definition = AssetDatabase.LoadAssetAtPath<ToolkitMeetingDefinition>(root + "Meeting.asset");
            if (!definition)
            {
                definition = ScriptableObject.CreateInstance<ToolkitMeetingDefinition>();
                AssetDatabase.CreateAsset(definition, root + "Meeting.asset");
            }
            var images = original.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            definition.portraitFrame = images.First(i => i.sprite && i.sprite.name == "UI_Frames_8").sprite;
            definition.shadow = images.First(i => i.sprite && i.sprite.name == "ButtonShadow_0").sprite;
            EditorUtility.SetDirty(definition);
            var go = new GameObject("Toolkit Character Meeting");
            try
            {
                var screen = go.AddComponent<ToolkitMeetingScreen>();
                var serialized = new SerializedObject(screen);
                serialized.FindProperty("definition").objectReferenceValue = definition;
                serialized.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(root + "Theme.asset");
                serialized.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(root + "Meeting.uxml");
                serialized.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "Runtime.tss");
                serialized.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(root + "TextSettings.asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, root + "CharacterMeeting.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
