using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TimelessEchoes.Upgrades;
using Blindsided.Utilities;
namespace TimelessEchoes.Skills
{
    // Shared presentation only; calculations and ordering are the existing Skills panel implementation.
    public static class SkillTotalsPresentation
    {
        public readonly struct BonusLine
        {
            public readonly string Text, Kind;
            public readonly Skill Skill;
            public readonly BaseStat Stat;
            public BonusLine(string text, string kind, Skill skill = null, BaseStat stat = null)
            { Text = text; Kind = kind; Skill = skill; Stat = stat; }
        }
        public static string BuildForSkill(SkillController controller, Skill skill, List<BonusLine> rows = null)
        {
            if (!controller || !skill) return string.Empty;
            // Presentation-only accumulator: preserve source-skill attribution, including
            // effects which grant XP to a different skill. Never modify the live cache.
            var source = new MilestoneEffectAggregator();
            foreach (var milestone in skill.milestones)
            {
                if (!milestone) continue;
                var state = controller.GetMilestoneState(skill, milestone);
                if (state == null || state.TierIndex < 0) continue;
                var active = state.IsActive && milestone.CanActivate;
                var effect = active ? milestone.ActiveEffect : milestone.PassiveEffect;
                if (!effect) continue;
                effect.Apply(new MilestoneEffectContext(controller, skill, milestone,
                    active ? MilestoneEffectSource.Active : MilestoneEffectSource.Passive, source),
                    active ? milestone.GetActiveValue(state.TierIndex) : milestone.GetPassiveValue(state.TierIndex));
            }
            return Build(controller, new[] { skill }, source, rows);
        }
        public static string Build(SkillController controller, IEnumerable<Skill> skills, MilestoneEffectAggregator source = null, List<BonusLine> rows = null)
        {
            if (controller == null || controller.Aggregator == null) return string.Empty;
            var aggregator = source ?? controller.Aggregator;
            var metadata = new Dictionary<string, BonusLine>();
            string Describe(string text, string kind, Skill skill = null)
            { metadata[text] = new BonusLine(text, kind, skill); return text; }
            var taskSpeedEntries = new List<(string SkillName, string Line)>();
            var combatSkill = controller.CombatSkill;
            foreach (var skill in skills)
            {
                if (skill == null || skill == combatSkill)
                    continue;

                float multiplier = controller.GetTaskSpeedMultiplier(skill);
                float bonusPercent = (multiplier - 1f) * 100f;
                if (bonusPercent <= 0.0001f)
                    continue;

                string skillLabel = ResolveSkillName(skill);
                if (string.IsNullOrWhiteSpace(skillLabel))
                {
                    skillLabel = skill.name;
                    if (string.IsNullOrWhiteSpace(skillLabel))
                        skillLabel = "Skill";
                }

                taskSpeedEntries.Add((skillLabel, Describe($"+{bonusPercent:0.#}% {skillLabel} Task Speed (From {skillLabel} lvl)", "speed", skill)));
            }

            taskSpeedEntries.Sort((left, right) => string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));
            var taskSpeedLines = taskSpeedEntries.Select(entry => entry.Line).ToList();

            var totalContributions = new Dictionary<BaseStat, StatContribution>();
            foreach (var kvp in aggregator.EnumerateTotalFlatBonuses())
                AccumulateStatContribution(totalContributions, kvp.Key, kvp.Value, 0f);
            foreach (var kvp in aggregator.EnumerateTotalPercentBonuses())
                AccumulateStatContribution(totalContributions, kvp.Key, 0f, kvp.Value);

            var statLines = BuildStatLines(totalContributions, metadata);

            float globalResourceBonus = aggregator.GetGlobalResourceBonus();
            float globalExperienceBonus = aggregator.GetGlobalExperienceBonus();

            var resourceLines = new List<string>();
            if (globalResourceBonus > 0f)
                resourceLines.Add(Describe($"{globalResourceBonus * 100f:0.#}% Bonus Resources (All Tasks)", "resources"));

