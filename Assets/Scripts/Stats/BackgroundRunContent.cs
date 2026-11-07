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
        private readonly List<CombatBuff> combatBuffs = new();
        private readonly bool killScaling;
        private readonly int killsPerLevel, levelsPerTier;
        private readonly double shopSeconds;
        private readonly bool permanentShop;
        private readonly List<(BuffManager.BuffState state, List<BuffEffect> effects)> savedBuffs = new();
        private readonly double echoTaskShare, echoKillShare;
        private readonly System.Random random;
        private readonly ProceduralTaskGenerator.WeightedTaskCategory[] categories;

        public BackgroundRunContent(MapGenerationConfig config, bool killMode, int killsPerTier, int levels,
            RunSaveData run, double taskShare = 0, double killShare = 0)
        {
            map = config; killScaling = killMode; killsPerLevel = killsPerTier; levelsPerTier = levels;
            skills = SkillController.Instance; hero = HeroStatSystem.GetSnapshotWithoutBuffs();
            var shop = ShopService.Instance; permanentShop = shop && shop.PermanentEnabled; shopSeconds = shop ? shop.Remaining : 0;
            var recipes = Blindsided.Utilities.AssetCache.GetAll<BuffRecipe>().Where(r => r && !r.IsRetired).ToArray();
            if (run.Buffs?.Active != null)
                foreach (var state in run.Buffs.Active)
                {
                    if (state == null) continue;
                    var recipe = recipes.FirstOrDefault(r => r.name == state.Recipe);
                    if (recipe) savedBuffs.Add((state, recipe.GetAggregatedEffects()));
                }
            var manager = BuffManager.Instance;
            var automatic = new HashSet<BuffRecipe>();
            if (manager)
                for (var slot = 0; slot < manager.UnlockedSlots; slot++)
                {
                    var recipe = manager.GetAssigned(slot);
                    if (manager.IsSlotAutoCasting(slot) && recipe && recipe.IsUnlocked && !recipe.IsRetired)
                        automatic.Add(recipe);
                }
            foreach (var recipe in recipes.Where(r => automatic.Contains(r) || savedBuffs.Any(b => b.state.Recipe == r.name)))
            {
                var state = run.Buffs?.Active?.FirstOrDefault(b => b != null && b.Recipe == recipe.name);
                var effects = recipe.GetAggregatedEffects();
                var cooldown = run.Buffs?.Cooldowns != null && run.Buffs.Cooldowns.TryGetValue(recipe.name, out var cd) ? cd : 0;
                combatBuffs.Add(new CombatBuff
                {
                    Effects = effects, Auto = automatic.Contains(recipe), Remaining = state?.Remaining ?? 0,
                    ExpireAtDistance = state?.ExpireAtDistance ?? 0, InitialCooldown = cooldown,
                    Duration = recipe.GetDuration(), Cooldown = recipe.GetCooldown(),
                    DistanceBased = recipe.durationType == BuffDurationType.DistancePercent,
                    DistanceLimit = Mathf.Max(1, GameplayStatTracker.Instance ? GameplayStatTracker.Instance.LongestRun : 1) * recipe.GetDuration(),
                    ConcurrentCooldown = effects.Any(e => e.type == BuffEffectType.MaxDistancePercent || e.type == BuffEffectType.MaxDistanceIncrease),
                    Echoes = recipe.echoConfig != null && (recipe.echoConfig.echoType == EchoType.Combat || recipe.echoConfig.echoType == EchoType.All)
                        ? recipe.GetEchoCount() : 0
                });
            }
            echoTaskShare = taskShare; echoKillShare = killShare;
            random = new System.Random(unchecked((int)(run.CheckpointUtcTicks ^ (run.CheckpointUtcTicks >> 32) ^ run.Stats.Kills)));
            var settings = map.taskGeneratorSettings;
            categories = new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting };
        }

        private float BuffValue(BuffEffectType type, double elapsed, double distance)
        {
            float total = 0;
            foreach (var buff in savedBuffs)
                if (float.IsFinite(buff.state.ExpireAtDistance) && buff.state.ExpireAtDistance > 0 ? distance < buff.state.ExpireAtDistance : elapsed < buff.state.Remaining)
                    foreach (var effect in buff.effects)
                        if (effect.type == type) total += type == BuffEffectType.ResourceMultiplier && Mathf.Approximately(effect.value, 1) ? 100 : effect.value;
            return total;
        }

        public (double multiplier, double flat) CapModifiers(double elapsed, double distance) =>
            (1 + BuffValue(BuffEffectType.MaxDistancePercent, elapsed, distance) / 100d,
                BuffValue(BuffEffectType.MaxDistanceIncrease, elapsed, distance));

        public void EndRun() => savedBuffs.Clear();

        private sealed class CombatBuff
        {
            public List<BuffEffect> Effects;
            public double Remaining, ExpireAtDistance, InitialCooldown, Duration, Cooldown, DistanceLimit;
            public int Echoes;
            public bool Auto, DistanceBased, ConcurrentCooldown;

            public bool Active(double seconds, double distance, bool firstRun)
            {
                if (DistanceBased)
                    return firstRun && ExpireAtDistance > distance && Remaining > 0 ||
                           Auto && distance < DistanceLimit && seconds >= (firstRun ? InitialCooldown : 0);
                if (firstRun && Remaining > 0)
                {
                    if (seconds < Remaining) return true;
                    seconds -= Remaining + (ConcurrentCooldown ? Math.Max(0, InitialCooldown - Remaining) : Cooldown);
                }
                else if (firstRun) seconds -= InitialCooldown;
                var period = ConcurrentCooldown ? Math.Max(Duration, Cooldown) : Duration + Cooldown;
                return Auto && Duration > 0 && seconds >= 0 && period > 0 && seconds % period < Duration;
            }
        }

        private (HeroStatsSnapshot stats, int echoes) CombatProfile(double distance, double seconds, bool firstRun)
        {
            var stats = hero;
            float Value(BuffEffectType type) => combatBuffs.Where(b => b.Active(seconds, distance, firstRun))
                .Sum(b => b.Effects.Where(e => e.type == type).Sum(e => e.value));
            stats.damage *= Math.Max(0, 1 + Value(BuffEffectType.DamagePercent) / 100);
            stats.attacksPerSecond *= Math.Max(0, 1 + Value(BuffEffectType.AttackSpeedPercent) / 100);
            stats.defense *= Math.Max(0, 1 + Value(BuffEffectType.DefensePercent) / 100);
            stats.critChancePercent += Math.Max(0, Value(BuffEffectType.CritChancePercent));
            stats.critDamagePercent += Math.Max(0, Value(BuffEffectType.CritDamagePercent));
            stats.healthRegenPerSecond *= 1 + Math.Max(0, Value(BuffEffectType.HealthRegenPercent)) / 100;
            var echoes = combatBuffs.Where(b => b.Active(seconds, distance, firstRun)).Sum(b => b.Echoes);
            return (stats, echoes);
        }

        private (double damage, bool defeated) Encounter(TimelessEchoes.Enemies.EnemyData enemy,
            double distance, int kills, (HeroStatsSnapshot stats, int echoes) profile)
        {
            var level = enemy.GetLevel((float)distance - map.taskGeneratorSettings.enemySpawnXOffset - enemy.minX);
            if (killScaling && killsPerLevel > 0) level += kills / killsPerLevel * levelsPerTier;
            var killBonus = EnemyKillTracker.Instance ? EnemyKillTracker.Instance.GetDamageMultiplier(enemy) : 1;
            return Combat.ExpectedEncounter(profile.stats, enemy, level, profile.echoes, killBonus);
        }

        public BackgroundRunEstimator.CombatRates CombatRates(double distance, int kills, double seconds, bool firstRun)
        {
            var profile = CombatProfile(distance, seconds, firstRun);
            double total = 0, damage = 0, defeated = 0;
            foreach (var enemy in map.taskGeneratorSettings.enemies)
            {
                var weight = EnemyWeight(enemy, (float)distance);
                if (weight <= 0) continue;
                var encounter = Encounter(enemy, distance, kills, profile);
                damage += weight * encounter.damage;
                if (encounter.defeated) defeated += weight;
                total += weight;
            }
            return new BackgroundRunEstimator.CombatRates(total > 0 ? damage / total : 0,
                profile.stats.healthRegenPerSecond, total > 0 ? defeated / total : 0);
        }

        private float EnemyWeight(TimelessEchoes.Enemies.EnemyData enemy, float x)
        {
            if (!enemy || !enemy.prefab || enemy.GetSpawnTerrainError() != null) return 0;
            var settings = map.taskGeneratorSettings; var local = x - settings.enemySpawnXOffset;
            var opening = map.name == "Woods" && !StaticReferences.CompletedNpcTasks.Contains("Ivan1") ? 50 : 0;
            if (local < Mathf.Max(settings.minX, enemy.minX, opening) || local > enemy.maxX) return 0;
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

        private sealed class Choices<T> where T : class
        {
            private readonly (T item, double upper)[] entries;
            private readonly double total;
            public Choices(IEnumerable<T> source, Func<T, float> weight)
            {
                var list = new List<(T, double)>();
                foreach (var item in source)
                {
                    var value = weight(item);
                    if (!(value > 0)) continue;
                    total += value; list.Add((item, total));
                }
                entries = list.ToArray();
            }
            public T Pick(System.Random random)
            {
                var roll = random.NextDouble() * total;
                foreach (var entry in entries) if (roll <= entry.upper) return entry.item;
                return entries.Length > 0 ? entries[entries.Length - 1].item : null;
            }
        }

        private T Choose<T>(IEnumerable<T> source, Func<T, float> weight) where T : class => new Choices<T>(source, weight).Pick(random);

        public void Award(BackgroundRunEstimator.Step step, double elapsed, double runSeconds, bool firstRun)
        {
            var tracker = GameplayStatTracker.Instance;
            var combat = CombatProfile(step.Distance, runSeconds, firstRun);
            // Content eligibility is sampled per projected step (at most two seconds),
            // while each completion still rolls and awards its own loot and experience.
            var tasks = new Dictionary<ProceduralTaskGenerator.WeightedTaskCategory, Choices<TaskData>>();
            if (step.Tasks > 0)
                foreach (var category in categories)
                    if (CategoryWeight(category, (float)step.Distance) > 0)
                        tasks[category] = new Choices<TaskData>(category.tasks, task => TaskWeight(task, (float)step.Distance));
            var taskCategories = new Choices<ProceduralTaskGenerator.WeightedTaskCategory>(tasks.Keys,
                category => category.GetWeight((float)step.Distance));
            var enemies = step.Kills > 0 ? new Choices<TimelessEchoes.Enemies.EnemyData>(map.taskGeneratorSettings.enemies, enemy =>
            {
                var weight = EnemyWeight(enemy, (float)step.Distance);
                return weight > 0 && Encounter(enemy, step.Distance, tracker.CurrentRunKills, combat).defeated ? weight : 0;
            }) : null;
            for (var i = 0; i < step.Tasks; i++)
            {
                var category = taskCategories.Pick(random);
                if (category == null) continue;
                var task = tasks[category].Pick(random);
                if (!task) continue;
                var xp = Experience(task.associatedSkill, task.xpForCompletion, echoTaskShare, elapsed, step.Distance, true);
                Drops(task.resourceDrops, task.additionalLootChances, task.associatedSkill, elapsed, step.Distance);
                foreach (var bonus in DropResolver.RollBonusDrops(task.bonusDrops, () => (float)random.NextDouble()))
                    ResourceManager.Instance.Add(bonus.resource, bonus.count, eligibleForTierRoll: false);
                tracker.RegisterTaskComplete(task, task.taskDuration, xp);
            }
            for (var i = 0; i < step.Kills; i++)
            {
                var enemy = enemies.Pick(random);
                if (!enemy) continue;
                Experience(skills ? skills.CombatSkill : null, enemy.experience, echoKillShare, elapsed, step.Distance, false);
                Drops(enemy.resourceDrops, enemy.additionalLootChances, skills ? skills.CombatSkill : null, elapsed, step.Distance);
                EnemyKillTracker.Instance?.RegisterKill(enemy); tracker.AddKill(enemy);
            }
        }

        private float Experience(Skill skill, float amount, double echoShare, double elapsed, double distance, bool task)
        {
            if (!skills || !skill) return 0;
            if (task) amount *= 1 + Mathf.Max(0, skills.Aggregator.GetProcChance(skill, MilestoneProcType.DoubleXP));
            amount *= (float)(1 - .75 * echoShare);
            amount *= 1 + skills.Aggregator.GetExperienceBonus(skill);
            amount *= 1 + BuffValue(BuffEffectType.ExperienceBonusFraction, elapsed, distance);
            // Already accounted for foreground multipliers; do not multiply a second time.
            return skills.GrantQuestExperience(skill, amount);
        }

        private void Drops(IEnumerable<ResourceDrop> drops, IList<float> extra, Skill skill, double elapsed, double distance)
        {
            var stack = skills ? 1 + Mathf.Max(0, skills.Aggregator.GetProcChance(skill, MilestoneProcType.DoubleResources)) : 1;
            var gain = skills ? skills.GetResourceGainMultiplier() * skills.GetResourceBonusMultiplier(skill) : 1;
            var buff = 1 + BuffValue(BuffEffectType.ResourceMultiplier, elapsed, distance) / 100;
            var shop = permanentShop || elapsed < shopSeconds ? ShopRules.GatheringMultiplier : 1;
            foreach (var drop in DropResolver.RollDrops(drops, extra, skill, false, () => (float)random.NextDouble()))
            {
                var raw = drop.count * stack;
                var amount = (raw * gain * buff + CauldronResourceYield.Supplement(Oracle.oracle?.saveData, drop.resource, raw)) * shop;
                ResourceManager.Instance.Add(drop.resource, amount);
            }
        }

        public double EstimateResourcesPerWork(bool combat, double x, IDictionary<Resource, double> expectedBonusAmounts = null)
        {
            // Read-only sampling uses its own RNG; never consumes Unity's global sequence.
            expectedBonusAmounts?.Clear();
            var sampleRandom = new System.Random(175);
            double total = 0;
            for (var i = 0; i < 128; i++)
            {
                Skill skill; IEnumerable<ResourceDrop> drops; IList<float> extra;
                IEnumerable<TaskData.BonusDrop> bonuses = null;
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
                    skill = task.associatedSkill; drops = task.resourceDrops; extra = task.additionalLootChances; bonuses = task.bonusDrops;
                }
                var stack = skills ? 1 + Mathf.Max(0, skills.Aggregator.GetProcChance(skill, MilestoneProcType.DoubleResources)) : 1;
                var gain = skills ? skills.GetResourceGainMultiplier() * skills.GetResourceBonusMultiplier(skill) : 1;
                foreach (var drop in DropResolver.RollDrops(drops, extra, skill, false, () => (float)sampleRandom.NextDouble()))
                {
                    var tier = !drop.resource.DisableAlterEcho && ResourceManager.Instance ? 1 + ResourceManager.Instance.GetTierBonusPercent(ResourceManager.Instance.GetTier(drop.resource)) / 100d : 1;
                    var buff = 1 + BuffValue(BuffEffectType.ResourceMultiplier, 0, x) / 100;
                    var shop = permanentShop || shopSeconds > 0 ? ShopRules.GatheringMultiplier : 1;
                    var raw = drop.count * stack;
                    total += (raw * gain * buff + CauldronResourceYield.Supplement(Oracle.oracle?.saveData, drop.resource, raw)) * shop * tier;
                }
                if (bonuses != null)
                    foreach (var bonus in bonuses)
                    {
                        var amount = DropResolver.ExpectedBonusAmount(bonus);
                        total += amount;
                        if (expectedBonusAmounts == null || amount <= 0 || !bonus.resource) continue;
                        expectedBonusAmounts.TryGetValue(bonus.resource, out var previous);
                        expectedBonusAmounts[bonus.resource] = previous + amount / 128;
                    }
            }
            return total / 128;
        }
    }
}
