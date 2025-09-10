#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.Quests.QuestUtils;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
#if UNITY_ANDROID || UNITY_IOS
using CloudOnce;
#endif

namespace TimelessEchoes
{
    /// <summary>
    ///     Handles unlocking Steam achievements.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
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

#if UNITY_ANDROID || UNITY_IOS
        private readonly HashSet<string> pendingMobileUnlocks = new HashSet<string>();
        private TimelessEchoes.Stats.GameplayStatTracker tracker;
        private void OnMobileDistanceAdded(float _)
        {
            EvaluateMobileStatMilestones();
        }
        private void OnMobileRunEnded(bool _)
        {
            EvaluateMobileStatMilestones();
        }
        private void OnMobileTaskCompleted()
        {
            EvaluateMobileStatMilestones();
        }
        private void OnMobileBuffCast(TimelessEchoes.Buffs.BuffRecipe _, bool __)
        {
            EvaluateMobileStatMilestones();
        }
        private void OnMobileHeroDied()
        {
            EvaluateMobileStatMilestones();
        }

        private void OnMobileSignedInChanged(bool isSignedIn)
        {
            if (!isSignedIn) return;
            Cloud.OnSignedInChanged -= OnMobileSignedInChanged;
            // Flush any queued unlocks
            foreach (var apiName in pendingMobileUnlocks)
            {
                MapAndUnlockMobile(apiName);
            }
            pendingMobileUnlocks.Clear();

            // Evaluate stat-based milestones now that we're signed in
            EvaluateMobileStatMilestones();
        }

        private void TryUnlockMobile(string apiName)
        {
            if (Cloud.IsSignedIn)
            {
                MapAndUnlockMobile(apiName);
            }
            else
            {
                // Defer until signed in
                pendingMobileUnlocks.Add(apiName);
                Cloud.OnSignedInChanged -= OnMobileSignedInChanged;
                Cloud.OnSignedInChanged += OnMobileSignedInChanged;
                // Proactively attempt sign-in to flush queued unlocks
                Cloud.SignIn();
            }
        }

        private static void MapAndUnlockMobile(string apiName)
        {
            switch (apiName)
            {
                case "MeetIvan":
                    Achievements.MeetIvan.Unlock();
                    break;
                case "MeetFarmers":
                    Achievements.MeetFarmers.Unlock();
                    break;
                case "MeetBarkley":
                    Achievements.MeetBarkley.Unlock();
                    break;
                case "MeetOldTimer":
                    Achievements.MeetOldTimer.Unlock();
                    break;
                case "MeetGill":
                    Achievements.MeetGill.Unlock();
                    break;
                case "Mildred":
                    Achievements.Mildred.Unlock();
                    break;
                // Death milestones
                case "Die10":
                    Achievements.Die10.Unlock();
                    break;
                case "Die250":
                    Achievements.Die250.Unlock();
                    break;
                case "Die1000":
                    Achievements.Die1000.Unlock();
                    break;
                // Distance milestones
                case "Reached100":
                    Achievements.Reached100.Unlock();
                    break;
                case "Reached1000":
                    Achievements.Reached1000.Unlock();
                    break;
                case "Reached10000":
                    Achievements.Reached10000.Unlock();
                    break;
                // Total kilometers
                case "Kilometers1":
                    Achievements.Kilometers1.Unlock();
                    break;
                case "Kilometers100":
                    Achievements.Kilometers100.Unlock();
                    break;
                case "Kilometers1000":
                    Achievements.Kilometers1000.Unlock();
                    break;
                // Tasks completed
                case "Tasks100":
                    Achievements.Tasks100.Unlock();
                    break;
                case "Tasks5k":
                    Achievements.Tasks5k.Unlock();
                    break;
                case "Tasks25k":
                    Achievements.Tasks25k.Unlock();
                    break;
                // Eva level
                case "EvaLevel10":
                    Achievements.EvaLevel10.Unlock();
                    break;
                case "EvaLevel50":
                    Achievements.EvaLevel50.Unlock();
                    break;
                case "EvaLevel100":
                    Achievements.EvaLevel100.Unlock();
                    break;
                case "EvaLevel200":
                    Achievements.EvaLevel200.Unlock();
                    break;
                case "EvaLevel500":
                    Achievements.EvaLevel500.Unlock();
                    break;
                // Ivan level
                case "IvanLevel10":
                    Achievements.IvanLevel10.Unlock();
                    break;
                case "IvanLevel50":
                    Achievements.IvanLevel50.Unlock();
                    break;
                case "IvanLevel100":
                    Achievements.IvanLevel100.Unlock();
                    break;
                case "IvanLevel500":
                    Achievements.IvanLevel500.Unlock();
                    break;
                case "IvanLevel1000":
                    Achievements.IvanLevel1000.Unlock();
                    break;
                default:
                    break;
            }
        }

