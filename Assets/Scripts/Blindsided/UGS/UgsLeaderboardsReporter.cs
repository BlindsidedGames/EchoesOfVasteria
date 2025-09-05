using System;
using System.Threading.Tasks;
using TimelessEchoes.Stats;
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

        public static UgsLeaderboardsReporter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UgsLeaderboardsReporter>();
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
            if (FindFirstObjectByType<UgsLeaderboardsReporter>() == null)
            {
                new GameObject("UgsLeaderboardsReporter").AddComponent<UgsLeaderboardsReporter>();
            }
        }

        [SerializeField] private float updateInterval = 5f;
        private float lastUpdate;
        private bool forceUpload;

        // Track last values we've successfully sent to avoid redundant uploads.
        private int lastDistanceReached = -1;
        private int lastDistanceTravelledKm = -1;
        private int lastKills = -1;
        private int lastTasks = -1;

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

        private async Task TryUploadAllAsync()
        {
            var tracker = GameplayStatTracker.Instance;
            if (tracker == null)
                return;

            try
            {
                await UgsInitializer.EnsureInitializedAsync();

                // Distance Reached (world units) -> int
                var distanceReached = Mathf.FloorToInt(tracker.HighestDistance);
                lastDistanceReached = await TrySubmitAsync(UgsLeaderboardIds.DistanceReached, distanceReached, lastDistanceReached);

                // Distance Travelled as kilometers (int)
                var distanceKm = Mathf.FloorToInt((float)(tracker.DistanceTravelled / 1000.0));
                lastDistanceTravelledKm = await TrySubmitAsync(UgsLeaderboardIds.DistanceTravelled, distanceKm, lastDistanceTravelledKm);

                // Total kills
                lastKills = await TrySubmitAsync(UgsLeaderboardIds.Kills, tracker.TotalKills, lastKills);

                // Total tasks
                lastTasks = await TrySubmitAsync(UgsLeaderboardIds.Tasks, tracker.TasksCompleted, lastTasks);
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

        private async Task<int> TrySubmitAsync(string leaderboardId, int score, int lastUploaded)
        {
            if (score <= lastUploaded)
                return lastUploaded; // only submit improvements to reduce traffic; server policy still applies

            var res = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            if (res != null)
                lastUploaded = (int)Math.Floor(res.Score);

            return lastUploaded;
        }
    }
}
