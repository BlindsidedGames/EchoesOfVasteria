using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Buffs;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Quests;
using TimelessEchoes.Shop;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Stats
{
    // Adapter to authored content and the existing live progression owners. This
    // prototype deliberately does not run encounter AI or fabricate NPC interactions.
    public sealed class BackgroundRunContent
    {
        private readonly MapGenerationConfig map;
        private readonly SkillController skills;
        private readonly HeroStatsSnapshot hero;
        private readonly bool killScaling;
        private readonly int killsPerLevel, levelsPerTier;
        private readonly double shopSeconds;
        private readonly bool permanentShop;
        private readonly float resourceBuff, xpBuff, buffSeconds;
        private readonly double echoTaskShare, echoKillShare;
        private readonly System.Random random;
        private readonly ProceduralTaskGenerator.WeightedTaskCategory[] categories;

        public BackgroundRunContent(MapGenerationConfig config, bool killMode, int killsPerTier, int levels,
            RunSaveData run, double taskShare = 0, double killShare = 0)
        {
            map = config; killScaling = killMode; killsPerLevel = killsPerTier; levelsPerTier = levels;
            skills = SkillController.Instance; hero = HeroStatSystem.GetSnapshot();
            var shop = ShopService.Instance; permanentShop = shop && shop.PermanentEnabled; shopSeconds = shop ? shop.Remaining : 0;
            resourceBuff = BuffManager.Instance ? BuffManager.Instance.ResourceGainMultiplier : 1;
            xpBuff = BuffManager.Instance ? BuffManager.Instance.ExperienceGainMultiplier : 1;
            buffSeconds = run.Buffs?.Active?.Count > 0 ? run.Buffs.Active.Max(b => b.Remaining) : 0;
            echoTaskShare = taskShare; echoKillShare = killShare;
            random = new System.Random(unchecked((int)(run.CheckpointUtcTicks ^ (run.CheckpointUtcTicks >> 32) ^ run.Stats.Kills)));
            var settings = map.taskGeneratorSettings;
            categories = new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting };
        }

        public double Pressure(double distance, int kills)
        {
            double total = 0, pressure = 0;
            foreach (var enemy in map.taskGeneratorSettings.enemies)
            {
                var weight = EnemyWeight(enemy, (float)distance);
                if (weight <= 0) continue;
                var level = enemy.GetLevel((float)distance - map.taskGeneratorSettings.enemySpawnXOffset - enemy.minX);
                if (killScaling && killsPerLevel > 0) level += kills / killsPerLevel * levelsPerTier;
                var crit = 1 + Mathf.Clamp01(hero.critChancePercent / 100) * Mathf.Max(0, hero.critDamagePercent) / 100;
                var killBonus = EnemyKillTracker.Instance ? EnemyKillTracker.Instance.GetDamageMultiplier(enemy) : 1;
                var dps = Combat.ApplyDefense(hero.damage * crit * killBonus, enemy.GetDefenseForLevel(level)) * hero.attacksPerSecond;
                var exposure = enemy.GetMaxHealthForLevel(level) / Math.Max(.01, dps);
                pressure += weight * exposure * enemy.attackSpeed * Combat.ApplyDefense(enemy.GetDamageForLevel(level), hero.defense);
                total += weight;
            }
            return total > 0 ? pressure / total : 0;
        }

        private float EnemyWeight(TimelessEchoes.Enemies.EnemyData enemy, float x)
        {
            if (!enemy || !enemy.prefab || enemy.GetSpawnTerrainError() != null) return 0;
            var settings = map.taskGeneratorSettings; var local = x - settings.enemySpawnXOffset;
            if (local < Mathf.Max(settings.minX, enemy.minX) || local > enemy.maxX) return 0;
            var t = map.tilemapChunkSettings;
            if (!enemy.spawnTerrains.Any(terrain => terrain == t.bottomTerrain || terrain == t.middleTerrain || terrain == t.topTerrain)) return 0;
            var curve = settings.GetEnemyWeightCurve(enemy);
            return Mathf.Max(0, curve != null && curve.length > 0 ? curve.Evaluate(x) : settings.GetEnemyWeight(enemy));
        }

        private float CategoryWeight(ProceduralTaskGenerator.WeightedTaskCategory category, float x)
            => category != null && QuestUtils.QuestCompleted(category.requiredQuest) &&
                (string.IsNullOrEmpty(category.requiredNpcId) || StaticReferences.CompletedNpcTasks.Contains(category.requiredNpcId)) &&
                category.tasks.Any(task => TaskWeight(task, x) > 0) ? category.GetWeight(x) : 0;

        private float TaskWeight(TaskData task, float x)
        {
            if (!task || !task.taskPrefab) return 0;
            var terrain = map.tilemapChunkSettings;
            return task.GetWeight(x) * task.GetTerrainMultiplier(terrain.bottomTerrain, terrain.middleTerrain, terrain.topTerrain);
        }

        private T Choose<T>(IEnumerable<T> source, Func<T, float> weight) where T : class
        {
            var candidates = source.Where(x => weight(x) > 0).ToArray();
            var total = candidates.Sum(x => (double)weight(x));
            var roll = random.NextDouble() * total;
            foreach (var entry in candidates) { roll -= weight(entry); if (roll <= 0) return entry; }
            return candidates.LastOrDefault();
        }

        public void Award(BackgroundRunEstimator.Step step, double elapsed)
        {
            var tracker = GameplayStatTracker.Instance;
            for (var i = 0; i < step.Tasks; i++)
            {
                var category = Choose(categories, c => CategoryWeight(c, (float)step.Distance));
                if (category == null) continue;
                var task = Choose(category.tasks, t => TaskWeight(t, (float)step.Distance));
                if (!task) continue;
                var xp = Experience(task.associatedSkill, task.xpForCompletion, echoTaskShare, elapsed, true);
                Drops(task.resourceDrops, task.additionalLootChances, task.associatedSkill, elapsed);
                foreach (var bonus in DropResolver.RollBonusDrops(task.bonusDrops, () => (float)random.NextDouble()))
                    ResourceManager.Instance.Add(bonus.resource, bonus.count, eligibleForTierRoll: false);
                tracker.RegisterTaskComplete(task, task.taskDuration, xp);
            }
            for (var i = 0; i < step.Kills; i++)
            {
                var enemy = Choose(map.taskGeneratorSettings.enemies, e => EnemyWeight(e, (float)step.Distance));
                if (!enemy) continue;
                Experience(skills ? skills.CombatSkill : null, enemy.experience, echoKillShare, elapsed, false);
                Drops(enemy.resourceDrops, enemy.additionalLootChances, skills ? skills.CombatSkill : null, elapsed);
                EnemyKillTracker.Instance?.RegisterKill(enemy); tracker.AddKill(enemy);
            }
        }

        private float Experience(Skill skill, float amount, double echoShare, double elapsed, bool task)
        {
            if (!skills || !skill) return 0;
            if (task) amount *= 1 + Mathf.Max(0, skills.Aggregator.GetProcChance(skill, MilestoneProcType.DoubleXP));
            amount *= (float)(1 - .75 * echoShare);
            amount *= 1 + skills.Aggregator.GetExperienceBonus(skill);
            amount *= elapsed < buffSeconds ? xpBuff : 1;
            // Already accounted for foreground multipliers; do not multiply a second time.
            return skills.GrantQuestExperience(skill, amount);
        }

        private void Drops(IEnumerable<ResourceDrop> drops, IList<float> extra, Skill skill, double elapsed)
        {
            var stack = skills ? 1 + Mathf.Max(0, skills.Aggregator.GetProcChance(skill, MilestoneProcType.DoubleResources)) : 1;
            var gain = skills ? skills.GetResourceGainMultiplier() * skills.GetResourceBonusMultiplier(skill) : 1;
            var buff = elapsed < buffSeconds ? resourceBuff : 1;
            var shop = permanentShop || elapsed < shopSeconds ? ShopRules.GatheringMultiplier : 1;
            foreach (var drop in DropResolver.RollDrops(drops, extra, skill, false, () => (float)random.NextDouble()))
            {
                var raw = drop.count * stack;
                var amount = (raw * gain * buff + CauldronResourceYield.Supplement(Oracle.oracle?.saveData, drop.resource, raw)) * shop;
                ResourceManager.Instance.Add(drop.resource, amount);
            }
        }

        public double EstimateResourcesPerWork(bool combat, double x)
        {
            // Read-only sampling uses its own RNG; never consumes Unity's global sequence.
            var sampleRandom = new System.Random(175);
            double total = 0;
            for (var i = 0; i < 128; i++)
            {
                Skill skill; IEnumerable<ResourceDrop> drops; IList<float> extra;
                if (combat)
                {
                    var enemy = Choose(map.taskGeneratorSettings.enemies, e => EnemyWeight(e, (float)x));
                    if (!enemy) continue;
                    skill = skills ? skills.CombatSkill : null; drops = enemy.resourceDrops; extra = enemy.additionalLootChances;
                }
                else
                {
                    var category = Choose(categories, c => CategoryWeight(c, (float)x));
                    var task = category == null ? null : Choose(category.tasks, t => TaskWeight(t, (float)x));
                    if (!task) continue;
                    skill = task.associatedSkill; drops = task.resourceDrops; extra = task.additionalLootChances;
                }
                var stack = skills ? 1 + Mathf.Max(0, skills.Aggregator.GetProcChance(skill, MilestoneProcType.DoubleResources)) : 1;
                var gain = skills ? skills.GetResourceGainMultiplier() * skills.GetResourceBonusMultiplier(skill) : 1;
                foreach (var drop in DropResolver.RollDrops(drops, extra, skill, false, () => (float)sampleRandom.NextDouble()))
                {
                    var tier = ResourceManager.Instance ? 1 + ResourceManager.Instance.GetTierBonusPercent(ResourceManager.Instance.GetTier(drop.resource)) / 100d : 1;
                    total += drop.count * stack * gain * tier;
                }
            }
            return total / 128;
        }
    }
}
