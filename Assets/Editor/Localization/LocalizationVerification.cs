using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Runs the existing owning tests through Unity's Test Runner; no game scene/save is started.
internal sealed class LocalizationVerification : ICallbacks
{
    private static TestRunnerApi runner;
    [MenuItem("Tools/Localization/Run localization behavior verification")]
    private static void Run()
    {
        if (runner) return;
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new LocalizationVerification());
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            assemblyNames = new[] { "Localization.Behaviour.Tests" } }));
    }
    public void RunStarted(ITestAdaptor tests) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        var directory = Path.Combine(Path.GetTempPath(), "eov-tutorial-investigation");
        Directory.CreateDirectory(directory);
        TestRunnerApi.SaveResultToFile(result, Path.Combine(directory, "localization-final-editmode.xml"));
        Debug.Log("Final localization behavior verification: " + result.PassCount + " passed, " + result.FailCount + " failed, " + result.Duration + " seconds.");
        Object.DestroyImmediate(runner); runner = null;
    }
}
