using TimelessEchoes.UI.Toolkit;
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
                {
                    var skill = configInstance.capableSkills[0];
                    skillText = skill != null ? ToolkitLocalization.Text("skill." + skill.name, skill.skillName) : null;
                }
                else
                    skillText = ToolkitLocalization.Text("milestone-effect." + name + ".fallbackSkillLabel", fallbackSkillLabel);
            }
            else
            {
                skillText = ToolkitLocalization.Text("milestone-effect." + name + ".fallbackSkillLabel", fallbackSkillLabel);
            }

            if (string.IsNullOrWhiteSpace(skillText) && !string.IsNullOrWhiteSpace(skillName))
                skillText = skillName;
            if (string.IsNullOrWhiteSpace(skillText))
                skillText = ToolkitLocalization.Text("milestone.various", "various");

            var echoStat = BaseStatService.GetStat("Echo Lifetime");
            float bonus = echoStat != null ? BaseStatService.GetTotalValue(echoStat) : 0f;

            float totalDuration = echoDuration + bonus;
            string percent = (Mathf.Max(0f, magnitude) * 100f).ToString("0.#", CultureInfo.InvariantCulture);
            int count = Mathf.Max(1, echoCount);
            // Whole sentences preserve grammar and allow translated argument ordering.
            bool combat = configInstance?.echoType == EchoType.Combat;
            if (combat)
                return count == 1
                    ? ToolkitLocalization.Text("milestone.echo.combat-one", "Provides a {0}% chance to summon an Echo that assist in combat for {1:0.#} seconds.", percent, totalDuration)
                    : ToolkitLocalization.Text("milestone.echo.combat-many", "Provides a {0}% chance to summon {1} Echoes that assist in combat for {2:0.#} seconds.", percent, count, totalDuration);
            return count == 1
                ? ToolkitLocalization.Text("milestone.echo.task-one", "Provides a {0}% chance to summon an Echo that perform {1} tasks for {2:0.#} seconds.", percent, skillText, totalDuration)
                : ToolkitLocalization.Text("milestone.echo.task-many", "Provides a {0}% chance to summon {1} Echoes that perform {2} tasks for {3:0.#} seconds.", percent, count, skillText, totalDuration);
        }
    }
}
