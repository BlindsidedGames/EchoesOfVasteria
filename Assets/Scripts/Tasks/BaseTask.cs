using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Base component for task behaviours.
    /// Provides default implementations of <see cref="ITask"/> members.
    /// </summary>
    public abstract class BaseTask : MonoBehaviour, ITask
    {
        /// <summary>
        /// The world location relevant to this task.
        /// </summary>
        public abstract Transform Target { get; }

        /// <summary>
        /// Begin the task's logic. Override to perform initialization.
        /// </summary>
        public virtual void StartTask()
        {
        }

        /// <summary>
        /// Determine if the task has completed.
        /// </summary>
        public abstract bool IsComplete();
    }
}
