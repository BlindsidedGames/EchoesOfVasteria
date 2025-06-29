using System.Collections.Generic;
using TimelessEchoes.Tasks;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
using UnityEngine;

namespace TimelessEchoes.MapGeneration.Chunks
{
    /// <summary>
    /// Manages spawning and removal of procedural chunks in front of the camera.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [SerializeField] private ProceduralChunkGenerator chunkPrefab;
        [Header("Tilemaps")]
        [SerializeField] private Tilemap waterMap;
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

        private void Start()
        {
            if (taskController == null)
                taskController = GetComponent<TaskController>();
            if (camera == null && taskController != null)
                camera = taskController.MapCamera;

            for (var i = 0; i < preloadChunks; i++)
                SpawnChunk();

            RelocateAndScan();
        }

        private void Update()
        {
            if (camera == null || chunkPrefab == null)
                return;

            var camX = camera.transform.position.x;
            var last = chunks.Count > 0 ? chunks[chunks.Count - 1] : null;
            if (last == null || camX + 32f > last.transform.position.x + chunkWidth)
                SpawnChunk();

            var removed = false;
            while (chunks.Count > 0 && chunks[0].transform.position.x + chunkWidth < camX - chunkWidth)
            {
                var old = chunks[0];
                chunks.RemoveAt(0);
                Destroy(old.gameObject);
                removed = true;
            }

            if (removed)
                RelocateAndScan();
        }

        private void SpawnChunk()
        {
            var chunk = Instantiate(chunkPrefab, new Vector3(nextX, 0f, 0f), Quaternion.identity, transform);
            chunk.SetTilemaps(waterMap, sandMap, grassMap, decorationMap);
            if (spawnRoot != null)
                chunk.SetSpawnRoot(spawnRoot);
            RemoveLocalTilemaps(chunk);
            chunk.Generate(taskController, lastSandDepth, lastGrassDepth);
            lastSandDepth = chunk.EndSandDepth;
            lastGrassDepth = chunk.EndGrassDepth;
            nextX += chunkWidth;
            chunks.Add(chunk);
            RelocateAndScan();
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
            if (taskController?.Pathfinder == null)
                return;

            var pf = taskController.Pathfinder;
            var grid = pf.data.gridGraph;
            if (grid == null)
            {
                pf.Scan();
                return;
            }

            if (camera != null)
            {
                var center = grid.center;
                center.x = camera.transform.position.x;
                grid.center = center;
                grid.UpdateTransform();
            }

            pf.Scan();
        }
    }
}
