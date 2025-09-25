using System.Globalization;
using System.Linq;
using UnityEngine;
using TimelessEchoes.Hero;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "SpawnEchoEffect", menuName = "SO/Milestones/Effects/Spawn Echo")]
    public class MilestoneSpawnEchoEffectDefinition : MilestoneEffectDefinition
    {
        [SerializeField] private TimelessEchoes.EchoSpawnConfig echoSpawnConfig;
        [SerializeField] [Min(0f)] private float echoDuration = 10f;
        [SerializeField] [Min(1)] private int echoCount = 1;
        [SerializeField] [Tooltip("Optional explicit label used when no capable skills are specified on the config.")]
        private string fallbackSkillLabel = "all available";
        [SerializeField] [Tooltip("When enabled and the config has no explicit skills, restrict spawned echoes to the milestone's skill instead of all skills.")]
        private bool restrictToSourceSkillWhenConfigEmpty = false;

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            var configInstance = echoSpawnConfig ?? new TimelessEchoes.EchoSpawnConfig();
            bool hasSpecificSkills = configInstance.capableSkills != null && configInstance.capableSkills.Count > 0;
            bool useAssociatedFallback = !hasSpecificSkills && restrictToSourceSkillWhenConfigEmpty;

            int count = Mathf.Max(1, echoCount);
            context.Aggregator.AddSpawnEntry(
                context.Skill,
                configInstance,
                Mathf.Max(0f, magnitude),
                echoDuration,
                count,
                useAssociatedFallback);
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            var configInstance = echoSpawnConfig;
            bool hasSpecificSkills = configInstance != null && configInstance.capableSkills != null && configInstance.capableSkills.Count > 0;

            string skillText;
            if (hasSpecificSkills)
            {
                if (configInstance.capableSkills.Count == 1)
                    skillText = configInstance.capableSkills[0]?.skillName;
                else
                    skillText = fallbackSkillLabel;
            }
            else
            {
                skillText = fallbackSkillLabel;
            }

            if (string.IsNullOrWhiteSpace(skillText) && !string.IsNullOrWhiteSpace(skillName))
                skillText = skillName;
            if (string.IsNullOrWhiteSpace(skillText))
                skillText = "various";

            var echoStat = BaseStatService.GetStat("Echo Lifetime");
            float bonus = echoStat != null ? BaseStatService.GetTotalValue(echoStat) : 0f;

            float totalDuration = echoDuration + bonus;
            string percent = (Mathf.Max(0f, magnitude) * 100f).ToString("0.#", CultureInfo.InvariantCulture);
            int count = Mathf.Max(1, echoCount);
            string echoLabel = count == 1 ? "an Echo" : $"{count} Echoes";

            string actionPhrase = configInstance?.echoType switch
            {
                EchoType.Combat => "assist in combat",
                EchoType.TaskOnly => $"perform {skillText} tasks",
                _ => $"perform {skillText} tasks"
            };

            return $"Provides a {percent}% chance to summon {echoLabel} that {actionPhrase} for {totalDuration:0.#} seconds.";
        }
    }
}
