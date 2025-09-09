using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// One-time sanitation for the migrated duck helmet:
    /// - Removes Move Speed affix
    /// - If Attack Rate affix value > 1.5, reduce to 1.2
    /// Also disables stat->gear migration flag.
    /// Runs for any upgrade past 1.2.12 and is idempotent.
    /// </summary>
    internal sealed class Migration_DuckHelmetSanitation : ISaveMigration
    {
        public int? TargetSchema => null;
        public string TargetVersion => "1.2.12"; // Version where stat->gear migration was introduced
        public string Id => "DuckHelmetSanitation";

        public void Apply(GameData data)
        {
            if (data == null) return;
            // Hard-disable any migration logic by marking it done
            data.StatUpgradesMigratedToGear = true;

            // One-time guard; if already sanitized, do nothing further
            if (data.DuckHelmetSanitized)
                return;

            try
            {
                var equipment = data.EquipmentBySlot;
                if (equipment == null)
                {
                    data.DuckHelmetSanitized = true;
                    return;
                }

                if (!equipment.TryGetValue("Helmet", out var helm) || helm == null)
                {
                    data.DuckHelmetSanitized = true;
                    return;
                }

                // Only operate on the migrated duck helmet: rarity must be null/empty
                if (!string.IsNullOrWhiteSpace(helm.rarity))
                {
                    data.DuckHelmetSanitized = true;
                    return;
                }

                var allStats = AssetCache.GetAll<StatDefSO>(string.Empty) ?? System.Array.Empty<StatDefSO>();
                var moveDef = allStats.FirstOrDefault(s => s != null && s.heroMapping == HeroStatMapping.MoveSpeed);
                var atkDef = allStats.FirstOrDefault(s => s != null && s.heroMapping == HeroStatMapping.AttackRate);

                bool Matches(string statId, StatDefSO def)
                {
                    if (def == null || string.IsNullOrWhiteSpace(statId)) return false;
                    if (!string.IsNullOrWhiteSpace(def.id) && statId.Equals(def.id, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                    return statId.Equals(def.name, System.StringComparison.OrdinalIgnoreCase);
                }

                if (helm.affixes == null)
                {
                    data.DuckHelmetSanitized = true;
                    return;
                }

                // Remove Move Speed affix
                if (moveDef != null)
                    helm.affixes.RemoveAll(a => a != null && Matches(a.statId, moveDef));

                // Reduce excessive Attack Rate
                if (atkDef != null)
                {
                    for (int i = 0; i < helm.affixes.Count; i++)
                    {
                        var a = helm.affixes[i];
                        if (a == null) continue;
                        if (Matches(a.statId, atkDef) && a.value > 1.5f)
                        {
                            a.value = 1.2f;
                            helm.affixes[i] = a;
                        }
                    }
                }

                equipment["Helmet"] = helm;
            }
            finally
            {
                data.DuckHelmetSanitized = true;
            }
        }
    }
}
