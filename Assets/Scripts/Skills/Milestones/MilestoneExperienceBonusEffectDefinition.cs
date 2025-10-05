using System.Globalization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "ExperienceBonusEffect", menuName = "SO/Milestones/Effects/Experience Bonus")]
    public class MilestoneExperienceBonusEffectDefinition : MilestoneEffectDefinition
    {
        [SerializeField]
        [Tooltip("When enabled, applies the bonus to every skill's experience gain.")]
        private bool applyToAllSkills = false;

        [SerializeField]
        [HideIf(nameof(applyToAllSkills))]
        [Tooltip("Optional skill override used when Apply To All Skills is disabled.")]
        private Skill overrideSkill;

        [SerializeField]
        [Tooltip("String.Format template. {0} => skill name, {1} => formatted percent.")]
        private string skillDescriptionTemplate = "Increases {0} experience gained by {1}.";

        [SerializeField]
        [Tooltip("String.Format template for the global case. {0} => formatted percent.")]
        private string globalDescriptionTemplate = "Increases all skills experience gained by {0}.";

        [SerializeField]
        private string percentageFormat = "0.#";

        [SerializeField]
        [Tooltip("Label used when no skill name is available.")]
        private string fallbackSkillLabel = "this skill";

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            float clamped = Mathf.Max(0f, magnitude);
            if (clamped <= 0f)
                return;

            if (applyToAllSkills)
            {
                context.Aggregator.AddExperienceBonus(null, clamped);
                return;
            }

            var targetSkill = overrideSkill != null ? overrideSkill : context.Skill;
            context.Aggregator.AddExperienceBonus(targetSkill, clamped);
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            float percent = Mathf.Max(0f, magnitude) * 100f;
            string formattedPercent = percent.ToString(percentageFormat, CultureInfo.InvariantCulture) + "%";

            if (applyToAllSkills)
                return string.Format(globalDescriptionTemplate, formattedPercent);

            string resolvedSkill = overrideSkill != null ? overrideSkill.skillName : skillName;
            if (string.IsNullOrWhiteSpace(resolvedSkill))
                resolvedSkill = fallbackSkillLabel;

            return string.Format(skillDescriptionTemplate, resolvedSkill, formattedPercent);
        }
    }
}