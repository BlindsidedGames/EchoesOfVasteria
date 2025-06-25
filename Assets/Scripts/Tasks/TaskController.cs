using System.Collections.Generic;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using Unity.Cinemachine;
using UnityEngine;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Controls progression through a list of tasks.
    /// </summary>
    public class TaskController : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> taskObjects = new();
        private readonly Dictionary<ITask, MonoBehaviour> taskMap = new();

        [SerializeField] private Transform entryPoint;
        [SerializeField] private Transform exitPoint;

        [SerializeField] private LayerMask enemyMask = ~0;

        [SerializeField] private AstarPath astarPath;

        [SerializeField] public HeroController hero;
        [SerializeField] private CinemachineCamera mapCamera;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;

        private int currentIndex = -1;
        public List<ITask> tasks { get; private set; } = new();

        /// <summary>
        ///     Read-only access to the objects used when building the task list.
        /// </summary>
        public IReadOnlyList<MonoBehaviour> TaskObjects => taskObjects;

        public Transform EntryPoint => entryPoint;
        public Transform ExitPoint => exitPoint;
        public AstarPath Pathfinder => astarPath;
        public CinemachineCamera MapCamera => mapCamera;
        public MonoBehaviour CurrentTaskObject => currentTaskObject;

        private void AcquireHero()
        {
            if (hero == null)
            {
                hero = GetComponentInChildren<HeroController>(true);
                if (hero == null)
                    TELogger.Log("TaskController hero reference is null", this);
            }
        }

        private void Awake()
        {
            AcquireHero();
            if (mapCamera == null)
                mapCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        private void Update()
        {
            RemoveDeadEnemyTasks();
            RemoveCompletedTasks();
        }

        private void OnEnable()
        {
            AcquireHero();
            ResetTasks();
        }

        /// <summary>
        ///     Remove any previously assigned task objects.
        /// </summary>
        public void ClearTaskObjects()
        {
            taskObjects.Clear();
            taskMap.Clear();
        }

        /// <summary>
        ///     Add a task source object if it is not already present.
        /// </summary>
        public void AddTaskObject(MonoBehaviour obj)
        {
            if (obj == null || taskObjects.Contains(obj))
                return;
            taskObjects.Add(obj);
        }

        public void ResetTasks()
        {
            AcquireHero();
            if (hero == null)
                TELogger.Log("ResetTasks called but hero is null", this);
            currentIndex = -1;
            tasks.Clear();
            taskMap.Clear();
            currentTaskObject = null;

            // Build the task list in the order provided by the editor
            foreach (var obj in taskObjects)
            {
                if (obj == null) continue;

                // If an enemy component is supplied, ensure it has a KillEnemyTask
                var enemy = obj.GetComponent<Enemy>();
                if (enemy != null)
                {
                    var hp = enemy.GetComponent<Health>();
                    if (hp != null)
                        hp.Init((int)hp.MaxHealth);
                    var kill = enemy.GetComponent<KillEnemyTask>();
                    if (kill == null)
                        kill = enemy.gameObject.AddComponent<KillEnemyTask>();
                    kill.target = enemy.transform;
                    tasks.Add(kill);
                    taskMap[kill] = obj;
                    continue;
                }

                if (obj is ITask existing)
                {
                    tasks.Add(existing);
                    taskMap[existing] = obj;
                    continue;
                }

                var compTask = obj.GetComponent<ITask>();
                if (compTask != null)
                {
                    tasks.Add(compTask);
                    taskMap[compTask] = obj;
                }
            }

            hero?.SetTask(null);
            hero?.SetDestination(entryPoint);
            SelectEarliestTask();
        }


        /// <summary>
        ///     Remove enemy tasks whose targets are dead or destroyed.
        /// </summary>
        private void RemoveDeadEnemyTasks()
        {
            var removed = false;
            for (var i = tasks.Count - 1; i >= 0; i--)
            {
                var task = tasks[i];
                if (task == null)
                {
                    if (i <= currentIndex)
                        currentIndex--;
                    tasks.RemoveAt(i);
                    if (taskMap.TryGetValue(task, out var obj))
                    {
                        taskObjects.Remove(obj);
                        taskMap.Remove(task);
                    }
                    removed = true;
                    continue;
                }

                if (task is KillEnemyTask kill)
                {
                    var health = kill.target != null ? kill.target.GetComponent<Health>() : null;
                    if (kill.target == null || health == null || health.CurrentHealth <= 0f)
                    {
                        if (i <= currentIndex)
                            currentIndex--;
                        tasks.RemoveAt(i);
                        if (taskMap.TryGetValue(task, out var obj))
                        {
                            taskObjects.Remove(obj);
                            taskMap.Remove(task);
                        }
                        if (kill != null)
                            Destroy(kill);
                        removed = true;
                    }
                }
            }

            if (removed && hero != null)
            {
                hero.SetTask(null);
                SelectEarliestTask();
            }
        }

        /// <summary>
        ///     Advance to the earliest available task and start it if one exists.
        /// </summary>
        public void SelectEarliestTask()
        {
            if (hero == null)
                TELogger.Log("SelectEarliestTask called but hero is null", this);
            RemoveCompletedTasks();
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null || task.IsComplete())
                    continue;
                currentIndex = i;
                currentTaskName = task.GetType().Name;
                currentTaskObject = null;
                if (taskMap.TryGetValue(task, out var obj))
                    currentTaskObject = obj;
                else if (task is MonoBehaviour mb)
                    currentTaskObject = mb;
                TELogger.Log($"Starting task: {currentTaskName}", this);
                hero?.SetTask(task);
                task.StartTask();
                return;
            }

            currentTaskName = "Complete";
            currentIndex = tasks.Count;
            TELogger.Log("All tasks complete", this);
            currentTaskObject = null;
            hero?.SetDestination(exitPoint);
        }

        /// <summary>
        ///     Remove a task from tracking.
        /// </summary>
        public void RemoveTask(ITask task)
        {
            if (task == null) return;
            var index = tasks.IndexOf(task);
            if (index < 0) return;
            RemoveTaskAt(index);
        }

        private void RemoveTaskAt(int index)
        {
            if (index < 0 || index >= tasks.Count) return;
            var task = tasks[index];
            tasks.RemoveAt(index);
            if (taskMap.TryGetValue(task, out var obj))
            {
                taskObjects.Remove(obj);
                taskMap.Remove(task);
            }
            if (index <= currentIndex)
                currentIndex--;
        }

        private void RemoveCompletedTasks()
        {
            for (var i = tasks.Count - 1; i >= 0; i--)
            {
                var t = tasks[i];
                if (t == null || t.IsComplete())
                    RemoveTaskAt(i);
            }
        }

        private void GatherEnemyTasks()
        {
            var enemies = GetComponentsInChildren<Enemy>();
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
            var current = entryPoint ? entryPoint.position : transform.position;
            while (remaining.Count > 0)
            {
                var bestIndex = 0;
                var bestDist = Distance(current, remaining[0]);
                for (var i = 1; i < remaining.Count; i++)
                {
                    var d = Distance(current, remaining[i]);
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