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
        [SerializeField] private Disciple data;
        private bool dataAssigned;

        public void SetData(Disciple d)
        {
            data = d;
            dataAssigned = true;
            if (isActiveAndEnabled && !setup && RequirementsMet)
                LoadState();
        }

        private readonly Dictionary<Resource, double> stored = new();
        private readonly Dictionary<Resource, double> collectedTotals = new();

        private static Dictionary<string, Resource> lookup;
        private ResourceManager resourceManager;
        private bool setup;

        public float Interval => data != null ? data.generationInterval : 0f;
        public string DiscipleName => data != null ? data.name : string.Empty;
        public float Progress { get; private set; }
        public IReadOnlyList<Disciple.ResourceEntry> ResourceEntries => data ? data.resources : null;
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
            if (data == null || data.requiredQuest == null)
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(data.requiredQuest.questId, out var rec) && rec.Completed;
        }

        private void Awake()
        {
            OnSaveData += SaveState;
            OnLoadData += LoadState;
            OnQuestHandin += OnQuestHandinEvent;
            OnResetData += ResetState;
        }

        private void OnEnable()
        {
            if (!setup && dataAssigned && QuestCompleted())
                LoadState();
        }

        private void OnDisable()
        {
            SaveState();
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            OnQuestHandin -= OnQuestHandinEvent;
            OnResetData -= ResetState;
        }

        public void Tick(float deltaTime)
        {
            if (!setup || !RequirementsMet) return;

            Progress += deltaTime;
            while (Progress >= Interval && Interval > 0f)
            {
                Progress -= Interval;
                AddCycle();
            }

            // UI elements update themselves via DiscipleGeneratorUIManager
        }

        public void ApplyOfflineProgress(double seconds)
        {
            if (!setup || seconds <= 0 || !RequirementsMet) return;
            Progress += (float)seconds;
            while (Progress >= Interval && Interval > 0f)
            {
                Progress -= Interval;
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

            // Persist the updated resource totals immediately
            SaveState();
        }

        private void AddCycle()
        {
            if (data == null) return;
            foreach (var entry in data.resources)
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
            if (oracle.saveData.Disciples == null)
                oracle.saveData.Disciples = new Dictionary<string, GameData.DiscipleGenerationRecord>();

            string id = data != null ? data.name : gameObject.name;

            var rec = new GameData.DiscipleGenerationRecord
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
            oracle.saveData.Disciples[id] = rec;
        }

        private void ResetState()
        {
            stored.Clear();
            collectedTotals.Clear();
            Progress = 0f;
        }

        private void LoadState()
        {
            if (oracle == null || !QuestCompleted()) return;
            setup = true;
            EnsureLookup();
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            stored.Clear();
            collectedTotals.Clear();
            Progress = 0f;
            string id = data != null ? data.name : gameObject.name;
            if (oracle.saveData.Disciples.TryGetValue(id, out var rec) && rec != null)
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
            if (!setup && dataAssigned && data != null && data.requiredQuest != null && questId == data.requiredQuest.questId && QuestCompleted())
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