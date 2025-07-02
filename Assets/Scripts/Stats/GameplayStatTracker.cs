using System.Collections.Generic;
using UnityEngine;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Stats
{
    public class GameplayStatTracker : MonoBehaviour
    {
        private readonly Dictionary<string, int> taskCounts = new();
        private readonly Dictionary<string, float> taskTimes = new();
        private readonly Dictionary<Resource, int> itemsReceived = new();
        private readonly Dictionary<Resource, int> itemsSpent = new();

        private float distanceTravelled;
        private float highestDistance;
        private int totalKills;
        private int tasksCompleted;
        private int deaths;
        private float damageDealt;
        private float damageTaken;

        private Vector3 lastHeroPos;
        private static Dictionary<string, Resource> lookup;

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

        private void SaveState()
        {
            if (oracle == null) return;

            var t = new GameData.TaskStats();
            foreach (var pair in taskCounts)
                t.Completed[pair.Key] = pair.Value;
            foreach (var pair in taskTimes)
                t.TimeSpent[pair.Key] = pair.Value;
            oracle.saveData.Tasks = t;

            var i = new GameData.ItemStats();
            foreach (var pair in itemsReceived)
                if (pair.Key != null)
                    i.ItemsReceived[pair.Key.name] = pair.Value;
            foreach (var pair in itemsSpent)
                if (pair.Key != null)
                    i.ItemsSpent[pair.Key.name] = pair.Value;
            oracle.saveData.Items = i;

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

            oracle.saveData.Tasks ??= new GameData.TaskStats();
            oracle.saveData.Items ??= new GameData.ItemStats();
            oracle.saveData.General ??= new GameData.GeneralStats();

            taskCounts.Clear();
            foreach (var pair in oracle.saveData.Tasks.Completed)
                taskCounts[pair.Key] = pair.Value;
            taskTimes.Clear();
            foreach (var pair in oracle.saveData.Tasks.TimeSpent)
                taskTimes[pair.Key] = pair.Value;

            itemsReceived.Clear();
            foreach (var pair in oracle.saveData.Items.ItemsReceived)
                if (lookup.TryGetValue(pair.Key, out var res))
                    itemsReceived[res] = pair.Value;

            itemsSpent.Clear();
            foreach (var pair in oracle.saveData.Items.ItemsSpent)
                if (lookup.TryGetValue(pair.Key, out var res))
                    itemsSpent[res] = pair.Value;

            var g = oracle.saveData.General;
            distanceTravelled = g.DistanceTravelled;
            highestDistance = g.HighestDistance;
            totalKills = g.TotalKills;
            tasksCompleted = g.TasksCompleted;
            deaths = g.Deaths;
            damageDealt = g.DamageDealt;
            damageTaken = g.DamageTaken;
        }

        public void RegisterTaskComplete(string type, float duration)
        {
            if (string.IsNullOrEmpty(type)) return;

            if (taskCounts.ContainsKey(type))
                taskCounts[type] += 1;
            else
                taskCounts[type] = 1;

            if (taskTimes.ContainsKey(type))
                taskTimes[type] += duration;
            else
                taskTimes[type] = duration;

            tasksCompleted++;
        }

        public void AddItemReceived(Resource item, int count)
        {
            if (item == null || count <= 0) return;
            if (itemsReceived.ContainsKey(item))
                itemsReceived[item] += count;
            else
                itemsReceived[item] = count;
        }

        public void AddItemSpent(Resource item, int count)
        {
            if (item == null || count <= 0) return;
            if (itemsSpent.ContainsKey(item))
                itemsSpent[item] += count;
            else
                itemsSpent[item] = count;
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
