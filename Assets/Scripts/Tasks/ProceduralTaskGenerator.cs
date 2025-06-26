using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;
using TimelessEchoes.MapGeneration;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Generates tasks and enemies procedurally within a rectangular area. The order of
    ///     generated tasks is determined by their world X position from left to
    ///     right. Enemies are spawned but not added to the task list.
    /// </summary>
    [RequireComponent(typeof(TaskController))]
    public class ProceduralTaskGenerator : MonoBehaviour
    {
        [TabGroup("Settings", "Area")] [SerializeField]
        private float minX;

        [TabGroup("Settings", "Area")] [SerializeField]
        private float maxX = 990f;

        [TabGroup("Settings", "Area")] [SerializeField]
        private float height = 18f;

        [TabGroup("Settings", "Area")] [SerializeField]
        private float density = 0.1f;

        [TabGroup("Settings", "Generation")] [SerializeField]
        private LayerMask blockingMask;

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> enemies = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> otherTasks = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> waterTasks = new();

        [TabGroup("Settings", "References")] [SerializeField]
        private Tilemap waterMap;
        [TabGroup("Settings", "References")] [SerializeField]
        private Tilemap sandMap;

        private readonly List<GameObject> generatedObjects = new();

        private TaskController controller;

        private void Awake()
        {
            controller = GetComponent<TaskController>();
            EnsureTilemaps();
        }

        private void EnsureTilemaps()
        {
            if (waterMap != null && sandMap != null)
                return;

            var chunk = GetComponent<TilemapChunkGenerator>();
            if (chunk != null)
            {
                if (waterMap == null)
                    waterMap = chunk.WaterMap;
                if (sandMap == null)
                    sandMap = chunk.SandMap;
            }

            if (waterMap == null || sandMap == null)
            {
                var maps = GetComponentsInChildren<Tilemap>();
                foreach (var m in maps)
                {
                    if (waterMap == null && m.gameObject.name.Contains("Blocking"))
                        waterMap = m;
                    else if (sandMap == null && m.gameObject.name == "BG")
                        sandMap = m;
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + new Vector3((minX + maxX) * 0.5f, height * 0.5f, 0f);
            var size = new Vector3(maxX - minX, height, 0f);
            Gizmos.DrawWireCube(center, size);
        }

        private void ClearSpawnedObjects()
        {
            foreach (var obj in generatedObjects)
            {
                if (obj == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(obj);
                else
#endif
                    Destroy(obj);
            }

            generatedObjects.Clear();
        }

        /// <summary>
        ///     Clear all spawned task objects and remove them from the controller.
        /// </summary>
        public void Clear()
        {
            if (controller == null)
                controller = GetComponent<TaskController>();

            ClearSpawnedObjects();
            controller?.ClearTaskObjects();
        }

        /// <summary>
        ///     Generate and assign tasks based on the configured settings.
        /// </summary>
        [Button]
        public void Generate()
        {
            if (controller == null)
                controller = GetComponent<TaskController>();
            if (controller == null)
                return;

            ClearSpawnedObjects();
            controller.ClearTaskObjects();

            var count = Mathf.RoundToInt((maxX - minX) * density);
            if (count <= 0)
                return;

            // This list now only stores objects that will become tasks.
            var spawnedTasks = new List<(float x, MonoBehaviour obj)>();
            for (var i = 0; i < count; i++)
            {
                var localX = Random.Range(minX, maxX);
                var progress = Mathf.InverseLerp(minX, maxX, localX);

                var allowWater = TryGetWaterEdge(localX, out var waterPos);
                var (entry, isEnemy, isWaterTask) = PickEntry(progress, allowWater);
                if (entry == null || entry.prefab == null)
                    continue;

                var pos = isWaterTask ? waterPos : RandomPositionAtX(localX);

                var attempts = 0;
                while (attempts < 5 && (HasBlockingCollider(pos) || IsBlockedAhead(pos)))
                {
                    if (isWaterTask)
                    {
                        localX = Random.Range(minX, maxX);
                        if (!TryGetWaterEdge(localX, out waterPos))
                            break;
                        pos = waterPos;
                    }
                    else
                    {
                        pos = RandomPositionAtX(localX);
                    }
                    attempts++;
                }

                if (HasBlockingCollider(pos) || IsBlockedAhead(pos))
                    continue;

                var obj = Instantiate(entry.prefab, pos, Quaternion.identity, transform);
                generatedObjects.Add(obj);

                if (!isEnemy)
                {
                    var mono = obj.GetComponent<MonoBehaviour>();
                    if (mono != null)
                        spawnedTasks.Add((pos.x, mono));
                }
            }

            // Sort and add the collected tasks to the controller.
            spawnedTasks.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (var pair in spawnedTasks)
                controller.AddTaskObject(pair.obj);

            controller.ResetTasks();
        }

        private Vector3 RandomPosition()
        {
            var x = Random.Range(minX, maxX);
            var y = Random.Range(0f, height);

            var worldX = transform.position.x + x;
            var worldY = transform.position.y + y;

            return new Vector3(worldX, worldY, 0f);
        }

        private Vector3 RandomPositionAtX(float localX)
        {
            var y = Random.Range(0f, height);
            var worldX = transform.position.x + localX;
            var worldY = transform.position.y + y;
            return new Vector3(worldX, worldY, 0f);
        }

        private bool TryGetWaterEdge(float localX, out Vector3 position)
        {
            position = Vector3.zero;
            EnsureTilemaps();
            if (waterMap == null || sandMap == null)
                return false;

            var worldX = transform.position.x + localX;
            var cell = waterMap.WorldToCell(new Vector3(worldX, transform.position.y, 0f));

            int maxY = Mathf.Max(waterMap.cellBounds.yMax, sandMap.cellBounds.yMax);
            int minY = Mathf.Min(waterMap.cellBounds.yMin, sandMap.cellBounds.yMin) - 1;

            for (int y = maxY; y >= minY; y--)
            {
                var water = waterMap.HasTile(new Vector3Int(cell.x, y, 0));
                var sand = sandMap.HasTile(new Vector3Int(cell.x, y + 1, 0));
                if (water && sand)
                {
                    position = waterMap.GetCellCenterWorld(new Vector3Int(cell.x, y, 0));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Determine if the exact XY position contains a collider on the blocking mask.
        /// </summary>
        private bool HasBlockingCollider(Vector2 point)
        {
            return Physics2D.OverlapPoint(point, blockingMask) != null;
        }

        /// <summary>
        ///     Check if there is a blocking collider directly in front of the given position.
        /// </summary>
        private bool IsBlockedAhead(Vector3 pos)
        {
            const float checkRadius = 0.4f;
            var hits = Physics2D.OverlapCircleAll(pos, checkRadius, blockingMask);
            foreach (var h in hits)
                if (h.bounds.min.y > pos.y - 0.1f)
                    return true;
            return false;
        }

        /// <summary>
        ///     Picks an entry from the available lists and identifies if it's an enemy.
        /// </summary>
        /// <param name="progress">The normalized position in the generation area.</param>
        /// <returns>A tuple containing the chosen WeightedSpawn and a boolean that is true if it's an enemy.</returns>
        private (WeightedSpawn entry, bool isEnemy, bool isWaterTask) PickEntry(float progress, bool allowWaterTasks)
        {
            // Calculate total weight for each category
            var enemyTotalWeight = 0f;
            foreach (var e in enemies)
                enemyTotalWeight += e.GetWeight(progress);

            var otherTasksTotalWeight = 0f;
            foreach (var t in otherTasks)
                otherTasksTotalWeight += t.GetWeight(progress);

            var waterTasksTotalWeight = 0f;
            if (allowWaterTasks)
                foreach (var w in waterTasks)
                    waterTasksTotalWeight += w.GetWeight(progress);

            var totalWeight = enemyTotalWeight + otherTasksTotalWeight + waterTasksTotalWeight;
            if (totalWeight <= 0f)
                return (null, false, false);

            var r = Random.value * totalWeight;

            // Check if we should spawn an enemy
            if (r < enemyTotalWeight)
            {
                foreach (var e in enemies)
                {
                    r -= e.GetWeight(progress);
                    if (r <= 0f)
                        return (e, true, false); // Return the entry and mark it as an enemy.
                }
            }
            else
            {
                // Adjust random value for the next category
                r -= enemyTotalWeight;

                if (allowWaterTasks && r < waterTasksTotalWeight)
                {
                    foreach (var w in waterTasks)
                    {
                        r -= w.GetWeight(progress);
                        if (r <= 0f)
                            return (w, false, true);
                    }
                }
                else
                {
                    r -= waterTasksTotalWeight;

                    // Spawn another task
                    foreach (var t in otherTasks)
                    {
                        r -= t.GetWeight(progress);
                        if (r <= 0f)
                            return (t, false, false); // Return the entry and mark it as NOT an enemy.
                    }
                }
            }

            return (null, false, false); // Should not be reached if weights are positive.
        }

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedSpawn
        {
            [Required] public GameObject prefab;

            [MinValue(0)] public float weight = 1f;

            [Range(0f, 1f)] public float minProgress;

            [Range(0f, 1f)] public float maxProgress = 1f;

            public float GetWeight(float progress)
            {
                if (prefab == null) return 0f;
                if (progress < minProgress || progress > maxProgress)
                    return 0f;
                return Mathf.Max(0f, weight);
            }
        }
    }
}