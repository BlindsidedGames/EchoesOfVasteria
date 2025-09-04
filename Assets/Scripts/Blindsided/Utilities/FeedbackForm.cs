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


        private readonly string formUrl =
            "https://docs.google.com/forms/u/0/d/e/1FAIpQLScDFgaPvj7D8qCj_Z9l-u9nZ0aj0rgOOE90XKzMMLv_evF9xA/formResponse";

        [Button]
        public void SubmitFeedback()
        {
            StartCoroutine(Post(errorType, error));
        }

        private IEnumerator Post(string entry1, string errorToReport)
        {
            var form = new WWWForm();
            form.AddField("entry.220430120", entry1);
            form.AddField("entry.1630659015", errorToReport);

            using (var www = UnityWebRequest.Post(formUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                    Debug.Log("Feedback submitted successfully.");
                else
                    Debug.LogError("Error in feedback submission: " + www.error);
            }
        }

        public static FeedbackForm Instance;

        private void Awake()
        {
            Instance = this;
        }

        // Static helper API for submitting feedback from anywhere (e.g., exceptions)
        public static void Submit(string type, string message)
        {
            try
            {
                EnsureInstance();
                if (Instance == null)
                {
                    Debug.LogWarning("FeedbackForm.Submit called but no instance could be created.");
                    return;
                }

                Instance.StartCoroutine(Instance.Post(type, message));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Feedback submission failed: " + e);
            }
        }

        public static void SubmitException(string type, System.Exception ex, string extra = null)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(extra)) sb.AppendLine(extra);
            // Include application and platform context for triage
            sb.AppendLine($"AppVersion: {Application.version}");
            sb.AppendLine($"Platform: {Application.platform}");
            sb.AppendLine($"Unity: {Application.unityVersion}");
            sb.AppendLine("== Exception ==");
            sb.AppendLine(BuildExceptionReport(ex));
            Submit(type, sb.ToString());
        }

        private static string BuildExceptionReport(System.Exception ex)
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
                sb.AppendLine($"[#{index}] {e.GetType().FullName}: {e.Message}");
                if (e.Data != null && e.Data.Count > 0)
                {
                    sb.AppendLine("Data:");
                    foreach (var key in e.Data.Keys)
                    {
                        var val = e.Data[key];
                        sb.AppendLine($" - {key}: {val}");
                    }
                }
                sb.AppendLine("StackTrace:");
                sb.AppendLine(string.IsNullOrEmpty(e.StackTrace) ? "<no stack trace>" : e.StackTrace);
            }
            catch { /* best-effort */ }
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;
            // Try to find an existing instance first
            Instance = Object.FindFirstObjectByType<FeedbackForm>();
            if (Instance != null) return;

            // Create a lightweight host if none exists
            var go = new GameObject("FeedbackForm");
            Instance = go.AddComponent<FeedbackForm>();
            Object.DontDestroyOnLoad(go);
        }
    }
}
