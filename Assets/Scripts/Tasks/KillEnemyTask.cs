using UnityEngine;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for eliminating a specific enemy target.
    /// </summary>
    public class KillEnemyTask : BaseTask
    {
        public Transform target;
        private Health health;
        private bool complete;

        public override Transform Target => target;

        public override void StartTask()
        {
            // Remove any previous death listener in case this component is reused
            if (health != null)
                health.OnDeath -= OnDeath;

            complete = false;

            if (target == null)
            {
                complete = true;
                return;
            }

            health = target.GetComponent<Health>();
            if (health != null)
                health.OnDeath += OnDeath;

            if (health == null || health.CurrentHealth <= 0f)
                complete = true;
        }

        private void OnDeath()
        {
            complete = true;
            if (health != null)
                health.OnDeath -= OnDeath;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
        }

        public override bool IsComplete()
        {
            if (complete) return true;
            if (health == null) return target == null;
            return health.CurrentHealth <= 0f;
        }
    }
}
