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
        private bool dataLoaded;
        private float readyAtTime;

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
        private int lastDistanceReachedSeasonal = -1;

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
            Blindsided.EventHandler.OnLoadData += MarkDataLoaded;
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

            // Avoid submitting at startup before systems and saves are ready
            if (!IsReadyToUpload())
                return;

            try
            {
                await UgsInitializer.EnsureInitializedAsync();
                var metadata = LeaderboardMetadata.Build();

                // Distance Reached (world units) -> int
                var distanceReached = Mathf.FloorToInt(tracker.HighestDistance);
                lastDistanceReached = await TrySubmitAsync(UgsLeaderboardIds.DistanceReached, distanceReached, lastDistanceReached, metadata);
                // If eligible, also submit to the seasonal board
                var oc = Blindsided.Oracle.oracle;
                if (oc != null && oc.IsSeasonalEligible())
                {
                    lastDistanceReachedSeasonal = await TrySubmitAsync(UgsLeaderboardIds.DistanceReachedSeasonal, distanceReached, lastDistanceReachedSeasonal, metadata);
                }
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

        private async Task<int> TrySubmitAsync(string leaderboardId, int score, int lastUploaded, object metadata)
        {
            // Never submit an initial zero at boot; wait until we have a meaningful value
            if (lastUploaded < 0 && score <= 0)
                return lastUploaded;

            if (score <= lastUploaded)
                return lastUploaded; // only submit improvements to reduce traffic; server policy still applies

            var options = new AddPlayerScoreOptions { Metadata = metadata };
            var res = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
            if (res != null)
                lastUploaded = (int)Math.Floor(res.Score);

            return lastUploaded;
        }

        private bool IsReadyToUpload()
        {
            if (dataLoaded)
                return true;
            // Fallback: small grace period to let systems initialize
            return Time.unscaledTime >= readyAtTime;
        }

        private void MarkDataLoaded()
        {
            dataLoaded = true;
        }

        private void OnDestroy()
        {
            Blindsided.EventHandler.OnLoadData -= MarkDataLoaded;
            if (instance == this)
                instance = null;
        }
    }
}
