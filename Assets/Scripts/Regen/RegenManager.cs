using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Hero;
using TimelessEchoes.Enemies;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Regen
{
    /// <summary>
    ///     Handles fish donations and applies health regeneration to the hero.
    /// </summary>
    public class RegenManager : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ResourceInventoryUI inventoryUI;
        [SerializeField] private List<Resource> fishResources = new();
        [SerializeField] private RegenEntryUIReferences entryPrefab;
        [SerializeField] private Transform entryParent;

        private readonly Dictionary<Resource, double> donations = new();
        private readonly List<RegenEntryUIReferences> entries = new();
        private Health heroHealth;

        private void Awake()
        {
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }
            if (inventoryUI == null)
            {
                inventoryUI = ResourceInventoryUI.Instance;
                if (inventoryUI == null)
                    TELogger.Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
            }
            heroHealth = FindFirstObjectByType<HeroController>()?.GetComponent<Health>();

            LoadState();
            BuildEntries();
            UpdateAllEntries();

            OnSaveData += SaveState;
            OnLoadData += LoadState;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged += UpdateAllEntries;
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            if (resourceManager != null)
                resourceManager.OnInventoryChanged -= UpdateAllEntries;
        }

        private void Update()
        {
            if (heroHealth == null)
                return;
            float regen = (float)GetTotalRegen();
            if (regen > 0f && heroHealth.CurrentHealth < heroHealth.MaxHealth)
                heroHealth.Heal(regen * Time.deltaTime);
        }

        private void BuildEntries()
        {
            if (entryPrefab == null || entryParent == null)
                return;

            foreach (Transform child in entryParent)
                Destroy(child.gameObject);
            entries.Clear();

            foreach (var res in fishResources)
            {
                if (res == null) continue;
                var entry = Instantiate(entryPrefab, entryParent);
                entry.costResourceUIReferences.resource = res;
                if (entry.costResourceUIReferences.selectButton != null)
                {
                    var r = res;
                    entry.costResourceUIReferences.selectButton.onClick.RemoveAllListeners();
                    entry.costResourceUIReferences.selectButton.onClick.AddListener(() => inventoryUI?.HighlightResource(r));
                }
                if (entry.donate10PercentButton != null)
                {
                    var r = res;
                    entry.donate10PercentButton.onClick.AddListener(() => DonatePercentage(r, 0.1f));
                }
                if (entry.donateAllButton != null)
                {
                    var r = res;
                    entry.donateAllButton.onClick.AddListener(() => DonateAll(r));
                }
                entries.Add(entry);
            }
        }

        private void UpdateAllEntries()
        {
            for (int i = 0; i < entries.Count && i < fishResources.Count; i++)
                UpdateEntry(i);
        }

        private void UpdateEntry(int index)
        {
            if (index < 0 || index >= entries.Count || index >= fishResources.Count)
                return;

            var entry = entries[index];
            var res = fishResources[index];
            if (entry == null || res == null) return;

            bool unlocked = resourceManager && resourceManager.IsUnlocked(res);
            double playerAmt = resourceManager ? resourceManager.GetAmount(res) : 0;
            double donated = donations.TryGetValue(res, out var val) ? val : 0;

            var costRefs = entry.costResourceUIReferences;
            if (costRefs.iconImage != null)
            {
                costRefs.iconImage.sprite = res.icon;
                costRefs.iconImage.enabled = unlocked;
            }
            if (costRefs.questionMarkImage != null)
                costRefs.questionMarkImage.enabled = !unlocked;
            if (costRefs.countText != null)
                costRefs.countText.text = unlocked ? Mathf.FloorToInt((float)playerAmt).ToString() : string.Empty;

            if (entry.fishNameText != null)
                entry.fishNameText.text = unlocked ? res.name : "???";

            if (entry.amountDonatedText != null)
                entry.amountDonatedText.text = Mathf.FloorToInt((float)donated).ToString();

            if (entry.regenText != null)
                entry.regenText.text = $"Granting {GetRegenFor(res):0.###} Regen";

            bool canDonate = playerAmt > 0;
            if (entry.donateAllButton != null)
                entry.donateAllButton.interactable = canDonate;
            if (entry.donate10PercentButton != null)
                entry.donate10PercentButton.interactable = playerAmt > 10;
        }

        private void DonatePercentage(Resource res, float pct)
        {
            if (resourceManager == null || res == null) return;
            double amount = Mathf.Floor((float)(resourceManager.GetAmount(res) * pct));
            if (amount <= 0) return;
            resourceManager.Spend(res, amount);
            AddDonation(res, amount);
            UpdateAllEntries();
        }

        private void DonateAll(Resource res)
        {
            if (resourceManager == null || res == null) return;
            double amount = resourceManager.GetAmount(res);
            if (amount <= 0) return;
            resourceManager.Spend(res, amount);
            AddDonation(res, amount);
            UpdateAllEntries();
        }

        private void AddDonation(Resource res, double amount)
        {
            if (donations.ContainsKey(res))
                donations[res] += amount;
            else
                donations[res] = amount;
        }

        private double GetRegenFor(Resource res)
        {
            if (!donations.TryGetValue(res, out var val) || val <= 0)
                return 0;
            return Mathf.Log10((float)val) / 10f;
        }

        public double GetTotalRegen()
        {
            double sum = 0;
            foreach (var pair in donations)
                sum += GetRegenFor(pair.Key);
            return sum;
        }

        private void SaveState()
        {
            if (oracle == null) return;
            oracle.saveData.FishDonations ??= new Dictionary<string, double>();
            oracle.saveData.FishDonations.Clear();
            foreach (var pair in donations)
                if (pair.Key != null)
                    oracle.saveData.FishDonations[pair.Key.name] = pair.Value;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.FishDonations ??= new Dictionary<string, double>();
            donations.Clear();
            foreach (var res in fishResources)
            {
                if (res == null) continue;
                oracle.saveData.FishDonations.TryGetValue(res.name, out var val);
                donations[res] = val;
            }
            UpdateAllEntries();
        }
    }
}
