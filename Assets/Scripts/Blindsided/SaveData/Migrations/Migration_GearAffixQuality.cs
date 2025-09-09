using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Gear;
using Blindsided.Utilities;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// Version migration for 1.2.18: populate GearAffixRecord.quality (double in [0,1]) for all equipped items.
    /// - Computes quality by inverting StatDefSO.RemapRoll against the legacy absolute value.
    /// - If the value equals the item's rarity floor (within epsilon), uses the quantile mapping to the floor value.
    /// - Leaves legacy 'value' untouched; from 1.2.18+, save path writes only 'quality'.
    /// Idempotent and safe to re-run.
    /// </summary>
    internal sealed class Migration_GearAffixQuality : ISaveMigration
    {
        public int? TargetSchema => null;
        public string TargetVersion => "1.2.18";
        public string Id => "GearAffixQuality_v1_2_18";

        public void Apply(GameData data)
        {
            if (data == null) return;
            var equipment = data.EquipmentBySlot;
            if (equipment == null || equipment.Count == 0) return;

            var allStats = AssetCache.GetAll<StatDefSO>(string.Empty) ?? Array.Empty<StatDefSO>();
            var allRarities = AssetCache.GetAll<RaritySO>(string.Empty) ?? Array.Empty<RaritySO>();

            // Pre-pass: legacy Boots movement speed max changed 5 -> 100. Multiply by 20 once before quality mapping.
            try
            {
                // Identify movement speed primarily by StatDefSO.id "6" (legacy id), with name/mapping fallbacks
                const string MoveSpeedId = "6";
                if (equipment.TryGetValue("Boots", out var boots) && boots != null && boots.affixes != null)
                {
                    // Local resolver because the helper below is declared later
                    StatDefSO LocalResolve(string id)
                    {
                        if (string.IsNullOrWhiteSpace(id)) return null;
                        var s = allStats.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(x.id) && string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                        if (s != null) return s;
                        return allStats.FirstOrDefault(x => x != null && string.Equals(x.name, id, StringComparison.OrdinalIgnoreCase));
                    }

                    for (int i = 0; i < boots.affixes.Count; i++)
                    {
                        var a = boots.affixes[i];
                        if (a == null) continue;

                        // Determine if this affix represents Move Speed: prefer id match ("6"), then name, then mapping as last resort
                        bool isMoveSpeed = false;
                        var resolved = LocalResolve(a.statId);
                        if (!string.IsNullOrWhiteSpace(a.statId))
                        {
                            if (string.Equals(a.statId, MoveSpeedId, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(a.statId, "Move Speed", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(a.statId, "movement speed", StringComparison.OrdinalIgnoreCase))
                            {
                                isMoveSpeed = true;
                            }
                        }
                        if (!isMoveSpeed && resolved != null)
                        {
                            if (string.Equals(resolved.id, MoveSpeedId, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(resolved.name, "Move Speed", StringComparison.OrdinalIgnoreCase)
                                || resolved.heroMapping == HeroStatMapping.MoveSpeed)
                            {
                                isMoveSpeed = true;
                            }
                        }

                        if (isMoveSpeed)
                        {
                            // Only scale legacy values (<= 5). This keeps the operation idempotent.
                            if (a.value > 0f && a.value <= 5.0001f)
                            {
                                a.value *= 20f;
                                boots.affixes[i] = a;
                            }
                        }
                    }

                    // Persist back explicitly for clarity
                    equipment["Boots"] = boots;
                }
            }
            catch
            {
                // Non-fatal: if anything goes wrong, continue with the main migration.
            }

            StatDefSO ResolveStat(string id)
            {
                if (string.IsNullOrWhiteSpace(id)) return null;
                // Prefer id match, then asset name
                var s = allStats.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(x.id) && string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (s != null) return s;
                return allStats.FirstOrDefault(x => x != null && string.Equals(x.name, id, StringComparison.OrdinalIgnoreCase));
            }

            RaritySO ResolveRarity(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return null;
                return allRarities.FirstOrDefault(r => r != null && string.Equals(r.name, name, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var kv in equipment)
            {
                var rec = kv.Value;
                if (rec == null || rec.affixes == null) continue;
                var rarity = ResolveRarity(rec.rarity);
                for (int i = 0; i < rec.affixes.Count; i++)
                {
                    var a = rec.affixes[i];
                    if (a == null) continue;

                    // If already has a plausible quality and legacy value appears unused, skip (idempotent)
                    if (a.quality >= 0.0 && a.quality <= 1.0 && Math.Abs(a.value) < 1e-12)
                        continue;

                    var def = ResolveStat(a.statId);
                    if (def == null)
                        continue; // stat not found; leave as-is

                    // Safety net: if this affix is Movement Speed on legacy scale (<=5), scale before computing quality
                    try
                    {
                        const string MoveSpeedId = "6";
                        if (a.value > 0f && a.value <= 5.0001f)
                        {
                            // Prefer id "6"; then by resolved name; then mapping as a fallback
                            bool isMoveSpeed = false;
                            if (!string.IsNullOrWhiteSpace(a.statId))
                            {
                                if (string.Equals(a.statId, MoveSpeedId, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(a.statId, "Move Speed", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(a.statId, "movement speed", StringComparison.OrdinalIgnoreCase))
                                {
                                    isMoveSpeed = true;
                                }
                            }
                            if (!isMoveSpeed)
                            {
                                if (string.Equals(def.id, MoveSpeedId, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(def.name, "Move Speed", StringComparison.OrdinalIgnoreCase)
                                    || def.heroMapping == HeroStatMapping.MoveSpeed)
                                {
                                    isMoveSpeed = true;
                                }
                            }
                            if (isMoveSpeed)
                                a.value *= 20f;
                        }
                    }
                    catch { /* non-fatal */ }

                    // Compute best-effort quality via linear normalization across [min,max].
                    // If value equals the rarity floor, map to floor quantile.
                    double q = StatRollMath.ToNormalizedQuantileLinear(def, a.value, rarity);
                    a.quality = q;
                    rec.affixes[i] = a;
                }
                // No dictionary write-back here; rec is a reference type (GearItemRecord),
                // so in-place mutations above are already reflected in equipment.
            }
        }
    }
}
