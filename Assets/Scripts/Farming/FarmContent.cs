using TimelessEchoes.UI.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using TimelessEchoes.Skills;
using UnityEngine.Localization;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Farming/Content")]
    public sealed class FarmContent : ScriptableObject
    {
        public const string IntroductionId = "Farm.Garden.Introduction.v1";
        public const int AutoReplantLevel = 20;
        public const int AutoWaterLevel = 50;
        public static int PlantingCost(bool orchard) => 1;
        public string displayNameKey = "farm.display-name";
        public string displayName = "Fields";
        public LocalizedString localizedDisplayName = new("TownUI", "farm.display-name");
        public string DisplayName => localizedDisplayName == null || localizedDisplayName.IsEmpty ? displayName : localizedDisplayName.GetLocalizedString();
        public double baseDurationSeconds = 600;
        public double durationMultiplierPerCropLevel = 1.02;
        public double minimumDurationSeconds = 60;
        public int minimumCropCapLevel = 100;
        public int lastCropCapLevel = 5000;
        public double baseYield = 10;
        public int harvestXp = 10;
        public float seedChance = .1f;
        public List<FarmRecipe> recipes = new();
        public List<FarmBuild> builds = new();
        private static FarmContent cached;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cached = null;
        // Navigation polls the display name even when Fields is closed. Reuse the
        // authored object instead of asking Unity to preload its graph every poll.
        public static FarmContent Load() => cached ? cached : cached = Resources.Load<FarmContent>("Farming/FarmContent");
        public FarmRecipe Recipe(string id) => recipes?.FirstOrDefault(r => r != null && r.id == id);
        public FarmBuild Build(string id) => builds?.FirstOrDefault(b => b != null && b.questId == id);
        public static int SkillLevel(GameData data, string key)
        {
            var controller = SkillController.Instance;
            var skill = controller ? controller.FindSkillByIdentifier(key) : null;
            return skill ? controller.GetLevel(skill, data) : data?.SkillData?.TryGetValue(key, out var saved) == true ? Math.Max(1, saved.Level) : 1;
        }
        public static bool Completed(GameData data, string id) => data?.Quests?.TryGetValue(id, out var quest) == true && quest?.Completed == true;
        public static double YieldMultiplier(int level) => 1 + .01 * (Math.Max(1, level) - 1d);
        public static double SpeedFactor(int level) => level >= 60 ? .7 : level >= 30 ? .8 : level >= 10 ? .9 : 1;
        public int GrowthCapLevel(FarmRecipe recipe)
        {
            var count = 0; var rank = 0;
            foreach (var candidate in recipes)
            {
                if (candidate?.source == null) continue;
                count++;
                var comparison = candidate.source.requiredSkillLevel.CompareTo(recipe.source.requiredSkillLevel);
                if (comparison < 0 || comparison == 0 && string.CompareOrdinal(candidate.id, recipe.id) < 0) rank++;
            }
            return Math.Max(minimumCropCapLevel,
                (int)Math.Round(lastCropCapLevel * (rank + 1d) / Math.Max(1, count)));
        }
        public double GrowthDuration(FarmRecipe recipe, int fieldsLevel)
        {
            var level = Math.Max(1, fieldsLevel);
            var baseSeconds = baseDurationSeconds * Math.Pow(durationMultiplierPerCropLevel, Math.Max(1, recipe.source.requiredSkillLevel) - 1);
            var capLevel = GrowthCapLevel(recipe);
            if (level >= capLevel) return minimumDurationSeconds;
            // Retain the early milestones, then keep improving toward each crop's cap.
            var seconds = baseSeconds * SpeedFactor(level);
            if (level > 60)
                seconds *= Math.Pow(minimumDurationSeconds / (baseSeconds * SpeedFactor(60)),
                    (double)(level - 60) / (capLevel - 60));
            return Math.Max(minimumDurationSeconds, seconds);
        }
        public int HarvestExperience(FarmRecipe recipe)
        {
            // Scale faster than base growth time so later seeds improve XP per minute.
            // Fields speed upgrades and watering shorten the wait without reducing XP.
            var durationRatio = Math.Pow(durationMultiplierPerCropLevel, Math.Max(1, recipe.source.requiredSkillLevel) - 1);
            return checked((int)Math.Ceiling(harvestXp * Math.Pow(durationRatio, 1.5)));
        }
        public static int XpRequired(int level) => checked(5 + Math.Max(1, level) / 2);
        public static void AddXp(FarmState state, int amount)
        {
            if (amount < 0 || state.TwinsLevel < 1 || state.TwinsXp < 0) throw new InvalidOperationException("Invalid twins progression.");
            state.TwinsXp = checked(state.TwinsXp + amount);
            while (state.TwinsLevel < int.MaxValue && state.TwinsXp >= XpRequired(state.TwinsLevel))
            { state.TwinsXp -= XpRequired(state.TwinsLevel); state.TwinsLevel++; }
        }
        public string BuildGate(GameData data, FarmBuild build)
        {
            if (data?.Farm == null || data.General == null || build == null) return ToolkitLocalization.Text("fields.error.unknown-build", "Unknown construction.");
            if (!Completed(data, build.previousQuestId)) return build.previousQuestId == IntroductionId
                ? ToolkitLocalization.Text("fields.error.farming-introduction", "Complete Flora and Tillman's introduction first.")
                : ToolkitLocalization.Text("fields.error.preceding-build", "Build the earlier plots first.");
            if (data.Farm.TwinsLevel < build.twinsLevel) return ToolkitLocalization.Text("fields.error.fields-level", "Fields level {0} required.", build.twinsLevel);
            if (build.sources == null || build.sources.Count == 0) return ToolkitLocalization.Text("fields.error.missing-source", "Missing gathering sources.");
            foreach (var source in build.sources)
                if (!source || !source.associatedSkill || SkillLevel(data, source.associatedSkill.name) < source.requiredSkillLevel ||
                    data.General.MaxRunDistance < source.GetEffectiveMinX()) return ToolkitLocalization.Text("fields.error.source-inaccessible", "Gathering source not accessible yet.");
            return null;
        }
        public bool CanPlant(GameData data, FarmRecipe recipe) => recipe != null && recipe.source && recipe.source.associatedSkill &&
            Completed(data, IntroductionId) && SkillLevel(data, recipe.source.associatedSkill.name) >= recipe.source.requiredSkillLevel;
    }
    [Serializable] public sealed class FarmRecipe
    {
        public string id;
        public string displayName;
        public Resource output;
        public Resource secondaryOutput;
        public double secondaryYieldRatio;
        public TaskData source;
        public string seedId;
        public Resource paidInput;
        public bool orchard;
        public Sprite packIcon, unknownIcon;
        public Sprite[] stages = Array.Empty<Sprite>();
        public string DisplayName => string.IsNullOrEmpty(displayName) ? ToolkitLocalization.Name(output) : ToolkitLocalization.Text("fields.tree." + id, displayName);
    }
    [Serializable] public sealed class FarmBuild
    {
        public string questId, previousQuestId, title;
        public int twinsLevel, gardenCapacity, orchardCapacity;
        public List<TaskData> sources = new();
        public List<FarmBuildCost> costs = new();
    }
    [Serializable] public sealed class FarmBuildCost { public Resource resource; public int amount; }
}
