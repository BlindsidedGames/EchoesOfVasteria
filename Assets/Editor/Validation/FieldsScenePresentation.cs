using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit authoring correction only: no gameplay, save selection or scene save.
[InitializeOnLoad]
internal sealed class FieldsScenePresentation : ICallbacks
{
    private static TestRunnerApi runner;
    static FieldsScenePresentation() => TestRunnerApi.RegisterTestCallback(new FieldsScenePresentation());
    [MenuItem("Tools/Fields/Apply unbuilt scene presentation")]
    private static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Use Edit Mode; player state must not be changed.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Main.unity") throw new System.InvalidOperationException("Expected Main scene.");
        var town = scene.GetRootGameObjects().Single(g => g.name == "Hometown").transform;
        var farmers = town.Find("Farmers");
        var renderers = new[] { farmers.Find("FarmingTasks"), farmers.Find("Fence") }.Where(t => t)
            .SelectMany(t => t.GetComponentsInChildren<SpriteRenderer>(true))
            .Concat(farmers.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Crops")
                .SelectMany(t => t.GetComponentsInChildren<SpriteRenderer>(true))).Distinct().ToArray();
        Undo.RecordObjects(renderers, "Correct Fields unbuilt presentation");
        foreach (var renderer in renderers) { renderer.enabled = false; EditorUtility.SetDirty(renderer); }
        Debug.Log("Fields unbuilt authoring presentation: disabled " + renderers.Length + " legacy renderers; NPCs/tasks/colliders and unrelated scene edits preserved. Scene not saved.");
    }
    [MenuItem("Tools/Fields/Run isolated presentation regression")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (runner) Object.DestroyImmediate(runner);
        SessionState.SetBool("Fields.PresentationRegression.Pending", true);
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
            testNames = new[] { "Tests.PlayMode.FieldsProductionPlayModeTests.FreshFieldsHideLegacyArtAndShowOnlyNormallyConstructedBeds" } }));
    }
    public void RunStarted(ITestAdaptor tests) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        if (!SessionState.GetBool("Fields.PresentationRegression.Pending", false)) return;
        SessionState.SetBool("Fields.PresentationRegression.Pending", false);
        var dir = Path.Combine(Path.GetTempPath(), "eov-tutorial-investigation"); Directory.CreateDirectory(dir);
        TestRunnerApi.SaveResultToFile(result, Path.Combine(dir,"fields-presentation-regression.xml"));
        Debug.Log("Fields isolated presentation regression: " + result.PassCount + " passed, " + result.FailCount + " failed.");
        if (runner) Object.DestroyImmediate(runner); runner = null;
    }
}
