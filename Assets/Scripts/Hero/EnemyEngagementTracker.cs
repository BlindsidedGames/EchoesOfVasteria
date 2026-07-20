using System;
using System.Collections.Generic;
using TimelessEchoes.Enemies;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Tracks enemies that have engaged with this hero or echo.
    /// Manages death/disengage subscriptions and provides query methods.
    /// HeroBase handles the filtering logic; this component purely tracks.
    /// </summary>
    public class EnemyEngagementTracker : MonoBehaviour
    {
        private readonly HashSet<Enemy> engagedEnemies = new();
        private readonly Dictionary<Enemy, Action> deathHandlers = new();
        private readonly Dictionary<Enemy, Action<Enemy>> disengageHandlers = new();
        private readonly List<Enemy> removalBuffer = new(16);
        private readonly Dictionary<Enemy, Transform> enemyTargets = new();

        /// <summary>
        /// Number of currently tracked enemies.
        /// </summary>
        public int Count => engagedEnemies.Count;

        /// <summary>
        /// True if at least one enemy is being tracked.
        /// </summary>
        public bool HasEngagedEnemies => engagedEnemies.Count > 0;

        /// <summary>
        /// Read-only access to the set of engaged enemies.
        /// </summary>
        public IReadOnlyCollection<Enemy> EngagedEnemies => engagedEnemies;

        /// <summary>
        /// Register an enemy for tracking. Subscribes to death and disengage events.
        /// If the enemy is already tracked, updates the recorded target.
        /// </summary>
        /// <param name="enemy">The enemy to track.</param>
        /// <param name="target">The transform the enemy was targeting when engaged.</param>
        /// <returns>True if newly registered, false if already tracked (target updated).</returns>
        // The tracker intentionally keeps engagement state while disabled. OnDestroy
        // calls Clear(), but UDR0004 only recognizes an OnDisable cleanup pattern.
#pragma warning disable UDR0004
        public bool RegisterEnemy(Enemy enemy, Transform target)
        {
            if (enemy == null)
                return false;

            var hp = enemy.Health;
            if (hp == null)
                return false;

            if (engagedEnemies.Contains(enemy))
            {
                // Already tracking - just update target
                enemyTargets[enemy] = target;
                return false;
            }

            engagedEnemies.Add(enemy);
            enemyTargets[enemy] = target;

            // Subscribe to death event
            Action deathHandler = () => UnregisterEnemy(enemy);
            hp.OnDeath += deathHandler;
            deathHandlers[enemy] = deathHandler;

            // Subscribe to disengage (reusing OnEngage event)
            Action<Enemy> disengageHandler = null;
            disengageHandler = e =>
            {
                if (e == enemy && !e.IsEngaged)
                    UnregisterEnemy(enemy);
            };
            Enemy.OnEngage += disengageHandler;
            disengageHandlers[enemy] = disengageHandler;

            return true;
        }
#pragma warning restore UDR0004

        /// <summary>
        /// Unregister an enemy, removing it from tracking and unsubscribing from events.
        /// </summary>
        /// <param name="enemy">The enemy to unregister.</param>
        /// <returns>True if the enemy was being tracked, false otherwise.</returns>
        public bool UnregisterEnemy(Enemy enemy)
        {
            if (enemy == null)
                return false;

            if (!engagedEnemies.Remove(enemy))
                return false;

            enemyTargets.Remove(enemy);

            if (deathHandlers.TryGetValue(enemy, out var deathHandler))
            {
                var hp = enemy.Health;
                if (hp != null)
                    hp.OnDeath -= deathHandler;
                deathHandlers.Remove(enemy);
            }

            if (disengageHandlers.TryGetValue(enemy, out var disengageHandler))
            {
                Enemy.OnEngage -= disengageHandler;
                disengageHandlers.Remove(enemy);
            }

            return true;
        }

        /// <summary>
        /// Check if an enemy is currently being tracked.
        /// </summary>
        public bool IsEngaged(Enemy enemy)
        {
            return enemy != null && engagedEnemies.Contains(enemy);
        }

        /// <summary>
        /// Get the target transform that was recorded when the enemy engaged.
        /// </summary>
        public bool TryGetTarget(Enemy enemy, out Transform target)
        {
            if (enemy != null && enemyTargets.TryGetValue(enemy, out target))
                return true;
            target = null;
            return false;
        }

        /// <summary>
        /// Update the recorded target for an enemy.
        /// </summary>
        public void UpdateTarget(Enemy enemy, Transform target)
        {
            if (enemy != null && engagedEnemies.Contains(enemy))
                enemyTargets[enemy] = target;
        }

        /// <summary>
        /// Remove enemies that are null, dead, or no longer engaged.
        /// Call this periodically (e.g., in UpdateBehavior) to keep tracking clean.
        /// </summary>
        public void CleanupStaleEnemies()
        {
            removalBuffer.Clear();

            foreach (var enemy in engagedEnemies)
            {
                var hp = enemy != null ? enemy.Health : null;
                if (enemy == null || hp == null || hp.CurrentHealth <= 0f || !enemy.IsEngaged)
                    removalBuffer.Add(enemy);
            }

            foreach (var enemy in removalBuffer)
                UnregisterEnemy(enemy);
        }

        /// <summary>
        /// Find the nearest engaged enemy matching an optional filter.
        /// </summary>
        /// <param name="position">Position to measure distance from.</param>
        /// <param name="filter">Optional filter predicate. If null, all engaged enemies are considered.</param>
        /// <returns>The nearest enemy matching the filter, or null if none found.</returns>
        public Enemy GetNearestEngaged(Vector2 position, Func<Enemy, Transform, bool> filter = null)
        {
            Enemy chosen = null;
            var bestSqr = float.PositiveInfinity;

            foreach (var enemy in engagedEnemies)
            {
                if (enemy == null)
                    continue;
                if (!enemy.TryGetTransformSafe(out var enemyTransform))
                    continue;

                // Apply filter if provided
                if (filter != null)
                {
                    enemyTargets.TryGetValue(enemy, out var target);
                    if (!filter(enemy, target))
                        continue;
                }

                var offset = (Vector2)enemyTransform.position - position;
                var sqr = offset.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    chosen = enemy;
                }
            }

            return chosen;
        }

        /// <summary>
        /// Clear all tracked enemies and unsubscribe from all events.
        /// Call this in OnDisable to prevent leaks.
        /// </summary>
        public void Clear()
        {
            foreach (var enemy in engagedEnemies)
            {
                if (deathHandlers.TryGetValue(enemy, out var deathHandler))
                {
                    var hp = enemy != null ? enemy.Health : null;
                    if (hp != null)
                        hp.OnDeath -= deathHandler;
                }

                if (disengageHandlers.TryGetValue(enemy, out var disengageHandler))
                    Enemy.OnEngage -= disengageHandler;
            }

            engagedEnemies.Clear();
            deathHandlers.Clear();
            disengageHandlers.Clear();
            enemyTargets.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
