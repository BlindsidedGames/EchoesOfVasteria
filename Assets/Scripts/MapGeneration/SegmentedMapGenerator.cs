using System;
using System.Collections;
using System.Collections.Generic;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    ///     Generates map segments at runtime and recycles the oldest as the hero progresses.
    /// </summary>
    [RequireComponent(typeof(TilemapChunkGenerator))]
    [RequireComponent(typeof(ProceduralTaskGenerator))]
    [RequireComponent(typeof(TaskController))]
    public class SegmentedMapGenerator : MonoBehaviour
    {
        [SerializeField] private Vector2Int segmentSize = new(64, 18);
        [SerializeField] private Transform segmentParent;
        [SerializeField] private Transform decorParent;
        [SerializeField] private AstarPath pathfinder;
        [SerializeField] [Min(1)] private int warmupSegmentCount = 3;

        private TilemapChunkGenerator chunkGenerator;
        private ProceduralTaskGenerator taskGenerator;
        private TaskController controller;

        private readonly Queue<Segment> segments = new();
        private readonly List<MonoBehaviour> tmpRemovalList = new();
        private int nextSegmentX;
        private bool generating;

        // Incremental A* update tracking
        private bool gridInitialized;
        private int lastLeftTile;
        private int lastRightTile;
        private Vector3 lastGraphCenter;

        private bool initialGenerationComplete;
        private float initialGenerationProgressValue;
        private WaitUntil waitForInitialGeneration;

        public bool InitialGenerationComplete => initialGenerationComplete;

        public float InitialGenerationProgress =>
            initialGenerationComplete ? 1f : initialGenerationProgressValue;

        public event Action<float> InitialGenerationProgressed;
        public event Action InitialGenerationFinished;

        private class Segment
        {
            public int startX;
            public GameObject tasks;
            public GameObject decor;
        }

        private void Awake()
        {
            ResetInitialGenerationTracking();
            chunkGenerator = GetComponent<TilemapChunkGenerator>();
            taskGenerator = GetComponent<ProceduralTaskGenerator>();
            controller = GetComponent<TaskController>();
            controller.BeginInitialGeneration();
            ApplyConfig(GameManager.CurrentGenerationConfig);
            if (segmentParent == null)
                segmentParent = transform;
            if (decorParent == null)
                decorParent = segmentParent;
        }

        private void ApplyConfig(MapGenerationConfig cfg)
        {
            if (cfg == null) return;

            segmentSize = cfg.segmentedMapSettings.segmentSize;
        }

        private void ResetInitialGenerationTracking()
        {
            initialGenerationComplete = false;
            initialGenerationProgressValue = 0f;
            waitForInitialGeneration = null;
        }

        public IEnumerator WaitForInitialGeneration()
        {
            if (InitialGenerationComplete)
                yield break;

            waitForInitialGeneration ??= new WaitUntil(() => initialGenerationComplete);
            yield return waitForInitialGeneration;
        }

        private void UpdateInitialGenerationProgress(float normalizedProgress)
        {
            if (initialGenerationComplete)
                return;

            initialGenerationProgressValue = Mathf.Clamp01(normalizedProgress);
            InitialGenerationProgressed?.Invoke(initialGenerationProgressValue);
        }

        private void CompleteInitialGeneration()
        {
            if (initialGenerationComplete)
                return;

            controller?.EndInitialGeneration();
            initialGenerationComplete = true;
            initialGenerationProgressValue = 1f;
            InitialGenerationProgressed?.Invoke(initialGenerationProgressValue);
            InitialGenerationFinished?.Invoke();
        }

        private IEnumerator Start()
        {
            // AstarPath's automatic scan is disabled on the prefab. The one scan
            // below must see the completed terrain and colliders.
            var warmupCount = Mathf.Max(1, warmupSegmentCount);
            for (var i = 0; i < warmupCount; i++)
            {
                yield return StartCoroutine(CreateSegment());
                UpdateInitialGenerationProgress((i + 1f) / warmupCount);
            }

            // Ensure TilemapCollider2D has rebuilt before first scan
            yield return new WaitForEndOfFrame();
            Physics2D.SyncTransforms();
            yield return null; // extra frame for composite/tiles to finish updating

            yield return MoveGraph(false);
            CompleteInitialGeneration();
        }

        private void Update()
        {
            if (generating || controller == null || controller.hero == null)
                return;

            if (segments.Count < 3)
                return;

            // Avoid Queue.ToArray() to prevent per-frame allocations
            Segment third = null;
            var i = 0;
            foreach (var s in segments)
            {
                if (i == 2) { third = s; break; }
                i++;
            }

            if (third == null) return;

            var heroX = controller.hero.transform.position.x;
            if (heroX >= third.startX)
                StartCoroutine(ShiftSegments());
        }

        private IEnumerator ShiftSegments()
        {
            generating = true;
            var old = segments.Dequeue();
            chunkGenerator.ClearSegment(new Vector2Int(old.startX, 0), segmentSize);

            // Reuse a cached list to avoid per-shift allocations
            tmpRemovalList.Clear();
            tmpRemovalList.AddRange(controller.TaskObjects);
            for (var idx = 0; idx < tmpRemovalList.Count; idx++)
            {
                var obj = tmpRemovalList[idx];
                if (obj == null) continue;
                var t = obj.transform;
                if (t != null && old.tasks != null && t.IsChildOf(old.tasks.transform))
                    controller.RemoveTaskObject(obj);
            }
            tmpRemovalList.Clear();

            // Release all children under the old roots back to pools, then release the roots
            if (old.tasks != null)
            {
                var t = old.tasks.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                    Blindsided.Utilities.Pooling.PoolManager.Release(t.GetChild(i).gameObject);
                Blindsided.Utilities.Pooling.PoolManager.Release(old.tasks);
            }
            if (old.decor != null)
            {
                var d = old.decor.transform;
                for (int i = d.childCount - 1; i >= 0; i--)
                    Blindsided.Utilities.Pooling.PoolManager.Release(d.GetChild(i).gameObject);
                Blindsided.Utilities.Pooling.PoolManager.Release(old.decor);
            }

            yield return StartCoroutine(CreateSegment());

            // Wait for tilemap/collider systems to update before rescanning A*
            yield return new WaitForEndOfFrame();
            Physics2D.SyncTransforms();
            // One more frame helps when using CompositeCollider2D
            yield return null;

            yield return MoveGraph(true);
            generating = false;
        }

        private IEnumerator CreateSegment()
        {
            var offset = new Vector2Int(nextSegmentX, 0);
            // Pool decor/task roots and recycle instead of new GameObjects
            var decorRoot = Blindsided.Utilities.Pooling.PoolManager.GetNamed("SegmentDecor");
            decorRoot.name = $"SegmentDecor_{offset.x}";
            decorRoot.transform.SetParent(decorParent, false);
            // Async tile/decor generation to avoid long main-thread spikes
#if UNITY_EDITOR
            // Screenshot recipes require fixed generation ordering. Frame-budget
            // yields otherwise let animation/AI run between placement operations.
            if (TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotSession)
                chunkGenerator?.GenerateSegment(offset, segmentSize, decorRoot.transform);
            else
#endif
            if (chunkGenerator != null)
                yield return chunkGenerator.GenerateSegmentAsync(offset, segmentSize, decorRoot.transform);
            else
                yield return null;

            var tasksRoot = Blindsided.Utilities.Pooling.PoolManager.GetNamed("SegmentTasks");
            tasksRoot.name = $"SegmentTasks_{offset.x}";
            tasksRoot.transform.SetParent(segmentParent, false);

            var minX = Mathf.Max(taskGenerator.MinX, offset.x);
            var maxX = offset.x + segmentSize.x;
            if (maxX > minX)
            {
#if UNITY_EDITOR
                if (TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotSession)
                    taskGenerator?.GenerateSegment(minX, maxX, tasksRoot.transform);
                else
#endif
                if (taskGenerator != null)
                    yield return taskGenerator.GenerateSegmentAsync(minX, maxX, tasksRoot.transform);
                else
                    yield return null;

                foreach (var task in taskGenerator.Controller.TaskObjects)
                {
                    if (task != null && task.transform.IsChildOf(tasksRoot.transform))
                        chunkGenerator.ClearDecorAtPosition(task.transform.position);
                }
            }

            var scenery = chunkGenerator.PlaceWoodlandScenery(offset, segmentSize, decorRoot.transform, controller.TaskObjects);
#if UNITY_EDITOR
            if (TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotSession)
                while (scenery.MoveNext()) { }
            else
#endif
                yield return scenery;
            segments.Enqueue(new Segment { startX = offset.x, tasks = tasksRoot, decor = decorRoot });
            nextSegmentX += segmentSize.x;
        }

        private IEnumerator MoveGraph(bool asynchronous)
        {
            if (pathfinder == null)
                yield break;
            if (!pathfinder.isActiveAndEnabled)
                pathfinder.enabled = true;
            var astar = AstarPath.active;
            if (astar == null || !astar.isActiveAndEnabled)
                yield break;
            var gg = pathfinder.data != null ? pathfinder.data.gridGraph : null;
            if (gg == null)
                yield break;

            if (segments.Count < 3)
                yield break;

            // Get first and third segments without allocating arrays
            Segment first = null, third = null; var idx = 0;
            foreach (var s in segments)
            {
                if (idx == 0) first = s;
                if (idx == 2) { third = s; break; }
                idx++;
            }
            if (first == null || third == null) yield break;

            // Calculate trimmed bounds inside outer segments
            var left = first.startX + 1;
            var right = third.startX + segmentSize.x - 1;
            var widthTiles = right - left;

            // Initial full scan once; incremental updates afterward
            var newCenter = new Vector3(left - 1 + widthTiles * 0.5f, segmentSize.y * 0.5f, 0f);
            if (!gridInitialized)
            {
                // Perform graph resize + scan while pathfinding threads are paused to avoid races
                using (astar.PausePathfinding())
                {
                    gg.SetDimensions(Mathf.Max(2, widthTiles * 2), Mathf.Max(2, segmentSize.y * 2), gg.nodeSize);
                    gg.center = newCenter;
                    astar.Scan(gg);
                }

                lastLeftTile = left;
                lastRightTile = right;
                lastGraphCenter = newCenter;
                gridInitialized = true;
                yield break;
            }

            // Keep the complete rescan: retained segments can contain changed task
            // colliders, so moving only the exposed grid strip leaves stale nodes.
            // A*'s asynchronous scan keeps the old graph valid until publication and
            // budgets preparation across frames instead of blocking a combat frame.
            gg.center = newCenter;
            if (asynchronous)
            {
                foreach (var progress in astar.ScanAsync(gg)) yield return null;
            }
            else
            {
                using (astar.PausePathfinding()) astar.Scan(gg);
            }

            lastLeftTile = left;
            lastRightTile = right;
            lastGraphCenter = newCenter;
        }
    }
}
