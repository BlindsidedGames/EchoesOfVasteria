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

        /// <summary>
        ///     Attempt to claim this task for the specified hero.
        /// </summary>
        /// <returns>True if the claim succeeded.</returns>
        bool Claim(HeroController hero);

        /// <summary>
        ///     Release a claim held by the specified hero.
        /// </summary>
        void ReleaseClaim(HeroController hero);

        /// <summary>
        ///     The hero currently claiming this task, or null if unclaimed.
        /// </summary>
        HeroController ClaimedBy { get; }
    }
}