using System;
using System.Collections.Generic;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// Repairs only absent containers and null records for the six supported authored skill keys.
    /// Existing progress and unknown records remain intact; no farm ownership/rewards are granted.
    /// </summary>
    internal sealed class Migration_SchemaV4CurrentCollections : IPostVersionSaveMigration, IConditionalRepairSaveMigration
    {
        // Asset-backed compatibility keys, not per-save eligibility or level requirements:
        // Assets/Scriptables/Skills/{Combat,Mining,Woodcutting,Fishing,Farming,Looting}.asset.
        private static readonly string[] KnownSkillKeys =
            { "Combat", "Mining", "Woodcutting", "Fishing", "Farming", "Looting" };

        public int? TargetSchema => 4;
        public string TargetVersion => null;
        public string Id => "SchemaV4CurrentCollections";

        internal static bool IsKnownSkillKey(string key) => Array.IndexOf(KnownSkillKeys, key) >= 0;

        internal static string GetInvalidKnownProgressError(GameData data)
        {
            if (data?.SkillData == null) return null;
            foreach (var key in KnownSkillKeys)
                if (data.SkillData.TryGetValue(key, out var record) && record != null &&
                    (float.IsNaN(record.CurrentXP) || float.IsInfinity(record.CurrentXP)))
                    return $"Known skill '{key}' contains nonfinite XP. The original save was preserved; " +
                           "use a verified backup or recovery instead of resetting its progress.";
            return null;
        }

        public bool NeedsRepair(GameData data)
        {
            if (data?.SkillData == null || data.Farm == null || !TimelessEchoes.Farming.FarmJournal.HasValidBounds(data.Farm)) return true;
            foreach (var key in KnownSkillKeys)
                if (data.SkillData.TryGetValue(key, out var record) && record == null) return true;
            return false;
        }

        public void Apply(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Farm ??= new TimelessEchoes.Farming.FarmState();
            TimelessEchoes.Farming.FarmJournal.UpgradeLegacy(data.Farm);
            data.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            foreach (var key in KnownSkillKeys)
                if (data.SkillData.TryGetValue(key, out var record) && record == null)
                    data.SkillData[key] = new GameData.SkillProgress();
        }
    }
}
