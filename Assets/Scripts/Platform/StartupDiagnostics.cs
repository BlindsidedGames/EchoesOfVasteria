using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TimelessEchoes.Platform
{
    /// <summary>Opt-in internal-build breadcrumbs, stored separately from player saves.</summary>
    public static class StartupDiagnostics
    {
        public enum TestMode { Normal, PauseBeforeActivation, NoGameCenterSignIn, NoOnlineServices, NoMainUi, Minimal }
        public static TestMode Mode { get; private set; }
        public static bool ChoiceReady { get; private set; }
        public static bool AwaitingActivation { get; set; }
        public static bool SkipGameCenter => EditorScreenshotSession || Enabled && ChoiceReady && (Mode == TestMode.NoGameCenterSignIn || Mode == TestMode.Minimal);
        public static bool SkipOnlineServices => EditorScreenshotSession || Enabled && ChoiceReady && (Mode == TestMode.NoOnlineServices || Mode == TestMode.Minimal);
        public static bool EditorScreenshotSession
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetBool("EOV.ScreenshotStudio.Active", false);
#else
                return false;
#endif
            }
        }
        public static string EditorScreenshotRoot
        {
            get
            {
#if UNITY_EDITOR
                return EditorScreenshotSession ? UnityEditor.SessionState.GetString("EOV.ScreenshotStudio.Root", "") : null;
#else
                return null;
#endif
            }
        }
        public static bool SkipMainUi => Enabled && ChoiceReady && (Mode == TestMode.NoMainUi || Mode == TestMode.Minimal);
        private static TaskCompletionSource<bool> selection = new();
        public static Task WaitForSelection() => !Enabled || ChoiceReady ? Task.CompletedTask : selection.Task;
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
        private static readonly object Gate = new();
        private static readonly Stopwatch Clock = new();
        private static Checkpoint current;
        private static Timer sampler;
        private static string checkpointPath;

        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class Checkpoint
        {
            public string version, phase, gameServices, utc, device, os, test, operation;
            public bool completed, nativeMemoryAvailable;
            public long allocatedBytes, physicalFootprintBytes, peakFootprintBytes;
            public long availableMemoryBytes, minimumAvailableMemoryBytes = -1;
            public double elapsedSeconds, mainThreadStallSeconds;
            public int memoryWarnings;
            public string lastException;
        }
        private static readonly Dictionary<string, Checkpoint> Results = new();
        public static string PreviousResults { get; private set; }
        private static string resultsPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection()
        {
            sampler?.Dispose();
            sampler = null;
            Active = false;
            ChoiceReady = false;
            AwaitingActivation = false;
            selection = new TaskCompletionSource<bool>();
        }

#if UNITY_IOS && !UNITY_EDITOR && EOV_STARTUP_DIAGNOSTICS
        [DllImport("__Internal")]
        private static extern int EovReadStartupMemory(out ulong footprint, out ulong available);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if EOV_STARTUP_DIAGNOSTICS
            sampler?.Dispose();
            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceivedThreaded -= OnLog;
            Application.quitting -= Stop;
            Active = false;
            Phase = "Starting";
            Progress = 0.02f;
            PreviousAttempt = null;
            GameServicesStatus = "Not requested";
            checkpointPath = Path.Combine(Application.persistentDataPath, "startup-diagnostics.json");
            resultsPath = Path.Combine(Application.persistentDataPath, "startup-results.json");
            Results.Clear();
            PreviousResults = "";
            try
            {
                if (File.Exists(resultsPath))
                {
                    var results = JsonConvert.DeserializeObject<Dictionary<string, Checkpoint>>(File.ReadAllText(resultsPath));
                    if (results != null) foreach (var pair in results) Results[pair.Key] = pair.Value;
                }
                if (File.Exists(checkpointPath))
                {
                    var previous = JsonConvert.DeserializeObject<Checkpoint>(File.ReadAllText(checkpointPath));
                    if (previous != null && !previous.completed)
                        PreviousAttempt = $"Last incomplete launch ({previous.version}, {previous.test ?? "Normal"}): {previous.phase}\n" + Describe(previous);
                }
                foreach (var pair in Results)
                    PreviousResults += $"{pair.Key}: {(pair.Value.completed ? "Reached town" : pair.Value.phase)} · {pair.Value.elapsedSeconds:0.0}s\n";
            }
            catch (Exception) { /* Diagnostics must never prevent startup. */ }
            Clock.Restart();
            lastHeartbeat = 0;
            current = new Checkpoint { version = Application.version, device = SystemInfo.deviceModel,
                os = SystemInfo.operatingSystem, phase = Phase, gameServices = GameServicesStatus };
            Application.lowMemory += OnLowMemory;
            Application.logMessageReceivedThreaded += OnLog;
            Application.quitting += Stop;
            var driver = new GameObject("Internal startup diagnostics").AddComponent<StartupDiagnosticMenu>();
            UnityEngine.Object.DontDestroyOnLoad(driver.gameObject);
