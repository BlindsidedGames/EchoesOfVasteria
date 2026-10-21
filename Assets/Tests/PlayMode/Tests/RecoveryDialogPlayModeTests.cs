#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator RecoveryChoicesFitBothLayoutsAndRespectBusyState()
        {
            var instance = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Recovery.prefab"));
            var screen = instance.GetComponent<ToolkitDialogScreen>();
            var chosen = 0;
            var output = Path.Combine(Path.GetTempPath(), "eov-save-recovery-review");
            Directory.CreateDirectory(output);
            try
            {
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(960, 2078) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Recovery review");
                    screen.Show("Save Recovery Required", "More than one version of File 3 was found. Choose the progress to keep. All original snapshots remain saved.",
                        "Retry", "Keep Old Files & Start Fresh", () => {}, () => {}, choices: new[]
                        {
                            new ToolkitDialogScreen.Choice { Text = "Keep Adventurer\n21/10/2026 8:00 AM · 96.57% complete · 2381.4 hours played", Select = () => chosen++ },
                            new ToolkitDialogScreen.Choice { Text = "Keep Adventurer\n20/10/2026 1:46 PM · 88.06% complete · 2366.0 hours played", Select = () => chosen++ }
                        });
                    for (var frame = 0; frame < 5; frame++) yield return null;
                    var document = instance.GetComponent<UIDocument>();
                    var ui = document.rootVisualElement;
                    var choices = ui.Q<ScrollView>("dialog-scroll").Query<Button>().ToList();
                    Assert.AreEqual(2, choices.Count);
                    Assert.LessOrEqual(choices[0].worldBound.yMax, choices[1].worldBound.yMin);
                    Assert.LessOrEqual(choices[1].worldBound.yMax, ui.Q<Button>("dialog-primary").worldBound.yMin);
                    foreach (var choice in choices)
                    {
                        Assert.Greater(choice.worldBound.height, 0);
                        Assert.GreaterOrEqual(choice.worldBound.xMin, 0);
                        Assert.LessOrEqual(choice.worldBound.xMax, ui.worldBound.xMax);
                    }
                    FieldsClick(choices[0]);
                    Assert.AreEqual(size.x == 1920 ? 1 : 2, chosen);
                    screen.SetInteractable(false);
                    Assert.True(choices.All(c => !c.enabledInHierarchy));
                    screen.SetInteractable(true);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, size.x == 1920 ? "landscape.png" : "portrait.png"));
                    yield return null;
                }
                screen.Hide();
                Assert.IsNull(instance.GetComponent<UIDocument>().panelSettings);
            }
            finally
            {
                Object.DestroyImmediate(instance);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Game");
            }
        }
    }
}
#endif
