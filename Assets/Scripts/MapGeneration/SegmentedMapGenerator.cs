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

        private class Segment
        {
            public int startX;
            public GameObject tasks;
            public GameObject decor;
        }

        private void Awake()
        {
            chunkGenerator = GetComponent<TilemapChunkGenerator>();
            taskGenerator = GetComponent<ProceduralTaskGenerator>();
            controller = GetComponent<TaskController>();
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

        private IEnumerator Start()
        {
            for (var i = 0; i < 3; i++)
                yield return StartCoroutine(CreateSegment());

            MoveGraph();
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
            MoveGraph();
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
            if (chunkGenerator != null)
                yield return chunkGenerator.GenerateSegmentAsync(offset, segmentSize, decorRoot.transform, 8);
            else
                yield return null;

            var tasksRoot = Blindsided.Utilities.Pooling.PoolManager.GetNamed("SegmentTasks");
            tasksRoot.name = $"SegmentTasks_{offset.x}";
            tasksRoot.transform.SetParent(segmentParent, false);

            var minX = Mathf.Max(taskGenerator.MinX, offset.x);
            var maxX = offset.x + segmentSize.x;
            if (maxX > minX)
            {
                if (taskGenerator != null)
                    yield return taskGenerator.GenerateSegmentAsync(minX, maxX, tasksRoot.transform, 10);
                else
                    yield return null;

                foreach (var task in taskGenerator.Controller.TaskObjects)
                {
                    if (task != null && task.transform.IsChildOf(tasksRoot.transform))
                        chunkGenerator.ClearDecorAtPosition(task.transform.position);
                }
            }

            segments.Enqueue(new Segment { startX = offset.x, tasks = tasksRoot, decor = decorRoot });
            nextSegmentX += segmentSize.x;
        }

        private void MoveGraph()
        {
            if (pathfinder == null)
                return;

            if (pathfinder == null)
                return;
            if (!pathfinder.isActiveAndEnabled)
                pathfinder.enabled = true;
            var astar = AstarPath.active;
            if (astar == null || !astar.isActiveAndEnabled)
                return;
            var gg = pathfinder.data != null ? pathfinder.data.gridGraph : null;
            if (gg == null)
                return;

            if (segments.Count < 3)
                return;

            // Get first and third segments without allocating arrays
            Segment first = null, third = null; var idx = 0;
            foreach (var s in segments)
            {
                if (idx == 0) first = s;
                if (idx == 2) { third = s; break; }
                idx++;
            }
            if (first == null || third == null) return;

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
                return;
            }

            // Translate grid without full rescan using GridGraph API.
            // Must run inside an A* work item (thread-safe window).
            astar.AddWorkItem(() =>
            {
                gg.RelocateNodes(
                    center: newCenter,
                    rotation: Quaternion.Euler(gg.rotation),
                    nodeSize: gg.nodeSize,
                    aspectRatio: gg.aspectRatio,
                    isometricAngle: gg.isometricAngle);

                // Update only the newly revealed slice (queue graph update after relocation)
                var deltaTilesInner = left - lastLeftTile; // +ve when moving right
                if (deltaTilesInner != 0)
                {
                    var addWidth = Mathf.Abs(deltaTilesInner);
                    int updateLeft, updateRight;
                    if (deltaTilesInner > 0)
                    {
                        updateLeft = right - addWidth;
                        updateRight = right;
                    }
                    else
                    {
                        updateLeft = left;
                        updateRight = left + addWidth;
                    }

                    var cx = (updateLeft + updateRight) * 0.5f;
                    var w = Mathf.Max(0.001f, Mathf.Abs(updateRight - updateLeft));
                    var bounds = new Bounds(new Vector3(cx, segmentSize.y * 0.5f, 0f), new Vector3(w, segmentSize.y, 1f));
                    var guo = new Pathfinding.GraphUpdateObject(bounds);
                    astar.UpdateGraphs(guo);
                }

                lastLeftTile = left;
                lastRightTile = right;
                lastGraphCenter = newCenter;
            });
            // Note: last* fields are updated inside the work item above
        }
    }
}
