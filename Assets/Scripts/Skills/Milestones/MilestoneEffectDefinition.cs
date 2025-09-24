using UnityEngine;

namespace TimelessEchoes.Skills
{
    /// <summary>
    /// Source that contributed an effect when recalculating milestone bonuses.
    /// </summary>
    public enum MilestoneEffectSource
    {
        Passive,
        Active,
        SetThreePiece,
        SetSixPiece
    }

    /// <summary>
    /// Built-in proc types that milestone effects can contribute toward.
    /// </summary>
    public enum MilestoneProcType
    {
        InstantTask,
        InstantKill,
        DoubleResources,
        DoubleXP
    }

    /// <summary>
    /// Context passed to <see cref="MilestoneEffectDefinition"/> when applying an effect.
    /// </summary>
    public readonly struct MilestoneEffectContext
    {
        public readonly SkillController Controller;
        public readonly Skill Skill;
        public readonly MilestoneDefinition SourceDefinition;
        public readonly MilestoneEffectSource Source;
        public readonly MilestoneEffectAggregator Aggregator;

        public MilestoneEffectContext(
            SkillController controller,
            Skill skill,
            MilestoneDefinition sourceDefinition,
            MilestoneEffectSource source,
            MilestoneEffectAggregator aggregator)
        {
            Controller = controller;
            Skill = skill;
            SourceDefinition = sourceDefinition;
            Source = source;
            Aggregator = aggregator;
        }
    }

    /// <summary>
    /// Base class for all milestone effect authoring assets.
    /// </summary>
    public abstract class MilestoneEffectDefinition : ScriptableObject
    {
        /// <summary>
        /// Applies the effect's contribution using the supplied magnitude.
        /// </summary>
        /// <param name=\"context\">Contextual data about the source milestone/set.</param>
        /// <param name=\"magnitude\">Tier-scaled value configured on the milestone or set entry.</param>
        public abstract void Apply(MilestoneEffectContext context, float magnitude);

        /// <summary>
        /// Builds a user-facing description for the effect using the supplied magnitude.
        /// </summary>
        /// <param name=\"magnitude\">Tier-scaled value configured on the milestone or set entry.</param>
        /// <param name=\"skillName\">Optional skill label for contextual descriptions.</param>
        /// <param name=\"isActive\">Whether this description represents the active state.</param>
        public abstract string GetDescription(float magnitude, string skillName, bool isActive);
    }
}
