using System;
using Blindsided;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>One guarded exit attempt shared by both UI presentations.</summary>
    public sealed class GameQuitRequest
    {
        public bool InProgress { get; private set; }

        public bool TryQuit(Func<bool> prepare, Action quit)
        {
            if (InProgress) return false;
            InProgress = true;
            if (!prepare()) { InProgress = false; return false; }
            quit();
            return true;
        }

        public static bool Prepare()
        {
            if (Oracle.oracle == null || Oracle.oracle.PrepareForQuit()) return true;
            Debug.LogError("Quit cancelled because the final save could not be committed safely.");
            return false;
        }

        public static void Exit()
        {
            Application.Quit();
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }
    }
}
