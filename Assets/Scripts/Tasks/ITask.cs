using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Represents a single task that the hero can perform.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// World location relevant to this task.
        /// </summary>
        Transform Target { get; }
        /// <summary>
        /// Returns true if the task has been completed.
        /// </summary>
        bool IsComplete();

        /// <summary>
        /// Begin the task's logic. Usually this spawns any objectives or begins
        /// tracking progress.
        /// </summary>
        void StartTask();
    }
}
