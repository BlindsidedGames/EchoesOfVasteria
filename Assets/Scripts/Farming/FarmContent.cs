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
        public static int PlantingCost(bool orchard) => orchard ? 1 : 9;
        public string displayNameKey = "farm.display-name";
        public string displayName = "Fields";
        public LocalizedString localizedDisplayName = new("TownUI", "farm.display-name");
        public string DisplayName => localizedDisplayName == null || localizedDisplayName.IsEmpty ? displayName : localizedDisplayName.GetLocalizedString();
        public double baseDurationSeconds = 1800;
        public double baseYield = 10;
        public int harvestXp = 10;
        public float seedChance = .1f;
        public List<FarmRecipe> recipes = new();
        public List<FarmBuild> builds = new();
        public static FarmContent Load() => Resources.Load<FarmContent>("Farming/FarmContent");
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
            if (!Completed(data, build.previousQuestId)) return ToolkitLocalization.Text("fields.error.preceding-build", "Complete the preceding construction quest.");
            if (data.Farm.TwinsLevel < build.twinsLevel) return ToolkitLocalization.Text("fields.error.twins-level", "Twins level {0} required.", build.twinsLevel);
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
        public Resource output;
        public TaskData source;
        public string seedId;
        public Resource paidInput;
        public bool orchard;
        public Sprite packIcon, unknownIcon;
        public Sprite[] stages = Array.Empty<Sprite>();
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
