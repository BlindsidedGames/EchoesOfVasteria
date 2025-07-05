using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.SaveData;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Generates resources over time for a single NPC.
    /// </summary>
    public class NPCResourceGenerator : MonoBehaviour
    {
        [Serializable]
        public class ResourceEntry
        {
            public Resource resource;
            public double amount = 1;
        }

        [SerializeField] private string npcId;
        [SerializeField] private List<ResourceEntry> resources = new();
        [SerializeField] private float generationInterval = 5f;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressImage;
        [SerializeField] private Transform progressUIParent;
        [SerializeField] private NpcGeneratorProgressUI progressUIPrefab;

        private readonly Dictionary<Resource, double> stored = new();
        private readonly List<NpcGeneratorProgressUI> uiEntries = new();
        private float progress;

        private static Dictionary<string, Resource> lookup;
        private ResourceManager resourceManager;

        public string NpcId => npcId;
        public float Interval => generationInterval;
        public float Progress => progress;
        public double GetStoredAmount(Resource resource)
        {
            return stored.TryGetValue(resource, out var val) ? val : 0;
        }

        private void Awake()
        {
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void Start()
        {
            LoadState();
            BuildProgressUI();
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        public void Tick(float deltaTime)
        {
            progress += deltaTime;
            while (progress >= generationInterval && generationInterval > 0f)
            {
                progress -= generationInterval;
                AddCycle();
            }
            UpdateUI();
        }

        public void ApplyOfflineProgress(double seconds)
        {
            if (seconds <= 0) return;
            progress += (float)seconds;
            while (progress >= generationInterval && generationInterval > 0f)
            {
                progress -= generationInterval;
                AddCycle();
            }
            UpdateUI();
        }

        public void CollectResources()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceManager == null) return;

            foreach (var pair in stored)
            {
                resourceManager.Add(pair.Key, pair.Value);
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

        private void UpdateUI()
        {
            float pct = generationInterval > 0f ? Mathf.Clamp01(progress / generationInterval) : 0f;
            if (progressSlider != null)
                progressSlider.value = pct;
            if (progressImage != null)
                progressImage.fillAmount = pct;
        }

        private void SaveState()
        {
            if (oracle == null || string.IsNullOrEmpty(npcId)) return;
            if (oracle.saveData.NpcGeneration == null)
                oracle.saveData.NpcGeneration = new Dictionary<string, GameData.NpcGenerationRecord>();

            var rec = new GameData.NpcGenerationRecord
            {
                StoredResources = new Dictionary<string, double>(),
                LastGenerationTime = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };
            foreach (var pair in stored)
            {
                if (pair.Key != null)
                    rec.StoredResources[pair.Key.name] = pair.Value;
            }
            oracle.saveData.NpcGeneration[npcId] = rec;
        }

        private void LoadState()
        {
            if (oracle == null || string.IsNullOrEmpty(npcId)) return;
            EnsureLookup();
            oracle.saveData.NpcGeneration ??= new Dictionary<string, GameData.NpcGenerationRecord>();

            stored.Clear();
            progress = 0f;
            if (oracle.saveData.NpcGeneration.TryGetValue(npcId, out var rec) && rec != null)
            {
                foreach (var pair in rec.StoredResources)
                {
                    if (lookup.TryGetValue(pair.Key, out var res) && res != null)
                        stored[res] = pair.Value;
                }
            }
            UpdateUI();
            BuildProgressUI();
        }

        private void BuildProgressUI()
        {
            if (progressUIParent == null || progressUIPrefab == null)
                return;

            foreach (Transform child in progressUIParent)
                Destroy(child.gameObject);
            uiEntries.Clear();

            foreach (var entry in resources)
            {
                if (entry.resource == null) continue;
                var ui = Instantiate(progressUIPrefab, progressUIParent);
                ui.SetData(this, entry.resource, entry.amount);
                uiEntries.Add(ui);
            }
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
    }
}
