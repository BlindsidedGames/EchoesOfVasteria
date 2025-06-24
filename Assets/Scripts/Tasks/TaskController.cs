using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using TimelessEchoes;
using Unity.Cinemachine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Controls progression through a list of tasks.
    /// </summary>
    public class TaskController : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> taskObjects = new();
        public List<ITask> tasks { get; private set; } = new();

        /// <summary>
        /// Read-only access to the objects used when building the task list.
        /// </summary>
        public IReadOnlyList<MonoBehaviour> TaskObjects => taskObjects;

        /// <summary>
        /// Remove any previously assigned task objects.
        /// </summary>
        public void ClearTaskObjects()
        {
            taskObjects.Clear();
        }

        /// <summary>
        /// Add a task source object if it is not already present.
        /// </summary>
        public void AddTaskObject(MonoBehaviour obj)
        {
            if (obj == null || taskObjects.Contains(obj))
                return;
            taskObjects.Add(obj);
        }

        [SerializeField] private Transform entryPoint;
        [SerializeField] private Transform exitPoint;

        public Transform EntryPoint => entryPoint;
        public Transform ExitPoint => exitPoint;

        [SerializeField] private LayerMask enemyMask = ~0;

        [SerializeField] public Hero.HeroController hero;
        [SerializeField] private CinemachineCamera mapCamera;
        public CinemachineCamera MapCamera => mapCamera;
        [SerializeField] private float engageRange = 2f;
        [SerializeField] private string currentTaskName;

        private int currentIndex = -1;

        private void Awake()
        {
            if (hero == null)
                hero = GetComponent<Hero.HeroController>();
            if (mapCamera == null)
                mapCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        private void OnEnable()
        {
            ResetTasks();
        }

        public void ResetTasks()
        {
            currentIndex = -1;
            tasks.Clear();

            // Build the task list in the order provided by the editor
            foreach (var obj in taskObjects)
            {
                if (obj == null) continue;

                // If an enemy component is supplied, ensure it has a KillEnemyTask
                var enemy = obj.GetComponent<Enemies.Enemy>();
                if (enemy != null)
                {
                    // Ensure the enemy's health is initialized before tasks start
                    var hp = enemy.GetComponent<Enemies.Health>();
                    if (hp != null)
                        hp.Init((int)hp.MaxHealth);
                    var kill = enemy.GetComponent<KillEnemyTask>();
                    if (kill == null)
                        kill = enemy.gameObject.AddComponent<KillEnemyTask>();
                    kill.target = enemy.transform;
                    tasks.Add(kill);
                    continue;
                }

                if (obj is ITask existing)
                {
                    tasks.Add(existing);
                    continue;
                }

                var compTask = obj.GetComponent<ITask>();
                if (compTask != null)
                    tasks.Add(compTask);
            }

            hero?.SetTask(null);
            hero?.SetDestination(entryPoint);
            SelectNextTask();
        }

        private void Update()
        {
            EngageNearbyEnemies();

            if (currentIndex < 0 || currentIndex >= tasks.Count)
                return;

            var active = tasks[currentIndex];

            if (active.IsComplete())
                SelectNextTask();
        }

        /// <summary>
        /// Check all enemy tasks and activate any enemies near the hero.
        /// </summary>
        private void EngageNearbyEnemies()
        {
            if (hero == null)
                return;

            foreach (var task in tasks)
            {
                if (task is KillEnemyTask kill)
                {
                    var target = kill.target;
                    if (target == null)
                        continue;

                    float dist = Vector3.Distance(hero.transform.position, target.position);
                    if (dist <= engageRange)
                    {
                        var set = target.GetComponent<AIDestinationSetter>();
                        if (set != null)
                            set.target = hero.transform;
                    }
                }
            }
        }

        /// <summary>
        /// Advance to the next task and start it if available.
        /// </summary>
        public void SelectNextTask()
        {
            currentIndex++;
            if (currentIndex < tasks.Count)
            {
                var task = tasks[currentIndex];
                currentTaskName = task.GetType().Name;
                hero?.SetTask(task);
                task.StartTask();
            }
            else
            {
                currentTaskName = "Complete";
                hero?.SetDestination(exitPoint);
            }
        }

        private void GatherEnemyTasks()
        {
            var enemies = GetComponentsInChildren<Enemies.Enemy>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (((1 << enemy.gameObject.layer) & enemyMask) == 0) continue;
                var tr = enemy.transform;
                var task = enemy.GetComponent<KillEnemyTask>();
                if (task == null)
                    task = enemy.gameObject.AddComponent<KillEnemyTask>();
                task.target = tr;
                tasks.Add(task);
            }
        }

        private void SortTasksByDistance()
        {
            var remaining = new List<ITask>(tasks);
            tasks = new List<ITask>();
            Vector3 current = entryPoint ? entryPoint.position : transform.position;
            while (remaining.Count > 0)
            {
                int bestIndex = 0;
                float bestDist = Distance(current, remaining[0]);
                for (int i = 1; i < remaining.Count; i++)
                {
                    float d = Distance(current, remaining[i]);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIndex = i;
                    }
                }
                var chosen = remaining[bestIndex];
                remaining.RemoveAt(bestIndex);
                tasks.Add(chosen);
                if (chosen.Target != null)
                    current = chosen.Target.position;
            }
        }

        private static float Distance(Vector3 from, ITask task)
        {
            return task.Target != null ? Vector3.Distance(from, task.Target.position) : float.MaxValue;
        }
    }
}
