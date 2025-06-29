// In: Scripts/MapGeneration/Chunks/ChunkManager.cs

using System.Collections;
using System.Collections.Generic;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes.MapGeneration.Chunks
{
    public class ChunkManager : MonoBehaviour
    {
        [SerializeField] private ProceduralChunkGenerator chunkPrefab;
        [Header("Tilemaps")] [SerializeField] private Tilemap waterMap;
        [SerializeField] private Tilemap sandMap;
        [SerializeField] private Tilemap grassMap;
        [SerializeField] private Tilemap decorationMap;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private TaskController taskController;
        [SerializeField] private CinemachineCamera camera;
        [SerializeField] private HeroController hero;
        [SerializeField] private int chunkWidth = 64;
        [SerializeField] private int preloadChunks = 2;

        private readonly List<ProceduralChunkGenerator> chunks = new();
        private int lastSandDepth = 3;
        private int lastGrassDepth = 3;
        private float nextX;
        private bool isBusy;

        private void Start()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();
            if (camera == null && taskController != null)
                camera = taskController.MapCamera;
            if (hero == null && taskController != null)
                hero = taskController.hero;

            StartCoroutine(PreloadInitialChunks());
        }

        private IEnumerator PreloadInitialChunks()
        {
            isBusy = true;
            for (var i = 0; i < preloadChunks; i++)
                yield return StartCoroutine(SpawnChunkSequence(false));
            isBusy = false;
        }

        private void Update()
        {
            if (isBusy || hero == null || chunkPrefab == null)
                return;

            if (chunks.Count == 0)
            {
                StartCoroutine(SpawnChunkSequence(false));
                return;
            }

            var middleChunk = chunks[chunks.Count / 2];
            if (middleChunk == null) return;

            var triggerX = middleChunk.transform.position.x + chunkWidth / 2f;
            if (hero.transform.position.x >= triggerX)
                StartCoroutine(SpawnChunkSequence(true));
        }

        private IEnumerator SpawnChunkSequence(bool destroyOld)
        {
            isBusy = true;
            try
            {
                var chunk = Instantiate(chunkPrefab, new Vector3(nextX, 0f, 0f), Quaternion.identity, transform);
                chunk.SetTilemaps(waterMap, sandMap, grassMap, decorationMap);
                if (spawnRoot != null)
                    chunk.SetSpawnRoot(spawnRoot);
                chunk.SetTaskController(taskController);
                RemoveLocalTilemaps(chunk);

                chunk.GenerateTiles(lastSandDepth, lastGrassDepth);

                yield return new WaitForEndOfFrame();

                lastSandDepth = chunk.EndSandDepth;
                lastGrassDepth = chunk.EndGrassDepth;
                nextX += chunkWidth;
                chunks.Add(chunk);

                UpdatePathfinding();
                chunk.SpawnTasks();

                yield return new WaitForEndOfFrame();

                if (destroyOld)
                    DestroyOffCameraChunk();
            }
            finally
            {
                isBusy = false;
            }
        }

        private void RemoveLocalTilemaps(ProceduralChunkGenerator chunk)
        {
            var maps = chunk.GetComponentsInChildren<Tilemap>();
            foreach (var m in maps)
                if (m != waterMap && m != sandMap && m != grassMap && m != decorationMap)
                    Destroy(m.gameObject);
        }

        private void UpdatePathfinding()
        {
            if (taskController?.Pathfinder == null || chunks.Count == 0)
                return;

            var pf = taskController.Pathfinder;
            var grid = pf.data.gridGraph;
            if (grid == null)
            {
                pf.Scan();
                return;
            }

            var middleChunk = chunks[chunks.Count / 2];
            if (middleChunk == null) return;

            var newGraphCenter = middleChunk.transform.position + new Vector3(chunkWidth / 2f, 0, 0);
            grid.center = new Vector3(newGraphCenter.x, 9f, newGraphCenter.z);

            grid.UpdateTransform();
            pf.Scan();
        }

        private void DestroyOffCameraChunk()
        {
            if (chunks.Count == 0)
                return;

            var first = chunks[0];
            var camX = camera != null ? camera.transform.position.x : hero.transform.position.x;
            if (first.transform.position.x + chunkWidth >= camX - chunkWidth)
                return;

            chunks.RemoveAt(0);
            first.RemoveTasksFromController(taskController);
            Destroy(first.gameObject);
        }
    }
}