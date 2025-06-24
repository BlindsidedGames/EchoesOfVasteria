using System.Collections.Generic;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Upgrades
{
    public class ResourceManager : MonoBehaviour
    {
        private static Dictionary<string, Resource> lookup;

        /// <summary>
        ///     Invoked whenever the stored resource amounts or unlocked state changes.
        /// </summary>
        public event System.Action OnInventoryChanged;

        /// <summary>
        ///     Invoked whenever resources are added via <see cref="Add"/>.
        /// </summary>
        public event System.Action<Resource, double> OnResourceAdded;

        [Title("Debug Controls")] [SerializeField]
        private Resource debugResource;

        [SerializeField] private double debugAmount = 1;
        private readonly Dictionary<Resource, double> amounts = new();
        private readonly HashSet<Resource> unlocked = new();

        private void InvokeInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

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

        [Button]
        private void AddDebugResource()
        {
            Add(debugResource, debugAmount);
            SaveState();
        }

        [Button]
        private void UnlockDebugResource()
        {
            if (debugResource != null)
            {
                unlocked.Add(debugResource);
                SaveState();
            }
        }

        public double GetAmount(Resource resource)
        {
            return amounts.TryGetValue(resource, out var value) ? value : 0;
        }

        public void Add(Resource resource, double amount)
        {
            if (resource == null || amount <= 0) return;
            unlocked.Add(resource);
            if (amounts.ContainsKey(resource))
                amounts[resource] += amount;
            else
                amounts[resource] = amount;
            OnResourceAdded?.Invoke(resource, amount);
            InvokeInventoryChanged();
        }

        public bool Spend(Resource resource, double amount)
        {
            if (resource == null || amount <= 0) return true;
            var current = GetAmount(resource);
            if (current < amount) return false;
            amounts[resource] = current - amount;
            InvokeInventoryChanged();
            return true;
        }

        public bool IsUnlocked(Resource resource)
        {
            return resource != null && unlocked.Contains(resource);
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new Dictionary<string, GameData.ResourceEntry>();
            foreach (var pair in amounts)
            {
                if (pair.Key == null) continue;
                dict[pair.Key.name] = new GameData.ResourceEntry
                {
                    Earned = unlocked.Contains(pair.Key),
                    Amount = pair.Value
                };
            }

            foreach (var res in unlocked)
            {
                if (res == null) continue;
                if (!dict.ContainsKey(res.name))
                    dict[res.name] = new GameData.ResourceEntry { Earned = true, Amount = 0 };
            }

            oracle.saveData.Resources = dict;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            EnsureLookup();
            amounts.Clear();
            unlocked.Clear();
            foreach (var pair in oracle.saveData.Resources)
                if (lookup.TryGetValue(pair.Key, out var res) && res != null)
                {
                    amounts[res] = pair.Value.Amount;
                    if (pair.Value.Earned) unlocked.Add(res);
                }
            InvokeInventoryChanged();
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