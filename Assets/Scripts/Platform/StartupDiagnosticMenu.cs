using UnityEngine;

namespace TimelessEchoes.Platform
{
    // Independent of UI Toolkit so its creation can be isolated by the same build.
    public sealed class StartupDiagnosticMenu : MonoBehaviour
    {
        private Vector2 scroll;
        private bool hidden;
        private void Start()
        {
            foreach (var arg in System.Environment.GetCommandLineArgs())
                if (arg.StartsWith("-eovDiagnosticMode=") && System.Enum.TryParse<StartupDiagnostics.TestMode>(arg.Substring(19), out var mode))
                    StartupDiagnostics.Choose(mode);
        }
        private void OnDestroy() => StartupDiagnostics.EndSession();
        private void Update() => StartupDiagnostics.Heartbeat();
        private void OnGUI()
        {
            if (!StartupDiagnostics.Enabled || hidden) return;
            GUI.depth = -10000;
            var scale = Mathf.Max(1f, Screen.width / 480f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
            GUILayout.BeginArea(new Rect(10, Mathf.Max(20, height * .07f), width - 20, height * .86f), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Startup diagnostics", new GUIStyle(style) { fontSize = 23, fontStyle = FontStyle.Bold });
            if (!StartupDiagnostics.ChoiceReady)
            {
                GUILayout.Label("Choose one check per launch. Close and reopen the app to try another. Your saves stay in place.", style);
                Option("1. Normal startup", StartupDiagnostics.TestMode.Normal, button);
                Option("2. Pause before town activation", StartupDiagnostics.TestMode.PauseBeforeActivation, button);
                Option("3. Skip Game Center sign-in", StartupDiagnostics.TestMode.NoGameCenterSignIn, button);
                Option("4. Skip online services (UGS)", StartupDiagnostics.TestMode.NoOnlineServices, button);
                Option("5. Skip main UI Toolkit screens", StartupDiagnostics.TestMode.NoMainUi, button);
                Option("6. Skip sign-in, online services and main UI", StartupDiagnostics.TestMode.Minimal, button);
                GUILayout.Label(StartupDiagnostics.PreviousAttempt ?? "", style);
                GUILayout.Label(StartupDiagnostics.PreviousResults ?? "", style);
            }
            else
            {
                GUILayout.Label(StartupDiagnostics.Mode + " — " + StartupDiagnostics.Phase, style);
                GUILayout.Label(StartupDiagnostics.CurrentDetails, style);
                if (StartupDiagnostics.AwaitingActivation && GUILayout.Button("Continue: activate town", button, GUILayout.MinHeight(44)))
                    StartupDiagnostics.AwaitingActivation = false;
                GUILayout.Label(StartupDiagnostics.PreviousAttempt ?? "", style);
                if (!StartupDiagnostics.Active)
                {
                    GUILayout.Label("Check complete. Close and reopen the app for another check.", style);
                    if (GUILayout.Button("Hide diagnostics", button, GUILayout.MinHeight(44))) hidden = true;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        private static void Option(string title, StartupDiagnostics.TestMode mode, GUIStyle style)
        {
            if (GUILayout.Button(title, style, GUILayout.MinHeight(44))) StartupDiagnostics.Choose(mode);
        }
    }
}
