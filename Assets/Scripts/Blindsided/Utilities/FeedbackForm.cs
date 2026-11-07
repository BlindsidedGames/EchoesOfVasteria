using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace Blindsided.Utilities
{
    public class FeedbackForm : MonoBehaviour
    {
        public string errorType;
        public string error;


        private const string FormUrl =
            "https://docs.google.com/forms/d/e/1FAIpQLSfNND3n6lfEj2LiwolJWyN0yDXUM1Y8bvDHFT8GELgJhHtwcA/formResponse?hl=en";

        [Button]
        public void SubmitFeedback()
        {
            Submit(errorType, error);
        }

        private IEnumerator Post(string entry1, string errorToReport, Action<bool> finished = null)
        {
            var form = new WWWForm();
            form.AddField("entry.762473998", entry1);
            form.AddField("entry.184011040", errorToReport);

            using (var www = UnityWebRequest.Post(FormUrl, form))
            {
                www.timeout = 12;
                yield return www.SendWebRequest();

                var sent = www.result == UnityWebRequest.Result.Success &&
                           www.downloadHandler.text.Contains("Your response has been recorded");
                if (sent)
                    Debug.Log("Feedback submitted successfully.");
                else
                    Debug.LogWarning($"Feedback submission was not confirmed (HTTP {www.responseCode}).");
                finished?.Invoke(sent);
            }
        }

        public static FeedbackForm Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // Static helper API for submitting feedback from anywhere (e.g., exceptions)
        public static void Submit(string type, string message, Action<bool> finished = null)
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipOnlineServices)
            {
                finished?.Invoke(false);
                return;
            }

            try
            {
                EnsureInstance();
                if (Instance == null)
                {
                    Debug.LogWarning("FeedbackForm.Submit called but no instance could be created.");
                    finished?.Invoke(false);
                    return;
                }

                Instance.StartCoroutine(Instance.Post(type, message, finished));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Feedback submission failed: " + e);
                finished?.Invoke(false);
            }
        }

        public static void SubmitException(string type, System.Exception ex, string extra = null)
        {
            Submit(type, FormatReport(extra, ex));
        }

        internal static string FormatReport(string context, Exception exception = null)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(context)) sb.AppendLine(context);
            sb.AppendLine($"Build: {Application.version} ({Application.buildGUID})");
            sb.AppendLine($"Platform: {Application.platform}");
            sb.AppendLine($"Device: {SystemInfo.deviceModel}");
            sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
            if (exception != null) sb.Append(FormatExceptionForReport(exception));
            return sb.ToString(0, Math.Min(sb.Length, 4000));
        }

        private static string FormatExceptionForReport(Exception ex)
        {
            if (ex == null) return "<null exception>";
            var sb = new System.Text.StringBuilder();

            if (ex is System.AggregateException agg)
            {
                var flat = agg.Flatten();
                int i = 0;
                foreach (var inner in flat.InnerExceptions)
                {
                    AppendExceptionBlock(sb, inner, i++);
                }
            }
            else
            {
                int i = 0;
                var current = ex;
                while (current != null)
                {
                    AppendExceptionBlock(sb, current, i++);
                    current = current.InnerException;
                }
            }

            return sb.ToString();
        }

        private static void AppendExceptionBlock(System.Text.StringBuilder sb, System.Exception e, int index)
        {
            try
            {
                // Raw messages and Exception.Data can carry save values or device paths.
                sb.AppendLine($"[#{index}] {e.GetType().FullName} (0x{e.HResult:X8})");
                sb.AppendLine(new System.Diagnostics.StackTrace(e, false).ToString());
            }
            catch { /* best-effort */ }
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;
            // A scene-authored FeedbackForm is a manual entry point only. Requests
            // belong to one host so reloading a scene cannot drop their callbacks.
            var go = new GameObject("FeedbackForm");
            Instance = go.AddComponent<FeedbackForm>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
    }
}
