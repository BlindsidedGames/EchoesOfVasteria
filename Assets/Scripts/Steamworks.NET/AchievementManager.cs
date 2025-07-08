#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace TimelessEchoes
{
    /// <summary>
    /// Handles unlocking Steam achievements.
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
                    {
                        instance = new GameObject("AchievementManager").AddComponent<AchievementManager>();
                    }
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
            DontDestroyOnLoad(gameObject);
        }

        private void UnlockAchievement(string apiName)
        {
            if (!SteamManager.Initialized)
                return;

            if (SteamUserStats.SetAchievement(apiName))
            {
                SteamUserStats.StoreStats();
            }
        }

        /// <summary>
        /// Called when an NPC is met.
        /// </summary>
        public void NotifyNpcMet(string npcId)
        {
            if (npcId == "Ivan1")
            {
                UnlockAchievement("MeetIvan");
            }
        }
#else
        public static AchievementManager Instance => null;
#endif
    }
}
