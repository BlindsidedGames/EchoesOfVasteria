// In: Scripts/MapGeneration/Chunks/ChunkManager.cs

using System.Collections;
using System.Collections.Generic;
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

            StartCoroutine(PreloadInitialChunks());
        }

        private IEnumerator PreloadInitialChunks()
        {
            isBusy = true;
            for (var i = 0; i < preloadChunks; i++) yield return StartCoroutine(SpawnAndScanSequence());
            isBusy = false;
        }

        private void Update()
        {
            if (isBusy || camera == null || chunkPrefab == null)
                return;

            var camX = camera.transform.position.x;

            if (chunks.Count > 0 && chunks[0] != null &&
                chunks[0].transform.position.x + chunkWidth < camX - chunkWidth)
                StartCoroutine(DestroyAndScanSequence());
            else if (chunks.Count == 0 || camX + chunkWidth > nextX) StartCoroutine(SpawnAndScanSequence());
        }

        private IEnumerator SpawnAndScanSequence()
        {
            isBusy = true;
            try
            {
                var chunk = Instantiate(chunkPrefab, new Vector3(nextX, 0f, 0f), Quaternion.identity, transform);
                chunk.SetTilemaps(waterMap, sandMap, grassMap, decorationMap);
                if (spawnRoot != null)
                    chunk.SetSpawnRoot(spawnRoot);
                RemoveLocalTilemaps(chunk);

                // --- Start of Changed Code ---

                // This now calls the synchronous Generate method.
                // It will generate both terrain AND tasks immediately.
                chunk.Generate(taskController, lastSandDepth, lastGrassDepth);

                // Now that tasks are placed, we wait for the physics colliders for the
                // terrain to be fully generated before we scan for pathfinding.
                yield return new WaitForEndOfFrame();

                // --- End of Changed Code ---

                lastSandDepth = chunk.EndSandDepth;
                lastGrassDepth = chunk.EndGrassDepth;
                nextX += chunkWidth;
                chunks.Add(chunk);

                RelocateAndScan();
            }
            finally
            {
                isBusy = false;
            }
        }

        private IEnumerator DestroyAndScanSequence()
        {
            isBusy = true;
            try
            {
                if (chunks.Count > 0 && chunks[0] != null)
                {
                    var old = chunks[0];
                    chunks.RemoveAt(0);
                    Destroy(old.gameObject);
                }

                yield return new WaitForEndOfFrame();
                RelocateAndScan();
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

        private void RelocateAndScan()
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
    }
}