using System;

namespace Blindsided
{
    public static class EventHandler
    {
        public static event Action UpdateUiEvent;
        public static event Action<float> AwayFor;
        public static event Action OnUnlockNexusEvent;
        public static event Action UpdateTextsForTimeScaleEvent;
        
        public static event Action OnSaveData;
        public static event Action OnLoadData;
        public static event Action OnResetData;
        public static event Action<string> OnQuestHandin;
        
        public static void SaveData()
        {
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

        public static void UpdateTextsForTimeScale()
        {
            UpdateTextsForTimeScaleEvent?.Invoke();
        }
    }
}