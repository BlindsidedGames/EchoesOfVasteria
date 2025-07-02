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
        }

        public void AddKill()
        {
            totalKills++;
        }

        public void AddDeath()
        {
            deaths++;
        }

        public void AddDamageDealt(float amount)
        {
            if (amount > 0f) damageDealt += amount;
        }

        public void AddDamageTaken(float amount)
        {
            if (amount > 0f) damageTaken += amount;
        }
    }
}
