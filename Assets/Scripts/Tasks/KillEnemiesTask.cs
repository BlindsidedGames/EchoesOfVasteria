using System.Linq;
using UnityEngine;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task requiring a number of enemies to be defeated.
    /// </summary>
    public class KillEnemiesTask : BaseTask
    {
        [SerializeField] private int requiredKills = 3;
        private int currentKills;
        private Health[] tracked;

        public override Transform Target => transform;

        public override void StartTask()
        {
            currentKills = 0;
            tracked = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            foreach (var h in tracked)
                h.OnDeath += OnEnemyDeath;
        }

        private void OnDestroy()
        {
            if (tracked != null)
            {
                foreach (var h in tracked)
                    h.OnDeath -= OnEnemyDeath;
            }
        }

        private void OnEnemyDeath()
        {
            currentKills++;
        }

        public override bool IsComplete()
        {
            return currentKills >= requiredKills;
        }
    }
}
