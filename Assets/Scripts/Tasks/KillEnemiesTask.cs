using System.Linq;
using UnityEngine;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task requiring a number of enemies to be defeated.
    /// </summary>
    public class KillEnemiesTask : MonoBehaviour, ITask
    {
        [SerializeField] private int requiredKills = 3;
        private int currentKills;
        private Health[] tracked;

        public Transform Target => transform;

        public void StartTask()
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

        public bool IsComplete()
        {
            return currentKills >= requiredKills;
        }
    }
}
