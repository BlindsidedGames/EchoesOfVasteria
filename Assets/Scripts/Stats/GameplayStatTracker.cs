#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Enemies;
using TimelessEchoes.Tasks;
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
        public float LongestRun { get; private set; }

        public float ShortestRun { get; private set; }

        public float AverageRun { get; private set; }

        public float MaxRunDistance { get; private set; } = 50f;

        public int CurrentRunKills { get; private set; }

        public double CurrentRunBonusResources { get; private set; }

        public float CurrentRunDistance { get; private set; }

        public bool RunInProgress { get; private set; }

        private Vector3 lastHeroPos;
        private static Dictionary<string, Resource> lookup;
        private static Dictionary<int, TaskData> taskLookup;

        private void Awake()
        {
            Instance = this;
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in Resources.LoadAll<Resource>(""))
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
        }

        private static void EnsureTaskLookup()
        {
            if (taskLookup != null) return;
            taskLookup = new Dictionary<int, TaskData>();
            foreach (var data in Resources.LoadAll<TaskData>(""))
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
#if !DISABLESTEAMWORKS
            SteamStatsUpdater.Instance?.UpdateStats();
#endif
        }

        public GameData.TaskRecord GetTaskRecord(TaskData data)
        {
            return data != null && taskRecords.TryGetValue(data, out var record) ? record : null;
        }

        public void AddDistance(float dist)
        {
            if (dist > 0f)
            {
                DistanceTravelled += dist;
                if (RunInProgress) OnDistanceAdded?.Invoke(dist);
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
                CurrentRunKills++;
        }

        public void AddDeath()
        {
            Deaths++;
        }

        public void AddDamageDealt(float amount)
        {
            if (amount > 0f)
            {
                DamageDealt += amount;
                if (RunInProgress)
                    currentRunDamageDealt += amount;
            }
        }

        public void AddDamageTaken(float amount)
        {
            if (amount > 0f)
            {
                DamageTaken += amount;
                if (RunInProgress)
                    currentRunDamageTaken += amount;
            }
        }

        public void AddTimesReaped()
        {
            TimesReaped++;
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
                }
            }
        }

        public void IncreaseMaxRunDistance(float amount)
        {
            if (amount <= 0f) return;
            MaxRunDistance += amount;
            SaveState();
        }

        public void BeginRun()
        {
            runStartTime = Time.time;
            lastHeroPos = Vector3.zero;
            RunInProgress = true;
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

        public void EndRun(bool died)
        {
            if (!RunInProgress)
                return;
            var record = new GameData.RunRecord
            {
                RunNumber = nextRunNumber,
                Duration = Time.time - runStartTime,
                Distance = CurrentRunDistance,
                TasksCompleted = currentRunTasks,
                ResourcesCollected = currentRunResources,
                BonusResourcesCollected = CurrentRunBonusResources,
                EnemiesKilled = CurrentRunKills,
                DamageDealt = currentRunDamageDealt,
                DamageTaken = currentRunDamageTaken,
                Died = died,
                Abandoned = false
            };
            AddRunRecord(record);
            nextRunNumber++;

            OnRunEnded?.Invoke(died);
            ResetCurrentRun();
        }

        public void AbandonRun()
        {
            if (!RunInProgress)
                return;
            var record = new GameData.RunRecord
            {
                RunNumber = nextRunNumber,
                Duration = Time.time - runStartTime,
                Distance = CurrentRunDistance,
                TasksCompleted = currentRunTasks,
                ResourcesCollected = currentRunResources,
                BonusResourcesCollected = CurrentRunBonusResources,
                EnemiesKilled = CurrentRunKills,
                DamageDealt = currentRunDamageDealt,
                DamageTaken = currentRunDamageTaken,
                Died = false,
                Abandoned = true
            };
            AddRunRecord(record);
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
#if !DISABLESTEAMWORKS
            SteamStatsUpdater.Instance?.UpdateStats();
#endif
        }
    }
}