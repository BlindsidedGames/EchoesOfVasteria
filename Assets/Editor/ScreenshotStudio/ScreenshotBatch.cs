#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.Editor.Capture
{
    /// <summary>Same capture workflow as the window, suitable for a scheduled local recapture.</summary>
    [InitializeOnLoad]
    public static class ScreenshotBatch
    {
        private const string Key = "EOV.ScreenshotStudio.Batch";
        static ScreenshotBatch() => EditorApplication.update += Tick;
        public static void Run()
        {
            var path = Environment.GetEnvironmentVariable("EOV_SCREENSHOT_SCENARIO");
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Set EOV_SCREENSHOT_SCENARIO to a scenario folder.");
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 240);
            ScreenshotSession.Begin(path);
        }
        private static void Tick()
        {
            var phase = SessionState.GetInt(Key, 0); if (phase == 0) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0))
                    throw new TimeoutException(ScreenshotSession.Status);
                if (phase == 1 && ScreenshotSession.Ready && !ScreenshotSession.Busy)
                {
                    ScreenshotSession.CaptureAll(true); SessionState.SetInt(Key, 2);
                }
                else if (phase == 2 && !ScreenshotSession.Busy)
                {
                    if (!ScreenshotSession.Status.StartsWith("Captured ", StringComparison.Ordinal)) throw new InvalidOperationException(ScreenshotSession.Status);
                    File.WriteAllText(Path.Combine(ScreenshotStudio.OutputRoot, "last-batch.txt"), ScreenshotSession.Status);
                    ScreenshotSession.End(); SessionState.SetInt(Key, 3);
                }
                else if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
                { SessionState.SetInt(Key, 0); EditorApplication.Exit(0); }
            }
            catch (Exception e)
            {
                Debug.LogException(e); SessionState.SetInt(Key, 0); ScreenshotSession.End();
                EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }
    }
}
#endif
