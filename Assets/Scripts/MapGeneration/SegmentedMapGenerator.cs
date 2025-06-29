using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    ///     Manages a scrolling map made of procedural segments.
    ///     Segments are generated using <see cref="TilemapChunkGenerator"/> and
    ///     <see cref="ProceduralTaskGenerator"/>. When the hero enters the
    ///     rightmost segment the leftmost is removed and a new one spawns on the
    ///     right.
    /// </summary>
    public class SegmentedMapGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private HeroController hero;
        [SerializeField] private TaskController taskController;
        [SerializeField] private AstarPath pathfinder;
        [SerializeField] private int segmentWidth = 64;
        [SerializeField] private int segmentHeight = 18;
        [SerializeField] private float graphYOffset = 9f;

        private readonly Queue<GameObject> segments = new();
        private int currentIndex;
        private bool shifting;

        private void Start()
        {
            StartCoroutine(InitializeSegments());
        }

        private void Update()
        {
            if (shifting || hero == null) return;

            var shiftPoint = (currentIndex + 2) * segmentWidth;
            if (hero.transform.position.x >= shiftPoint)
                StartCoroutine(ShiftSegments());
        }

        private IEnumerator InitializeSegments()
        {
            for (var i = 0; i < 3; i++)
                yield return StartCoroutine(CreateSegment(i * segmentWidth));

            MoveAndRescanGrid();
        }

        private IEnumerator ShiftSegments()
        {
            shifting = true;

            if (segments.Count > 0)
            {
                var old = segments.Dequeue();
                if (old != null) Destroy(old);
            }

            currentIndex++;
            var newX = (currentIndex + 2) * segmentWidth - segmentWidth;
            yield return StartCoroutine(CreateSegment(newX));

            MoveAndRescanGrid();
            shifting = false;
        }

        private IEnumerator CreateSegment(float worldX)
        {
            var seg = Instantiate(segmentPrefab, new Vector3(worldX, 0f, 0f), Quaternion.identity, transform);
            segments.Enqueue(seg);

            var chunk = seg.GetComponent<TilemapChunkGenerator>();
            chunk?.Generate();

            yield return null; // allow colliders to update

            var taskGen = seg.GetComponentInChildren<ProceduralTaskGenerator>();
            if (taskGen != null)
            {
                taskGen.Controller = taskController;
                taskGen.GenerateRuntime();
            }
        }

        private void MoveAndRescanGrid()
        {
            if (pathfinder == null) return;

            var graph = pathfinder.data.gridGraph;
            if (graph == null) return;

            graph.center = new Vector3((currentIndex + 2) * segmentWidth, graphYOffset, 0f);
            graph.SetDimensions(128, segmentHeight, graph.nodeSize);
            pathfinder.Scan();
        }
    }
}
