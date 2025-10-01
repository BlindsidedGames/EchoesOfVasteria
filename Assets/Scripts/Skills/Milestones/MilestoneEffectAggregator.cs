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
        private readonly Dictionary<BaseStat, float> _flatStatBonuses = new();
        private readonly Dictionary<BaseStat, float> _percentStatBonuses = new();
        private readonly Dictionary<Skill, float> _taskResourceBonuses = new();
        private float _globalResourceBonus;
        private float _cauldronTasteBonus;

        private static readonly SkillMilestoneSummary EmptySummary = new();
        private static readonly System.Collections.Generic.IReadOnlyDictionary<TimelessEchoes.Upgrades.BaseStat, float> EmptyStatBonusMap = new System.Collections.Generic.Dictionary<TimelessEchoes.Upgrades.BaseStat, float>();

        public void Reset()
        {
            foreach (var summary in _skillSummaries.Values)
                summary.Reset();

            _flatStatBonuses.Clear();
            _percentStatBonuses.Clear();
            _taskResourceBonuses.Clear();
            _globalResourceBonus = 0f;
            _cauldronTasteBonus = 0f;
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

        public void AddFlatStatBonus(Skill skill, BaseStat upgrade, float amount)
        {
            if (upgrade == null || amount == 0f)
                return;

            if (_flatStatBonuses.ContainsKey(upgrade))
                _flatStatBonuses[upgrade] += amount;
            else
                _flatStatBonuses.Add(upgrade, amount);

            if (skill == null)
                return;

            var summary = GetOrCreateSummary(skill);
            if (summary.FlatStatBonuses.ContainsKey(upgrade))
                summary.FlatStatBonuses[upgrade] += amount;
            else
                summary.FlatStatBonuses.Add(upgrade, amount);
        }

        public void AddPercentStatBonus(Skill skill, BaseStat upgrade, float amount)
        {
            if (upgrade == null || amount == 0f)
                return;

            if (_percentStatBonuses.ContainsKey(upgrade))
                _percentStatBonuses[upgrade] += amount;
            else
                _percentStatBonuses.Add(upgrade, amount);

            if (skill == null)
                return;

            var summary = GetOrCreateSummary(skill);
            if (summary.PercentStatBonuses.ContainsKey(upgrade))
                summary.PercentStatBonuses[upgrade] += amount;
            else
                summary.PercentStatBonuses.Add(upgrade, amount);
        }

        public void AddResourceBonus(Skill skill, float percent)
        {
            if (percent == 0f)
                return;

            if (skill == null)
            {
                AddGlobalResourceBonus(percent);
                return;
            }

            if (_taskResourceBonuses.ContainsKey(skill))
                _taskResourceBonuses[skill] += percent;
            else
                _taskResourceBonuses.Add(skill, percent);

            var summary = GetOrCreateSummary(skill);
            summary.ResourceBonusPercent += percent;
        }

        public void AddGlobalResourceBonus(float percent)
        {
            if (percent == 0f)
                return;

            _globalResourceBonus += percent;
        }

        public void AddCauldronTasteBonus(float amount)
        {
            if (amount <= 0f)
                return;

            _cauldronTasteBonus += amount;
        }

        public float GetCauldronTasteBonus() => _cauldronTasteBonus;

        public float GetFlatStatBonus(BaseStat upgrade)
        {
            if (upgrade == null)
                return 0f;

            return _flatStatBonuses.TryGetValue(upgrade, out var total) ? total : 0f;
        }

        public float GetPercentStatBonus(BaseStat upgrade)
        {
            if (upgrade == null)
                return 0f;

            return _percentStatBonuses.TryGetValue(upgrade, out var total) ? total : 0f;
        }

        public float GetResourceBonus(Skill skill)
        {
            float total = _globalResourceBonus;
            if (skill != null && _taskResourceBonuses.TryGetValue(skill, out var bonus))
                total += bonus;
            return total;
        }

        public float GetGlobalResourceBonus() => _globalResourceBonus;

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<BaseStat, float>> EnumerateTotalFlatBonuses() => _flatStatBonuses;

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<BaseStat, float>> EnumerateTotalPercentBonuses() => _percentStatBonuses;

        public SkillMilestoneSummary GetSummary(Skill skill)
        {
            if (skill == null)
                return EmptySummary;

            return _skillSummaries.TryGetValue(skill, out var summary) ? summary : EmptySummary;
        }

        public System.Collections.Generic.IReadOnlyDictionary<BaseStat, float> GetFlatStatBonusesForSkill(Skill skill)
        {
            if (skill == null)
                return EmptyStatBonusMap;

            if (_skillSummaries.TryGetValue(skill, out var summary) && summary.FlatStatBonuses.Count > 0)
                return summary.FlatStatBonuses;

            return EmptyStatBonusMap;
        }

        public System.Collections.Generic.IReadOnlyDictionary<BaseStat, float> GetPercentStatBonusesForSkill(Skill skill)
        {
            if (skill == null)
                return EmptyStatBonusMap;

            if (_skillSummaries.TryGetValue(skill, out var summary) && summary.PercentStatBonuses.Count > 0)
                return summary.PercentStatBonuses;

            return EmptyStatBonusMap;
        }

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Skill, SkillMilestoneSummary>> EnumerateSkillSummaries() => _skillSummaries;

    }

    public sealed class SkillMilestoneSummary
    {
        public float InstantTaskChance;
        public float InstantKillChance;
        public float DoubleResourceChance;
        public float DoubleXpChance;
        public float ResourceBonusPercent;
        public System.Collections.Generic.Dictionary<BaseStat, float> FlatStatBonuses = new();
        public System.Collections.Generic.Dictionary<BaseStat, float> PercentStatBonuses = new();
        public List<SpawnEchoEntry> SpawnEchoes = new();

        internal void Reset()
        {
            InstantTaskChance = 0f;
            InstantKillChance = 0f;
            DoubleResourceChance = 0f;
            DoubleXpChance = 0f;
            ResourceBonusPercent = 0f;
            if (FlatStatBonuses.Count > 0)
                FlatStatBonuses.Clear();
            if (PercentStatBonuses.Count > 0)
                PercentStatBonuses.Clear();
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

