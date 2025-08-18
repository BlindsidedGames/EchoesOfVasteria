using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    [DefaultExecutionOrder(-1)]
    public class EquipmentController : MonoBehaviour
    {
        public static EquipmentController Instance { get; private set; }

        [SerializeField] private List<string> slots = new() { "Weapon", "Helmet", "Chest", "Boots" };

        private readonly Dictionary<string, GearItem> equippedBySlot = new();

        public event Action OnEquipmentChanged;

        public IReadOnlyList<string> Slots => slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        public GearItem GetEquipped(string slot)
        {
            return slot != null && equippedBySlot.TryGetValue(slot, out var item) ? item : null;
        }

        public void Equip(GearItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.slot)) return;
            equippedBySlot[item.slot] = item;
            OnEquipmentChanged?.Invoke();
			// Persist snapshot immediately so save file reflects latest equipment even if SaveData event doesn't fire before exit
			SaveState();
        }

        public void Unequip(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return;
            if (equippedBySlot.Remove(slot))
                OnEquipmentChanged?.Invoke();
			// Persist snapshot immediately so save file reflects latest equipment even if SaveData event doesn't fire before exit
			SaveState();
        }

        public float GetTotalForStat(StatDefSO stat)
        {
            if (stat == null) return 0f;
            float sum = 0f;
            foreach (var kv in equippedBySlot)
            {
                var gi = kv.Value;
                if (gi == null) continue;
                foreach (var a in gi.affixes)
                    if (a != null && a.stat == stat)
                        sum += a.value;
            }
            return sum;
        }

        public float GetCritChance(StatDefSO critStat)
        {
            return GetTotalForStat(critStat);
        }

        public float GetTotalForMapping(HeroStatMapping mapping)
        {
            float sum = 0f;
            foreach (var kv in equippedBySlot)
            {
                var gi = kv.Value;
                if (gi == null) continue;
                foreach (var a in gi.affixes)
                {
                    if (a == null || a.stat == null) continue;
                    if (a.stat.heroMapping == mapping)
                        sum += a.value;
                }
            }
            return sum;
        }

        #region Save/Load
        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new System.Collections.Generic.Dictionary<string, Blindsided.SaveData.GearItemRecord>();
            foreach (var kv in equippedBySlot)
            {
                var slot = kv.Key;
                var item = kv.Value;
                if (item == null) continue;
                var rec = new Blindsided.SaveData.GearItemRecord
                {
                    slot = slot,
                    rarity = item.rarity != null ? item.rarity.name : null,
                    affixes = new System.Collections.Generic.List<Blindsided.SaveData.GearAffixRecord>()
                };
                foreach (var a in item.affixes)
                {
                    if (a == null || a.stat == null) continue;
                    rec.affixes.Add(new Blindsided.SaveData.GearAffixRecord
                    {
                        statId = a.stat.id ?? a.stat.name,
                        value = a.value
                    });
                }
                dict[slot] = rec;
            }
            oracle.saveData.EquipmentBySlot = dict;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            equippedBySlot.Clear();
            var data = oracle.saveData.EquipmentBySlot;
            if (data == null) return;

			var allRarities = AssetCache.GetAll<RaritySO>("");
			var allStats = AssetCache.GetAll<StatDefSO>("");
			if (allRarities == null || allRarities.Length == 0)
				Debug.LogWarning("EquipmentController: No RaritySO assets found in Resources – cannot fully reconstruct gear visuals.");
			if (allStats == null || allStats.Length == 0)
				Debug.LogWarning("EquipmentController: No StatDefSO assets found in Resources – cannot reconstruct gear affixes.");
            foreach (var kv in data)
            {
                var rec = kv.Value;
                if (rec == null) continue;
                // Resolve the canonical slot name. Prefer the record's slot, but
                // fall back to the dictionary key for compatibility with older saves
                // where the record.slot field may be null/empty or differently cased.
                var resolvedSlot = ResolveSlotName(rec.slot, kv.Key);
                if (string.IsNullOrWhiteSpace(resolvedSlot))
                    continue; // cannot determine a valid slot; skip

                var item = new GearItem { slot = resolvedSlot, rarity = null };
				if (!string.IsNullOrWhiteSpace(rec.rarity))
				{
					item.rarity = allRarities.FirstOrDefault(r => r != null && r.name == rec.rarity);
					if (item.rarity == null)
						Debug.LogWarning($"EquipmentController: Rarity '{rec.rarity}' not found in Resources – item sprite may be missing for slot '{resolvedSlot}'.");
				}

                if (rec.affixes != null)
                {
                    foreach (var ar in rec.affixes)
                    {
                        if (ar == null) continue;
						var stat = allStats.FirstOrDefault(s => s != null && (s.id == ar.statId || s.name == ar.statId));
						if (stat == null)
						{
							Debug.LogWarning($"EquipmentController: Stat '{ar.statId}' not found in Resources – affix skipped for slot '{resolvedSlot}'.");
							continue;
						}
                        item.affixes.Add(new GearAffix { stat = stat, value = ar.value });
                    }
                }
                if (!string.IsNullOrWhiteSpace(item.slot) && slots.Contains(item.slot))
                    equippedBySlot[item.slot] = item;
            }
            OnEquipmentChanged?.Invoke();
        }
        #endregion

        private string NormalizeSlotName(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return slot;
            if (slot == "Helm") return "Helmet";
            return slot;
        }

        private string ResolveSlotName(string recordSlot, string dictKeySlot)
        {
            // Try preferred: recordSlot, then fallback: dictKeySlot
            foreach (var candidate in new[] { recordSlot, dictKeySlot })
            {
                var s = candidate;
                if (string.IsNullOrWhiteSpace(s)) continue;
                s = s.Trim();
                // Map common alias
                if (s.Equals("Helm", System.StringComparison.OrdinalIgnoreCase))
                    s = "Helmet";
                // If candidate already exactly matches a configured slot, accept
                var exact = slots.FirstOrDefault(x => x != null && x.Equals(s, System.StringComparison.Ordinal));
                if (!string.IsNullOrEmpty(exact)) return exact;
                // Case-insensitive match to configured slots
                var ci = slots.FirstOrDefault(x => x != null && x.Equals(s, System.StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(ci)) return ci;
            }
            return null;
        }
    }
}


