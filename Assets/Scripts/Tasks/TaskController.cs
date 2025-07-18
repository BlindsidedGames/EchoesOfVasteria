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


        [SerializeField] private LayerMask enemyMask = ~0;

        [SerializeField] private AstarPath astarPath;

        [SerializeField] public HeroController hero;
        [SerializeField] public float maxBacktrackDistance = -1f;
        // Positive values give tasks located behind the hero a priority bonus
        [SerializeField] public float backtrackingAdditionalWeight = 0f;
        [SerializeField] private CinemachineCamera mapCamera;
        [SerializeField] private string currentTaskName;
        [SerializeField] private MonoBehaviour currentTaskObject;
        private readonly Dictionary<ITask, MonoBehaviour> taskMap = new();
        private readonly Dictionary<ITask, float> taskStartTimes = new();

        private int currentIndex = -1;
        public List<ITask> tasks { get; } = new();

        /// <summary>
        ///     Read-only access to the objects used when building the task list.
        /// </summary>
        public IReadOnlyList<MonoBehaviour> TaskObjects => taskObjects;

        public AstarPath Pathfinder => astarPath;
        public CinemachineCamera MapCamera => mapCamera;
        public MonoBehaviour CurrentTaskObject => currentTaskObject;

        private void Awake()
        {
            AcquireHero();
            if (mapCamera == null)
                mapCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        private void Update()
        {
            PruneTasks();
        }

        private void OnEnable()
        {
            AcquireHero();
            ResetTasks();
        }

        private void AcquireHero()
        {
            if (hero == null)
            {
                hero = GetComponentInChildren<HeroController>(true);
                if (hero == null)
                    Log("TaskController hero reference is null", TELogCategory.Task, this);
            }
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

        /// <summary>
        /// Add a task object during runtime and register its task.
        /// </summary>
        public void AddRuntimeTaskObject(MonoBehaviour obj)
        {
            if (obj == null || taskObjects.Contains(obj))
                return;

            taskObjects.Add(obj);

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
                SortTaskListsByProximity();
                return;
            }

            if (obj is ITask existing)
            {
                tasks.Add(existing);
                taskMap[existing] = obj;
                SortTaskListsByProximity();
                return;
            }

            var compTask = obj.GetComponent<ITask>();
            if (compTask != null)
            {
                tasks.Add(compTask);
                taskMap[compTask] = obj;
            }

            SortTaskListsByProximity();
        }

        /// <summary>
        /// Remove a task object and any tasks associated with it.
        /// </summary>
        public void RemoveTaskObject(MonoBehaviour obj)
        {
            if (obj == null)
                return;

            var toRemove = new List<ITask>();
            foreach (var pair in taskMap)
            {
                if (pair.Value == obj)
                    toRemove.Add(pair.Key);
            }

            foreach (var task in toRemove)
                RemoveTask(task);

            taskObjects.Remove(obj);
        }

        public void ResetTasks()
        {
            AcquireHero();
            if (hero == null)
                Log("ResetTasks called but hero is null", TELogCategory.Task, this);
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
            SortTaskListsByProximity();

            hero?.SetTask(null);
            SelectEarliestTask();
        }


        /// <summary>
        ///     Remove null, completed or expired tasks.
        /// </summary>
        private void PruneTasks()
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
                    if (kill.target == null || health == null || health.CurrentHealth <= 0f || kill.IsComplete())
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

                    continue;
                }

                if (task.IsComplete())
                {
                    RemoveTaskAt(i);
                    removed = true;
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
        public void SelectEarliestTask(HeroController actor = null)
        {
            var h = actor != null ? actor : hero;
            if (h == null)
                Log("SelectEarliestTask called but hero is null", TELogCategory.Task, this);
            RemoveCompletedTasks();
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null || task.IsComplete())
                    continue;
                if (task is BaseTask bt && bt.ClaimedBy != null && bt.ClaimedBy != h)
                    continue;
                currentIndex = i;
                currentTaskName = task.GetType().Name;
                currentTaskObject = null;
                if (taskMap.TryGetValue(task, out var obj))
                    currentTaskObject = obj;
                else if (task is MonoBehaviour mb)
                    currentTaskObject = mb;
                h?.SetTask(task);
                bool restart = false;
                if (task is BaseTask baseTask && baseTask.taskData != null)
                    restart = baseTask.taskData.resetProgressOnInterrupt;

                if (!taskStartTimes.ContainsKey(task) || restart)
                {
                    var msg = taskStartTimes.ContainsKey(task) ? "Restarting" : "Starting";
                    Log($"{msg} task: {currentTaskName}", TELogCategory.Task, this);
                    task.StartTask();
                    taskStartTimes[task] = Time.time;
                }
                else
                {
                    Log($"Resuming task: {currentTaskName}", TELogCategory.Task, this);
                }
                return;
            }

            currentTaskName = "Complete";
            currentIndex = tasks.Count;
            Log("All tasks complete", TELogCategory.Task, this);
            currentTaskObject = null;
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

            float duration = 0f;
            if (taskStartTimes.TryGetValue(task, out var start))
            {
                duration = Time.time - start;
                taskStartTimes.Remove(task);
            }
            if (taskMap.TryGetValue(task, out var obj))
            {
                taskObjects.Remove(obj);
                taskMap.Remove(task);
                if (obj != null)
                {
                    if (task is OpenChestTask)
                        Destroy(obj.GetComponent<OpenChestTask>());
                    else if (task is WoodcuttingTask)
                        Destroy(obj.GetComponent<WoodcuttingTask>());
                    else if (task is FarmingTask)
                        Destroy(obj.GetComponent<FarmingTask>());
                    else
                        Destroy(obj.gameObject);
                }
            }
            else if (task is MonoBehaviour mb)
            {
                if (task is OpenChestTask)
                    Destroy(mb);
                else if (task is WoodcuttingTask)
                    Destroy(mb);
                else if (task is FarmingTask)
                    Destroy(mb);
                else
                    Destroy(mb.gameObject);
            }

            if (task != null && task.IsComplete())
            {
                var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance ??
                              FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
                TaskData data = (task as BaseTask)?.taskData;
                float xp = (task as BaseTask)?.LastGrantedXp ?? 0f;
                tracker?.RegisterTaskComplete(data, duration, xp);
            }

            if (index <= currentIndex)
                currentIndex--;
        }


        private void SortTaskListsByProximity()
        {
            var pairs = new List<(Vector3 pos, MonoBehaviour obj, ITask task)>();

            foreach (var task in tasks)
            {
                taskMap.TryGetValue(task, out var obj);
                Vector3 pos = Vector3.zero;
                if (obj != null)
                    pos = obj.transform.position;
                else if (task != null && task.Target != null)
                    pos = task.Target.position;

                pairs.Add((pos, obj, task));
            }

            tasks.Clear();
            taskObjects.Clear();
            taskMap.Clear();

            Vector3 currentPos = hero != null ? hero.transform.position : Vector3.zero;

            while (pairs.Count > 0)
            {
                int bestIndex = -1;
                float bestDist = float.PositiveInfinity;

                for (int i = 0; i < pairs.Count; i++)
                {
                    var (p, _, _) = pairs[i];
                    float deltaX = currentPos.x - p.x;
                    if (maxBacktrackDistance >= 0f && deltaX > maxBacktrackDistance)
                        continue;

                    float d = Vector3.Distance(currentPos, p);
                    if (deltaX > 0f && backtrackingAdditionalWeight > 0f)
                        d -= deltaX * backtrackingAdditionalWeight;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIndex = i;
                    }
                }

                if (bestIndex == -1)
                {
                    for (int i = 0; i < pairs.Count; i++)
                    {
                        var (p, _, _) = pairs[i];
                        float deltaX = currentPos.x - p.x;
                        float d = Vector3.Distance(currentPos, p);
                        if (deltaX > 0f && backtrackingAdditionalWeight > 0f)
                            d -= deltaX * backtrackingAdditionalWeight;
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestIndex = i;
                        }
                    }
                }

                var (pos, obj, task) = pairs[bestIndex];
                pairs.RemoveAt(bestIndex);

                tasks.Add(task);
                if (obj != null)
                {
                    taskObjects.Add(obj);
                    taskMap[task] = obj;
                }

                currentPos = pos;
            }
        }

        public void RemoveCompletedTasks()
        {
            for (var i = tasks.Count - 1; i >= 0; i--)
            {
                var t = tasks[i];
                if (t == null || t.IsComplete())
                    RemoveTaskAt(i);
            }
        }
    }
}