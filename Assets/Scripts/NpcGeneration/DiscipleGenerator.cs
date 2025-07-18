using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Quests;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Generates resources over time for a single disciple.
    /// </summary>
    public class DiscipleGenerator : MonoBehaviour
    {
        [Serializable]
        public class ResourceEntry
        {
            public Resource resource;
            public double amount = 1;
        }

        [SerializeField] private Disciple data;
        [SerializeField] private List<ResourceEntry> resources = new();
        [SerializeField] private QuestData requiredQuest;
        [SerializeField] private float generationInterval = 5f;

        public void SetData(Disciple d)
        {
            data = d;
            ApplyData();
        }

        private void ApplyData()
        {
            if (data == null)
                return;
            generationInterval = data.generationInterval;
            requiredQuest = data.requiredQuest;
            resources = new List<ResourceEntry>();
            foreach (var entry in data.resources)
            {
                if (entry == null) continue;
                var copy = new ResourceEntry { resource = entry.resource, amount = entry.amount };
                resources.Add(copy);
            }
        }

        private readonly Dictionary<Resource, double> stored = new();
        private readonly Dictionary<Resource, double> collectedTotals = new();

        private static Dictionary<string, Resource> lookup;
        private ResourceManager resourceManager;
        private bool setup;

        public float Interval => generationInterval;
        public float Progress { get; private set; }
        public IReadOnlyList<ResourceEntry> ResourceEntries => resources;
        public bool RequirementsMet => QuestCompleted();

        public double GetStoredAmount(Resource resource)
        {
            return stored.TryGetValue(resource, out var val) ? val : 0;
        }

        public double GetTotalCollected(Resource resource)
        {
            return collectedTotals.TryGetValue(resource, out var val) ? val : 0;
        }

        private bool QuestCompleted()
        {
            if (requiredQuest == null)
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(requiredQuest.questId, out var rec) && rec.Completed;
        }

        private void Awake()
        {
            OnSaveData += SaveState;
            OnLoadData += LoadState;
            OnQuestHandin += OnQuestHandinEvent;
            ApplyData();
        }

        private void OnEnable()
        {
            if (!setup && QuestCompleted())
                LoadState();
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            OnQuestHandin -= OnQuestHandinEvent;
        }

        public void Tick(float deltaTime)
        {
            if (!setup) return;

            Progress += deltaTime;
            while (Progress >= generationInterval && generationInterval > 0f)
            {
                Progress -= generationInterval;
                AddCycle();
            }

            // UI elements update themselves via DiscipleGeneratorUIManager
        }

        public void ApplyOfflineProgress(double seconds)
        {
            if (!setup || seconds <= 0) return;
            Progress += (float)seconds;
            while (Progress >= generationInterval && generationInterval > 0f)
            {
                Progress -= generationInterval;
                AddCycle();
            }

            // UI elements update themselves via DiscipleGeneratorUIManager
        }

        public void CollectResources()
        {
            if (!setup) return;
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }
            if (resourceManager == null) return;

            foreach (var pair in stored)
            {
                resourceManager.Add(pair.Key, pair.Value);
                if (collectedTotals.ContainsKey(pair.Key))
                    collectedTotals[pair.Key] += pair.Value;
                else
                    collectedTotals[pair.Key] = pair.Value;
            }

            stored.Clear();
        }

        private void AddCycle()
        {
            foreach (var entry in resources)
            {
                if (entry.resource == null || entry.amount <= 0) continue;
                if (stored.ContainsKey(entry.resource))
                    stored[entry.resource] += entry.amount;
                else
                    stored[entry.resource] = entry.amount;
            }
        }

        private void SaveState()
        {
            if (!setup || oracle == null) return;
            if (oracle.saveData.NpcGeneration == null)
                oracle.saveData.NpcGeneration = new Dictionary<string, GameData.NpcGenerationRecord>();

            string id = gameObject.name;

            var rec = new GameData.NpcGenerationRecord
            {
                StoredResources = new Dictionary<string, double>(),
                TotalCollected = new Dictionary<string, double>(),
                Progress = Progress,
                LastGenerationTime = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };
            foreach (var pair in stored)
                if (pair.Key != null)
                    rec.StoredResources[pair.Key.name] = pair.Value;
            foreach (var pair in collectedTotals)
                if (pair.Key != null)
                    rec.TotalCollected[pair.Key.name] = pair.Value;
            oracle.saveData.NpcGeneration[id] = rec;
        }

        private void LoadState()
        {
            if (oracle == null || !QuestCompleted()) return;
            setup = true;
            EnsureLookup();
            oracle.saveData.NpcGeneration ??= new Dictionary<string, GameData.NpcGenerationRecord>();

            stored.Clear();
            collectedTotals.Clear();
            Progress = 0f;
            string id = gameObject.name;
            if (oracle.saveData.NpcGeneration.TryGetValue(id, out var rec) && rec != null)
            {
                foreach (var pair in rec.StoredResources)
                    if (lookup.TryGetValue(pair.Key, out var res) && res != null)
                        stored[res] = pair.Value;
                foreach (var pair in rec.TotalCollected)
                    if (lookup.TryGetValue(pair.Key, out var res) && res != null)
                        collectedTotals[res] = pair.Value;
                Progress = rec.Progress;

                var now = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
                var seconds = now - rec.LastGenerationTime;
                if (seconds > 0)
                    ApplyOfflineProgress(seconds);
            }

            // UI will be created by DiscipleGeneratorUIManager
        }

        private void OnQuestHandinEvent(string questId)
        {
            if (!setup && requiredQuest != null && questId == requiredQuest.questId && QuestCompleted())
                LoadState();
        }

        private static void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Resource>();
            foreach (var res in Resources.LoadAll<Resource>(""))
                if (res != null && !lookup.ContainsKey(res.name))
                    lookup[res.name] = res;
        }
    }
}