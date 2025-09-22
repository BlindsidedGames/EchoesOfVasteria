#if UNITY_EDITOR
using UnityEditor;

namespace Blindsided.UGS.Editor
{
    /// <summary>
    /// Adds a menu toggle under Tools so designers can enable Unity Gaming Services leaderboard submissions while testing in-editor.
    /// </summary>
    internal static class UgsLeaderboardSettingsMenu
    {
        private const string MenuPath = "Tools/Timeless Echoes/UGS/Submit Scores In Editor";

        [MenuItem(MenuPath)]
        private static void ToggleSubmitScores()
        {
            var enabled = UgsLeaderboardSettings.GetEditorSubmissionEnabled();
            UgsLeaderboardSettings.SetEditorSubmissionEnabled(!enabled);
            Menu.SetChecked(MenuPath, !enabled);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleSubmitScoresValidate()
        {
            Menu.SetChecked(MenuPath, UgsLeaderboardSettings.GetEditorSubmissionEnabled());
            return true;
        }
    }
}
#endif
