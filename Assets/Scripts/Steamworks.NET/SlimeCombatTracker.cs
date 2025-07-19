#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Enemies;

namespace TimelessEchoes
{
    /// <summary>
    /// Tracks slimes currently engaged with the hero. When at least five
    /// are active, awards the SlimeSwarm achievement.
    /// </summary>
    public class SlimeCombatTracker : MonoBehaviour
    {
#if !DISABLESTEAMWORKS
        private readonly HashSet<Enemy> engagedSlimes = new();
        private readonly Dictionary<Enemy, System.Action> deathHandlers = new();
        private readonly List<Enemy> removalBuffer = new();

        /// <summary>
        /// Current number of slimes engaging the hero.
        /// </summary>
        public int EngagedSlimeCount => engagedSlimes.Count;

        private void OnEnable()
        {
            Enemy.OnEngage += OnEnemyEngage;
        }

        private void OnDisable()
        {
            Enemy.OnEngage -= OnEnemyEngage;
            foreach (var pair in deathHandlers)
            {
                var hp = pair.Key != null ? pair.Key.GetComponent<Health>() : null;
                if (hp != null)
                    hp.OnDeath -= pair.Value;
            }
            engagedSlimes.Clear();
            deathHandlers.Clear();
        }

        private void Update()
        {
            removalBuffer.Clear();
            foreach (var slime in engagedSlimes)
            {
                if (slime == null || !slime.IsEngaged)
                    removalBuffer.Add(slime);
            }
            foreach (var slime in removalBuffer)
            {
                UnregisterSlime(slime);
            }
        }

        private void OnEnemyEngage(Enemy enemy)
        {
            if (enemy == null || enemy.Stats == null || !enemy.IsEngaged)
                return;

            var name = enemy.Stats.enemyName;
            if (string.IsNullOrEmpty(name) || !name.ToLowerInvariant().Contains("slime"))
                return;
            if (engagedSlimes.Contains(enemy))
                return;

            engagedSlimes.Add(enemy);
            var hp = enemy.GetComponent<Health>();
            if (hp != null)
            {
                System.Action handler = () => UnregisterSlime(enemy);
                deathHandlers[enemy] = handler;
                hp.OnDeath += handler;
            }

            if (engagedSlimes.Count >= 5)
                AchievementManager.Instance?.UnlockSlimeSwarm();
        }

        private void UnregisterSlime(Enemy enemy)
        {
            if (enemy == null)
                return;
            if (engagedSlimes.Remove(enemy) && deathHandlers.TryGetValue(enemy, out var handler))
            {
                var hp = enemy.GetComponent<Health>();
                if (hp != null)
                    hp.OnDeath -= handler;
                deathHandlers.Remove(enemy);
            }
        }
#else
        public int EngagedSlimeCount => 0;
#endif
    }
}
