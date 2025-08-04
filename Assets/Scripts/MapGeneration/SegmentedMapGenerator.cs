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
        private int nextSegmentX;
        private bool generating;

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

            var arr = segments.ToArray();
            var heroX = controller.hero.transform.position.x;
            if (heroX >= arr[2].startX)
                StartCoroutine(ShiftSegments());
        }

        private IEnumerator ShiftSegments()
        {
            generating = true;
            var old = segments.Dequeue();
            chunkGenerator.ClearSegment(new Vector2Int(old.startX, 0), segmentSize);

            foreach (var obj in new List<MonoBehaviour>(controller.TaskObjects))
            {
                if (obj == null) continue;
                if (obj.transform.IsChildOf(old.tasks.transform))
                    controller.RemoveTaskObject(obj);
            }

            Destroy(old.tasks);
            if (old.decor != null)
                Destroy(old.decor);

            yield return StartCoroutine(CreateSegment());
            MoveGraph();
            generating = false;
        }

        private IEnumerator CreateSegment()
        {
            var offset = new Vector2Int(nextSegmentX, 0);
            var decorRoot = new GameObject($"SegmentDecor_{offset.x}");
            decorRoot.transform.SetParent(decorParent, false);
            chunkGenerator.GenerateSegment(offset, segmentSize, decorRoot.transform);
            yield return null;

            var tasksRoot = new GameObject($"SegmentTasks_{offset.x}");
            tasksRoot.transform.SetParent(segmentParent, false);

            var minX = Mathf.Max(taskGenerator.MinX, offset.x);
            var maxX = offset.x + segmentSize.x;
            if (maxX > minX)
                taskGenerator.GenerateSegment(minX, maxX, tasksRoot.transform);

            segments.Enqueue(new Segment { startX = offset.x, tasks = tasksRoot, decor = decorRoot });
            nextSegmentX += segmentSize.x;
        }

        private void MoveGraph()
        {
            if (pathfinder == null)
                return;

            var gg = pathfinder.data.gridGraph;
            if (gg == null)
                return;

            if (segments.Count < 3)
                return;

            var arr = segments.ToArray();

            // Calculate the leftmost and rightmost bounds one tile in from the
            // outer segments. The A* grid uses two nodes per tile, so we convert
            // the tile span into node counts when setting the dimensions.
            var left = arr[0].startX + 1; // 1 tile in from the left chunk
            var right = arr[2].startX + segmentSize.x - 1; // 1 tile in from the right chunk
            var widthTiles = right - left; // total tiles covered by the grid

            gg.SetDimensions(widthTiles * 2, segmentSize.y * 2, gg.nodeSize);
            gg.center = new Vector3(left - 1 + widthTiles * 0.5f, segmentSize.y * 0.5f, 0f);
            pathfinder.Scan();
        }
    }
}