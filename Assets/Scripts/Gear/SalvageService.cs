using UnityEngine;
using TimelessEchoes.Upgrades;
using System.Collections.Generic;

namespace TimelessEchoes.Gear
{
    public class SalvageService : MonoBehaviour
    {
        public static SalvageService Instance { get; private set; }

		// Salvage uses core-configured resource drops (chunks/crystals) rather than a single shards currency.
		[SerializeField] private Vector2Int salvageYieldRange = new Vector2Int(1, 3);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

		public int Salvage(GearItem item)
        {
			if (item == null) return 0;
			var rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
			if (rm == null) return 0;

			var drops = item.core != null ? item.core.salvageDrops : null;
			if (drops == null || drops.Count == 0)
				return 0;

			int totalAwardedEntries = 0;
			foreach (var drop in drops)
			{
				if (drop == null || drop.resource == null) continue;
				if (Random.value > drop.dropChance) continue;
				int min = Mathf.Max(0, drop.dropRange.x);
				int max = Mathf.Max(min, drop.dropRange.y);
				float t = Random.value;
				t *= t; // bias towards lower values, consistent with other drop logic
				int amount = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
				if (amount <= 0) continue;
				rm.Add(drop.resource, amount);
				totalAwardedEntries++;
			}
			return totalAwardedEntries;
        }
    }
}


