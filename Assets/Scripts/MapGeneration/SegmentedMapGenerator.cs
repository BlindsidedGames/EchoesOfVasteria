using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using TimelessEchoes.Tasks;

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
        [SerializeField]
        private MapGenerationConfig config;

        [SerializeField] private Vector2Int segmentSize = new(64, 18);
        [SerializeField] private Transform segmentParent;
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
        }

        private void Awake()
        {
            chunkGenerator = GetComponent<TilemapChunkGenerator>();
            taskGenerator = GetComponent<ProceduralTaskGenerator>();
            controller = GetComponent<TaskController>();
            ApplyConfig();
            if (segmentParent == null)
                segmentParent = transform;
        }

        private void ApplyConfig()
        {
            if (config == null) return;

            segmentSize = config.segmentedMapSettings.segmentSize;
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

            yield return StartCoroutine(CreateSegment());
            MoveGraph();
            generating = false;
        }

        private IEnumerator CreateSegment()
        {
            var offset = new Vector2Int(nextSegmentX, 0);
            chunkGenerator.GenerateSegment(offset, segmentSize);
            yield return null;

            var tasksRoot = new GameObject($"SegmentTasks_{offset.x}");
            tasksRoot.transform.SetParent(segmentParent, false);

            var minX = Mathf.Max(taskGenerator.MinX, offset.x);
            var maxX = offset.x + segmentSize.x;
            if (maxX > minX)
                taskGenerator.GenerateSegment(minX, maxX, tasksRoot.transform);

            segments.Enqueue(new Segment { startX = offset.x, tasks = tasksRoot });
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
            var start = arr[1].startX;
            gg.SetDimensions(segmentSize.x * 4, segmentSize.y, gg.nodeSize);
            gg.center = new Vector3(start + segmentSize.x, 9f, 0f);
            pathfinder.Scan();
        }
    }
}
