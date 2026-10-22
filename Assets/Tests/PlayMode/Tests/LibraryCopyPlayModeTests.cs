#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TimelessEchoes.Buffs;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator LibraryBuffEffectsAndDefenseReferenceSurviveSimulatorRotation()
        {
            yield return LocalizationSettings.InitializationOperation;
            var oldLocale = LocalizationSettings.SelectedLocale;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            var type = Type.GetType("UnityEditor.DeviceSimulation.SimulatorWindow, UnityEditor.DeviceSimulatorModule");
            var existing = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => w.GetType() == type);
            var simulator = existing ? existing : EditorWindow.GetWindow(type);
            var main = type.GetField("m_Main", Private).GetValue(simulator);
            var simulation = main.GetType().GetProperty("ScreenSimulation").GetValue(main);
            var rotationProperty = simulation.GetType().GetProperty("DeviceRotation");
            var oldOrientation = UnityEngine.Device.Screen.orientation;
            var oldRotation = oldOrientation == ScreenOrientation.LandscapeLeft ? 90 : oldOrientation == ScreenOrientation.LandscapeRight ? 270 : oldOrientation == ScreenOrientation.PortraitUpsideDown ? 180 : 0;
            var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor"));
            void Rotate(bool landscape)
            {
                main.GetType().GetMethod("Enable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(main, null);
                rotationProperty.SetValue(simulation, landscape ? 90 : 0);
                simulation.GetType().GetMethod("RequestOrientation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(simulation, new object[] { landscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait });
                simulation.GetType().GetMethod("ApplyChanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(simulation, null);
                simulator.Focus();
            }
            var host = new GameObject("Protected Library UI", typeof(UIDocument));
            var definition = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ToolkitBookDefinition>("Assets/UI/Toolkit/Library.asset"));
            // Exercise the renderer independently of quest-unlock gates in this empty save.
            foreach (var section in definition.sections) section.visibility = Array.Empty<ToolkitVisibilityRule>();
            var buffs = definition.sections.First(s => s.id == "help-buffs");
            var recipeIndex = Array.FindIndex(buffs.illustrations, i => i.buff && i.buff.name == "Resource Doubler");
            var recipe = Object.Instantiate(buffs.illustrations[recipeIndex].buff);
            buffs.illustrations[recipeIndex].buff = recipe;
            var initialEffects = recipe.baseEffects.ToArray(); var initialDuration = recipe.baseDuration; var initialCooldown = recipe.baseCooldown;
            ToolkitBookView view = null; PanelSettings panel = null; RenderTexture capture = null;
            try
            {
                panel = ToolkitPanel.CreateSettings(AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Toolkit/Runtime.tss"), AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset"));
                var document = host.GetComponent<UIDocument>(); document.panelSettings = panel;
                view = new ToolkitBookView(definition, AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset"), AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Toolkit/Book.uxml"));
                document.rootVisualElement.Add(view.Root);
                foreach (var landscape in new[] { false, true, false })
                {
                    Rotate(landscape); for (int frame = 0; frame < 8; frame++) yield return null;
                    Assert.AreEqual(!landscape, ToolkitResponsiveLayout.IsPortrait, "Simulator screen: " + UnityEngine.Device.Screen.width + "x" + UnityEngine.Device.Screen.height);
                    if (capture) { panel.targetTexture = null; Object.DestroyImmediate(capture); }
                    capture = new RenderTexture(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height, 24); capture.Create(); panel.targetTexture = capture;
                    var area = ToolkitResponsiveLayout.SafeScreenArea;
                    view.Root.style.position = Position.Absolute; view.Root.style.left = area.x; view.Root.style.top = area.y + ToolkitResponsiveLayout.WindowTop;
                    view.Root.style.width = area.width; view.Root.style.height = area.height - ToolkitResponsiveLayout.WindowTop - ToolkitResponsiveLayout.WindowBottom;
                    view.Root.EnableInClassList("library-narrow", area.width < 500); view.RefreshPortrait();
                    yield return null; yield return null;
                    if (!landscape) { FieldsClick(view.Root.Q<Button>(className: "portrait-topic-picker")); yield return null; }
                    FieldsClick(view.Root.Q<Button>("help-buffs")); yield return null; yield return null;
                    Assert.AreEqual("help-buffs", view.SelectedSectionId);
                    var figure = view.Root.Q<Button>("library-illustration-" + recipeIndex); view.Scroll.ScrollTo(figure); yield return null;
                    FieldsClick(figure); yield return null;
                    Assert.IsNotEmpty(view.Root.Q<Label>("library-buff-effects").text);
                    var effect = recipe.baseEffects[0]; effect.value = 137; recipe.baseEffects[0] = effect;
                    recipe.baseDuration = 23; recipe.baseCooldown = 47;
                    view.RefreshSharedEffects();
                    StringAssert.Contains("137%", view.Root.Q<Label>("library-buff-effects").text);
                    StringAssert.Contains("23s", System.Text.RegularExpressions.Regex.Replace(view.Root.Q<Label>("library-buff-effects").text, "<[^>]+>", ""));
                    recipe.baseEffects = initialEffects.ToList(); recipe.baseDuration = initialDuration; recipe.baseCooldown = initialCooldown; view.RefreshSharedEffects();
                    Assert.LessOrEqual(figure.worldBound.xMax, view.Scroll.contentViewport.worldBound.xMax + 1, "Buff selector must fit the article width.");
                    var path = Path.GetFullPath("Artifacts/LibraryTutorialCopyReview20261022/simulator"); Directory.CreateDirectory(path);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    var render = capture;
                    var active = RenderTexture.active; var image = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
                    try { RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(path, landscape ? "library-landscape.png" : "library-portrait.png"), image.EncodeToPNG()); }
                    finally { RenderTexture.active = active; Object.DestroyImmediate(image); }
                    if (!landscape) { FieldsClick(view.Root.Q<Button>(className: "portrait-topic-picker")); yield return null; }
                    FieldsClick(view.Root.Q<Button>("help-combat")); yield return null;
                    FieldsClick(view.Root.Q<Button>("library-reference")); yield return null; yield return null;
                    var formula = view.Root.Q<Label>("library-article-body").text;
                    Assert.False(formula.Contains("{0"), "Gameplay-backed formula must be formatted for the reader.");
                    StringAssert.Contains("25", formula);
                    Assert.AreEqual(.5f, TimelessEchoes.Combat.ApplyDefense(1, 25), .0001f);
                    Assert.AreEqual(.25f, TimelessEchoes.Combat.ApplyDefense(1, 75), .0001f);
                }
            }
            finally
            {
                view?.Dispose(); if (panel) panel.targetTexture = null; if (capture) Object.DestroyImmediate(capture); if (panel) Object.DestroyImmediate(panel); Object.DestroyImmediate(host); Object.DestroyImmediate(recipe); Object.DestroyImmediate(definition);
                LocalizationSettings.SelectedLocale = oldLocale;
                rotationProperty.SetValue(simulation, oldRotation);
                simulation.GetType().GetMethod("RequestOrientation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(simulation, new object[] { oldOrientation });
                simulation.GetType().GetMethod("ApplyChanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(simulation, null);
                if (!existing) simulator.Close();
                gameView.Focus();
            }
        }
    }
}
#endif
