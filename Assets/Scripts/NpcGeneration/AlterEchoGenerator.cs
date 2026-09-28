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
    public class AlterEchoGenerator : MonoBehaviour
    {
        [SerializeField] private Resource resource;

        private ResourceManager resourceManager;
        private bool setup;
        private GameData boundSaveData;
        private bool suppressDisableSave;

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
            if (!(ratePerMinute > 0d) || double.IsNaN(ratePerMinute) || double.IsInfinity(ratePerMinute))
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
            if (!suppressDisableSave)
                SaveState();
        }

        /// <summary>
        /// Captures this generator when it still belongs to the active save tree, then prevents
        /// Unity's deferred destruction callback from writing into a subsequently loaded slot.
        /// </summary>
        internal void PrepareForRebuild()
        {
            SaveState();
            suppressDisableSave = true;
        }

        internal bool IsOwnedBy(GameData data)
        {
            return setup && data != null && ReferenceEquals(boundSaveData, data);
        }

        public void Tick(float deltaTime)
        {
            if (!IsOwnedBy(oracle?.saveData) || resource == null) return;
            AdvanceProgress(deltaTime);
        }

        public void ApplyOfflineProgress(double seconds)
        {
            if (!IsOwnedBy(oracle?.saveData) || resource == null) return;
            AdvanceProgress(seconds);
        }

        public void CollectResources()
        {
            CollectResources(true);
        }

        public void CollectResources(bool triggerSave)
        {
            if (!IsOwnedBy(oracle?.saveData) || stored <= 0) return;
            resourceManager ??= ResourceManager.Instance;
            if (resourceManager == null)
            {
                Log("ResourceManager missing", TELogCategory.Resource, this);
                return;
            }

            resourceManager.Add(resource, stored, trackStats: false, eligibleForTierRoll: false);
            var existingCollected = totalCollected >= 0d && !double.IsNaN(totalCollected) &&
                                    !double.IsInfinity(totalCollected)
                ? totalCollected
                : 0d;
            totalCollected = stored >= double.MaxValue - existingCollected
                ? double.MaxValue
                : existingCollected + stored;
            stored = 0;
            SaveState();

            // Persist collected resources to in-memory save (defer disk write)
            if (triggerSave)
            {
                try
                {
                    Blindsided.EventHandler.SaveData();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"SaveData after alter-echo collect failed: {ex}");
                }
            }
        }

        private void AdvanceProgress(double seconds)
        {
            if (!(seconds > 0d) || double.IsNaN(seconds) || double.IsInfinity(seconds) ||
                !(Interval > 0f) || float.IsNaN(Interval) || float.IsInfinity(Interval) ||
                !(CycleAmount > 0d) || double.IsNaN(CycleAmount) || double.IsInfinity(CycleAmount))
                return;

            var existingProgress = Progress;
            if (!(existingProgress >= 0f) || float.IsNaN(existingProgress) || float.IsInfinity(existingProgress))
                existingProgress = 0f;

            var accumulated = existingProgress + seconds;
            if (double.IsNaN(accumulated) || double.IsInfinity(accumulated))
                return;

            var cycles = Math.Floor(accumulated / Interval);
            if (cycles > 0d)
            {
                var gained = cycles * CycleAmount;
                var existingStored = stored >= 0d && !double.IsNaN(stored) && !double.IsInfinity(stored)
                    ? stored
                    : 0d;
                if (double.IsInfinity(gained) || gained >= double.MaxValue - existingStored)
                    stored = double.MaxValue;
                else if (!double.IsNaN(gained) && gained > 0d)
                    stored = existingStored + gained;
            }

            var remainder = accumulated - cycles * Interval;
            Progress = remainder >= 0d && remainder < Interval && !double.IsNaN(remainder)
                ? (float)remainder
                : 0f;
        }

        private void SaveState()
        {
            var currentSaveData = oracle?.saveData;
            if (!setup || resource == null || currentSaveData == null ||
                !ReferenceEquals(boundSaveData, currentSaveData))
                return;

            currentSaveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            var rec = new GameData.DiscipleGenerationRecord
            {
                StoredResources = new Dictionary<string, double> { { resource.name, stored } },
                TotalCollected = new Dictionary<string, double> { { resource.name, totalCollected } },
                Progress = Progress,
                LastGenerationTime = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };
            currentSaveData.Disciples[resource.name] = rec;
        }

        private void ResetState()
        {
            stored = 0;
            totalCollected = 0;
            Progress = 0f;
        }

        private void LoadState()
        {
            var currentSaveData = oracle?.saveData;
            if (resource == null || currentSaveData == null)
                return;

            // A generator is owned by the save tree it was configured for. During a slot switch,
            // old generators remain alive until Unity processes Destroy at the end of the frame;
            // they must not bind themselves to the newly loaded tree in that window.
            if (boundSaveData != null && !ReferenceEquals(boundSaveData, currentSaveData))
                return;

            boundSaveData = currentSaveData;
            setup = true;
            currentSaveData.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();

            stored = 0;
            totalCollected = 0;
            Progress = 0f;

            if (currentSaveData.Disciples.TryGetValue(resource.name, out var rec) && rec != null)
            {
                if (rec.StoredResources != null &&
                    rec.StoredResources.TryGetValue(resource.name, out var savedStored) &&
                    savedStored >= 0d && !double.IsNaN(savedStored) && !double.IsInfinity(savedStored))
                    stored = savedStored;
                if (rec.TotalCollected != null &&
                    rec.TotalCollected.TryGetValue(resource.name, out var savedCollected) &&
                    savedCollected >= 0d && !double.IsNaN(savedCollected) && !double.IsInfinity(savedCollected))
                    totalCollected = savedCollected;
                Progress = rec.Progress >= 0f && !float.IsNaN(rec.Progress) && !float.IsInfinity(rec.Progress)
                    ? rec.Progress
                    : 0f;
            }
        }
    }
}

