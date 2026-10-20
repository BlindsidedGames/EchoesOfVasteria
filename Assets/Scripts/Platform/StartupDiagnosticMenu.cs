using UnityEngine;

namespace TimelessEchoes.Platform
{
    // Independent of UI Toolkit so its creation can be isolated by the same build.
    public sealed class StartupDiagnosticMenu : MonoBehaviour
    {
#if UNITY_IOS && !UNITY_EDITOR && EOV_STARTUP_DIAGNOSTICS
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int EovReadStartupDiagnosticMode([System.Runtime.InteropServices.Out] byte[] buffer, int capacity);
#endif
        private Vector2 scroll;
        private bool hidden;
        private void Start()
        {
            foreach (var arg in System.Environment.GetCommandLineArgs())
                if (arg.StartsWith("-eovDiagnosticMode=") && System.Enum.TryParse<StartupDiagnostics.TestMode>(arg.Substring(19), out var mode))
                    StartupDiagnostics.Choose(mode);
#if UNITY_IOS && !UNITY_EDITOR && EOV_STARTUP_DIAGNOSTICS
            if (!StartupDiagnostics.ChoiceReady)
            {
                var buffer = new byte[64];
                var count = EovReadStartupDiagnosticMode(buffer, buffer.Length);
                if (count > 0 && count < buffer.Length && System.Enum.TryParse<StartupDiagnostics.TestMode>(System.Text.Encoding.UTF8.GetString(buffer, 0, count), out var nativeMode))
                    StartupDiagnostics.Choose(nativeMode);
            }
#endif
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
            var area = new Rect(10, Mathf.Max(20, height * .07f), width - 20, height * .86f);
            var previousColor = GUI.color;
            GUI.color = new Color(.08f, .07f, .065f, 1f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUILayout.BeginArea(area, GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Startup diagnostics", new GUIStyle(style) { fontSize = 23, fontStyle = FontStyle.Bold });
            if (!StartupDiagnostics.ChoiceReady)
            {
                GUILayout.Label("Choose one check per launch. Swipe the app away, then reopen it to try another check. Your saves stay in place.", style);
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
                    GUILayout.Label("Check complete. Swipe the app away, then reopen it to try another check.", style);
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