            var experienceLines = new List<string>();
            if (globalExperienceBonus > 0f)
                experienceLines.Add(Describe($"{globalExperienceBonus * 100f:0.#}% Bonus Experience (All Skills)", "xp"));

            var perSkillResourceEntries = new List<(string SkillName, string Line)>();
            var perSkillExperienceEntries = new List<(string SkillName, string Line)>();
            var procEntries = new List<(int Order, string SkillName, string Line)>();
            var echoEntries = new List<(string SkillName, string Line)>();

            foreach (var pair in aggregator.EnumerateSkillSummaries())
            {
                var summary = pair.Value;
                if (summary == null)
                    continue;

                string skillName = ResolveSkillName(pair.Key);

                if (summary.InstantTaskChance > 0f)
                    procEntries.Add((0, skillName, Describe($"{summary.InstantTaskChance * 100f:0.#}% Chance to Instantly Complete Tasks", "speed", pair.Key)));

                if (summary.InstantKillChance > 0f)
                    procEntries.Add((1, skillName, Describe($"{summary.InstantKillChance * 100f:0.#}% Chance to Instantly Kill", "damage", pair.Key)));

                if (summary.DoubleResourceChance > 0f)
                    procEntries.Add((2, skillName, Describe($"{summary.DoubleResourceChance * 100f:0.#}% Chance to Double Resources", "resources", pair.Key)));

                if (summary.DoubleXpChance > 0f)
                    procEntries.Add((3, skillName, Describe($"{summary.DoubleXpChance * 100f:0.#}% Chance to Double XP", "xp", pair.Key)));

                if (summary.ResourceBonusPercent > 0f)
                {
                    float totalPercent = summary.ResourceBonusPercent + globalResourceBonus;
                    string skillLabel = !string.IsNullOrWhiteSpace(skillName) ? skillName : "Associated Skill";
                    perSkillResourceEntries.Add((skillLabel, Describe($"{totalPercent * 100f:0.#}% Bonus Resources ({skillLabel})", "resources", pair.Key)));
                }

                if (summary.ExperienceBonusPercent > 0f)
                {
                    float totalPercent = summary.ExperienceBonusPercent + globalExperienceBonus;
                    string skillLabel = !string.IsNullOrWhiteSpace(skillName) ? skillName : "Associated Skill";
                    perSkillExperienceEntries.Add((skillLabel, Describe($"{totalPercent * 100f:0.#}% Bonus Experience ({skillLabel})", "xp", pair.Key)));
                }

                foreach (var entry in summary.SpawnEchoes)
                {
                    if (entry.Chance <= 0f)
                        continue;

                    string label = BuildEchoSpawnLine(entry, pair.Key);
                    if (!string.IsNullOrEmpty(label))
                        echoEntries.Add((skillName, Describe(label, "echo", pair.Key)));
                }
            }

            perSkillResourceEntries.Sort((left, right) => string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));
            foreach (var entry in perSkillResourceEntries)
                resourceLines.Add(entry.Line);

