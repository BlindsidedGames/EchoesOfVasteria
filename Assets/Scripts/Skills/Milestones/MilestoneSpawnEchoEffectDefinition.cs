using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "SpawnEchoEffect", menuName = "SO/Milestones/Effects/Spawn Echo")]
    public class MilestoneSpawnEchoEffectDefinition : MilestoneEffectDefinition
    {
        [SerializeField] private TimelessEchoes.EchoSpawnConfig echoSpawnConfig;
        [SerializeField] [Min(0f)] private float echoDuration = 10f;
        [SerializeField] [Tooltip("Optional explicit skill label override used in generated descriptions.")]
        private string fallbackSkillLabel = "various";

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            var targetSkill = context.Skill;
            context.Aggregator.AddSpawnEntry(targetSkill, echoSpawnConfig, Mathf.Max(0f, magnitude), echoDuration);
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            string skillText = skillName;
            if (echoSpawnConfig != null && echoSpawnConfig.capableSkills != null && echoSpawnConfig.capableSkills.Count > 0)
            {
                if (echoSpawnConfig.capableSkills.Count == 1)
                    skillText = echoSpawnConfig.capableSkills[0]?.skillName ?? skillName;
                else
                    skillText = "various";
            }

            if (string.IsNullOrWhiteSpace(skillText))
                skillText = fallbackSkillLabel;

            var controller = TimelessEchoes.Upgrades.StatUpgradeController.Instance;
            float bonus = 0f;
            if (controller != null)
            {
                var echoUpgrade = controller.AllUpgrades?.FirstOrDefault(u => u != null && u.name == "Echo Lifetime");
                if (echoUpgrade != null)
                    bonus = controller.GetTotalValue(echoUpgrade);
            }

            float totalDuration = echoDuration + bonus;
            string percent = (Mathf.Max(0f, magnitude) * 100f).ToString("0.#", CultureInfo.InvariantCulture);
            return $"Provides a {percent}% chance to summon an Echo that performs {skillText} tasks for {totalDuration:0.#} seconds.";
        }
    }
}