        private void EvaluateMobileStatMilestones()
        {
            // Lazy fetch tracker
            tracker ??= TimelessEchoes.Stats.GameplayStatTracker.Instance;

            // Distance milestones (use both HighestDistance and CurrentRunDistance safeguards)
            float highest = tracker != null ? Mathf.Max(tracker.HighestDistance, tracker.CurrentRunDistance) : 0f;
            if (highest >= 100f) TryUnlockMobile("Reached100");
            if (highest >= 1000f) TryUnlockMobile("Reached1000");
            if (highest >= 10000f) TryUnlockMobile("Reached10000");

            // Total kilometers
            double km = tracker != null ? tracker.DistanceTravelled / 1000.0 : 0.0;
            if (km >= 1.0) TryUnlockMobile("Kilometers1");
            if (km >= 100.0) TryUnlockMobile("Kilometers100");
            if (km >= 1000.0) TryUnlockMobile("Kilometers1000");

            // Tasks completed
            int tasks = tracker != null ? tracker.TasksCompleted : 0;
            if (tasks >= 100) TryUnlockMobile("Tasks100");
            if (tasks >= 5000) TryUnlockMobile("Tasks5k");
            if (tasks >= 25000) TryUnlockMobile("Tasks25k");

            // Buffs cast thresholds: 10, 100, 1000, 2500, 9001
            int buffs = tracker != null ? tracker.BuffsCast : 0;
            if (buffs >= 10) TryUnlockMobile("Buffs1");
            if (buffs >= 100) TryUnlockMobile("Buffs2");
            if (buffs >= 1000) TryUnlockMobile("Buffs3");
            if (buffs >= 2500) TryUnlockMobile("Buffs4");
            if (buffs >= 9001) TryUnlockMobile("Buffs5");

            // Deaths thresholds: 10, 250, 1000
            int deaths = tracker != null ? tracker.Deaths : 0;
            if (deaths >= 10) TryUnlockMobile("Die10");
            if (deaths >= 250) TryUnlockMobile("Die250");
            if (deaths >= 1000) TryUnlockMobile("Die1000");

            // Eva/Ivan level milestones (from save data)
            int evaLevel = 0;
            int ivanLevel = 0;
            if (Blindsided.Oracle.oracle != null && Blindsided.Oracle.oracle.saveData != null)
            {
                evaLevel = Mathf.Max(Blindsided.Oracle.oracle.saveData.CauldronEvaLevel, 0);
                ivanLevel = Mathf.Max(Blindsided.Oracle.oracle.saveData.CraftingMasteryLevel, 0);
            }

            if (evaLevel >= 10) TryUnlockMobile("EvaLevel10");
            if (evaLevel >= 50) TryUnlockMobile("EvaLevel50");
            if (evaLevel >= 100) TryUnlockMobile("EvaLevel100");
            if (evaLevel >= 200) TryUnlockMobile("EvaLevel200");
            if (evaLevel >= 500) TryUnlockMobile("EvaLevel500");

            if (ivanLevel >= 10) TryUnlockMobile("IvanLevel10");
            if (ivanLevel >= 50) TryUnlockMobile("IvanLevel50");
            if (ivanLevel >= 100) TryUnlockMobile("IvanLevel100");
            if (ivanLevel >= 500) TryUnlockMobile("IvanLevel500");
            if (ivanLevel >= 1000) TryUnlockMobile("IvanLevel1000");
        }
#endif

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
            else if (npcId == "OldTimer1")
                UnlockAchievement("MeetOldTimer");
        }

        private void OnEnable()
        {
            OnQuestHandin += OnQuestHandinHandler;
            OnLoadData += OnLoadDataHandler;
#if UNITY_ANDROID || UNITY_IOS
            tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            if (tracker != null)
            {
                tracker.OnDistanceAdded += OnMobileDistanceAdded;
                tracker.OnRunEnded += OnMobileRunEnded;
                tracker.OnTaskCompletedEvent += OnMobileTaskCompleted;
            }
            var bm = TimelessEchoes.Buffs.BuffManager.Instance;
            if (bm != null)
            {
                bm.OnBuffCast += OnMobileBuffCast;
            }
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.HeroDied += OnMobileHeroDied;
            }
#endif
        }

        private void OnDisable()
        {
            OnQuestHandin -= OnQuestHandinHandler;
            OnLoadData -= OnLoadDataHandler;
#if UNITY_ANDROID || UNITY_IOS
            Cloud.OnSignedInChanged -= OnMobileSignedInChanged;
            if (tracker != null)
            {
                tracker.OnDistanceAdded -= OnMobileDistanceAdded;
                tracker.OnRunEnded -= OnMobileRunEnded;
                tracker.OnTaskCompletedEvent -= OnMobileTaskCompleted;
            }
            var bm = TimelessEchoes.Buffs.BuffManager.Instance;
            if (bm != null)
            {
                bm.OnBuffCast -= OnMobileBuffCast;
            }
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.HeroDied -= OnMobileHeroDied;
            }
#endif
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
#if UNITY_ANDROID || UNITY_IOS
            EvaluateMobileStatMilestones();
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
            // On mobile, we can't query native achievement states here in a unified way,
            // so we re-apply unlocks based on current game state. CloudOnce handles duplicates.
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

            // Evaluate stat-based achievements as well
            EvaluateMobileStatMilestones();
#endif
        }
    }
}
