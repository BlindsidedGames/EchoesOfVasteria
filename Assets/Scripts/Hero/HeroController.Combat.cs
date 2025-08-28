#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System;
using System.Collections;
using Blindsided.Utilities.Pooling;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using UnityEngine;
using static TimelessEchoes.TELogger;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Hero
{
    public partial class HeroController
    {
        public static event Action OnMainHeroDiceChanged;
        private Transform FindNearestEnemy(float range)
        {
            Transform nearest = null;
            var best = float.MaxValue;
            var enemies = EnemyActivator.ActiveEnemies;
            if (enemies == null)
                return null;
            Vector2 pos = transform.position;

            var cam = EnemyActivator.Instance != null
                ? EnemyActivator.Instance.GetComponent<Camera>()
                : null;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            var checkBounds = false;
            if (cam != null)
            {
                const float padding = 2f;
                min = cam.ViewportToWorldPoint(Vector3.zero) - Vector3.one * padding;
                max = cam.ViewportToWorldPoint(Vector3.one) + Vector3.one * padding;
                checkBounds = true;
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Transform enemyTransform = null;
                try
                {
                    enemyTransform = enemy.transform;
                }
                catch
                {
                    // Enemy may have been destroyed mid-iteration
                    continue;
                }

                if (enemyTransform == null) continue;

                if (checkBounds)
                {
                    var p = enemyTransform.position;
                    if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y)
                        continue;
                }

                var hp = enemy.GetComponent<Health>();
                if (hp == null || hp.CurrentHealth <= 0f) continue;
                var d = Vector2.Distance(pos, enemyTransform.position);
                if (d <= range && d < best)
                {
                    best = d;
                    nearest = enemyTransform;
                }
            }

            return nearest;
        }

        private Transform FindNearestEnemy()
        {
            return FindNearestEnemy(stats.visionRange);
        }

        private void HandleCombat(Transform enemy)
        {
            ai.canMove = true;

            if (state != State.Combat)
            {
                Log($"Hero entering combat with {enemy.name}", TELogCategory.Combat, this);
                if (diceUnlocked && diceRoller != null && !isRolling)
                {
                    var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
                    var cooldown = rate > 0f ? 1f / rate : 0.5f;
                    StartCoroutine(RollForCombat(cooldown));
                }
            }

            state = State.Combat;
            setter.target = enemy;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f) return;

            var dist = Vector2.Distance(transform.position, enemy.position);
            if (dist <= stats.visionRange)
            {
                var rate = HeroStatSystem.GetSnapshot().attacksPerSecond;
                var cooldown = rate > 0f ? 1f / rate : float.PositiveInfinity;
                if (allowAttacks && Time.time - lastAttack >= cooldown && !isRolling)
                {
                    lastMoveDir = enemy.position - transform.position;
                    Attack(enemy);
                    lastAttack = Time.time;
                }
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

            // Refresh UI only for the main hero; echoes keep their own local multiplier
            if (!IsEcho)
            {
                HeroStatSystem.MarkDirty(DirtyMask.Damage, DirtyReason.DiceUsed);
                HeroStatSystem.ForceRunStartRefresh();
                OnMainHeroDiceChanged?.Invoke();
            }
        }

        private void OnEnemyEngage(Enemy enemy)
        {
            if (enemy == null)
                return;

            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.CurrentHealth <= 0f)
            {
                UnregisterEngagedEnemy(enemy);
                return;
            }

            if (enemy.IsEngaged)
            {
                if (!engagedEnemies.Contains(enemy))
                {
                    engagedEnemies.Add(enemy);

                    Action deathHandler = () => UnregisterEngagedEnemy(enemy);
                    hp.OnDeath += deathHandler;
                    enemyDeathHandlers[enemy] = deathHandler;

                    Action<Enemy> disengageHandler = null;
                    disengageHandler = e =>
                    {
                        if (e == enemy && !e.IsEngaged)
                            UnregisterEngagedEnemy(enemy);
                    };
                    Enemy.OnEngage += disengageHandler;
                    enemyDisengageHandlers[enemy] = disengageHandler;
                }
            }
            else
            {
                UnregisterEngagedEnemy(enemy);
                return;
            }

            if (!allowAttacks)
                return;

            if (currentEnemy != null && currentEnemy != enemy.transform)
                return;

            if (currentEnemy == null)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = enemy.transform;
                currentEnemyHealth = hp;
                currentEnemyHealth.SetHealthBarVisible(true);
            }

            if (state == State.PerformingTask && CurrentTask != null)
                CurrentTask.OnInterrupt(this);

            HandleCombat(enemy.transform);
        }

        private void UnregisterEngagedEnemy(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (engagedEnemies.Remove(enemy))
            {
                if (enemyDeathHandlers.TryGetValue(enemy, out var death))
                {
                    var hp = enemy.GetComponent<Health>();
                    if (hp != null)
                        hp.OnDeath -= death;
                    enemyDeathHandlers.Remove(enemy);
                }

                if (enemyDisengageHandlers.TryGetValue(enemy, out var disengage))
                {
                    Enemy.OnEngage -= disengage;
                    enemyDisengageHandlers.Remove(enemy);
                }
            }

            Transform enemyTransformSafe = null;
            try
            {
                enemyTransformSafe = enemy.transform;
            }
            catch
            {
                enemyTransformSafe = null;
            }

            if (enemyTransformSafe != null && currentEnemy == enemyTransformSafe)
            {
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemy = null;
                currentEnemyHealth = null;
            }
        }

        private void Attack(Transform target)
        {
            if (stats.projectilePrefab == null || target == null) return;

            var enemy = target.GetComponent<Health>();
            if (enemy == null || enemy.CurrentHealth <= 0f) return;

            animator.Play("Attack");
            if (AutoBuffAnimator != null && AutoBuffAnimator.isActiveAndEnabled)
                AutoBuffAnimator.Play("Attack");

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
                var enemyStats = target.GetComponent<Enemy>()?.Stats;
                var bonus = killTracker != null ? killTracker.GetDamageMultiplier(enemyStats) : 1f;
                var snap = HeroStatSystem.GetSnapshot();
                var dmgBase = snap.damage * combatDamageMultiplier;
                var total = dmgBase * bonus;

                // Crit chance (2x damage) from centralized snapshot
                var critChance = Mathf.Clamp01(snap.critChancePercent / 100f);

                var isCritical = false;
                if (critChance > 0f && Random.value < Mathf.Clamp01(critChance))
                {
                    total *= 2f;
                    isCritical = true;
                    var tracker = GameplayStatTracker.Instance ??
                                  FindFirstObjectByType<GameplayStatTracker>();
                    tracker?.AddCriticalHit();
                }

                var bonusDamage = total - dmgBase;
                proj.Init(target, total, true, null, combatSkill, bonusDamage, isCritical);
            }
        }
    }
}