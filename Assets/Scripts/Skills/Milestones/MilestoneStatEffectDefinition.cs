using System;
using System.Globalization;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "StatEffect", menuName = "SO/Milestones/Effects/Stat Bonus")]
    public class MilestoneStatEffectDefinition : MilestoneEffectDefinition
    {
        [SerializeField] private BaseStat baseStat;
        [SerializeField] private bool percentBonus;
        [SerializeField] [Tooltip("String.Format template. {0} => formatted amount, {1} => stat name.")]
        private string descriptionTemplate = "Increases {1} by {0}.";
        [SerializeField] private string amountFormat = "0.#";

        public override void Apply(MilestoneEffectContext context, float magnitude)
        {
            if (percentBonus)
                context.Aggregator.AddPercentStatBonus(baseStat, magnitude);
            else
                context.Aggregator.AddFlatStatBonus(baseStat, magnitude);
        }

        public override string GetDescription(float magnitude, string skillName, bool isActive)
        {
            var statName = baseStat != null ? baseStat.name : "stat";
            string formatted = percentBonus
                ? (magnitude * 100f).ToString(amountFormat, CultureInfo.InvariantCulture) + "%"
                : magnitude.ToString(amountFormat, CultureInfo.InvariantCulture);
            return string.Format(descriptionTemplate, formatted, statName);
        }
    }
}
