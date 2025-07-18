using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Represents a single task that the hero can perform.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        ///     World location relevant to this task.
        /// </summary>
        Transform Target { get; }

        /// <summary>
        ///     The hero currently assigned to this task. Null if unclaimed.
        /// </summary>
        HeroController ClaimedBy { get; }

        /// <summary>
        ///     True if the task has been claimed by a hero.
        /// </summary>
        bool IsClaimed { get; }

        /// <summary>
        ///     Mark the task as claimed by the provided hero.
        /// </summary>
        void Claim(HeroController hero);

        /// <summary>
        ///     Release the claim on this task if owned by the provided hero.
        /// </summary>
        void Unclaim(HeroController hero);

        /// <summary>
        ///     A property to indicate if this task should prevent the hero from moving.
        /// </summary>
        bool BlocksMovement { get; }

        /// <summary>
        ///     Returns true if the task has been completed.
        /// </summary>
        bool IsComplete();

        /// <summary>
        ///     Begin the task's logic. Usually this spawns any objectives or begins
        ///     tracking progress.
        /// </summary>
        void StartTask();

        /// <summary>
        ///     Called by the HeroController once it reaches the task's target destination.
        /// </summary>
        void OnArrival(HeroController hero);

        /// <summary>
        ///     Called every frame while the hero is at the task location and performing the task.
        /// </summary>
        void Tick(HeroController hero);

        /// <summary>
        ///     Called by the HeroController when the task is interrupted (e.g., by combat).
        /// </summary>
        void OnInterrupt(HeroController hero);
    }
}