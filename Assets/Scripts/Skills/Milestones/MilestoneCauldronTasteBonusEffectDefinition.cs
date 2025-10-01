using System.Globalization;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "CauldronTasteBonusEffect", menuName = "SO/Milestones/Effects/Cauldron Taste Bonus")]
    public class MilestoneCauldronTasteBonusEffectDefinition : MilestoneEffectDefinition
    {
        [SerializeField]
        [Tooltip("String.Format template. {0} => formatted extra stew value, {1} => formatted card multiplier.")]
        private string descriptionTemplate = "+{0} Stew per taste; cards gained x{1}.";

        [SerializeField]
        private string extraValueFormat = "0.#";

        [SerializeField]
        private string multiplierFormat = "0.#";

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            float bonus = Mathf.Max(0f, magnitude);
            if (bonus <= 0f)
                return;

            context.Aggregator.AddCauldronTasteBonus(bonus);
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            float bonus = Mathf.Max(0f, magnitude);
            float multiplier = 1f + bonus;

            string extraFormatted = bonus.ToString(extraValueFormat, CultureInfo.InvariantCulture);
            string multiplierFormatted = multiplier.ToString(multiplierFormat, CultureInfo.InvariantCulture);

            return string.Format(descriptionTemplate, extraFormatted, multiplierFormatted);
        }
    }
}