            perSkillExperienceEntries.Sort((left, right) => string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));
            foreach (var entry in perSkillExperienceEntries)
                experienceLines.Add(entry.Line);

            procEntries.Sort((left, right) =>
            {
                int orderCompare = left.Order.CompareTo(right.Order);
                if (orderCompare != 0)
                    return orderCompare;

                return string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
            });

            var procLines = procEntries.Select(entry => entry.Line).ToList();

            echoEntries.Sort((left, right) =>
            {
                int skillCompare = string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
                if (skillCompare != 0)
                    return skillCompare;

                return string.Compare(left.Line, right.Line, System.StringComparison.OrdinalIgnoreCase);
            });

            var echoLines = echoEntries.Select(entry => entry.Line).ToList();

            var allLines = new List<string>();
            if (statLines.Count > 0)
                allLines.AddRange(statLines);
            if (resourceLines.Count > 0)
                allLines.AddRange(resourceLines);
            if (experienceLines.Count > 0)
                allLines.AddRange(experienceLines);
            if (taskSpeedLines.Count > 0)
                allLines.AddRange(taskSpeedLines);
            if (procLines.Count > 0)
                allLines.AddRange(procLines);
            if (echoLines.Count > 0)
                allLines.AddRange(echoLines);

            if (rows != null) { rows.Clear(); foreach (var line in allLines) rows.Add(metadata.TryGetValue(line, out var entry) ? entry : new BonusLine(line, "")); }
            return allLines.Count > 0 ? string.Join(System.Environment.NewLine, allLines) : string.Empty;
        }
        private static List<string> BuildStatLines(Dictionary<BaseStat, StatContribution> source, Dictionary<string, BonusLine> metadata)
        {
            var lines = new List<string>();
            if (source == null || source.Count == 0)
                return lines;

            foreach (var pair in source.OrderBy(p => p.Key != null ? p.Key.name : string.Empty))
            {
                string formatted = FormatStatLine(pair.Key, pair.Value);
                if (!string.IsNullOrEmpty(formatted))
                {
                    lines.Add(formatted);
                    metadata[formatted] = new BonusLine(formatted, "stat", stat: pair.Key);
                }
            }

            return lines;
        }

        private static string ResolveStatDisplayName(BaseStat stat)
        {
            if (stat == null)
                return "Stat";

            var def = stat.AssociatedStat;
            if (def != null && !string.IsNullOrWhiteSpace(def.displayName))
                return def.displayName;

            return !string.IsNullOrWhiteSpace(stat.name) ? stat.name : "Stat";
        }

        private static string GetIconTagFor(BaseStat stat)
        {
            if (stat == null)
                return string.Empty;

            var def = stat.AssociatedStat;
            if (def != null)
            {
                var iconFromMapping = StatIconLookup.GetIconTag(def.heroMapping);
                if (!string.IsNullOrEmpty(iconFromMapping))
                    return iconFromMapping;

                if (!string.IsNullOrWhiteSpace(def.displayName))
                {
                    var iconFromDisplay = StatIconLookup.GetIconTag(def.displayName);
                    if (!string.IsNullOrEmpty(iconFromDisplay))
                        return iconFromDisplay;
                }
            }

            if (!string.IsNullOrWhiteSpace(stat.name))
            {
                var iconFromName = StatIconLookup.GetIconTag(stat.name);
                if (!string.IsNullOrEmpty(iconFromName))
                    return iconFromName;
            }

            return string.Empty;
        }

        private static string ResolveSkillName(Skill skill)
        {
            if (skill == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(skill.skillName))
                return skill.skillName;

            return !string.IsNullOrWhiteSpace(skill.name) ? skill.name : string.Empty;
        }

        private static string BuildEchoSpawnLine(SpawnEchoEntry entry, Skill sourceSkill)
        {
            if (entry.Chance <= 0f)
                return string.Empty;

            int count = entry.Count > 0 ? entry.Count : 1;
            string target = GetEchoSpawnTargetLabel(entry, sourceSkill);
            string noun = count == 1 ? "Echo" : "Echoes";
            string descriptor = string.IsNullOrEmpty(target) ? noun : $"{target} {noun}";
            string subject = count > 1 ? $"{count} {descriptor}" : FormatSingleEchoDescriptor(descriptor);

            string triggerSkill = GetEchoSpawnTriggerSkillLabel(entry, sourceSkill);
            string triggerSuffix = string.IsNullOrEmpty(triggerSkill) ? string.Empty : $" when {triggerSkill}";

            return $"{entry.Chance * 100f:0.#}% Chance to spawn {subject}{triggerSuffix}";
        }

        private static string GetEchoSpawnTriggerSkillLabel(SpawnEchoEntry entry, Skill sourceSkill)
        {
            var config = entry.Config;

            if (config != null && config.capableSkills != null && config.capableSkills.Count > 0)
            {
                var names = new List<string>();
                foreach (var skill in config.capableSkills)
                {
                    var name = NormalizeTriggerLabel(ResolveSkillName(skill));
                    if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                        names.Add(name);
                }

                if (names.Count == 1)
                    return names[0];

                if (names.Count > 1)
                    return string.Join("/", names);
            }

            var fallback = NormalizeTriggerLabel(ResolveSkillName(sourceSkill));
            return !string.IsNullOrEmpty(fallback) ? fallback : string.Empty;
        }

        private static string NormalizeTriggerLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            if (string.Equals(label, "Combat", System.StringComparison.OrdinalIgnoreCase))
                return "Killing";

            return label;
        }

        private static string FormatSingleEchoDescriptor(string descriptor)
        {
            if (string.IsNullOrWhiteSpace(descriptor))
                return "an Echo";

            string trimmed = descriptor.Trim();
            if (trimmed.Length == 0)
                return "an Echo";

            char first = char.ToLowerInvariant(trimmed[0]);
            bool useAn = first == 'a' || first == 'e' || first == 'i' || first == 'o' || first == 'u';
            string article = useAn ? "an" : "a";
            return $"{article} {trimmed}";
        }

        private static string GetEchoSpawnTargetLabel(SpawnEchoEntry entry, Skill sourceSkill)
        {
            var config = entry.Config;

            if (config != null && config.capableSkills != null && config.capableSkills.Count > 0)
            {
                var names = new List<string>();
                foreach (var skill in config.capableSkills)
                {
                    var name = ResolveSkillName(skill);
                    if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                        names.Add(name);
                }

                if (names.Count == 1)
                    return names[0];
                if (names.Count > 1)
                    return "Selective";
            }

            if (entry.UseAssociatedSkillFallback)
            {
                var fallback = ResolveSkillName(sourceSkill);
                if (!string.IsNullOrEmpty(fallback))
                    return fallback;
            }

            if (config != null)
            {
                switch (config.echoType)
                {
                    case TimelessEchoes.Hero.EchoType.Combat:
                        return "Combat";
                    case TimelessEchoes.Hero.EchoType.TaskOnly:
                        return "Task";
                    case TimelessEchoes.Hero.EchoType.All:
                        return string.Empty;
                    case TimelessEchoes.Hero.EchoType.Selective:
                        return "Selective";
                }
            }

            var defaultName = ResolveSkillName(sourceSkill);
            return !string.IsNullOrEmpty(defaultName) ? defaultName : string.Empty;
        }

        private static string FormatStatLine(BaseStat stat, StatContribution contribution)
        {
            bool hasFlat = !Mathf.Approximately(contribution.Flat, 0f);
            bool hasPercent = !Mathf.Approximately(contribution.Percent, 0f);

            if (!hasFlat && !hasPercent)
                return string.Empty;

            var parts = new List<string>();
            if (hasFlat)
                parts.Add($"{(contribution.Flat >= 0f ? "+" : string.Empty)}{contribution.Flat:0.##}");
            if (hasPercent)
                parts.Add($"{(contribution.Percent >= 0f ? "+" : string.Empty)}{(contribution.Percent * 100f):0.##}%");

            string iconTag = GetIconTagFor(stat);
            if (!string.IsNullOrEmpty(iconTag))
                return $"{iconTag} {string.Join(" ", parts)}";

            string label = ResolveStatDisplayName(stat);
            return $"{label}: {string.Join(" ", parts)}";
        }

        private static void AccumulateStatContribution(Dictionary<BaseStat, StatContribution> map, BaseStat stat, float flatDelta, float percentDelta)
        {
            if (!map.TryGetValue(stat, out var contribution))
                contribution = default;

            contribution.Flat += flatDelta;
            contribution.Percent += percentDelta;
            map[stat] = contribution;
        }

        private struct StatContribution
        {
            public float Flat;
            public float Percent;
        }

    }
}
