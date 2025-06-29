// In: Scripts/MapGeneration/Chunks/ChunkManager.cs

using System.Collections;
using System.Collections.Generic;
using TimelessEchoes.Tasks;
using TimelessEchoes.Hero;
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
        private HeroController hero;
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
            if (taskController != null)
                hero = taskController.hero;

            StartCoroutine(PreloadInitialChunks());
        }

        private IEnumerator PreloadInitialChunks()
        {
            isBusy = true;
            for (var i = 0; i < preloadChunks; i++)
                yield return StartCoroutine(ChunkSequence());
            isBusy = false;
        }

        private void Update()
        {
            if (isBusy || hero == null || chunkPrefab == null)
                return;

            if (chunks.Count == 0)
            {
                StartCoroutine(ChunkSequence());
                return;
            }

            var heroX = hero.transform.position.x;
            var middleChunk = chunks[chunks.Count / 2];
            var middleCenter = middleChunk.transform.position.x + chunkWidth * 0.5f;

            if (heroX >= middleCenter)
                StartCoroutine(ChunkSequence());
        }

        private IEnumerator ChunkSequence()
        {
            isBusy = true;
            try
            {
                var chunk = Instantiate(chunkPrefab, new Vector3(nextX, 0f, 0f), Quaternion.identity, transform);
                chunk.SetTilemaps(waterMap, sandMap, grassMap, decorationMap);
                if (spawnRoot != null)
                    chunk.SetSpawnRoot(spawnRoot);
                RemoveLocalTilemaps(chunk);

                chunk.GenerateTerrainOnly(lastSandDepth, lastGrassDepth);
                yield return null;

                lastSandDepth = chunk.EndSandDepth;
                lastGrassDepth = chunk.EndGrassDepth;
                nextX += chunkWidth;
                chunks.Add(chunk);

                RelocateGrid();
                taskController?.Pathfinder?.Scan();

                chunk.GenerateTasks(taskController);

                yield return null;

                RemoveOldChunk();
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

        private void RelocateGrid()
        {
            if (taskController?.Pathfinder == null || chunks.Count == 0)
                return;

            var pf = taskController.Pathfinder;
            var grid = pf.data.gridGraph;
            if (grid == null)
                return;

            var middleChunk = chunks[chunks.Count / 2];
            if (middleChunk == null) return;

            var newGraphCenter = middleChunk.transform.position + new Vector3(chunkWidth / 2f, 0, 0);
            grid.center = new Vector3(newGraphCenter.x, 9f, newGraphCenter.z);

            grid.UpdateTransform();
        }

        private void RemoveOldChunk()
        {
            if (camera == null || chunks.Count == 0)
                return;

            var first = chunks[0];
            if (first == null)
                return;

            if (first.transform.position.x + chunkWidth < camera.transform.position.x - chunkWidth)
            {
                chunks.RemoveAt(0);

                if (taskController != null)
                    foreach (var obj in first.RuntimeTasks)
                        taskController.RemoveTaskObject(obj);

                Destroy(first.gameObject);
            }
        }
    }
}