#endif
        }

        public static void Choose(TestMode mode)
        {
            if (!Enabled || ChoiceReady) return;
            Mode = mode;
            current.test = mode.ToString();
            ChoiceReady = true;
            Active = true;
            Clock.Restart();
            CaptureMainThread();
            // Continue sampling during a blocked main thread without calling Unity APIs.
            sampler = new Timer(_ => Sample(), null, 500, 500);
            selection.TrySetResult(true);
        }

        private static double lastHeartbeat;
        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void Heartbeat()
        {
            if (!Active) return;
            lock (Gate) lastHeartbeat = Clock.Elapsed.TotalSeconds;
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void SetPhase(string phase, float progress)
        {
            if (!Active) return;
            Phase = phase;
            Progress = Mathf.Clamp01(progress);
            CaptureMainThread();
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void SetGameServicesStatus(string status)
        {
            lock (Gate)
            {
                if (!Active || current == null) return;
                GameServicesStatus = status;
                current.gameServices = status;
                SampleLocked();
            }
        }

        private static void CaptureMainThread()
        {
            var allocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            lock (Gate)
            {
                if (!Active || current == null) return;
                current.phase = Phase;
                current.operation = null;
                current.allocatedBytes = allocated;
                lastHeartbeat = Clock.Elapsed.TotalSeconds;
                SampleLocked();
            }
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void Operation(string description)
        {
            lock (Gate)
            {
                if (!Active || current == null) return;
                current.operation = description;
                SampleLocked();
            }
        }

        private static void Sample()
        {
            lock (Gate)
            {
                if (Active && current != null) SampleLocked();
            }
        }

        private static void SampleLocked()
        {
            try
            {
                current.elapsedSeconds = Clock.Elapsed.TotalSeconds;
                current.mainThreadStallSeconds = Math.Max(0, current.elapsedSeconds - lastHeartbeat);
                current.utc = DateTime.UtcNow.ToString("O");
#if UNITY_IOS && !UNITY_EDITOR && EOV_STARTUP_DIAGNOSTICS
                if (EovReadStartupMemory(out var footprint, out var available) != 0)
                {
                    current.nativeMemoryAvailable = true;
                    current.physicalFootprintBytes = (long)footprint;
                    current.availableMemoryBytes = (long)available;
                    current.peakFootprintBytes = Math.Max(current.peakFootprintBytes, (long)footprint);
                    current.minimumAvailableMemoryBytes = current.minimumAvailableMemoryBytes < 0 ?
                        (long)available : Math.Min(current.minimumAvailableMemoryBytes, (long)available);
                }
#endif
                Directory.CreateDirectory(Path.GetDirectoryName(checkpointPath));
                // Keep the previous valid checkpoint if termination interrupts the new write.
                WriteAtomic(checkpointPath, JsonConvert.SerializeObject(current));
                Results[current.test ?? "Normal"] = current;
                WriteAtomic(resultsPath, JsonConvert.SerializeObject(Results));
            }
            catch (Exception) { /* A failed diagnostic write must not affect the game. */ }
        }

        private static void WriteAtomic(string path, string json)
        {
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }

        private static void OnLowMemory()
        {
            lock (Gate)
            {
                if (!Active || current == null) return;
                current.memoryWarnings++;
                SampleLocked();
            }
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Assert && type != LogType.Error) return;
            lock (Gate)
            {
                if (!Active || current == null) return;
                var detail = type + ": " + message + "\n" + stack;
                current.lastException = detail.Substring(0, Math.Min(detail.Length, 4000));
                SampleLocked();
            }
        }

        public static string CurrentDetails
        {
            get { lock (Gate) return current == null ? "" : Describe(current); }
        }

        private static string Describe(Checkpoint value)
        {
            var text = $"{value.elapsedSeconds:0.0}s elapsed · main thread gap {value.mainThreadStallSeconds:0.0}s\n";
            text += value.nativeMemoryAvailable ?
                $"Memory {value.physicalFootprintBytes / 1048576.0:0} MiB · sampled peak {value.peakFootprintBytes / 1048576.0:0} MiB\n" +
                $"Headroom {value.availableMemoryBytes / 1048576.0:0} MiB · lowest {value.minimumAvailableMemoryBytes / 1048576.0:0} MiB\n" :
                $"Unity allocations {value.allocatedBytes / 1048576.0:0} MiB (native memory unavailable)\n";
            text += $"Memory warnings {value.memoryWarnings} · Game Center: {value.gameServices}";
            if (!string.IsNullOrEmpty(value.operation)) text += "\nLast operation: " + value.operation;
            if (!string.IsNullOrEmpty(value.lastException))
                text += "\nIssue: " + value.lastException.Split('\n')[0];
            return text;
        }

        [Conditional("EOV_STARTUP_DIAGNOSTICS")]
        public static void Complete()
        {
            lock (Gate)
            {
                if (!Active || current == null) return;
                Phase = current.phase = "Ready";
                Progress = 1;
                current.completed = true;
                SampleLocked();
                Active = false;
            }
            Stop();
        }

        private static void Stop()
        {
            sampler?.Dispose();
            sampler = null;
            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceivedThreaded -= OnLog;
            Application.quitting -= Stop;
        }

        public static void EndSession()
        {
            lock (Gate) Active = false;
            Stop();
        }
    }
}
