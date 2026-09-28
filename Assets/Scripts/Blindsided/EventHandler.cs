using System;
using System.Collections.Generic;

namespace Blindsided
{
    public static class EventHandler
    {
        private static int _lastSaveFrame = -1;
        public static event Action UpdateUiEvent;
        public static event Action<float> AwayFor;
        public static event Action OnUnlockNexusEvent;
        public static event Action UpdateTextsForTimeScaleEvent;
        public static event Action ApplicationBackgrounded;
        public static event Action ApplicationForegrounded;

        public static event Action OnSaveData;
        public static event Action OnLoadData;
        public static event Action OnResetData;
        public static event Action<string> OnQuestHandin;

        // Global run lifecycle events for cross-system coordination
        public static event Action OnRunStarted;
        public static event Action OnRunEnded;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _lastSaveFrame = -1;
            UpdateUiEvent = null;
            AwayFor = null;
            OnUnlockNexusEvent = null;
            UpdateTextsForTimeScaleEvent = null;
            ApplicationBackgrounded = null;
            ApplicationForegrounded = null;
            OnSaveData = null;
            OnLoadData = null;
            OnResetData = null;
            OnQuestHandin = null;
            OnRunStarted = null;
            OnRunEnded = null;
        }

        public static void SaveData(bool force = false)
        {
            // Routine callers are debounced, while transactional saves can force every
            // contributor to refresh the in-memory tree immediately before serialization.
            var frame = UnityEngine.Time.frameCount;
            if (!force && frame == _lastSaveFrame)
                return;

            var previousSaveFrame = _lastSaveFrame;
            _lastSaveFrame = frame; // also prevents a subscriber from recursively re-entering this frame
            try
            {
                InvokeAll(OnSaveData, "contribute data to the save transaction");
            }
            catch
            {
                // A rejected transaction was not a debounced success. Allow a corrected caller to
                // retry in the same frame without weakening the re-entrancy guard above.
                _lastSaveFrame = previousSaveFrame;
                throw;
            }
        }

        public static void LoadData()
        {
            InvokeAll(OnLoadData, "load runtime data");
        }

        public static void ResetData()
        {
            InvokeAll(OnResetData, "reset runtime data");
        }

        private static void InvokeAll(Action subscribers, string operation)
        {
            if (subscribers == null)
                return;

            List<Exception> failures = null;
            foreach (Action subscriber in subscribers.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (failures != null)
            {
                throw new AggregateException(
                    $"One or more systems failed to {operation}.",
                    failures);
            }
        }

        public static void QuestHandin(string questId)
        {
            OnQuestHandin?.Invoke(questId);
        }


        public static void UnlockNexusEvent()
        {
            OnUnlockNexusEvent?.Invoke();
        }


        public static void UpdateUi()
        {
            UpdateUiEvent?.Invoke();
        }


        public static void AwayForTime(float time)
        {
            AwayFor?.Invoke(time);
        }

        public static void ApplicationBackground()
        {
            InvokeAll(ApplicationBackgrounded, "enter the application background");
        }

        public static void ApplicationForeground()
        {
            InvokeAll(ApplicationForegrounded, "return to the application foreground");
        }

        public static void UpdateTextsForTimeScale()
        {
            UpdateTextsForTimeScaleEvent?.Invoke();
        }

        // Invoke when a new run begins (e.g., after GameplayStatTracker.BeginRun)
        public static void RunStarted()
        {
            OnRunStarted?.Invoke();
        }

        // Invoke when a run ends (after stats are finalized and RunInProgress is false)
        public static void RunEnded()
        {
            OnRunEnded?.Invoke();
        }
    }
}
