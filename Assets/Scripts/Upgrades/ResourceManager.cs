using System.Collections.Generic;
using UnityEngine;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Upgrades
{
    public class ResourceManager : MonoBehaviour
    {
        private Dictionary<Resource, int> amounts = new();

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

        private void SaveState()
        {
            if (oracle == null) return;
            oracle.saveData.ResourceAmounts = new Dictionary<Resource, int>(amounts);
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.ResourceAmounts ??= new Dictionary<Resource, int>();
            amounts = new Dictionary<Resource, int>(oracle.saveData.ResourceAmounts);
        }
    }
}
