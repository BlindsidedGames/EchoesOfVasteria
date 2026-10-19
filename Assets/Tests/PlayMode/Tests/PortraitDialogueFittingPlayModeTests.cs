#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PortraitDialogueKeepsFittingWhenTextTypographyAndViewportChange()
        {
            PlayModeWindow.SetCustomRenderingResolution(1080, 2339, "Portrait dialogue fitting");
            yield return null; yield return null;
            var host = new GameObject("Isolated portrait dialogue", typeof(UIDocument));
            PanelSettings panel = null;
            ToolkitMeetingView view = null;
            try
            {
                var theme = AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset");
                panel = ToolkitPanel.CreateSettings(
                    AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"),
                    AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
                var document = host.GetComponent<UIDocument>(); document.panelSettings = panel;
                view = new ToolkitMeetingView(theme,
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Toolkit/Meeting.uxml"), null,
                    AssetDatabase.LoadAssetAtPath<ToolkitMeetingDefinition>("Assets/UI/Toolkit/Meeting.asset"),
                    new[] { "Hello.", "Gather resources while exploring the river. Return to town when you are ready to craft equipment. Your inventory keeps the resources you gathered, and different maps offer different materials." }, () => { });
                document.rootVisualElement.Add(view.Root);
                view.LayoutPortrait(true);
                var dialogue = view.Root.Q<Label>("dialogue");
                dialogue.style.fontSize = 10;
                view.Advance();
                yield return SettleDialogue(view, 320);
                var shortHeight = view.Root.resolvedStyle.height;
                view.Advance();
                yield return SettleDialogue(view, 320);
                var longHeight = view.Root.resolvedStyle.height;
                Assert.Greater(longHeight, shortHeight, "The next dialogue must be fitted to its own content.");
                dialogue.style.fontSize = 22;
                yield return SettleDialogue(view, 320);
                Assert.Greater(view.Root.resolvedStyle.height, longHeight, "A font-size change must refit unchanged text.");
                dialogue.style.fontSize = 10;
                var previousWidth = dialogue.style.width;
                dialogue.style.width = 100;
                yield return SettleDialogue(view, 320);
                Assert.Greater(view.Root.resolvedStyle.height, longHeight, "A narrower text viewport must refit unchanged text.");
                dialogue.style.width = previousWidth;
                yield return SettleDialogue(view, 320);
                Assert.That(view.Root.resolvedStyle.height, Is.EqualTo(longHeight).Within(1));
                dialogue.style.fontSize = 22;
                yield return SettleDialogue(view, 110);
                Assert.LessOrEqual(view.Root.resolvedStyle.height, 110, "Long text must scroll within the available card height.");
            }
            finally
            {
                view?.Dispose();
                Object.DestroyImmediate(host);
                if (panel) Object.DestroyImmediate(panel);
            }
        }

        private static IEnumerator SettleDialogue(ToolkitMeetingView view, float availableHeight)
        {
            for (var i = 0; i < 6; i++) { view.FitPortrait(availableHeight); yield return null; }
        }
    }
}
#endif
