#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blindsided.UGS
{
    /// <summary>
    /// Centralizes runtime checks for whether Unity Gaming Services leaderboard submissions should run.
    /// Blocks submissions in the editor unless explicitly enabled through the editor toggle.
    /// </summary>
    public static class UgsLeaderboardSettings
    {
        private const string EditorPrefKey = "Blindsided.Ugs.SubmitInEditor";

        public static bool IsSubmissionEnabled
        {
            get
            {
#if UNITY_EDITOR
                return EditorPrefs.GetBool(EditorPrefKey, false);
#else
                return true;
#endif
            }
        }

#if UNITY_EDITOR
        public static bool GetEditorSubmissionEnabled() => EditorPrefs.GetBool(EditorPrefKey, false);

        public static void SetEditorSubmissionEnabled(bool enabled) => EditorPrefs.SetBool(EditorPrefKey, enabled);
#endif
    }
}
