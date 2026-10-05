using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
internal sealed class BetaFeedbackVerification : ICallbacks
{
    private static TestRunnerApi runner;
    private const string Pending = "EoV.BetaFeedback.Verification";
    static BetaFeedbackVerification() => TestRunnerApi.RegisterTestCallback(new BetaFeedbackVerification());
    [MenuItem("Tools/Beta feedback/Run orchard regression")]
    private static void Orchard() => Run("orchard", new Filter { testMode = TestMode.EditMode,
        groupNames = new[] { "^Tests\\.EditMode\\.OrchardContentTests\\." } });
    [MenuItem("Tools/Beta feedback/Run focused EditMode")]
    private static void Edit() => Run("editmode", new Filter { testMode = TestMode.EditMode,
        groupNames = new[] { "^Tests\\.EditMode\\.(OrchardContentTests|InventoryNavigationTests|SeedResourceTests|Fields.*Tests|Farm.*Tests)\\." } });
    [MenuItem("Tools/Beta feedback/Run focused PlayMode")]
    private static void Play() => Run("playmode", new Filter { testMode = TestMode.PlayMode,
        groupNames = new[] { "^Tests\\.PlayMode\\.FieldsProductionPlayModeTests\\." } });
    [MenuItem("Tools/Beta feedback/Run full EditMode")]
    private static void FullEdit() => Run("full-editmode", new Filter { testMode = TestMode.EditMode });
    [MenuItem("Tools/Beta feedback/Run full PlayMode")]
    private static void FullPlay() => Run("full-playmode", new Filter { testMode = TestMode.PlayMode });
    private static void Run(string name, Filter filter)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetString(Pending, name);
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.Execute(new ExecutionSettings(filter));
    }
    public void RunStarted(ITestAdaptor tests) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        var name = SessionState.GetString(Pending, "");
        if (name == "") return;
        SessionState.SetString(Pending, "");
        var directory = Path.Combine(Path.GetTempPath(), "eov-tutorial-investigation");
        Directory.CreateDirectory(directory);
        TestRunnerApi.SaveResultToFile(result, Path.Combine(directory, "beta-feedback-" + name + ".xml"));
        Debug.Log("Beta feedback " + name + ": " + result.PassCount + " passed, " + result.FailCount + " failed.");
        if (runner) Object.DestroyImmediate(runner); runner = null;
    }
}
