#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.EditMode
{
    public sealed class InterfaceThemeTests
    {
        [Test]
        public void DialogueAssignedInLightModeKeepsLinksReadableWhenAdvancingAndSwitchingTheme()
        {
            const string key = "EoV.Interface.LightMode";
            var existed = PlayerPrefs.HasKey(key);
            var saved = PlayerPrefs.GetInt(key);
            var original = ToolkitColorTheme.IsLight;
            ToolkitMeetingView view = null;
            try
            {
                ToolkitColorTheme.SetLight(true);
                view = new ToolkitMeetingView(
                    AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"),
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Toolkit/Meeting.uxml"), null,
                    AssetDatabase.LoadAssetAtPath<ToolkitMeetingDefinition>("Assets/UI/Toolkit/Meeting.asset"),
                    new[] { "Open <link=\"help:inventory\"><u><color=#EFB889>Inventory</color></u></link>.",
                        "Open <link=\"help:statistics/tasks\"><u><color=#EFB984>Tasks</color></u></link>." }, () => { });
                view.Advance();
                var dialogue = view.Root.Q<Label>("dialogue");
                StringAssert.DoesNotContain("#EFB889", dialogue.text, "New dialogue cannot retain the pale dark-mode link colour.");
                view.Advance();
                StringAssert.DoesNotContain("#EFB984", dialogue.text, "Each new line must use the current appearance.");
                var window = ScriptableObject.CreateInstance<ThemeWindow>();
                try
                {
                    window.Show(); window.rootVisualElement.Add(view.Root);
                    ToolkitColorTheme.SetLight(false);
                    StringAssert.Contains("#EFB984", dialogue.text, "Dark mode restores the authored colour.");
                    ToolkitColorTheme.SetLight(true);
                    StringAssert.Contains("#533039", dialogue.text);
                }
                finally { window.Close(); }
            }
            finally
            {
                view?.Dispose();
                ToolkitColorTheme.SetLight(original);
                if (existed) PlayerPrefs.SetInt(key, saved); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator SwitchingAppearanceUpdatesOpenNewAndReattachedScreensAndPersistsChoice()
        {
            const string key = "EoV.Interface.LightMode";
            var existed = PlayerPrefs.HasKey(key);
            var saved = PlayerPrefs.GetInt(key);
            var original = ToolkitColorTheme.IsLight;
            var window = ScriptableObject.CreateInstance<ThemeWindow>();
            try
            {
                window.Show();
                VisualElement Screen()
                {
                    var root = new VisualElement();
                    root.AddToClassList("gameplay"); root.AddToClassList("surface");
                    root.style.width = 100; root.style.height = 30;
                    root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Gameplay"));
                    ToolkitColorTheme.Bind(root); window.rootVisualElement.Add(root); return root;
                }
                ToolkitColorTheme.SetLight(false);
                var first = Screen();
                var help = new Label("<link=\"help:inventory\"><u><color=#EFB889>Inventory</color></u></link>");
                first.Add(help); ToolkitHelpLinks.Attach(help);
                yield return null;
                Assert.That((Color32)first.resolvedStyle.backgroundColor, Is.EqualTo(new Color32(41, 36, 38, 255)));

                ToolkitColorTheme.SetLight(true);
                var openedLater = Screen();
                yield return null;
                Assert.That((Color32)first.resolvedStyle.backgroundColor, Is.EqualTo(new Color32(213, 171, 135, 255)));
                Assert.That((Color32)openedLater.resolvedStyle.backgroundColor, Is.EqualTo(new Color32(213, 171, 135, 255)));
                Assert.That(PlayerPrefs.GetInt(key, -1), Is.EqualTo(1), "Choice must persist independently of a game save");
                StringAssert.Contains("<color=#533039>", help.text, "Authored help highlights need light-theme contrast");
                StringAssert.Contains("help:inventory", help.text);

                first.RemoveFromHierarchy();
                ToolkitColorTheme.SetLight(false);
                window.rootVisualElement.Add(first);
                yield return null;
                Assert.That((Color32)first.resolvedStyle.backgroundColor, Is.EqualTo(new Color32(41, 36, 38, 255)), "A reattached portrait/menu root must catch up");
                Assert.That((Color32)openedLater.resolvedStyle.backgroundColor, Is.EqualTo(new Color32(41, 36, 38, 255)));
                Assert.That(PlayerPrefs.GetInt(key, -1), Is.Zero);
                StringAssert.Contains("<color=#EFB889>", help.text, "Switching back restores the authored link colour");
            }
            finally
            {
                window.Close();
                ToolkitColorTheme.SetLight(original);
                if (existed) PlayerPrefs.SetInt(key, saved); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        private sealed class ThemeWindow : EditorWindow { }
    }
}
#endif
