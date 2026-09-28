using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.MapGeneration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Entry = TimelessEchoes.UI.Toolkit.ToolkitNavigationDefinition.Entry;
using Group = TimelessEchoes.UI.Toolkit.ToolkitNavigationDefinition.Group;
using NavAction = TimelessEchoes.UI.Toolkit.ToolkitNavigationDefinition.Action;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitNavigationMigration
    {
        [MenuItem("Tools/UI Toolkit/Import Navigation reference")]
        public static void Import()
        {
            const string root = "Assets/UI/Toolkit/";
            var manager = UnityEngine.Object.FindAnyObjectByType<TownWindowManager>(FindObjectsInactive.Include);
            if (!manager || Application.isPlaying) throw new InvalidOperationException("Main scene in Edit mode required.");
            var source = new SerializedObject(manager);
            var definition = AssetDatabase.LoadAssetAtPath<ToolkitNavigationDefinition>(root + "Navigation.asset");
            if (!definition) { definition = ScriptableObject.CreateInstance<ToolkitNavigationDefinition>(); AssetDatabase.CreateAsset(definition, root + "Navigation.asset"); }
            var entries = new List<Entry>();
            AddWindow("options", TownWindowManager.Window.Options, Group.Toolbar, true);
            AddWindow("stats", TownWindowManager.Window.Stats, Group.Toolbar, true);
            AddMenu("beginAdventureButton", "adventure", NavAction.AdventureMenu);
            AddMenu("hubButton", "hub", NavAction.HubMenu);
            AddMenu("townsfolkButton", "townsfolk", NavAction.TownsfolkMenu);
            AddWindow("quests", TownWindowManager.Window.Quests, Group.Toolbar);
            AddWindow("wiki", TownWindowManager.Window.Library, Group.Toolbar);
            var close = Read((Button)source.FindProperty("closeButton").objectReferenceValue, "close", Group.Toolbar, true);
            close.action = NavAction.Close; entries.Add(close);
            AddWindow("alterEchoes", TownWindowManager.Window.AlterEchoes, Group.Hub);
            AddWindow("skills", TownWindowManager.Window.Skills, Group.Hub);
            AddWindow("buffs", TownWindowManager.Window.Buffs, Group.Hub);
            AddWindow("forge", TownWindowManager.Window.Forge, Group.Townsfolk);
            AddWindow("cauldron", TownWindowManager.Window.Cauldron, Group.Townsfolk);
            var game = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include));
            var maps = game.FindProperty("generationButtons");
            var adventure = (GameObject)source.FindProperty("beginAdventureDropdown").objectReferenceValue;
            for (var i = 0; i < maps.arraySize; i++)
            {
                var item = maps.GetArrayElementAtIndex(i);
                var entry = Read((Button)item.FindPropertyRelative("button").objectReferenceValue, "map-" + i, Group.Adventure);
                entry.action = NavAction.BeginMap;
                entry.map = (MapGenerationConfig)item.FindPropertyRelative("config").objectReferenceValue;
                entry.width = ((RectTransform)adventure.transform).rect.width;
                entries.Add(entry);
            }
            definition.entries = entries.ToArray();
            definition.forgeAttention = ((GameObject)source.FindProperty("forgeAttentionObject").objectReferenceValue).GetComponent<Image>().sprite;
            definition.cauldronAttention = ((GameObject)source.FindProperty("cauldronAttentionObject").objectReferenceValue).GetComponent<Image>().sprite;
            var quests = (Button)source.FindProperty("quests").FindPropertyRelative("button").objectReferenceValue;
            definition.questAttention = quests.GetComponentsInChildren<Image>(true).First(i => i.gameObject != quests.gameObject).sprite;
            var pin = (GameObject)source.FindProperty("autoPin").objectReferenceValue;
            definition.autoPinFrame = pin.GetComponent<Image>().sprite;
            definition.autoPinLabel = ToolkitBookMigration.ImportText(pin.GetComponentInChildren<TMP_Text>(true), "navigation.auto-pin");
            var settings = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<SettingsPanelUI>(FindObjectsInactive.Include));
            definition.toggleOn = (Sprite)settings.FindProperty("onSprite").objectReferenceValue;
            definition.toggleOff = (Sprite)settings.FindProperty("offSprite").objectReferenceValue;
            var discord = (GameObject)source.FindProperty("discord").objectReferenceValue;
            definition.discord = discord.GetComponentsInChildren<Image>(true).First(i => i.gameObject != discord).sprite;
            var urlComponent = discord.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "OpenUrlButton");
            var urlProperties = new SerializedObject(urlComponent).GetIterator();
            while (urlProperties.Next(true)) if (urlProperties.propertyType == SerializedPropertyType.String && urlProperties.stringValue.StartsWith("https://")) definition.discordUrl = urlProperties.stringValue;
            var balanceManager = UnityEngine.Object.FindAnyObjectByType<NpcGeneration.AlterEchoGeneratorUIManager>(FindObjectsInactive.Include);
            var balanceText = (TMP_Text)new SerializedObject(balanceManager).FindProperty("availableResourcesText").objectReferenceValue;
            var balanceInset = balanceText.GetComponentInParent<Image>(true);
            definition.echoInset = balanceInset.sprite;
            definition.echoFrame = balanceInset.transform.parent.GetComponent<Image>().sprite;
            definition.balanceColor = balanceText.color;
            var progress = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include));
            var slider = (Slider)progress.FindProperty("distanceSlider").objectReferenceValue;
            definition.progressTrack = slider.transform.Find("Background").GetComponent<Image>().sprite;
            definition.progressFill = slider.fillRect.GetComponent<Image>().sprite;
            var progressImages = slider.GetComponentsInChildren<Image>(true);
            definition.progressHero = progressImages.Single(i => i.sprite && i.sprite.name == "Player_Main_Idle_6").sprite;
            definition.progressReaper = progressImages.Single(i => i.sprite && i.sprite.name == "Angel_2_47").sprite;
            definition.progressTextColor = ((TMP_Text)progress.FindProperty("distanceText").objectReferenceValue).color;
            EditorUtility.SetDirty(definition);
            var go = new GameObject("Toolkit Navigation");
            try
            {
                var screen = go.AddComponent<ToolkitNavigationScreen>();
                var so = new SerializedObject(screen);
                so.FindProperty("definition").objectReferenceValue = definition;
                so.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToolkitTheme>(root + "Theme.asset");
                so.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(root + "Navigation.uxml");
                so.FindProperty("runtimeTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "Runtime.tss");
                so.FindProperty("textSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(root + "TextSettings.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, root + "Navigation.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            AssetDatabase.SaveAssets();

            void AddWindow(string field, TownWindowManager.Window window, Group group, bool icon = false)
            {
                var button = (Button)source.FindProperty(field).FindPropertyRelative("button").objectReferenceValue;
                var entry = Read(button, field, group, icon); entry.action = NavAction.Window; entry.window = window; entries.Add(entry);
            }
            void AddMenu(string field, string id, NavAction action)
            {
                var entry = Read((Button)source.FindProperty(field).objectReferenceValue, id, Group.Toolbar);
                entry.action = action; entries.Add(entry);
            }
        }

        private static Entry Read(Button button, string id, Group group, bool icon = false)
        {
            if (!button) throw new InvalidOperationException("Missing original navigation button: " + id);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            var entry = new Entry
            {
                id = id, group = group, width = ((RectTransform)button.transform).rect.width,
                // The close control uses a transparent hit-area image around its visible icon.
                background = button.GetComponent<Image>().color.a > 0 ? button.GetComponent<Image>().sprite : null,
                fontSize = text ? text.fontSize : 8,
                visibility = ToolkitVisibilityImport.Read(button.transform),
                label = text ? ToolkitBookMigration.ImportText(text, "navigation." + id) : new ToolkitBookDefinition.Text()
            };
            // Newline at the end of original labels is layout whitespace, not content.
            entry.label.fallback = entry.label.fallback?.TrimEnd();
            if (icon)
            {
                var image = button.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.gameObject != button.gameObject);
                if (image) { entry.icon = image.sprite; entry.iconSize = ((RectTransform)image.transform).rect.size; entry.iconSliced = image.type == Image.Type.Sliced; }
            }
            entry.shadow = button.transform.parent.Cast<Transform>().Where(t => t != button.transform)
                .Select(t => t.GetComponent<Image>()).FirstOrDefault(i => i && i.sprite && i.sprite.name.Contains("Shadow"))?.sprite;
            return entry;
        }
    }
}
