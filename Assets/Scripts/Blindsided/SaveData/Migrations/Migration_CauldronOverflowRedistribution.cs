using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// For versions >= 1.2.16: Sanitize Cauldron card counts by capping RES:/BUFF: cards at
    /// their maximum tier thresholds, accumulating any overflow, and redistributing overflow
    /// into available non-maxed cards. Any remainder is distributed into Infinity (INF:) cards
    /// if present. Idempotent.
    /// </summary>
    internal sealed class Migration_CauldronOverflowRedistribution : ISaveMigration
    {
        public int? TargetSchema => null;
        public string TargetVersion => "1.2.17";
        public string Id => "CauldronOverflowRedistribution";

        public void Apply(GameData data)
        {
            if (data == null) return;
            data.CauldronCardCounts ??= new Dictionary<string, int>();

            // Load thresholds from configured CauldronConfig if available; otherwise fallback to defaults
            var cfg = AssetCache.GetAll<CauldronConfig>(string.Empty)?.FirstOrDefault(c => c != null);
            var resThresholds = cfg != null && cfg.resourceTierThresholds != null && cfg.resourceTierThresholds.Length > 0
                ? cfg.resourceTierThresholds
                : new int[8] { 1, 5, 20, 50, 100, 200, 350, 500 };
            var buffThresholds = cfg != null && cfg.buffTierThresholds != null && cfg.buffTierThresholds.Length > 0
                ? cfg.buffTierThresholds
                : new int[8] { 1, 3, 10, 25, 50, 100, 200, 300 };

            var counts = data.CauldronCardCounts;

            // Step 1: Cap RES:/BUFF: at their max and accumulate overflow
            long overflow = 0;
            // Enumerate a copy of keys to allow in-place updates
            var keys = counts.Keys.ToList();
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!(key.StartsWith("RES:") || key.StartsWith("BUFF:"))) continue;

                var value = counts.TryGetValue(key, out var v) ? v : 0;
                if (value <= 0) continue;

                int maxAllowed;
                if (key.StartsWith("RES:"))
                    maxAllowed = resThresholds.Length > 0 ? resThresholds[resThresholds.Length - 1] : value;
                else
                    maxAllowed = buffThresholds.Length > 0 ? buffThresholds[buffThresholds.Length - 1] : value;

                if (value > maxAllowed)
                {
                    overflow += (value - maxAllowed);
                    counts[key] = maxAllowed;
                }
            }

            if (overflow <= 0)
                return; // nothing to redistribute

            // Step 2: Build available, non-maxed RES:/BUFF: ids and their capacity to receive
            var capacities = new List<(string id, int room)>();

            // Resources: unlocked (Earned) and not DisableAlterEcho
            foreach (var res in AssetCache.GetAll<Resource>(string.Empty) ?? Array.Empty<Resource>())
            {
                if (res == null || res.DisableAlterEcho) continue;
                var resName = res.name;
                if (string.IsNullOrWhiteSpace(resName)) continue;
                // Unlocked check via save data
                var isUnlocked = data.Resources != null && data.Resources.TryGetValue(resName, out var rec) && rec != null && rec.Earned;
                if (!isUnlocked) continue;

                var id = $"RES:{resName}";
                var current = counts.TryGetValue(id, out var c) ? c : 0;
                var maxAllowed = resThresholds.Length > 0 ? resThresholds[resThresholds.Length - 1] : int.MaxValue;
                var room = Math.Max(0, maxAllowed - current);
                if (room > 0)
                    capacities.Add((id, room));
            }

            // Buffs: requiredQuest must be completed (if any)
            foreach (var buff in AssetCache.GetAll<BuffRecipe>(string.Empty) ?? Array.Empty<BuffRecipe>())
            {
                if (buff == null) continue;
                var buffName = buff.name;
                if (string.IsNullOrWhiteSpace(buffName)) continue;
                var required = buff.requiredQuest;
                bool unlocked = required == null;
                if (!unlocked)
                {
                    var qid = required != null ? required.questId : null;
                    if (string.IsNullOrEmpty(qid)) unlocked = true;
                    else
                    {
                        data.Quests ??= new Dictionary<string, GameData.QuestRecord>();
                        unlocked = data.Quests.TryGetValue(qid, out var qr) && qr != null && qr.Completed;
                    }
                }
                if (!unlocked) continue;

                var id = $"BUFF:{buffName}";
                var current = counts.TryGetValue(id, out var c) ? c : 0;
                var maxAllowed = buffThresholds.Length > 0 ? buffThresholds[buffThresholds.Length - 1] : int.MaxValue;
                var room = Math.Max(0, maxAllowed - current);
                if (room > 0)
                    capacities.Add((id, room));
            }

            // Deterministic ordering: by id ascending
            capacities.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

            // Step 3: Fill available capacities in order
            for (int i = 0; i < capacities.Count && overflow > 0; i++)
            {
                var cap = capacities[i];
                if (cap.room <= 0) continue;
                var take = (int)Math.Min((long)cap.room, overflow);
                if (take <= 0) continue;
                var cur = counts.TryGetValue(cap.id, out var c) ? c : 0;
                counts[cap.id] = cur + take;
                overflow -= take;
            }

            if (overflow <= 0)
                return;

            // Step 4: Distribute any remainder into Infinity cards (INF:<Stat>) if present
            var infinityIds = new List<string>();
            foreach (var inf in AssetCache.GetAll<InfinityCauldronStatSO>("Infinity") ?? Array.Empty<InfinityCauldronStatSO>())
            {
                if (inf == null) continue;
                infinityIds.Add($"INF:{inf.Stat}");
            }

            if (infinityIds.Count == 0)
                return; // No Infinity available; leave overflow discarded by design

            // Distribute evenly across Infinity ids to avoid hot loops
            var n = infinityIds.Count;
            var per = (int)(overflow / n);
            var rem = (int)(overflow % n);
            for (int i = 0; i < n; i++)
            {
                var add = per + (i < rem ? 1 : 0);
                if (add <= 0) continue;
                var id = infinityIds[i];
                var cur = counts.TryGetValue(id, out var c) ? c : 0;
                counts[id] = cur + add;
            }
        }
    }
}
