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
    /// Periodically uploads core gameplay stats to Steam leaderboards.
    /// Distance (highest reached), DistanceTravelled (km), Kills, Tasks.
    /// </summary>
    public class SteamLeaderboardsReporter : MonoBehaviour
    {
#if !DISABLESTEAMWORKS
        private static SteamLeaderboardsReporter instance;
        public static SteamLeaderboardsReporter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SteamLeaderboardsReporter>();
                    if (instance == null)
                        instance = new GameObject("SteamLeaderboardsReporter").AddComponent<SteamLeaderboardsReporter>();
                }
                return instance;
            }
        }

        [SerializeField]
        private float updateInterval = 5f;
        private float lastUpdate;
        private bool forceUpload;

        private class BoardRef
        {
            public string name;
            public SteamLeaderboard_t handle;
            public bool ready;
            public int lastUploaded;
            public CallResult<LeaderboardFindResult_t> findResult;
            public CallResult<LeaderboardScoreUploaded_t> uploadResult;
        }

        private BoardRef distance;
        private BoardRef distanceTravelled;
        private BoardRef kills;
        private BoardRef tasks;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitBoards();
        }

        private void Update()
        {
            if (!SteamManager.Initialized)
                return;

            if (forceUpload || Time.unscaledTime - lastUpdate >= updateInterval)
            {
                forceUpload = false;
                lastUpdate = Time.unscaledTime;
                TryUploadAll();
            }
        }

        public void RequestUploadNow()
        {
            forceUpload = true;
        }

        private void InitBoards()
        {
            distance = CreateBoard("Distance");
            distanceTravelled = CreateBoard("DistanceTravelled");
            kills = CreateBoard("Kills");
            tasks = CreateBoard("Tasks");

            StartFind(distance);
            StartFind(distanceTravelled);
            StartFind(kills);
            StartFind(tasks);
        }

        private BoardRef CreateBoard(string name)
        {
            var board = new BoardRef
            {
                name = name,
                handle = new SteamLeaderboard_t(0),
                ready = false,
                lastUploaded = -1
            };

            board.findResult = CallResult<LeaderboardFindResult_t>.Create((res, fail) => OnFindLeaderboard(board, res, fail));
            board.uploadResult = CallResult<LeaderboardScoreUploaded_t>.Create((res, fail) => OnUploadComplete(board, res, fail));
            return board;
        }

        private void StartFind(BoardRef board)
        {
            if (!SteamManager.Initialized) return;
            var call = SteamUserStats.FindLeaderboard(board.name);
            board.findResult.Set(call);
        }

        private void OnFindLeaderboard(BoardRef board, LeaderboardFindResult_t result, bool ioFailure)
        {
            if (ioFailure)
                return;

            if (result.m_bLeaderboardFound == 1)
            {
                board.handle = result.m_hSteamLeaderboard;
                board.ready = true;
            }
            else
            {
                // Fallback to create if not found
                var call = SteamUserStats.FindOrCreateLeaderboard(
                    board.name,
                    ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
                    ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
                board.findResult.Set(call);
            }
        }

        private void OnUploadComplete(BoardRef board, LeaderboardScoreUploaded_t result, bool ioFailure)
        {
            if (ioFailure)
                return;
            if (result.m_bSuccess == 1)
            {
                board.lastUploaded = result.m_nScore;
            }
        }

        private void TryUploadAll()
        {
            var tracker = Stats.GameplayStatTracker.Instance;
            if (tracker == null)
                return;

            // Distance (highest reached) in world units, floored to int
            TryUpload(distance, Mathf.FloorToInt(tracker.HighestDistance));

            // DistanceTravelled reported as kilometers (int)
            TryUpload(distanceTravelled, Mathf.FloorToInt(tracker.DistanceTravelled / 1000f));

            // Total kills
            TryUpload(kills, tracker.TotalKills);

            // Total tasks completed
            TryUpload(tasks, tracker.TasksCompleted);
        }

        private void TryUpload(BoardRef board, int score)
        {
            if (!SteamManager.Initialized)
                return;

            if (board == null)
                return;

            if (!board.ready || board.handle.m_SteamLeaderboard == 0)
            {
                // Try to (re)acquire the board handle
                StartFind(board);
                return;
            }

            if (score <= board.lastUploaded)
                return; // KeepBest means only upload improvements

            var call = SteamUserStats.UploadLeaderboardScore(
                board.handle,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                null,
                0);
            board.uploadResult.Set(call);
        }
#endif
    }
}


