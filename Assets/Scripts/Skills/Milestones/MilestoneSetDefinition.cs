using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "MilestoneSetDefinition", menuName = "SO/Milestones/Set Definition")]
    public class MilestoneSetDefinition : ScriptableObject
    {
        [SerializeField] private MilestoneSet set = MilestoneSet.None;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] [TextArea] private string threePieceDescription;
        [SerializeField] [TextArea] private string sixPieceDescription;
        [SerializeField] private List<SetBonusEffectEntry> threePieceEffects = new();
        [SerializeField] private List<SetBonusEffectEntry> sixPieceEffects = new();

        public MilestoneSet Set => set;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public string ThreePieceDescription => threePieceDescription;
        public string SixPieceDescription => sixPieceDescription;
        public IReadOnlyList<SetBonusEffectEntry> ThreePieceEffects => threePieceEffects;
        public IReadOnlyList<SetBonusEffectEntry> SixPieceEffects => sixPieceEffects;

        public void ApplyThreePiece(SkillController controller, MilestoneEffectAggregator aggregator)
        {
            ApplyInternal(controller, aggregator, threePieceEffects, MilestoneEffectSource.SetThreePiece);
        }

        public void ApplySixPiece(SkillController controller, MilestoneEffectAggregator aggregator)
        {
            ApplyInternal(controller, aggregator, sixPieceEffects, MilestoneEffectSource.SetSixPiece);
        }

        private void ApplyInternal(
            SkillController controller,
            MilestoneEffectAggregator aggregator,
            IReadOnlyList<SetBonusEffectEntry> entries,
            MilestoneEffectSource source)
        {
            if (entries == null || entries.Count == 0)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Effect == null)
                    continue;

                var targetSkill = entry.OverrideSkill != null
                    ? entry.OverrideSkill
                    : controller != null ? controller.CombatSkill : null;

                var context = new MilestoneEffectContext(controller, targetSkill, null, source, aggregator);
                entry.Effect.Apply(context, entry.Magnitude);
            }
        }
    }

    [Serializable]
    public class SetBonusEffectEntry
    {
        [SerializeField] private MilestoneEffectDefinition effect;
        [SerializeField] private float magnitude = 1f;
        [SerializeField] private Skill overrideSkill;

        public MilestoneEffectDefinition Effect => effect;
        public float Magnitude => magnitude;
        public Skill OverrideSkill => overrideSkill;
    }
}
