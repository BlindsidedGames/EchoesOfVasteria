#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using Steamworks;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.Quests.QuestUtils;
#endif

namespace TimelessEchoes
{
    /// <summary>
    ///     Handles unlocking Steam achievements.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
#if !DISABLESTEAMWORKS
        private static AchievementManager instance;

        public static AchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<AchievementManager>();
                    if (instance == null)
                        instance = new GameObject("AchievementManager").AddComponent<AchievementManager>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void UnlockAchievement(string apiName)
        {
            if (!SteamManager.Initialized)
                return;

            if (SteamUserStats.SetAchievement(apiName)) SteamUserStats.StoreStats();
        }

        /// <summary>
        ///     Called when an NPC is met.
        /// </summary>
        public void NotifyNpcMet(string npcId)
        {
            if (npcId == "Ivan1")
                UnlockAchievement("MeetIvan");
            else if (npcId == "Farmers1")
                UnlockAchievement("MeetFarmers");
            else if (npcId == "Barkley1")
                UnlockAchievement("MeetBarkley");
        }

        /// <summary>
        ///     Awards the SlimeSwarm achievement.
        /// </summary>
        public void UnlockSlimeSwarm()
        {
            UnlockAchievement("SlimeSwarm");
        }

        private void OnEnable()
        {
            OnQuestHandin += OnQuestHandinHandler;
            OnLoadData += OnLoadDataHandler;
        }

        private void OnDisable()
        {
            OnQuestHandin -= OnQuestHandinHandler;
            OnLoadData -= OnLoadDataHandler;
        }

        private void OnQuestHandinHandler(string questId)
        {
            if (questId == "The names Gill")
                UnlockAchievement("MeetGill");

            var mildredId = GameManager.Instance != null ? GameManager.Instance.mildredQuestId : null;
            if (!string.IsNullOrEmpty(mildredId) && questId == mildredId)
                UnlockAchievement("Mildred");
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DeferredCheck());
        }

        private IEnumerator DeferredCheck()
        {
            yield return null; // wait one frame after data loads
            CheckExistingAchievements();
        }

        private void CheckExistingAchievements()
        {
            if (!SteamManager.Initialized)
                return;

            bool achieved;
            if (StaticReferences.CompletedNpcTasks.Contains("Ivan1") &&
                SteamUserStats.GetAchievement("MeetIvan", out achieved) && !achieved)
                UnlockAchievement("MeetIvan");

            if (StaticReferences.CompletedNpcTasks.Contains("Farmers1") &&
                SteamUserStats.GetAchievement("MeetFarmers", out achieved) && !achieved)
                UnlockAchievement("MeetFarmers");

            if (StaticReferences.CompletedNpcTasks.Contains("Barkley1") &&
                SteamUserStats.GetAchievement("MeetBarkley", out achieved) && !achieved)
                UnlockAchievement("MeetBarkley");

            if (QuestCompleted("The names Gill") &&
                SteamUserStats.GetAchievement("MeetGill", out achieved) && !achieved)
                UnlockAchievement("MeetGill");

            var mildredId = GameManager.Instance != null ? GameManager.Instance.mildredQuestId : null;
            if (!string.IsNullOrEmpty(mildredId) && QuestCompleted(mildredId) &&
                SteamUserStats.GetAchievement("Mildred", out achieved) && !achieved)
                UnlockAchievement("Mildred");
        }

#else
        public static AchievementManager Instance => null;
#endif
    }
}