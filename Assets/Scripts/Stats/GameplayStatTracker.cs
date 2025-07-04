using System.Collections.Generic;
using UnityEngine;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Tasks;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Stats
{
    public class GameplayStatTracker : MonoBehaviour
    {
        private readonly Dictionary<TaskData, GameData.TaskRecord> taskRecords = new();

        private float distanceTravelled;
        private float highestDistance;
        private int totalKills;
        private int tasksCompleted;
        private int deaths;
        private float damageDealt;
        private float damageTaken;
        private double totalResourcesGathered;
        private readonly List<GameData.RunRecord> recentRuns = new();
        private int nextRunNumber = 1;
        private float currentRunDistance;
        private int currentRunTasks;
        private double currentRunResources;
        private int currentRunKills;
        private float currentRunDamageDealt;
        private float currentRunDamageTaken;
        private float longestRun;
        private float shortestRun;
        private float averageRun;

        public float DistanceTravelled => distanceTravelled;
        public float HighestDistance => highestDistance;
        public int TotalKills => totalKills;
        public int TasksCompleted => tasksCompleted;
        public int Deaths => deaths;
        public float DamageDealt => damageDealt;
        public float DamageTaken => damageTaken;
        public double TotalResourcesGathered => totalResourcesGathered;
        public IReadOnlyList<GameData.RunRecord> RecentRuns => recentRuns;
        public float LongestRun => longestRun;
        public float ShortestRun => shortestRun;
        public float AverageRun => averageRun;

        private Vector3 lastHeroPos;
        private static Dictionary<string, Resource> lookup;
        private static Dictionary<string, TaskData> taskLookup;

        private void Awake()
        {
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in Resources.LoadAll<Resource>(""))
            {
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
            }
        }

        private static void EnsureTaskLookup()
        {
            if (taskLookup != null) return;
            taskLookup = new Dictionary<string, TaskData>();
            foreach (var data in Resources.LoadAll<TaskData>(""))
            {
                if (data != null && !string.IsNullOrEmpty(data.taskID) && !taskLookup.ContainsKey(data.taskID))
                    taskLookup[data.taskID] = data;
            }
        }

        private void SaveState()
        {
            if (oracle == null) return;

            var t = new Dictionary<string, GameData.TaskRecord>();
            foreach (var pair in taskRecords)
                if (pair.Key != null)
                    t[pair.Key.taskID] = pair.Value;
            oracle.saveData.TaskRecords = t;

            var g = oracle.saveData.General ?? new GameData.GeneralStats();
            g.DistanceTravelled = distanceTravelled;
            g.HighestDistance = highestDistance;
            g.TotalKills = totalKills;
            g.TasksCompleted = tasksCompleted;
            g.Deaths = deaths;
            g.DamageDealt = damageDealt;
            g.DamageTaken = damageTaken;
            g.TotalResourcesGathered = totalResourcesGathered;
            g.RecentRuns = new List<GameData.RunRecord>(recentRuns);
            g.LongestRun = longestRun;
            g.ShortestRun = shortestRun;
            g.AverageRun = averageRun;
            g.NextRunNumber = nextRunNumber;
            oracle.saveData.General = g;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            EnsureLookup();

            oracle.saveData.TaskRecords ??= new Dictionary<string, GameData.TaskRecord>();
            oracle.saveData.General ??= new GameData.GeneralStats();

            taskRecords.Clear();
            EnsureTaskLookup();
            foreach (var pair in oracle.saveData.TaskRecords)
                if (taskLookup.TryGetValue(pair.Key, out var data))
                    taskRecords[data] = pair.Value;

            var g = oracle.saveData.General;
            distanceTravelled = g.DistanceTravelled;
            highestDistance = g.HighestDistance;
            totalKills = g.TotalKills;
            tasksCompleted = g.TasksCompleted;
            deaths = g.Deaths;
            damageDealt = g.DamageDealt;
            damageTaken = g.DamageTaken;
            totalResourcesGathered = g.TotalResourcesGathered;
            recentRuns.Clear();
            if (g.RecentRuns != null)
                recentRuns.AddRange(g.RecentRuns);
            longestRun = g.LongestRun;
            shortestRun = g.ShortestRun;
            averageRun = g.AverageRun;
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
            tasksCompleted++;
            currentRunTasks++;
        }

        public GameData.TaskRecord GetTaskRecord(TaskData data)
        {
            return data != null && taskRecords.TryGetValue(data, out var record) ? record : null;
        }

        public void AddDistance(float dist)
        {
            if (dist > 0f)
                distanceTravelled += dist;
        }

        public void RecordHeroPosition(Vector3 position)
        {
            if (lastHeroPos == Vector3.zero)
                lastHeroPos = position;
            else
            {
                AddDistance(Vector3.Distance(position, lastHeroPos));
                lastHeroPos = position;
            }
            if (position.x > highestDistance)
                highestDistance = position.x;
            if (position.x > currentRunDistance)
                currentRunDistance = position.x;
        }

        public void AddKill()
        {
            totalKills++;
            currentRunKills++;
        }

        public void AddDeath()
        {
            deaths++;
        }

        public void AddDamageDealt(float amount)
        {
            if (amount > 0f)
            {
                damageDealt += amount;
                currentRunDamageDealt += amount;
            }
        }

        public void AddDamageTaken(float amount)
        {
            if (amount > 0f)
            {
                damageTaken += amount;
                currentRunDamageTaken += amount;
            }
        }

        public void AddResources(double amount)
        {
            if (amount > 0)
            {
                totalResourcesGathered += amount;
                currentRunResources += amount;
            }
        }

        private void AddRunRecord(GameData.RunRecord record)
        {
            if (record == null || record.Distance <= 0f) return;
            recentRuns.Add(record);
            if (recentRuns.Count > 50)
                recentRuns.RemoveAt(0);

            if (record.Distance > longestRun) longestRun = record.Distance;
            if (shortestRun <= 0f || record.Distance < shortestRun) shortestRun = record.Distance;

            float sum = 0f;
            foreach (var r in recentRuns) sum += r.Distance;
            averageRun = recentRuns.Count > 0 ? sum / recentRuns.Count : 0f;
        }

        public void EndRun(bool died)
        {
            var record = new GameData.RunRecord
            {
                RunNumber = nextRunNumber,
                Distance = currentRunDistance,
                TasksCompleted = currentRunTasks,
                ResourcesCollected = currentRunResources,
                EnemiesKilled = currentRunKills,
                DamageDealt = currentRunDamageDealt,
                DamageTaken = currentRunDamageTaken,
                Died = died
            };
            AddRunRecord(record);
            nextRunNumber++;

            currentRunDistance = 0f;
            currentRunTasks = 0;
            currentRunResources = 0;
            currentRunKills = 0;
            currentRunDamageDealt = 0f;
            currentRunDamageTaken = 0f;
            lastHeroPos = Vector3.zero;
        }
    }
}
