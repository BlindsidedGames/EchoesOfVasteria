using System.Collections.Generic;
using References.UI;
using Sirenix.OdinInspector;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.Upgrades;
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
        public static RegenManager Instance { get; private set; }
        [SerializeField] private ResourceInventoryUI inventoryUI;
        [SerializeField] private List<Resource> fishResources = new();
        [SerializeField] private RegenEntryUIReferences entryPrefab;
        [SerializeField] private Transform entryParent;

        private readonly Dictionary<Resource, double> donations = new();
        private readonly List<RegenEntryUIReferences> entries = new();
        private Hero.HeroHealth heroHealth;

        private void Awake()
        {
            Instance = this;
            var manager = ResourceManager.Instance;
            if (manager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);

            if (inventoryUI == null)
            {
                inventoryUI = ResourceInventoryUI.Instance;
                if (inventoryUI == null)
                    Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
            }

            heroHealth = Hero.HeroHealth.Instance ??
                         FindFirstObjectByType<Hero.HeroHealth>();

            LoadState();
            BuildEntries();
            UpdateAllEntries();

            OnSaveData += SaveState;
            OnLoadData += LoadState;
            OnResetData += ResetDonations;
            if (manager != null)
                manager.OnInventoryChanged += UpdateAllEntries;
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
            OnResetData -= ResetDonations;
            var manager = ResourceManager.Instance;
            if (manager != null)
                manager.OnInventoryChanged -= UpdateAllEntries;
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (heroHealth == null)
                return;
            var regen = (float)GetTotalRegen();
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
                    entry.costResourceUIReferences.selectButton.onClick.AddListener(() =>
                        inventoryUI?.HighlightResource(r));
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
            for (var i = 0; i < entries.Count && i < fishResources.Count; i++)
                UpdateEntry(i);
        }

        private void UpdateEntry(int index)
        {
            if (index < 0 || index >= entries.Count || index >= fishResources.Count)
                return;

            var entry = entries[index];
            var res = fishResources[index];
            if (entry == null || res == null) return;

            var manager = ResourceManager.Instance;
            var unlocked = manager != null && manager.IsUnlocked(res);
            var playerAmt = manager != null ? manager.GetAmount(res) : 0;
            var donated = donations.TryGetValue(res, out var val) ? val : 0;

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

            var canDonate = playerAmt > 0;
            if (entry.donateAllButton != null)
                entry.donateAllButton.interactable = canDonate;
            if (entry.donate10PercentButton != null)
                entry.donate10PercentButton.interactable = playerAmt > 10;
        }

        private void DonatePercentage(Resource res, float pct)
        {
            var manager = ResourceManager.Instance;
            if (manager == null || res == null) return;
            double amount = Mathf.Floor((float)(manager.GetAmount(res) * pct));
            if (amount <= 0) return;
            AddDonation(res, amount);
            manager.Spend(res, amount);
            UpdateAllEntries();
        }

        private void DonateAll(Resource res)
        {
            var manager = ResourceManager.Instance;
            if (manager == null || res == null) return;
            var amount = manager.GetAmount(res);
            if (amount <= 0) return;
            AddDonation(res, amount);
            manager.Spend(res, amount);
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

        public double GetDonationTotal(Resource res = null)
        {
            if (res == null)
            {
                double sum = 0;
                foreach (var amt in donations.Values)
                    sum += amt;
                return sum;
            }

            return donations.TryGetValue(res, out var val) ? val : 0;
        }

        /// <summary>
        ///     Clears all stored fish donations and updates the UI.
        /// </summary>
        [Button]
        public void ResetDonations()
        {
            donations.Clear();
            SaveState();
            UpdateAllEntries();
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