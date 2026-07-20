using System;
using System.Threading.Tasks;
using TimelessEchoes.Stats;
using TimelessEchoes.Quests;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Core;
using UnityEngine;

namespace Blindsided.UGS
{
    /// <summary>
    /// Periodically uploads core gameplay stats to Unity Gaming Services leaderboards.
    /// Uses the same values as the Steam reporter to keep platforms in sync.
    /// </summary>
    public class UgsLeaderboardsReporter : MonoBehaviour
    {
        private static UgsLeaderboardsReporter instance;
        private bool dataLoaded;
        private float readyAtTime;

        public static UgsLeaderboardsReporter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<UgsLeaderboardsReporter>();
                    if (instance == null)
                        instance = new GameObject("UgsLeaderboardsReporter").AddComponent<UgsLeaderboardsReporter>();
                }
                return instance;
            }
        }

        // Ensure the reporter exists after each scene load without needing a scene reference.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            instance = null;
            if (FindAnyObjectByType<UgsLeaderboardsReporter>() == null)
            {
                new GameObject("UgsLeaderboardsReporter").AddComponent<UgsLeaderboardsReporter>();
            }
        }

        [SerializeField] private float updateInterval = 5f;
        private float lastUpdate;
        private bool forceUpload;

        // Track last values we've successfully sent to avoid redundant uploads.
        private double lastUploadedCompletionSeconds = -1d;
        private double lastUploadedCheaterSeconds = -1d;
        private double lastUploadedDistance = -1d;
        private int lastUploadedTasks = -1;

        private const double MinimumLegitSeconds = 3600d; // one hour
        private const double PlaytimeRatioThreshold = 0.5d;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Default small delay to allow systems to finish loading before first upload
            readyAtTime = Time.unscaledTime + 2f;

            // Consider the game fully loaded once save data is broadcast as loaded
            Blindsided.EventHandler.OnLoadData += OnSaveDataLoaded;
            HydrateUploadCache();
        }

        private void Update()
        {
            if (forceUpload || Time.unscaledTime - lastUpdate >= updateInterval)
            {
                forceUpload = false;
                lastUpdate = Time.unscaledTime;
                _ = TryUploadAllAsync();
            }
        }

        public void RequestUploadNow()
        {
            forceUpload = true;
        }

        private void HydrateUploadCache()
        {
            var general = Blindsided.Oracle.oracle?.saveData?.General;
            if (general == null)
                return;

            if (general.ConsoleUsed)
                return;
            lastUploadedCompletionSeconds = general.LastUploadedCompletionSeconds > 0d ? general.LastUploadedCompletionSeconds : -1d;
            lastUploadedCheaterSeconds = general.LastUploadedCompletionCheaterSeconds > 0d ? general.LastUploadedCompletionCheaterSeconds : -1d;
            lastUploadedDistance = general.LastUploadedDistanceReached > 0d ? general.LastUploadedDistanceReached : -1d;
            lastUploadedTasks = general.LastUploadedTasks > 0 ? general.LastUploadedTasks : -1;
        }

        private async Task TryUploadAllAsync()
        {
#if UNITY_EDITOR
            if (!UgsLeaderboardSettings.IsSubmissionEnabled)
                return;
#endif
            var tracker = GameplayStatTracker.Instance;
            if (tracker == null)
                return;

            // Avoid submitting at startup before systems and saves are ready
            if (!IsReadyToUpload())
                return;

            try
            {
                await UgsInitializer.EnsureInitializedAsync();

                await SubmitCompletionIfNeeded(tracker);
                await SubmitDistanceIfNeeded(tracker);
                await SubmitTasksIfNeeded(tracker);
            }
            catch (RequestFailedException ex)
            {
                Debug.LogWarning($"UGS leaderboard upload failed: {ex.ErrorCode} {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"UGS leaderboard upload failed: {ex.Message}");
            }
        }

        private bool IsReadyToUpload()
        {
            if (dataLoaded)
                return true;
            // Fallback: small grace period to let systems initialize
            return Time.unscaledTime >= readyAtTime;
        }

        private void OnSaveDataLoaded()
        {
            dataLoaded = true;
            HydrateUploadCache();
        }

        private void OnDestroy()
        {
            Blindsided.EventHandler.OnLoadData -= OnSaveDataLoaded;
            if (instance == this)
                instance = null;
        }

        private enum FraudReason
        {
            None,
            ConsoleUsed,
            BelowMinimumThreshold,
            ShorterThanPlaytime,
            Invalid
        }

        private async Task SubmitCompletionIfNeeded(GameplayStatTracker tracker)
        {
            var questManager = QuestManager.Instance;
            var oracle = Blindsided.Oracle.oracle;
            var saveData = oracle?.saveData;
            var general = saveData?.General;

            if (questManager == null || !questManager.AllQuestsComplete || saveData == null || general == null)
                return;

            var completionSeconds = Math.Max(0d, questManager.FinalQuestCompletionSeconds);
            if (completionSeconds <= 0d)
                return;

            var playTime = Math.Max(0d, saveData.PlayTime);
            var consoleUsed = general.ConsoleUsed;
            var fraud = EvaluateFraud(completionSeconds, playTime, consoleUsed);

            var metadata = BuildMetadata(fraud == FraudReason.None ? null : fraud.ToString());

            if (fraud == FraudReason.None)
            {
                // Avoid resubmitting unless we have a better time
                if (lastUploadedCompletionSeconds > 0d && completionSeconds >= lastUploadedCompletionSeconds - 0.5d)
                    return;
                await SubmitCompletionAsync(UgsLeaderboardIds.CompletionTime, completionSeconds, metadata, true);
            }
            else
            {
                if (lastUploadedCheaterSeconds > 0d && completionSeconds >= lastUploadedCheaterSeconds - 0.5d)
                    return;
                await SubmitCompletionAsync(UgsLeaderboardIds.CompletionTimeCheaters, completionSeconds, metadata, false);
            }
        }

        private async Task SubmitDistanceIfNeeded(GameplayStatTracker tracker)
        {
            var oracle = Blindsided.Oracle.oracle;
            var general = oracle?.saveData?.General;
            if (general == null || general.ConsoleUsed)
                return;

            var distance = Math.Floor(Math.Max(0d, tracker.HighestDistance));
            if (distance <= 0d)
                return;

            if (distance <= lastUploadedDistance)
                return;

            var metadata = BuildMetadata();
            await SubmitNumericAsync(UgsLeaderboardIds.DistanceReached, distance, metadata, score =>
            {
                lastUploadedDistance = score;
                general.LastUploadedDistanceReached = score;
            });
        }

        private async Task SubmitTasksIfNeeded(GameplayStatTracker tracker)
        {
            var oracle = Blindsided.Oracle.oracle;
            var general = oracle?.saveData?.General;
            if (general == null || general.ConsoleUsed)
                return;

            var totalTasks = Math.Max(0, tracker.TasksCompleted);
            if (totalTasks <= 0)
                return;

            if (totalTasks <= lastUploadedTasks)
                return;

            var metadata = BuildMetadata();
            await SubmitNumericAsync(UgsLeaderboardIds.Tasks, totalTasks, metadata, score =>
            {
                lastUploadedTasks = (int)score;
                general.LastUploadedTasks = (int)score;
            });
        }

        private FraudReason EvaluateFraud(double completionSeconds, double playTimeSeconds, bool consoleUsed)
        {
            if (consoleUsed)
                return FraudReason.ConsoleUsed;

            if (completionSeconds <= 0d)
                return FraudReason.Invalid;

            if (completionSeconds < MinimumLegitSeconds)
                return FraudReason.BelowMinimumThreshold;

            if (playTimeSeconds > 0d && completionSeconds < playTimeSeconds * PlaytimeRatioThreshold)
                return FraudReason.ShorterThanPlaytime;

            return FraudReason.None;
        }

        private async Task SubmitCompletionAsync(string leaderboardId, double seconds, object metadata, bool updateMainCache)
        {
            var general = Blindsided.Oracle.oracle?.saveData?.General;
            var score = Math.Max(0d, Math.Round(seconds, 2));
            var result = await SubmitScoreAsync(leaderboardId, score, metadata);
            if (result == null)
                return;

            var submitted = Math.Max(0d, Math.Round(result.Value, 2));
            if (updateMainCache)
            {
                lastUploadedCompletionSeconds = submitted;
                if (general != null)
                    general.LastUploadedCompletionSeconds = submitted;
            }
            else
            {
                lastUploadedCheaterSeconds = submitted;
                if (general != null)
                    general.LastUploadedCompletionCheaterSeconds = submitted;
            }
        }

        private async Task SubmitNumericAsync(string leaderboardId, double score, object metadata, Action<double> onSuccess)
        {
            var result = await SubmitScoreAsync(leaderboardId, score, metadata);
            if (result == null)
                return;
            onSuccess?.Invoke(result.Value);
        }

        private async Task<double?> SubmitScoreAsync(string leaderboardId, double score, object metadata)
        {
            try
            {
                var options = new AddPlayerScoreOptions { Metadata = metadata };
                var res = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
                return res?.Score;
            }
            catch (RequestFailedException ex)
            {
                Debug.LogWarning($"UGS leaderboard upload failed ({leaderboardId}): {ex.ErrorCode} {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"UGS leaderboard upload failed ({leaderboardId}): {ex.Message}");
                return null;
            }
        }

        private object BuildMetadata(string fraudReason = null)
        {
            var metadata = LeaderboardMetadata.Build();
            if (!string.IsNullOrEmpty(fraudReason) && metadata is System.Collections.Generic.Dictionary<string, object> dict)
            {
                dict["FraudReason"] = fraudReason;
            }
            return metadata;
        }
    }
}
