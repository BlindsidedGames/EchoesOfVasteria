using System;
using System.Globalization;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "ChanceEffect", menuName = "SO/Milestones/Effects/Chance Proc")]
    public class MilestoneChanceEffectDefinition : MilestoneEffectDefinition
    {
        [SerializeField] private MilestoneProcType procType = MilestoneProcType.InstantTask;
        [SerializeField] private Skill overrideSkill;
        [SerializeField] [Tooltip("String.Format template. {0} => formatted percentage, {1} => skill name.")]
        private string descriptionTemplate = "Provides a {0}% chance to trigger.";
        [SerializeField] private string percentageFormat = "0.#";

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            var targetSkill = overrideSkill != null ? overrideSkill : context.Skill;
            context.Aggregator.AddProcChance(targetSkill, procType, Mathf.Max(0f, magnitude));
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            var percent = Mathf.Max(0f, magnitude) * 100f;
            var formattedPercent = percent.ToString(percentageFormat, CultureInfo.InvariantCulture);
            var resolvedSkillName = string.IsNullOrWhiteSpace(skillName) ? "this skill" : skillName;
            return string.Format(descriptionTemplate, formattedPercent, resolvedSkillName);
        }
    }
}
