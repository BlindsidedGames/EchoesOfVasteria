using System;
using Blindsided;
using TimelessEchoes.Skills;
using UnityEngine;

namespace TimelessEchoes.Characters
{
    /// <summary>Visual tool progression follows each gathering skill, independently of the sword.</summary>
    public static class CharacterSkillTools
    {
        private static readonly int[] Levels = { 1, 7, 14, 20, 27, 36, 46, 56, 68 };
        private static readonly string[] Names = { "Woodcutting", "Mining", "Farming", "Fishing" };
        private static readonly Skill[] Skills = new Skill[4];
        private static CharacterCatalog lastCatalog;
        private static SkillController lastController;
        private static int[] skillByFrame;

        private static int TierForLevel(int level)
        {
            var tier = 0;
            while (tier + 1 < Levels.Length && level >= Levels[tier + 1]) tier++;
            return tier - 1; // Brown precedes the eight metal tiers.
        }

        public static Sprite IconFor(Skill skill, int level, Sprite fallback)
        {
            if (!skill) return fallback;
            var index = Array.FindIndex(Names, name => string.Equals(name, skill.name, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return fallback;
            var catalog = CharacterCatalog.Load();
            var offset = (TierForLevel(level) + 1) * Names.Length + index;
            return catalog && offset < catalog.skillToolIcons.Length && catalog.skillToolIcons[offset] ? catalog.skillToolIcons[offset] : fallback;
        }

        public static Sprite SpriteFor(CharacterCatalog catalog, CharacterProfile profile, int frame)
        {
            if (lastCatalog != catalog) {
                lastCatalog = catalog;
                var count = 0;
                foreach (var pose in catalog.poses) count = Math.Max(count, pose.start + pose.length);
                skillByFrame = new int[count]; Array.Fill(skillByFrame, -1);
                foreach (var pose in catalog.poses) {
                    var skill = pose.name.StartsWith("Chop_", StringComparison.Ordinal) ? 0 :
                        pose.name.StartsWith("Mine_", StringComparison.Ordinal) ? 1 :
                        pose.name.StartsWith("Till_", StringComparison.Ordinal) || pose.name.StartsWith("Watering_", StringComparison.Ordinal) ? 2 :
                        pose.name.StartsWith("Fishing_", StringComparison.Ordinal) ? 3 : -1;
                    for (var i = pose.start; i < pose.start + pose.length; i++) skillByFrame[i] = skill;
                }
            }
            var skillIndex = frame >= 0 && frame < skillByFrame.Length ? skillByFrame[frame] : -1;
            if (skillIndex < 0) return catalog.SpriteFor(profile, CharacterSlot.Tool, frame);
            var controller = SkillController.Instance;
            if (lastController != controller) { lastController = controller; Array.Clear(Skills, 0, Skills.Length); }
            var level = 1;
            if (controller) {
                if (!Skills[skillIndex]) Skills[skillIndex] = controller.FindSkillByIdentifier(Names[skillIndex]);
                level = controller.GetLevel(Skills[skillIndex], Oracle.oracle?.saveData);
            }
            var tier = TierForLevel(level);
            return catalog.Find(tier < 0 ? "Tool/SkillBrown" : "Tool/SkillMetal" + tier)?.At(frame) ?? catalog.SpriteFor(profile, CharacterSlot.Tool, frame);
        }
    }
}
