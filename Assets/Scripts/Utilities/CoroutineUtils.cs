using System;
using System.Collections;
using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Utility helpers for running actions after waiting for frames using coroutines.
    /// </summary>
    public static class CoroutineUtils
    {
        /// <summary>
        /// Runs an action after yielding the specified number of frames.
        /// </summary>
        /// <param name="host">MonoBehaviour used to start the coroutine.</param>
        /// <param name="action">Action to invoke after the delay.</param>
        /// <param name="frames">Number of frames to wait before invoking the action. Defaults to 1.</param>
        public static void RunNextFrame(MonoBehaviour host, Action action, int frames = 1)
        {
            if (host == null || action == null)
                return;

            host.StartCoroutine(RunNextFrameRoutine(action, frames));
        }

        private static IEnumerator RunNextFrameRoutine(Action action, int frames)
        {
            for (var i = 0; i < frames; i++)
                yield return null;
            action();
        }
    }
}
