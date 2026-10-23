// Steamworks guards
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.Quests.QuestUtils;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
#if UNITY_ANDROID || UNITY_IOS
using VoxelBusters.EssentialKit;
using TimelessEchoes.Platform;
#endif

namespace TimelessEchoes
{
    /// <summary>
    /// Cross-platform achievement manager: Steam on PC; VoxelBusters GameServices on iOS/Android.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        private static AchievementManager instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static AchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<AchievementManager>();
                    if (instance == null)
                        instance = new GameObject("AchievementManager").AddComponent<AchievementManager>();
                }

                return instance;
            }
        }

        [SerializeField] private bool evaluateOnLoad = true;

#if UNITY_ANDROID || UNITY_IOS
        private readonly HashSet<string> pendingMobileUnlocks = new HashSet<string>();
        // Per-session cache to avoid re-reporting the same achievement repeatedly during a play session
        private readonly HashSet<string> unlockedMobileThisSession = new HashSet<string>();
        private readonly HashSet<string> reportingMobile = new HashSet<string>();
        private float nextMobileEvaluation;
        private TimelessEchoes.Stats.GameplayStatTracker tracker;
#endif

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnEnable()
        {
            OnQuestHandin += OnQuestHandinHandler;
            OnLoadData += OnLoadDataHandler;

#if UNITY_ANDROID || UNITY_IOS
            tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            // Subscribe to auth changes to flush queued unlocks
            if (!TimelessEchoes.Platform.StartupDiagnostics.SkipGameCenter)
            {
                GameServices.OnAuthStatusChange -= OnAuthStatusChange;
                GameServices.OnAuthStatusChange += OnAuthStatusChange;
            }
#endif
        }

        private void OnDisable()
        {
            OnQuestHandin -= OnQuestHandinHandler;
            OnLoadData -= OnLoadDataHandler;

#if UNITY_ANDROID || UNITY_IOS
            GameServices.OnAuthStatusChange -= OnAuthStatusChange;
#endif
        }

        /// <summary>
        /// Called when an NPC is met.
        /// </summary>
        public void NotifyNpcMet(string npcId)
        {
            if (npcId == "Ivan1")
                UnlockAchievement("MeetIvan");
            else if (npcId == "Farmers1")
                UnlockAchievement("MeetFarmers");
            else if (npcId == "Barkley1")
                UnlockAchievement("MeetBarkley");
            else if (npcId == "OldTimer1")
                UnlockAchievement("MeetOldTimer");
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
            if (!evaluateOnLoad) return;
            StartCoroutine(DeferredCheck());
        }

        private IEnumerator DeferredCheck()
        {
            yield return null; // wait one frame after data loads
            CheckExistingAchievements();
#if UNITY_ANDROID || UNITY_IOS
            EvaluateMobileStatMilestones();
#endif
        }

        private void UnlockAchievement(string apiName)
        {
#if !DISABLESTEAMWORKS
            if (SteamManager.Initialized)
            {
                if (SteamUserStats.SetAchievement(apiName)) SteamUserStats.StoreStats();
            }
#endif

#if UNITY_ANDROID || UNITY_IOS
            TryUnlockMobile(apiName);
#endif
        }

        private void CheckExistingAchievements()
        {
#if !DISABLESTEAMWORKS
            if (SteamManager.Initialized)
            {
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

                if (StaticReferences.CompletedNpcTasks.Contains("OldTimer1") &&
                    SteamUserStats.GetAchievement("MeetOldTimer", out achieved) && !achieved)
                    UnlockAchievement("MeetOldTimer");

                if (QuestCompleted("The names Gill") &&
                    SteamUserStats.GetAchievement("MeetGill", out achieved) && !achieved)
                    UnlockAchievement("MeetGill");

                var mildredId = GameManager.Instance != null ? GameManager.Instance.mildredQuestId : null;
                if (!string.IsNullOrEmpty(mildredId) && QuestCompleted(mildredId) &&
                    SteamUserStats.GetAchievement("Mildred", out achieved) && !achieved)
                    UnlockAchievement("Mildred");
            }
#endif

#if UNITY_ANDROID || UNITY_IOS
            // On mobile, re-apply unlocks based on current game state (idempotent on server side).
            if (StaticReferences.CompletedNpcTasks.Contains("Ivan1"))
                UnlockAchievement("MeetIvan");
            if (StaticReferences.CompletedNpcTasks.Contains("Farmers1"))
                UnlockAchievement("MeetFarmers");
            if (StaticReferences.CompletedNpcTasks.Contains("Barkley1"))
                UnlockAchievement("MeetBarkley");
            if (StaticReferences.CompletedNpcTasks.Contains("OldTimer1"))
                UnlockAchievement("MeetOldTimer");
            if (QuestCompleted("The names Gill"))
                UnlockAchievement("MeetGill");
            var mildredIdMobile = GameManager.Instance != null ? GameManager.Instance.mildredQuestId : null;
            if (!string.IsNullOrEmpty(mildredIdMobile) && QuestCompleted(mildredIdMobile))
                UnlockAchievement("Mildred");
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        private void Update()
        {
            if (Time.unscaledTime < nextMobileEvaluation) return;
            nextMobileEvaluation = Time.unscaledTime + 5f;
            if (oracle == null || !oracle.HasLoadedCurrentSlotData) return;
            // Town levels and kills can change without a movement/task event.
            CheckExistingAchievements();
            EvaluateMobileStatMilestones();
            FlushPendingMobile();
        }

        private void FlushPendingMobile()
        {
            if (!GameServices.IsAuthenticated || pendingMobileUnlocks.Count == 0) return;
            foreach (var id in pendingMobileUnlocks.ToArray()) TryUnlockMobile(id);
        }

        private void OnAuthStatusChange(GameServicesAuthStatusChangeResult result, VoxelBusters.CoreLibrary.Error error)
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipGameCenter) return;
            if (result.AuthStatus != LocalPlayerAuthStatus.Authenticated) return;

            FlushPendingMobile();
            CheckExistingAchievements();
            EvaluateMobileStatMilestones();
        }

        private void TryUnlockMobile(string apiName)
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipGameCenter ||
                unlockedMobileThisSession.Contains(apiName) || reportingMobile.Contains(apiName)) return;
            if (!GameServices.IsAuthenticated)
            {
                if (pendingMobileUnlocks.Add(apiName))
                    MobileAuthDebouncer.RequestSilentAuth("AchievementUnlock");
                return;
            }

            pendingMobileUnlocks.Remove(apiName);
            reportingMobile.Add(apiName);
            GameServices.ReportAchievementProgress(apiName, 100.0, (success, error) =>
            {
                reportingMobile.Remove(apiName);
                if (success) unlockedMobileThisSession.Add(apiName);
                else pendingMobileUnlocks.Add(apiName);
            });
        }

        private void EvaluateMobileStatMilestones()
        {
            // Lazy fetch tracker
            tracker ??= TimelessEchoes.Stats.GameplayStatTracker.Instance;

            var data = oracle != null ? oracle.saveData : null;
            var progress = new AchievementProgress(
                highestDistance: tracker != null ? Mathf.Max(tracker.HighestDistance, tracker.CurrentRunDistance) : 0,
                kilometers: tracker != null ? tracker.DistanceTravelled / 1000.0 : 0,
                tasks: tracker != null ? tracker.TasksCompleted : 0,
                buffs: tracker != null ? tracker.BuffsCast : 0,
                deaths: tracker != null ? tracker.Deaths : 0,
                kills: tracker != null ? tracker.TotalKills : 0,
                evaLevel: data?.CauldronEvaLevel ?? 0,
                ivanLevel: data?.CraftingMasteryLevel ?? 0);
            AchievementMilestones.ReportEarned(progress, TryUnlockMobile);
        }
#endif
    }
}
