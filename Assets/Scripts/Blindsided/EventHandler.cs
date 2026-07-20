using System;

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
        
        public static void SaveData()
        {
            // Debounce: invoke at most once per frame
            var frame = UnityEngine.Time.frameCount;
            if (frame == _lastSaveFrame) return;
            _lastSaveFrame = frame;
            OnSaveData?.Invoke();
        }
        
        public static void LoadData()
        {
            OnLoadData?.Invoke();
        }
        
        public static void ResetData()
        {
            OnResetData?.Invoke();
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
            ApplicationBackgrounded?.Invoke();
        }

        public static void ApplicationForeground()
        {
            ApplicationForegrounded?.Invoke();
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
