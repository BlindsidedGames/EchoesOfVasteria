#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Enemies;
using TimelessEchoes.Tasks;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Stats
{
    [DefaultExecutionOrder(-1)]
    public class GameplayStatTracker : MonoBehaviour
    {
        public static GameplayStatTracker Instance { get; private set; }
        public event Action<float> OnDistanceAdded;
        public event Action<bool> OnRunEnded;
        private readonly Dictionary<TaskData, GameData.TaskRecord> taskRecords = new();

        private readonly List<GameData.RunRecord> recentRuns = new();
        private int nextRunNumber = 1;
        private float runStartTime;
        private int currentRunTasks;
        private double currentRunResources;
        private float currentRunDamageDealt;
        private float currentRunDamageTaken;
        private readonly Dictionary<string, GameData.MapStatistics> mapStats = new();
        private string currentMapKey;
        private readonly Dictionary<string, double> currentRunResourceAmounts = new();

        public float DistanceTravelled { get; private set; }

        public float HighestDistance { get; private set; }

        public int TotalKills { get; private set; }

        /// <summary>
        ///     Number of slime enemies defeated.
        /// </summary>
        public int SlimesKilled { get; private set; }

        public int TasksCompleted { get; private set; }

        public int Deaths { get; private set; }

        public float DamageDealt { get; private set; }

        public float DamageTaken { get; private set; }

        public int TimesReaped { get; private set; }

        /// <summary>
        ///     Number of times a buff has been cast.
        /// </summary>
        public int BuffsCast { get; private set; }

        public double TotalResourcesGathered { get; private set; }

        public IReadOnlyList<GameData.RunRecord> RecentRuns => recentRuns;
        public IReadOnlyDictionary<string, GameData.MapStatistics> MapStats => mapStats;
        public float LongestRun { get; private set; }

        public float ShortestRun { get; private set; }

        public float AverageRun { get; private set; }

        public float MaxRunDistance { get; private set; } = 50f;

        public int CurrentRunKills { get; private set; }

        public double CurrentRunBonusResources { get; private set; }

        public float CurrentRunDistance { get; private set; }

        public bool RunInProgress { get; private set; }

        private Vector3 lastHeroPos;
        public float CurrentRunSteps { get; private set; }
        public float LastRunSteps { get; private set; }
        public float LastRunDuration { get; private set; }
        // Aggregates across multiple runs until returning to town
        public int SessionDeaths { get; private set; }
        public int SessionReaps { get; private set; }
        public float SessionSteps { get; private set; }
        public float SessionDuration { get; private set; }
        private static Dictionary<string, Resource> lookup;
        private static Dictionary<int, TaskData> taskLookup;

        private void Awake()
        {
            Instance = this;
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void Start()
        {
            var rm = ResourceManager.Instance;
            if (rm != null)
                rm.OnResourceAdded += OnResourceAdded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            var rm = ResourceManager.Instance;
            if (rm != null)
                rm.OnResourceAdded -= OnResourceAdded;
        }

        private void OnResourceAdded(Resource resource, double amount, bool bonus)
        {
            if (!RunInProgress || resource == null) return;
            if (currentRunResourceAmounts.ContainsKey(resource.name))
                currentRunResourceAmounts[resource.name] += amount;
            else
                currentRunResourceAmounts[resource.name] = amount;
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
        }

        private static void EnsureTaskLookup()
        {
            if (taskLookup != null) return;
            taskLookup = new Dictionary<int, TaskData>();
            foreach (var data in Blindsided.Utilities.AssetCache.GetAll<TaskData>(""))
                if (data != null && !taskLookup.ContainsKey(data.taskID))
                    taskLookup[data.taskID] = data;
        }

        private void SaveState()
        {
            if (oracle == null) return;

            var t = new Dictionary<int, GameData.TaskRecord>();
            foreach (var pair in taskRecords)
                if (pair.Key != null)
                    t[pair.Key.taskID] = pair.Value;
            oracle.saveData.TaskRecords = t;

            var g = oracle.saveData.General ?? new GameData.GeneralStats();
            g.DistanceTravelled = DistanceTravelled;
            g.HighestDistance = HighestDistance;
            g.TotalKills = TotalKills;
            g.SlimesKilled = SlimesKilled;
            g.TasksCompleted = TasksCompleted;
            g.Deaths = Deaths;
            g.DamageDealt = DamageDealt;
            g.DamageTaken = DamageTaken;
            g.TimesReaped = TimesReaped;
            g.BuffsCast = BuffsCast;
            g.TotalResourcesGathered = TotalResourcesGathered;
            g.RecentRuns = new List<GameData.RunRecord>(recentRuns);
            g.LongestRun = LongestRun;
            g.ShortestRun = ShortestRun;
            g.AverageRun = AverageRun;
            g.MaxRunDistance = MaxRunDistance;
            g.NextRunNumber = nextRunNumber;
            oracle.saveData.General = g;

            oracle.saveData.MapStats = new Dictionary<string, GameData.MapStatistics>(mapStats);
        }

        private void LoadState()
        {
            if (oracle == null) return;
            EnsureLookup();

            oracle.saveData.TaskRecords ??= new Dictionary<int, GameData.TaskRecord>();
            oracle.saveData.General ??= new GameData.GeneralStats();

            taskRecords.Clear();
            EnsureTaskLookup();
            foreach (var pair in oracle.saveData.TaskRecords)
                if (taskLookup.TryGetValue(pair.Key, out var data))
                    taskRecords[data] = pair.Value;

            var g = oracle.saveData.General;
            DistanceTravelled = g.DistanceTravelled;
            HighestDistance = g.HighestDistance;
            TotalKills = g.TotalKills;
            SlimesKilled = g.SlimesKilled;
            TasksCompleted = g.TasksCompleted;
            Deaths = g.Deaths;
            DamageDealt = g.DamageDealt;
            DamageTaken = g.DamageTaken;
            TimesReaped = g.TimesReaped;
            BuffsCast = g.BuffsCast;
            TotalResourcesGathered = g.TotalResourcesGathered;
            recentRuns.Clear();
            if (g.RecentRuns != null)
                recentRuns.AddRange(g.RecentRuns);
            LongestRun = g.LongestRun;
            ShortestRun = g.ShortestRun;
            AverageRun = g.AverageRun;
            MaxRunDistance = g.MaxRunDistance > 0f ? g.MaxRunDistance : 50f;
            if (g.NextRunNumber > 0)
                nextRunNumber = g.NextRunNumber;
            else if (recentRuns.Count > 0)
                nextRunNumber = recentRuns[recentRuns.Count - 1].RunNumber + 1;
            else
                nextRunNumber = 1;

            oracle.saveData.MapStats ??= new Dictionary<string, GameData.MapStatistics>();
            mapStats.Clear();
            foreach (var pair in oracle.saveData.MapStats)
                if (pair.Key != null && pair.Value != null)
                    mapStats[pair.Key] = pair.Value;
        }

        public void RegisterTaskComplete(TaskData data, float duration, float xp)
        {
            if (data == null) return;

            if (!taskRecords.TryGetValue(data, out var record))
            {
                record = new GameData.TaskRecord();
                taskRecords[data] = record;
            }

            record.TotalCompleted += 1;
            record.TimeSpent += duration;
            record.XpGained += xp;
            TasksCompleted++;
            currentRunTasks++;
            var map = GetOrCreateCurrentMapStats();
            if (map != null)
                map.TasksCompleted++;
#if !DISABLESTEAMWORKS
            SteamStatsUpdater.Instance?.UpdateStats();
#endif
        }

        public GameData.TaskRecord GetTaskRecord(TaskData data)
        {
            return data != null && taskRecords.TryGetValue(data, out var record) ? record : null;
        }

        public GameData.MapStatistics GetMapStats(MapGenerationConfig config)
        {
            if (config == null) return null;
            mapStats.TryGetValue(config.name, out var stats);
            return stats;
        }

        private GameData.MapStatistics GetOrCreateCurrentMapStats()
        {
            if (string.IsNullOrEmpty(currentMapKey)) return null;
            if (!mapStats.TryGetValue(currentMapKey, out var stats))
            {
                stats = new GameData.MapStatistics();
                mapStats[currentMapKey] = stats;
            }
            return stats;
        }

        public void AddDistance(float dist)
        {
            if (dist > 0f)
            {
                DistanceTravelled += dist;
                if (RunInProgress)
                {
                    OnDistanceAdded?.Invoke(dist);
                    CurrentRunSteps += dist;
                    SessionSteps += dist;
                    var map = GetOrCreateCurrentMapStats();
                    if (map != null)
                        map.Steps += dist;
                }
            }
        }

        public void RecordHeroPosition(Vector3 position)
        {
            if (lastHeroPos == Vector3.zero)
            {
                lastHeroPos = position;
            }
            else
            {
                AddDistance(Vector3.Distance(position, lastHeroPos));
                lastHeroPos = position;
            }

            if (position.x > HighestDistance)
                HighestDistance = position.x;
            if (RunInProgress && position.x > CurrentRunDistance)
                CurrentRunDistance = position.x;
        }

        public void AddKill(EnemyData enemy)
        {
            TotalKills++;
            if (enemy != null && !string.IsNullOrEmpty(enemy.enemyName) &&
                enemy.enemyName.ToLowerInvariant().Contains("slime"))
                SlimesKilled++;
            if (RunInProgress)
            {
                CurrentRunKills++;
                var map = GetOrCreateCurrentMapStats();
                if (map != null)
                    map.Kills++;
            }
        }

        public void AddDeath()
        {
            Deaths++;
            if (RunInProgress)
            {
                SessionDeaths++;
                var map = GetOrCreateCurrentMapStats();
                if (map != null)
                    map.Deaths++;
            }
        }

        public void AddDamageDealt(float amount)
        {
            if (amount > 0f)
            {
                DamageDealt += amount;
                if (RunInProgress)
                {
                    currentRunDamageDealt += amount;
                    var map = GetOrCreateCurrentMapStats();
                    if (map != null)
                        map.DamageDealt += amount;
                }
            }
        }

        public void AddDamageTaken(float amount)
        {
            if (amount > 0f)
            {
                DamageTaken += amount;
                if (RunInProgress)
                {
                    currentRunDamageTaken += amount;
                    var map = GetOrCreateCurrentMapStats();
                    if (map != null)
                        map.DamageTaken += amount;
                }
            }
        }

        public void AddTimesReaped()
        {
            TimesReaped++;
            if (RunInProgress)
                SessionReaps++;
        }

        public void AddBuffCast()
        {
            BuffsCast++;
        }

        public void AddResources(double amount, bool bonus = false)
        {
            if (amount > 0)
            {
                TotalResourcesGathered += amount;
                if (RunInProgress)
                {
                    currentRunResources += amount;
                    if (bonus)
                        CurrentRunBonusResources += amount;
                    var map = GetOrCreateCurrentMapStats();
                    if (map != null)
                        map.ResourcesGathered += amount;
                }
            }
        }

        public void IncreaseMaxRunDistance(float amount)
        {
            if (amount <= 0f) return;
            MaxRunDistance += amount;
            SaveState();
        }

        public void BeginRun(MapGenerationConfig config)
        {
            currentMapKey = config != null ? config.name : null;
            runStartTime = Time.time;
            lastHeroPos = Vector3.zero;
            RunInProgress = true;
            CurrentRunSteps = 0f;
        }

        private void AddRunRecord(GameData.RunRecord record)
        {
            if (record == null || record.Distance <= 0f) return;
            recentRuns.Add(record);
            if (recentRuns.Count > 50)
                recentRuns.RemoveAt(0);

            if (record.Distance > LongestRun) LongestRun = record.Distance;
            if (ShortestRun <= 0f || record.Distance < ShortestRun) ShortestRun = record.Distance;

            var sum = 0f;
            foreach (var r in recentRuns) sum += r.Distance;
            AverageRun = recentRuns.Count > 0 ? sum / recentRuns.Count : 0f;
        }

        private void UpdateBestResourcePerMinute(float duration)
        {
            if (duration <= 0f) return;
            oracle.saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            foreach (var pair in currentRunResourceAmounts)
            {
                var perMinute = pair.Value * 60f / duration;
                if (oracle.saveData.Resources.TryGetValue(pair.Key, out var entry))
                {
                    if (perMinute > entry.BestPerMinute)
                        entry.BestPerMinute = perMinute;
                }
                else
                {
                    oracle.saveData.Resources[pair.Key] = new GameData.ResourceEntry
                    {
                        Earned = true,
                        Amount = 0,
                        BestPerMinute = perMinute
                    };
                }
            }
        }

        public void EndRun(bool died, bool reaped = false)
        {
            if (!RunInProgress)
                return;
            var duration = Time.time - runStartTime;
            LastRunDuration = duration;
            SessionDuration += duration;
            UpdateBestResourcePerMinute(duration);
            var record = new GameData.RunRecord
            {
                RunNumber = nextRunNumber,
                MapType = currentMapKey,
                Duration = duration,
                Distance = CurrentRunDistance,
                TasksCompleted = currentRunTasks,
                ResourcesCollected = currentRunResources,
                BonusResourcesCollected = CurrentRunBonusResources,
                EnemiesKilled = CurrentRunKills,
                DamageDealt = currentRunDamageDealt,
                DamageTaken = currentRunDamageTaken,
                Died = died,
                Reaped = reaped,
                Abandoned = false
            };
            AddRunRecord(record);
            LastRunSteps = CurrentRunSteps;
            var map = GetOrCreateCurrentMapStats();
            if (map != null)
            {
                if (CurrentRunDistance > map.LongestTrek)
                    map.LongestTrek = CurrentRunDistance;
            }
            nextRunNumber++;

            OnRunEnded?.Invoke(died);
            ResetCurrentRun();
        }

        public void AbandonRun()
        {
            if (!RunInProgress)
                return;
            var duration = Time.time - runStartTime;
            LastRunDuration = duration;
            SessionDuration += duration;
            UpdateBestResourcePerMinute(duration);
            var record = new GameData.RunRecord
            {
                RunNumber = nextRunNumber,
                MapType = currentMapKey,
                Duration = duration,
                Distance = CurrentRunDistance,
                TasksCompleted = currentRunTasks,
                ResourcesCollected = currentRunResources,
                BonusResourcesCollected = CurrentRunBonusResources,
                EnemiesKilled = CurrentRunKills,
                DamageDealt = currentRunDamageDealt,
                DamageTaken = currentRunDamageTaken,
                Died = false,
                Reaped = false,
                Abandoned = true
            };
            AddRunRecord(record);
            LastRunSteps = CurrentRunSteps;
            var map = GetOrCreateCurrentMapStats();
            if (map != null)
            {
                if (CurrentRunDistance > map.LongestTrek)
                    map.LongestTrek = CurrentRunDistance;
            }
            nextRunNumber++;
            OnRunEnded?.Invoke(false);
            ResetCurrentRun();
        }

        private void ResetCurrentRun()
        {
            CurrentRunDistance = 0f;
            currentRunTasks = 0;
            currentRunResources = 0;
            CurrentRunBonusResources = 0;
            CurrentRunKills = 0;
            currentRunDamageDealt = 0f;
            currentRunDamageTaken = 0f;
            lastHeroPos = Vector3.zero;
            runStartTime = Time.time;
            RunInProgress = false;
            currentMapKey = null;
            currentRunResourceAmounts.Clear();
            CurrentRunSteps = 0f;
#if !DISABLESTEAMWORKS
            SteamStatsUpdater.Instance?.UpdateStats();
#endif
        }

        /// <summary>
        ///     Resets session aggregates that span multiple runs until returning to town.
        ///     Call this before the first run of a session begins.
        /// </summary>
        public void BeginSession()
        {
            SessionDeaths = 0;
            SessionReaps = 0;
            SessionSteps = 0f;
            SessionDuration = 0f;
        }
    }
}