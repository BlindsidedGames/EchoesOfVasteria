using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace TimelessEchoes.Platform
{
    /// <summary>Opt-in internal-build breadcrumbs, stored separately from player saves.</summary>
    public static class StartupDiagnostics
    {
#if EOV_STARTUP_DIAGNOSTICS
        public static bool Enabled => true;
#else
        public static bool Enabled => false;
#endif
        public static bool Active { get; private set; }
        public static string Phase { get; private set; } = "Starting";
        public static float Progress { get; private set; }
        public static string PreviousAttempt { get; private set; }
        public static string GameServicesStatus { get; private set; } = "Not requested";

        [Serializable]
        private sealed class Checkpoint
        {
            public string version;
            public string phase;
            public string gameServices;
            public bool completed;
            public string utc;
            public long allocatedBytes;
        }

        private static string checkpointPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if EOV_STARTUP_DIAGNOSTICS
            Active = true;
            Phase = "Starting";
            Progress = 0.02f;
            PreviousAttempt = null;
            GameServicesStatus = "Not requested";
            checkpointPath = Path.Combine(Application.persistentDataPath, "startup-diagnostics.json");
            try
            {
                if (File.Exists(checkpointPath))
                {
                    var previous = JsonUtility.FromJson<Checkpoint>(File.ReadAllText(checkpointPath));
                    if (previous != null && !previous.completed)
                        PreviousAttempt = $"Last incomplete launch ({previous.version}): {previous.phase}. Game Center: {previous.gameServices}.";
                }
            }
            catch (Exception) { /* Diagnostics must never prevent startup. */ }
            Persist(false);
#endif
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void SetPhase(string phase, float progress)
        {
            if (!Active) return;
            Phase = phase;
            Progress = Mathf.Clamp01(progress);
            Persist(false);
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void SetGameServicesStatus(string status)
        {
            if (!Active) return;
            GameServicesStatus = status;
            Persist(false);
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void Complete()
        {
            if (!Active) return;
            Phase = "Ready";
            Progress = 1;
            Persist(true);
            Active = false;
        }

        private static void Persist(bool completed)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(checkpointPath));
                var record = new Checkpoint
                {
                    version = Application.version,
                    phase = Phase,
                    gameServices = GameServicesStatus,
                    completed = completed,
                    utc = DateTime.UtcNow.ToString("O"),
                    allocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()
                };
                File.WriteAllText(checkpointPath, JsonUtility.ToJson(record, true));
                UnityEngine.Debug.Log($"[Startup diagnostics] {Phase}; Game Center: {GameServicesStatus}; allocated: {record.allocatedBytes}");
            }
            catch (Exception) { /* A failed diagnostic write must not affect the game. */ }
        }
    }
}
