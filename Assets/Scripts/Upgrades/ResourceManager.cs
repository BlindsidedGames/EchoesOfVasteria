using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Upgrades
{
    public class ResourceManager : MonoBehaviour
    {
        private Dictionary<Resource, int> amounts = new();
        private HashSet<Resource> unlocked = new();

        [Title("Debug Controls")]
        [SerializeField] private Resource debugResource;
        [SerializeField] private int debugAmount = 1;

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

        public int GetAmount(Resource resource)
        {
            return amounts.TryGetValue(resource, out var value) ? value : 0;
        }

        public void Add(Resource resource, int amount)
        {
            if (resource == null || amount <= 0) return;
            unlocked.Add(resource);
            if (amounts.ContainsKey(resource))
                amounts[resource] += amount;
            else
                amounts[resource] = amount;
        }

        public bool Spend(Resource resource, int amount)
        {
            if (resource == null || amount <= 0) return true;
            var current = GetAmount(resource);
            if (current < amount) return false;
            amounts[resource] = current - amount;
            return true;
        }

        public bool IsUnlocked(Resource resource)
        {
            return resource != null && unlocked.Contains(resource);
        }

        private void SaveState()
        {
            if (oracle == null) return;
            oracle.saveData.ResourceAmounts = new Dictionary<Resource, int>(amounts);
            oracle.saveData.UnlockedResources = new HashSet<Resource>(unlocked);
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.ResourceAmounts ??= new Dictionary<Resource, int>();
            oracle.saveData.UnlockedResources ??= new HashSet<Resource>();
            amounts = new Dictionary<Resource, int>(oracle.saveData.ResourceAmounts);
            unlocked = new HashSet<Resource>(oracle.saveData.UnlockedResources);
        }
    }
}
