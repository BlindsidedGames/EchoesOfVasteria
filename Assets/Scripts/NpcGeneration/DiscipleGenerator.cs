using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Generates a single resource over time based on the player's best collection rate.
    /// </summary>
    public class DiscipleGenerator : MonoBehaviour
    {
        [SerializeField] private Resource resource;

        private ResourceManager resourceManager;
        private bool setup;

        private double stored;
        private double totalCollected;

        private double ratePerMinute;

        public Resource Resource => resource;
        public float Interval { get; private set; }
        public double CycleAmount { get; private set; }
        public float Progress { get; private set; }
        public bool RequirementsMet => true;

        public void Configure(Resource res, double rate)
        {
            resource = res;
            UpdateRate(rate);
            if (isActiveAndEnabled)
                LoadState();
        }

        public void UpdateRate(double rate)
        {
            ratePerMinute = rate;
            if (ratePerMinute <= 0)
            {
                Interval = 0f;
                CycleAmount = 0;
                return;
            }

            if (ratePerMinute <= 60)
            {
                Interval = (float)(60.0 / ratePerMinute);
                CycleAmount = 1;
            }
            else
            {
                Interval = 1f;
                CycleAmount = ratePerMinute / 60.0;
            }
        }

        public double GetStoredAmount(Resource r) => r == resource ? stored : 0;
        public double GetTotalCollected(Resource r) => r == resource ? totalCollected : 0;

        private void Awake()
        {
            OnSaveData += SaveState;
            OnLoadData += LoadState;
            OnResetData += ResetState;
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            OnResetData -= ResetState;
        }

        private void OnEnable()
        {
            if (!setup && resource != null)
                LoadState();
        }

        private void OnDisable()
        {
            SaveState();
        }

        public void Tick(float deltaTime)
        {
            if (!setup || Interval <= 0f || resource == null) return;

            Progress += deltaTime;
            while (Progress >= Interval)
            {
                Progress -= Interval;
                AddCycle();
            }
        }

        public void ApplyOfflineProgress(double seconds)
        {
            if (!setup || seconds <= 0 || Interval <= 0f || resource == null) return;

            Progress += (float)seconds;
            while (Progress >= Interval)
            {
                Progress -= Interval;
                AddCycle();
            }
        }

        public void CollectResources()
        {
            if (!setup || stored <= 0) return;
            resourceManager ??= ResourceManager.Instance;
            if (resourceManager == null)
            {
                Log("ResourceManager missing", TELogCategory.Resource, this);
                return;
            }

            resourceManager.Add(resource, stored, trackStats: false);
            totalCollected += stored;
            stored = 0;
            SaveState();

            // Persist collected resources to in-memory save (defer disk write)
            try
            {
                Blindsided.EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveData after disciple collect failed: {ex}");
            }
        }

        private void AddCycle()
        {
            stored += CycleAmount;
        }

        private void SaveState()
        {
            if (!setup || oracle == null || resource == null) return;
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            var rec = new GameData.DiscipleGenerationRecord
            {
                StoredResources = new Dictionary<string, double> { { resource.name, stored } },
                TotalCollected = new Dictionary<string, double> { { resource.name, totalCollected } },
                Progress = Progress,
                LastGenerationTime = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };
            oracle.saveData.Disciples[resource.name] = rec;
        }

        private void ResetState()
        {
            stored = 0;
            totalCollected = 0;
            Progress = 0f;
        }

        private void LoadState()
        {
            if (oracle == null || resource == null) return;
            setup = true;
            oracle.saveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            stored = 0;
            totalCollected = 0;
            Progress = 0f;

            if (oracle.saveData.Disciples.TryGetValue(resource.name, out var rec) && rec != null)
            {
                if (rec.StoredResources != null)
                    rec.StoredResources.TryGetValue(resource.name, out stored);
                if (rec.TotalCollected != null)
                    rec.TotalCollected.TryGetValue(resource.name, out totalCollected);
                Progress = rec.Progress;

                var now = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
                var seconds = now - rec.LastGenerationTime;
                if (seconds > 0)
                    ApplyOfflineProgress(seconds);
            }
        }
    }
}

