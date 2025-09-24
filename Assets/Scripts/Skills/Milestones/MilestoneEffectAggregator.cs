using System;
using System.Collections.Generic;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Skills
{
    /// <summary>
    /// Aggregates milestone contributions for quick runtime queries.
    /// </summary>
    public sealed class MilestoneEffectAggregator
    {
        private readonly Dictionary<Skill, SkillMilestoneSummary> _skillSummaries = new();
        private readonly Dictionary<StatUpgrade, float> _flatStatBonuses = new();
        private readonly Dictionary<StatUpgrade, float> _percentStatBonuses = new();

        private static readonly SkillMilestoneSummary EmptySummary = new();

        public void Reset()
        {
            foreach (var summary in _skillSummaries.Values)
                summary.Reset();

            _flatStatBonuses.Clear();
            _percentStatBonuses.Clear();
        }

        private SkillMilestoneSummary GetOrCreateSummary(Skill skill)
        {
            if (skill == null)
                return EmptySummary;

            if (!_skillSummaries.TryGetValue(skill, out var summary))
            {
                summary = new SkillMilestoneSummary();
                _skillSummaries.Add(skill, summary);
            }

            return summary;
        }

        public void AddProcChance(Skill skill, MilestoneProcType procType, float chance)
        {
            if (skill == null || chance <= 0f)
                return;

            var summary = GetOrCreateSummary(skill);
            switch (procType)
            {
                case MilestoneProcType.InstantTask:
                    summary.InstantTaskChance += chance;
                    break;
                case MilestoneProcType.InstantKill:
                    summary.InstantKillChance += chance;
                    break;
                case MilestoneProcType.DoubleResources:
                    summary.DoubleResourceChance += chance;
                    break;
                case MilestoneProcType.DoubleXP:
                    summary.DoubleXpChance += chance;
                    break;
            }
        }

        public float GetProcChance(Skill skill, MilestoneProcType procType)
        {
            if (skill == null)
                return 0f;

            if (!_skillSummaries.TryGetValue(skill, out var summary))
                return 0f;

            return procType switch
            {
                MilestoneProcType.InstantTask => summary.InstantTaskChance,
                MilestoneProcType.InstantKill => summary.InstantKillChance,
                MilestoneProcType.DoubleResources => summary.DoubleResourceChance,
                MilestoneProcType.DoubleXP => summary.DoubleXpChance,
                _ => 0f
            };
        }

        public void AddSpawnEntry(Skill skill, TimelessEchoes.EchoSpawnConfig config, float chance, float duration, int count, bool useAssociatedSkillFallback)
        {
            if (skill == null || config == null || chance <= 0f || duration <= 0f)
                return;

            if (count <= 0)
                count = 1;

            var summary = GetOrCreateSummary(skill);
            summary.SpawnEchoes.Add(new SpawnEchoEntry(config, chance, duration, count, useAssociatedSkillFallback));
        }

        public IReadOnlyList<SpawnEchoEntry> GetSpawnEntries(Skill skill)
        {
            if (skill != null && _skillSummaries.TryGetValue(skill, out var summary))
                return summary.SpawnEchoes;

            return Array.Empty<SpawnEchoEntry>();
        }

        public void AddFlatStatBonus(StatUpgrade upgrade, float amount)
        {
            if (upgrade == null || amount == 0f)
                return;

            if (_flatStatBonuses.ContainsKey(upgrade))
                _flatStatBonuses[upgrade] += amount;
            else
                _flatStatBonuses.Add(upgrade, amount);
        }

        public void AddPercentStatBonus(StatUpgrade upgrade, float amount)
        {
            if (upgrade == null || amount == 0f)
                return;

            if (_percentStatBonuses.ContainsKey(upgrade))
                _percentStatBonuses[upgrade] += amount;
            else
                _percentStatBonuses.Add(upgrade, amount);
        }

        public float GetFlatStatBonus(StatUpgrade upgrade)
        {
            if (upgrade == null)
                return 0f;

            return _flatStatBonuses.TryGetValue(upgrade, out var total) ? total : 0f;
        }

        public float GetPercentStatBonus(StatUpgrade upgrade)
        {
            if (upgrade == null)
                return 0f;

            return _percentStatBonuses.TryGetValue(upgrade, out var total) ? total : 0f;
        }
    }

    public sealed class SkillMilestoneSummary
    {
        public float InstantTaskChance;
        public float InstantKillChance;
        public float DoubleResourceChance;
        public float DoubleXpChance;
        public List<SpawnEchoEntry> SpawnEchoes = new();

        internal void Reset()
        {
            InstantTaskChance = 0f;
            InstantKillChance = 0f;
            DoubleResourceChance = 0f;
            DoubleXpChance = 0f;
            if (SpawnEchoes.Count > 0)
                SpawnEchoes.Clear();
        }
    }

    public readonly struct SpawnEchoEntry
    {
        public readonly TimelessEchoes.EchoSpawnConfig Config;
        public readonly float Chance;
        public readonly float Duration;
        public readonly int Count;
        public readonly bool UseAssociatedSkillFallback;

        public SpawnEchoEntry(TimelessEchoes.EchoSpawnConfig config, float chance, float duration, int count, bool useAssociatedSkillFallback)
        {
            Config = config;
            Chance = chance;
            Duration = duration;
            Count = count;
            UseAssociatedSkillFallback = useAssociatedSkillFallback;
        }
    }
}
