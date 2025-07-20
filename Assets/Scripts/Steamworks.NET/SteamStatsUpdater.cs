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
    /// Updates Steam user stats based on values from GameplayStatTracker.
    /// </summary>
    public class SteamStatsUpdater : MonoBehaviour
    {
#if !DISABLESTEAMWORKS
        private static SteamStatsUpdater instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static SteamStatsUpdater Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SteamStatsUpdater>();
                    if (instance == null)
                        instance = new GameObject("SteamStatsUpdater").AddComponent<SteamStatsUpdater>();
                }

                return instance;
            }
        }

        [SerializeField]
        private float updateInterval = 5f;
        private float lastUpdate;

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

        private void Update()
        {
            if (Time.unscaledTime - lastUpdate >= updateInterval)
            {
                lastUpdate = Time.unscaledTime;
                UpdateStats();
            }
        }

        /// <summary>
        /// Updates the DistanceReached, TotalKilometers and TasksCompleted stats.
        /// </summary>
        public void UpdateStats()
        {
            if (!SteamManager.Initialized)
                return;

            var tracker = Stats.GameplayStatTracker.Instance;
            if (tracker == null)
                return;

            bool changed = false;

            if (SteamUserStats.GetStat("DistanceReached", out int storedDistance))
            {
                int newDistance = Mathf.FloorToInt(tracker.LongestRun);
                if (newDistance > storedDistance)
                {
                    SteamUserStats.SetStat("DistanceReached", newDistance);
                    changed = true;
                }
            }

            if (SteamUserStats.GetStat("TotalKilometers", out float storedKm))
            {
                float newKm = tracker.DistanceTravelled / 1000f;
                newKm = Mathf.Round(newKm * 10000f) / 10000f;
                if (newKm > storedKm)
                {
                    SteamUserStats.SetStat("TotalKilometers", newKm);
                    changed = true;
                }
            }

            if (SteamUserStats.GetStat("TasksCompleted", out int storedTasks))
            {
                int newTasks = tracker.TasksCompleted;
                if (newTasks > storedTasks)
                {
                    SteamUserStats.SetStat("TasksCompleted", newTasks);
                    changed = true;
                }
            }

            if (SteamUserStats.GetStat("TimesReaped", out int storedReaps))
            {
                int newReaps = tracker.TimesReaped;
                if (newReaps > storedReaps)
                {
                    SteamUserStats.SetStat("TimesReaped", newReaps);
                    changed = true;
                }
            }

            if (SteamUserStats.GetStat("TotalKills", out int storedKills))
            {
                int newKills = tracker.TotalKills;
                if (newKills > storedKills)
                {
                    SteamUserStats.SetStat("TotalKills", newKills);
                    changed = true;
                }
            }

            if (SteamUserStats.GetStat("SlimesKilled", out int storedSlimes))
            {
                int newSlimes = tracker.SlimesKilled;
                if (newSlimes > storedSlimes)
                {
                    SteamUserStats.SetStat("SlimesKilled", newSlimes);
                    changed = true;
                }
            }

            if (SteamUserStats.GetStat("BuffsCast", out int storedBuffs))
            {
                int newBuffs = tracker.BuffsCast;
                if (newBuffs > storedBuffs)
                {
                    SteamUserStats.SetStat("BuffsCast", newBuffs);
                    changed = true;
                }
            }

            if (changed)
                SteamUserStats.StoreStats();
        }
#endif
    }
}
