using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    /// Manages three procedural map segments and recycles them as the hero
    /// progresses. Segments are generated using TilemapChunkGenerator and
    /// ProceduralTaskGenerator.
    /// </summary>
    public class ProceduralSegmentManager : MonoBehaviour
    {
        [SerializeField] private TilemapChunkGenerator segmentTemplate;
        [SerializeField] private ProceduralTaskGenerator taskTemplate;
        [SerializeField] private TaskController taskController;
        [SerializeField] private AstarPath astarPath;
        [SerializeField] private Transform hero;
        [SerializeField] private Transform segmentParent;
        [SerializeField] private Vector2Int segmentSize = new(64, 18);

        private readonly Queue<MapSegment> segments = new();
        private bool advancing;
        private int nextIndex;

        private void Start()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();
            for (var i = 0; i < 3; i++)
                segments.Enqueue(CreateSegment(i * segmentSize.x));
            StartCoroutine(GenerateInitial());
        }

        private IEnumerator GenerateInitial()
        {
            foreach (var seg in segments)
                yield return GenerateSegment(seg);
            MoveGraph();
            astarPath?.Scan();
        }

        private void Update()
        {
            if (advancing || hero == null)
                return;

            var middle = GetSegment(1);
            if (middle != null && hero.position.x >= middle.EndX)
                StartCoroutine(Advance());
        }

        private IEnumerator Advance()
        {
            advancing = true;

            if (segments.Count > 0)
            {
                var old = segments.Dequeue();
                if (old.Root != null)
                    Destroy(old.Root);
            }

            var seg = CreateSegment(nextIndex * segmentSize.x);
            nextIndex++;
            segments.Enqueue(seg);

            yield return GenerateSegment(seg);
            MoveGraph();
            astarPath?.Scan();
            advancing = false;
        }

        private MapSegment CreateSegment(float startX)
        {
            var root = new GameObject($"Segment_{nextIndex}");
            if (segmentParent != null)
                root.transform.SetParent(segmentParent);
            root.transform.position = new Vector3(startX, 0f, 0f);

            var chunk = Instantiate(segmentTemplate, root.transform);
            var tasks = Instantiate(taskTemplate, root.transform);
            tasks.Controller = taskController;

            return new MapSegment(root, chunk, tasks, startX, segmentSize.x);
        }

        private IEnumerator GenerateSegment(MapSegment seg)
        {
            seg.Chunk.Generate();
            yield return null; // wait for colliders
            seg.Tasks.Generate();
        }

        private MapSegment GetSegment(int index)
        {
            if (index < 0 || index >= segments.Count)
                return null;
            var i = 0;
            foreach (var seg in segments)
            {
                if (i == index)
                    return seg;
                i++;
            }
            return null;
        }

        private void MoveGraph()
        {
            if (astarPath == null)
                return;
            var grid = astarPath.data.gridGraph;
            var mid = GetSegment(1);
            if (grid != null && mid != null)
            {
                grid.center = new Vector3(mid.StartX + segmentSize.x, 9f, 0f);
                grid.SetDimensions(128, 18, grid.nodeSize);
            }
        }

        private class MapSegment
        {
            public GameObject Root { get; }
            public TilemapChunkGenerator Chunk { get; }
            public ProceduralTaskGenerator Tasks { get; }
            public float StartX { get; }
            private readonly float width;
            public float EndX => StartX + width;

            public MapSegment(GameObject root, TilemapChunkGenerator chunk,
                ProceduralTaskGenerator tasks, float startX, float width)
            {
                Root = root;
                Chunk = chunk;
                Tasks = tasks;
                StartX = startX;
                this.width = width;
            }
        }
    }
}
