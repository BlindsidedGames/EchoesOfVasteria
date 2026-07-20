using System;
using System.Collections;
using Blindsided.Utilities.Pooling;
using TimelessEchoes.Enemies;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.Utilities;
using UnityEngine;
using static TimelessEchoes.TELogger;
using static TimelessEchoes.Quests.QuestUtils;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Handles combat logic for the hero/echo: target selection, DPS estimation,
    /// attack execution, and dice rolling. Works in conjunction with HeroBase
    /// which manages the overall state machine and enemy engagement events.
    /// </summary>
    [RequireComponent(typeof(EnemyEngagementTracker))]
    public class HeroCombatController : MonoBehaviour
    {
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private string diceQuestID = "Protect the Town";

        private Transform currentEnemy;
        private EnemyHealth currentEnemyHealth;
        private Enemy currentEnemyComp;
        private float lastAttack = float.NegativeInfinity;
        private bool isRolling;
        private float combatDamageMultiplier = 1f;
        private bool diceUnlocked;

        // Dependencies set via Init
        private HeroBase hero;
        private EnemyEngagementTracker tracker;
        private Pathfinding.AIPath ai;
        private Pathfinding.AIDestinationSetter setter;

        // Cached references from HeroBase
        private HeroStats stats;
        private Animator animator;
        private Transform projectileOrigin;
        private Skill combatSkill;

        /// <summary>
        /// True if currently in combat state.
        /// </summary>
        public bool IsInCombat { get; private set; }

        /// <summary>
        /// Current combat damage multiplier from dice roll.
        /// </summary>
        public float CombatDamageMultiplier => combatDamageMultiplier;

        /// <summary>
        /// The current enemy being targeted.
        /// </summary>
        public Transform CurrentTarget => currentEnemy;

        /// <summary>
        /// True if dice roll animation is in progress.
        /// </summary>
        public bool IsRolling => isRolling;

        /// <summary>
        /// Initialize the combat controller with dependencies.
        /// </summary>
        public void Init(HeroBase heroBase, HeroStats heroStats, Animator anim,
            Transform projOrigin, Skill skill, Pathfinding.AIPath aiPath,
            Pathfinding.AIDestinationSetter destSetter, DiceRoller roller = null,
            string diceQuest = null)
        {
            hero = heroBase;
            stats = heroStats;
            animator = anim;
            projectileOrigin = projOrigin;
            combatSkill = skill;
            ai = aiPath;
            setter = destSetter;
            tracker = GetComponent<EnemyEngagementTracker>();

            if (roller != null)
                diceRoller = roller;
            if (!string.IsNullOrEmpty(diceQuest))
                diceQuestID = diceQuest;

            diceUnlocked = QuestCompleted(diceQuestID);
            if (diceRoller != null)
                diceRoller.gameObject.SetActive(diceUnlocked);
        }

        /// <summary>
        /// Reset combat state on enable.
        /// </summary>
        public void ResetState()
        {
            diceUnlocked = QuestCompleted(diceQuestID);
            if (diceRoller != null)
                diceRoller.gameObject.SetActive(diceUnlocked);

            var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
            lastAttack = Time.time - 1f / rate;
            isRolling = false;
            combatDamageMultiplier = 1f;
            IsInCombat = false;
        }

        /// <summary>
        /// Update combat state. Called each frame from HeroBase.UpdateBehavior.
        /// Returns true if still in combat, false if combat should exit.
        /// </summary>
        public bool UpdateCombat()
        {
            if (currentEnemy != null)
            {
                var hp = currentEnemyHealth;
                var enemyComp = currentEnemyComp;
                if (enemyComp == null)
                {
                    enemyComp = currentEnemy.GetComponent<Enemy>();
                    currentEnemyComp = enemyComp;
                }
                if (hp == null)
                {
                    hp = enemyComp != null ? enemyComp.Health : null;
                    currentEnemyHealth = hp;
                }

                var enemyGo = currentEnemy != null ? currentEnemy.gameObject : null;
                var isInactive = enemyGo == null || !enemyGo.activeInHierarchy ||
                                 (enemyComp != null && !enemyComp.isActiveAndEnabled);
                var pooledMarker = enemyComp != null ? enemyComp.PooledMarker : null;
                var isPooled = pooledMarker != null && pooledMarker.inPool;

                var enemyNotEngaged = !hero.IsEcho && !(tracker?.IsEngaged(enemyComp) ?? false);
                if (hp == null || hp.CurrentHealth <= 0f || enemyComp == null ||
                    enemyNotEngaged || isInactive || isPooled)
                {
                    ClearCurrentEnemy();
                }
                else if (hero.IsEcho && hero.AllowAttacks)
                {
                    // Echo overkill prevention: if we'd kill in one hit and others are already targeting
                    var otherDps = EstimateCombinedDps(currentEnemy);
                    if (otherDps > 0f)
                    {
                        float perHit = EstimatePerHitAgainst(enemyComp, hero);
                        if (perHit > 0f && hp.CurrentHealth <= perHit)
                            ClearCurrentEnemy();
                    }
                }
            }

            // Check if we should exit combat
            if (IsInCombat && !(tracker?.HasEngagedEnemies ?? false))
                return false;

            return IsInCombat;
        }

        /// <summary>
        /// Exit combat state and reset combat-related state.
        /// </summary>
        public void ExitCombat()
        {
            Log("Hero exiting combat", TELogCategory.Combat, this);
            combatDamageMultiplier = 1f;
            isRolling = false;
            diceRoller?.ResetRoll();
            ClearCurrentEnemy();
            IsInCombat = false;
        }

        /// <summary>
        /// Clear the current enemy target and hide its health bar.
        /// </summary>
        public void ClearCurrentEnemy()
        {
            currentEnemyHealth?.SetHealthBarVisible(false);
            currentEnemy = null;
            currentEnemyHealth = null;
            currentEnemyComp = null;
        }

        /// <summary>
        /// Clear the current enemy if it matches the given enemy.
        /// Called when an enemy is unregistered.
        /// </summary>
        public void ClearCurrentEnemyIfMatch(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (!enemy.TryGetTransformSafe(out var enemyTransformSafe))
                enemyTransformSafe = null;

            if (enemyTransformSafe != null && currentEnemy == enemyTransformSafe)
                ClearCurrentEnemy();
        }

        /// <summary>
        /// Resolve the nearest valid enemy target.
        /// </summary>
        /// <param name="isPerformingTask">Whether the hero is currently performing a task.</param>
        /// <param name="assistEchoWhileOnTask">Whether to assist echoes while on task.</param>
        /// <param name="assistEchoThreatRadius">Radius for echo assist.</param>
        /// <returns>The nearest enemy transform, or null.</returns>
        public Transform ResolveNearestTarget(bool isPerformingTask, bool assistEchoWhileOnTask,
            float assistEchoThreatRadius)
        {
            if (!hero.AllowAttacks || stats == null)
                return currentEnemy;

            if (currentEnemy != null)
                return currentEnemy;

            if (tracker != null && tracker.HasEngagedEnemies)
            {
                var heroPos = (Vector2)transform.position;
                var heroTransform = hero.transform;
                var chosen = tracker.GetNearestEngaged(heroPos, (enemy, target) =>
                {
                    // If targeting main hero, always assist
                    if (target == heroTransform)
                        return true;

                    // If on task and assist-while-on-task is disabled, skip
                    if (isPerformingTask && !assistEchoWhileOnTask)
                        return false;

                    // Check distance to enemy
                    if (!enemy.TryGetTransformSafe(out var et))
                        return false;
                    var distance = Vector2.Distance(heroPos, et.position);
                    return distance <= assistEchoThreatRadius;
                });

                if (chosen != null && chosen.TryGetTransformSafe(out var chosenTransform))
                    return chosenTransform;

                return null;
            }

            var range = hero.UnlimitedAggroRange ? hero.CombatAggroRange : stats.visionRange;
            return hero.IsEcho
                ? FindNearestEnemyTimeAware(range, hero.EchoAvoidIfTTKBelowSeconds)
                : FindNearestEnemy(range);
        }

        /// <summary>
        /// Attempt to engage the specified enemy.
        /// </summary>
        /// <param name="enemy">Enemy transform to engage.</param>
        /// <param name="onInterruptTask">Action to call if combat interrupts a task.</param>
        /// <returns>True if engagement succeeded.</returns>
        public bool TryEngageEnemy(Transform enemy, Action onInterruptTask = null)
        {
            if (!hero.AllowAttacks || stats == null || enemy == null)
                return false;

            if (setter == null || ai == null)
                return false;

            if (currentEnemy != enemy)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = enemy;
                currentEnemyComp = enemy.GetComponent<Enemy>();
                currentEnemyHealth = currentEnemyComp != null ? currentEnemyComp.Health : null;
            }
            else if (currentEnemyHealth == null)
            {
                if (currentEnemyComp == null)
                    currentEnemyComp = enemy.GetComponent<Enemy>();
                currentEnemyHealth = currentEnemyComp != null ? currentEnemyComp.Health : null;
            }

            setter.target = enemy;
            ai.simulateMovement = true;

            var enemyComp = currentEnemyComp ?? enemy.GetComponent<Enemy>();
            var enemyEngaged = enemyComp != null && (tracker?.IsEngaged(enemyComp) ?? false);
            var range = hero.UnlimitedAggroRange ? hero.CombatAggroRange : stats.visionRange;
            var delta = (Vector2)(enemy.position - transform.position);
            var withinRange = delta.sqrMagnitude <= range * range;

            if (!enemyEngaged && !withinRange)
                return true;

            if (currentEnemyHealth != null)
                currentEnemyHealth.SetHealthBarVisible(true);

            onInterruptTask?.Invoke();

            HandleCombat(enemy);
            return true;
        }

        /// <summary>
        /// Handle combat with the specified enemy.
        /// </summary>
        /// <param name="enemy">Enemy transform to fight.</param>
        /// <param name="wasPerformingTask">Whether we were performing a task before combat.</param>
        /// <param name="onReleaseTask">Action to release the current task.</param>
        public void HandleCombat(Transform enemy, bool wasPerformingTask = false, Action onReleaseTask = null)
        {
            ai.simulateMovement = true;

            if (!IsInCombat)
            {
                Log($"Hero entering combat with {enemy.name}", TELogCategory.Combat, this);
                if (diceUnlocked && diceRoller != null && !isRolling)
                {
                    var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
                    var cooldown = rate > 0f ? 1f / rate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
            }

            // Main hero releases task when entering combat
            if (!hero.IsEcho)
                onReleaseTask?.Invoke();

            IsInCombat = true;
            setter.target = enemy;

            var enemyComp = enemy.GetComponent<Enemy>();
            var hp = enemyComp != null ? enemyComp.Health : null;
            if (hp == null || hp.CurrentHealth <= 0f) return;

            var dist = Vector2.Distance(transform.position, enemy.position);
            if (dist <= stats.visionRange)
            {
                var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
                var cooldown = rate > 0f ? 1f / rate : float.PositiveInfinity;
                if (hero.AllowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                {
                    Attack(enemy);
                    lastAttack = Time.time;
                }
            }
        }

        // Cached camera reference for bounds checking (Phase 4 optimization)
        private Camera cachedCamera;
        private float cameraRefreshTime;
        private const float CameraRefreshInterval = 1f;

        /// <summary>
        /// Get camera bounds for visibility filtering. Returns false if no camera available.
        /// </summary>
        private bool TryGetCameraBounds(out Vector3 min, out Vector3 max)
        {
            min = max = Vector3.zero;

            // Refresh cached camera reference periodically
            if (cachedCamera == null || Time.time >= cameraRefreshTime)
            {
                cachedCamera = EnemyActivator.Instance != null
                    ? EnemyActivator.Instance.GetComponent<Camera>()
                    : null;
                cameraRefreshTime = Time.time + CameraRefreshInterval;
            }

            if (cachedCamera == null) return false;

            const float padding = 2f;
            min = cachedCamera.ViewportToWorldPoint(Vector3.zero) - Vector3.one * padding;
            max = cachedCamera.ViewportToWorldPoint(Vector3.one) + Vector3.one * padding;
            return true;
        }

        /// <summary>
        /// Check if a position is within camera bounds.
        /// </summary>
        private static bool IsInBounds(Vector3 p, Vector3 min, Vector3 max)
        {
            return p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
        }

        /// <summary>
        /// Find the nearest living enemy within camera bounds.
        /// </summary>
        public Transform FindNearestEnemy(float range)
        {
            Transform nearest = null;
            var bestSqr = float.MaxValue;
            var rangeSqr = range * range;
            var enemies = EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;

            Vector2 pos = transform.position;
            var checkBounds = TryGetCameraBounds(out var min, out var max);

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!enemy.TryGetTransformSafe(out var enemyTransform)) continue;

                if (checkBounds && !IsInBounds(enemyTransform.position, min, max))
                    continue;

                var hp = enemy.Health;
                if (hp == null || hp.CurrentHealth <= 0f) continue;

                var offset = (Vector2)enemyTransform.position - pos;
                var sqrDist = offset.sqrMagnitude;
                if (sqrDist <= rangeSqr && sqrDist < bestSqr)
                {
                    bestSqr = sqrDist;
                    nearest = enemyTransform;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Find the nearest enemy using vision range from stats.
        /// </summary>
        public Transform FindNearestEnemy()
        {
            return FindNearestEnemy(stats.visionRange);
        }

        /// <summary>
        /// Find the nearest enemy, avoiding enemies that would die too quickly (for echoes).
        /// </summary>
        public Transform FindNearestEnemyTimeAware(float range, float thresholdSec)
        {
            if (thresholdSec <= 0f)
                return FindNearestEnemy(range);

            Transform nearest = null;
            var bestSqr = float.MaxValue;
            var rangeSqr = range * range;
            var enemies = EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;

            Vector2 pos = transform.position;
            var checkBounds = TryGetCameraBounds(out var min, out var max);

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!enemy.TryGetTransformSafe(out var enemyTransform)) continue;

                if (checkBounds && !IsInBounds(enemyTransform.position, min, max))
                    continue;

                var hp = enemy.Health;
                if (hp == null || hp.CurrentHealth <= 0f) continue;

                var offset = (Vector2)enemyTransform.position - pos;
                var sqrDist = offset.sqrMagnitude;
                if (sqrDist > rangeSqr) continue;

                var combinedDps = EstimateCombinedDps(enemyTransform);
                if (combinedDps > 0f)
                {
                    var maxHp = Mathf.Max(0.0001f, hp.MaxHealth);
                    var ttk = maxHp / combinedDps;
                    if (ttk <= thresholdSec)
                        continue;
                }

                if (sqrDist < bestSqr)
                {
                    bestSqr = sqrDist;
                    nearest = enemyTransform;
                }
            }

            if (nearest == null)
                return FindNearestEnemy(range);
            return nearest;
        }

        /// <summary>
        /// Find a fallback enemy target (any living enemy, no range limit).
        /// </summary>
        public Transform FindFallbackEnemyTarget()
        {
            var enemies = EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;

            var origin = (Vector2)transform.position;
            Transform nearest = null;
            var bestSqr = float.PositiveInfinity;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!enemy.TryGetTransformSafe(out var enemyTransform)) continue;

                var hp = enemy.Health;
                if (hp == null || hp.CurrentHealth <= 0f) continue;

                var offset = (Vector2)enemyTransform.position - origin;
                var sqr = offset.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = enemyTransform;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Estimate DPS that the specified attacker can deal to the enemy.
        /// </summary>
        public float EstimateDpsAgainst(Enemy enemy, HeroBase attacker)
        {
            if (enemy == null || attacker == null)
                return 0f;
            if (!attacker.AllowAttacks)
                return 0f;

            if (!enemy.TryGetTransformSafe(out _)) return 0f;

            var snap = HeroStatSystem.GetSnapshot();
            var killTracker = EnemyKillTracker.Instance;
            var enemyStats = enemy.Stats;
            float killMult = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
            float critChance = Mathf.Clamp01(snap.critChancePercent / 100f);
            float critDamageMultiplier = 1f + Mathf.Max(0f, snap.critDamagePercent) / 100f;
            float expectedCritFactor = 1f + critChance * (critDamageMultiplier - 1f);

            // Get the attacker's combat controller for its damage multiplier
            var attackerCombat = attacker.CombatController;
            var attackerMultiplier = attackerCombat != null ? attackerCombat.CombatDamageMultiplier : 1f;

            float perHitBeforeDefense = snap.damage * attackerMultiplier * killMult * expectedCritFactor;
            float defense = enemy.GetDefense();
            float perHitAfterDefense = Combat.ApplyDefense(perHitBeforeDefense, defense);
            float attacksPerSecond = Mathf.Max(0f, snap.attacksPerSecond);
            return perHitAfterDefense * attacksPerSecond;
        }

        /// <summary>
        /// Estimate per-hit damage that the specified attacker can deal to the enemy.
        /// </summary>
        public float EstimatePerHitAgainst(Enemy enemy, HeroBase attacker)
        {
            if (enemy == null || attacker == null) return 0f;
            if (!attacker.AllowAttacks) return 0f;

            var snap = HeroStatSystem.GetSnapshot();
            var killTracker = EnemyKillTracker.Instance;
            var enemyStats = enemy.Stats;
            float killMult = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;

            var attackerCombat = attacker.CombatController;
            var attackerMultiplier = attackerCombat != null ? attackerCombat.CombatDamageMultiplier : 1f;

            float perHitBeforeDefense = snap.damage * attackerMultiplier * killMult;
            float defense = enemy.GetDefense();
            return Combat.ApplyDefense(perHitBeforeDefense, defense);
        }

        /// <summary>
        /// Estimate combined DPS from all heroes/echoes targeting this enemy.
        /// </summary>
        public float EstimateCombinedDps(Transform enemyTransform)
        {
            if (enemyTransform == null)
                return 0f;

            var enemy = enemyTransform.GetComponent<Enemy>();
            if (enemy == null)
                return 0f;

            float dps = 0f;

            // Main hero if targeting this enemy
            var main = HeroController.Instance;
            if (main != null && (object)main != hero)
            {
                var mainCombat = main.CombatController;
                var mainSetter = main.MovementController?.Setter;
                var t = mainCombat != null ? mainCombat.CurrentTarget : null;
                var st = mainSetter != null ? mainSetter.target : null;
                if ((t != null && t == enemyTransform) || (st != null && st == enemyTransform))
                    dps += EstimateDpsAgainst(enemy, main);
            }

            // Other combat-enabled echoes targeting this enemy
            foreach (var echo in EchoController.CombatEchoes)
            {
                if (echo == null || !echo.isActiveAndEnabled) continue;
                var hc = echo as HeroBase;
                if (hc == null || (object)hc == hero) continue;

                var echoCombat = hc.CombatController;
                var echoSetter = hc.MovementController?.Setter;
                var t = echoCombat != null ? echoCombat.CurrentTarget : null;
                var st = echoSetter != null ? echoSetter.target : null;
                if ((t != null && t == enemyTransform) || (st != null && st == enemyTransform))
                    dps += EstimateDpsAgainst(enemy, hc);
            }

            return dps;
        }

        private void Attack(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;

            var go = target.gameObject;
            if (go == null || !go.activeInHierarchy)
                return;

            var enemyComp = target.GetComponent<Enemy>();
            var pooled = enemyComp != null ? enemyComp.PooledMarker : go.GetComponent<PooledObject>();
            if (pooled != null && pooled.inPool)
                return;

            var enemyHealth = enemyComp != null ? enemyComp.Health : null;
            if (enemyHealth == null || enemyHealth.CurrentHealth <= 0f) return;

            animator.Play("Attack");
            hero.OnAttackStarted();

            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = PoolManager.Get(stats.projectilePrefab);
            projObj.transform.position = origin.position;
            projObj.transform.rotation = Quaternion.identity;

            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                var killTracker = EnemyKillTracker.Instance;
                if (killTracker == null)
                    Log("EnemyKillTracker missing", TELogCategory.Combat, this);

                var enemyStats = enemyComp?.Stats;
                var bonus = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
                var snap = HeroStatSystem.GetSnapshot();
                var dmgBase = snap.damage * combatDamageMultiplier;
                var critDamageMultiplier = 1f + Mathf.Max(0f, snap.critDamagePercent) / 100f;
                var total = dmgBase * bonus;

                var critChance = Mathf.Clamp01(snap.critChancePercent / 100f);
                var isCritical = false;
                if (critChance > 0f && UnityEngine.Random.value < critChance)
                {
                    total *= critDamageMultiplier;
                    isCritical = true;
                    var gameTracker = GameplayStatTracker.Instance ??
                                      UnityEngine.Object.FindAnyObjectByType<GameplayStatTracker>();
                    gameTracker?.AddCriticalHit();
                }

                var bonusDamage = total - dmgBase;
                proj.Init(target, total, true, null, combatSkill, bonusDamage, isCritical, -1, hero.IsEcho);
            }
        }

        private IEnumerator RollForCombat(float duration)
        {
            if (!diceUnlocked || diceRoller == null)
                yield break;

            isRolling = true;
            lastAttack = Time.time;

            yield return StartCoroutine(diceRoller.Roll(duration));

            combatDamageMultiplier = 1f + 0.1f * diceRoller.Result;
            isRolling = false;

            if (!hero.IsEcho)
            {
                HeroStatSystem.MarkDirty(DirtyMask.Damage, DirtyReason.DiceUsed);
                HeroStatSystem.ForceRunStartRefresh();
                HeroBase.InvokeMainHeroDiceChanged();
            }
        }
    }
}
