#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Tasks;
using TimelessEchoes.Enemies;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Hero
{
    public partial class HeroController
    {
        public void SetTask(ITask task)
        {
            if (CurrentTask is BaseTask oldBase)
                oldBase.ReleaseClaim(this);

            Log($"Hero assigned task: {task?.GetType().Name ?? "None"}", TELogCategory.Task, this);
            CurrentTask = task;
            currentTaskName = task != null ? task.GetType().Name : "None";
            currentTaskObject = task as MonoBehaviour;
            state = State.Idle;

            if (setter == null)
                setter = GetComponent<AIDestinationSetter>();

            if (setter != null)
            {
                setter.target = task?.Target;
                if (ai != null)
                    ai.Teleport(transform.position);
                else
                    ai?.SearchPath();
            }

            if (task is BaseTask newBase)
                newBase.Claim(this);
        }

        /// <summary>
        ///     Clear the reference to the active <see cref="TaskController" /> so
        ///     this hero no longer receives task assignments.
        /// </summary>
        public void ClearTaskController()
        {
            taskController = null;
        }

        private void UpdateBehavior()
        {
            if (stats == null) return;

            enemyRemovalBuffer.Clear();
            foreach (var enemy in engagedEnemies)
            {
                var hp = enemy != null ? enemy.GetComponent<Health>() : null;
                if (enemy == null || hp == null || hp.CurrentHealth <= 0f || !enemy.IsEngaged)
                    enemyRemovalBuffer.Add(enemy);
            }

            foreach (var enemy in enemyRemovalBuffer)
                UnregisterEngagedEnemy(enemy);

            if (currentEnemy != null)
            {
                var hp = currentEnemy.GetComponent<Health>();
                var enemyComp = currentEnemy.GetComponent<Enemy>();
                if (hp == null || hp.CurrentHealth <= 0f || enemyComp == null || !engagedEnemies.Contains(enemyComp))
                {
                    currentEnemyHealth?.SetHealthBarVisible(false);
                    currentEnemy = null;
                    currentEnemyHealth = null;
                }
            }

            Transform nearest = currentEnemy;
            if (allowAttacks && nearest == null)
            {
                if (engagedEnemies.Count > 0)
                {
                    var best = float.PositiveInfinity;
                    Enemy chosen = null;
                    foreach (var enemy in engagedEnemies)
                    {
                        if (enemy == null) continue;

                        Transform enemyTransform = null;
                        try
                        {
                            enemyTransform = enemy.transform;
                        }
                        catch
                        {
                            // Enemy may have been destroyed mid-frame
                            continue;
                        }

                        if (enemyTransform == null) continue;

                        var dist = Vector2.Distance(transform.position, enemyTransform.position);
                        if (dist < best)
                        {
                            best = dist;
                            chosen = enemy;
                        }
                    }

                    Transform chosenTransform = null;
                    if (chosen != null)
                    {
                        try { chosenTransform = chosen.transform; } catch { chosenTransform = null; }
                    }
                    nearest = chosenTransform;
                }
                else
                {
                    var range = UnlimitedAggroRange ? combatAggroRange : stats.visionRange;
                    nearest = FindNearestEnemy(range);
                }
            }

            if (allowAttacks && nearest != null)
            {
                if (currentEnemy != nearest)
                {
                    currentEnemyHealth?.SetHealthBarVisible(false);
                    currentEnemy = nearest;
                    currentEnemyHealth = nearest.GetComponent<Health>();
                    currentEnemyHealth?.SetHealthBarVisible(true);
                }
                else if (currentEnemyHealth == null)
                {
                    currentEnemyHealth = nearest.GetComponent<Health>();
                    currentEnemyHealth?.SetHealthBarVisible(true);
                }

                if (state == State.PerformingTask && CurrentTask != null) CurrentTask.OnInterrupt(this);
                HandleCombat(nearest);
                return;
            }

            if (state == State.Combat && engagedEnemies.Count == 0)
            {
                Log("Hero exiting combat", TELogCategory.Combat, this);
                combatDamageMultiplier = 1f;
                isRolling = false;
                diceRoller?.ResetRoll();
                currentEnemyHealth?.SetHealthBarVisible(false);
                currentEnemyHealth = null;
                state = State.Idle;
                taskController?.SelectEarliestTask(this);
            }

            if (CurrentTask == null || CurrentTask.IsComplete())
            {
                CurrentTask = null;
                state = State.Idle;
                taskController?.SelectEarliestTask(this);
            }

            if (CurrentTask == null)
            {
                var noVisibleTasks = taskController == null || !taskController.HasVisibleTasksForHero(this);
                if (taskController == null || taskController.tasks.Count == 0 || (IsEcho && noVisibleTasks))
                    AutoAdvance();
                else
                    setter.target = null;
                return;
            }

            var dest = CurrentTask.Target;
            if (setter.target != dest) setter.target = dest;

            if (IsAtDestination(dest))
            {
                if (state != State.PerformingTask)
                {
                    state = State.PerformingTask;
                    ai.canMove = !CurrentTask.BlocksMovement;
                    CurrentTask.OnArrival(this);
                }

                CurrentTask.Tick(this);
            }
            else
            {
                state = State.MovingToTask;
                ai.canMove = true;
            }
        }
    }
}
