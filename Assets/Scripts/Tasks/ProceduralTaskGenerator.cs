using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TimelessEchoes.MapGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;
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

        [TabGroup("Settings", "Generation")] [SerializeField] [MinValue(0)]
        private float otherTaskEdgeOffset = 1f;

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> enemies = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> otherTasks = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> waterTasks = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> grassTasks = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private bool allowGrassEdge = false;

        [TabGroup("Settings", "Generation")] [SerializeField] [MinValue(0)]
        private int grassTopBuffer = 2;

        [TabGroup("Settings", "References")] [SerializeField]
        private Tilemap waterMap;

        [TabGroup("Settings", "References")] [SerializeField]
        private Tilemap sandMap;

        [TabGroup("Settings", "References")] [SerializeField]
        private Tilemap grassMap;

        private readonly List<GameObject> generatedObjects = new();

        /// <summary>
        /// Optional parent for spawned task objects.
        /// </summary>
        public Transform SpawnParent { get; set; }

        private TaskController controller;

        /// <summary>
        /// Optional externally assigned TaskController. If null the component
        /// will attempt to locate one on the same GameObject.
        /// </summary>
        public TaskController Controller
        {
            get => controller;
            set => controller = value;
        }

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<TaskController>();
            EnsureTilemaps();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + new Vector3((minX + maxX) * 0.5f, height * 0.5f, 0f);
            var size = new Vector3(maxX - minX, height, 0f);
            Gizmos.DrawWireCube(center, size);
        }

        private void EnsureTilemaps()
        {
            if (waterMap != null && sandMap != null && grassMap != null)
                return;

            var chunk = GetComponent<TilemapChunkGenerator>();
            if (chunk != null)
            {
                if (waterMap == null)
                    waterMap = chunk.WaterMap;
                if (sandMap == null)
                    sandMap = chunk.SandMap;
                if (grassMap == null)
                    grassMap = chunk.GrassMap;
            }

            if (waterMap == null || sandMap == null || grassMap == null)
            {
                var maps = GetComponentsInChildren<Tilemap>();
                foreach (var m in maps)
                    if (waterMap == null && m.gameObject.name.Contains("Blocking"))
                        waterMap = m;
                    else if (sandMap == null && m.gameObject.name == "BG")
                        sandMap = m;
                    else if (grassMap == null && m.gameObject.name == "BG_Grass")
                        grassMap = m;
            }
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
            GenerateInternal(minX, maxX, transform, true);
        }

        public void GenerateSegment(float localMinX, float localMaxX, Transform parent)
        {
            GenerateInternal(localMinX, localMaxX, parent, false);
        }

        private void GenerateInternal(float localMinX, float localMaxX, Transform parent, bool clearExisting)
        {
            if (controller == null)
                controller = GetComponent<TaskController>();
            if (controller == null)
                return;

            if (clearExisting)
            {
                ClearSpawnedObjects();
                controller.ClearTaskObjects();
            }

            var count = Mathf.RoundToInt((localMaxX - localMinX) * density);
            if (count <= 0)
                return;

            var spawnedTasks = new List<(float x, MonoBehaviour obj)>();
            for (var i = 0; i < count; i++)
            {
                var localX = Random.Range(localMinX, localMaxX);
                var worldX = transform.position.x + localX;

                var allowWater = TryGetWaterEdge(localX, out var waterPos);
                var allowGrass = TryGetGrassPosition(localX, allowGrassEdge, out var grassPos);
                var (entry, isEnemy, isWaterTask, isGrassTask) = PickEntry(worldX, allowWater, allowGrass);
                if (entry == null || entry.prefab == null)
                    continue;

                var pos = isWaterTask ? waterPos : isGrassTask ? grassPos : RandomPositionAtX(localX);

                var attempts = 0;
                var positionIsValid = false;
                while (attempts < 5)
                {
                    if (isWaterTask)
                    {
                        positionIsValid = true;
                        break;
                    }

                    var isWaterTile = !isWaterTask && IsWaterTile(pos);
                    var isObstructed = HasBlockingCollider(pos) || IsBlockedAhead(pos) || isWaterTile;
                    var onWaterEdge = !isEnemy && allowWater &&
                                      Mathf.Abs(pos.y - waterPos.y) < otherTaskEdgeOffset;

                    if (!isObstructed && !onWaterEdge)
                    {
                        positionIsValid = true;
                        break;
                    }
                    if (isGrassTask)
                    {
                        if (!TryGetGrassPosition(localX, allowGrassEdge, out pos))
                            break;
                    }
                    else
                    {
                        pos = RandomPositionAtX(localX);
                    }
                    attempts++;
                }

                if (!positionIsValid)
                    continue;

                var parentTf = parent != null ? parent : SpawnParent != null ? SpawnParent : transform;
                var obj = Instantiate(entry.prefab, pos, Quaternion.identity, parentTf);
                generatedObjects.Add(obj);

                if (!isEnemy)
                {
                    var mono = obj.GetComponent<MonoBehaviour>();
                    if (mono != null)
                    {
                        if (clearExisting)
                            spawnedTasks.Add((pos.x, mono));
                        else
                            controller.AddRuntimeTaskObject(mono);
                    }
                }
            }

            if (clearExisting)
            {
                spawnedTasks.Sort((a, b) => a.x.CompareTo(b.x));
                foreach (var pair in spawnedTasks)
                    controller.AddTaskObject(pair.obj);

                controller.ResetTasks();
            }
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

            var maxY = Mathf.Max(waterMap.cellBounds.yMax, sandMap.cellBounds.yMax);
            var minY = Mathf.Min(waterMap.cellBounds.yMin, sandMap.cellBounds.yMin) - 1;

            for (var y = maxY; y >= minY; y--)
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
        ///     Attempt to find a grass tile at the given X coordinate.
        ///     Edge detection uses the presence of neighboring grass tiles
        ///     instead of checking the sand tilemap to avoid false positives
        ///     when sand extends behind the grass.
        /// </summary>
        /// <param name="localX">Local X position to sample.</param>
        /// <param name="includeEdge">If true, allow positions on the edge of the grass area.</param>
        /// <param name="position">The world position of a valid grass tile.</param>
        /// <returns>True if a valid tile was found.</returns>
        private bool TryGetGrassPosition(float localX, bool includeEdge, out Vector3 position)
        {
            position = Vector3.zero;
            EnsureTilemaps();
            if (grassMap == null)
                return false;

            var worldX = transform.position.x + localX;
            var cell = grassMap.WorldToCell(new Vector3(worldX, transform.position.y, 0f));

            var maxY = Mathf.Clamp(grassMap.cellBounds.yMax - grassTopBuffer, grassMap.cellBounds.yMin, grassMap.cellBounds.yMax);
            var minY = grassMap.cellBounds.yMin - 1;

            var validYs = new List<int>();
            for (var y = maxY; y >= minY; y--)
            {
                if (!grassMap.HasTile(new Vector3Int(cell.x, y, 0)))
                    continue;

                var below = grassMap.HasTile(new Vector3Int(cell.x, y - 1, 0));
                var left = grassMap.HasTile(new Vector3Int(cell.x - 1, y, 0));
                var right = grassMap.HasTile(new Vector3Int(cell.x + 1, y, 0));
                var belowLeft = grassMap.HasTile(new Vector3Int(cell.x - 1, y - 1, 0));
                var belowRight = grassMap.HasTile(new Vector3Int(cell.x + 1, y - 1, 0));
                var above = grassMap.HasTile(new Vector3Int(cell.x, y + 1, 0));

                var isCurrentTileSideEdge = !left || !right;
                var isGrassGroundLevel = !below;
                var isTopEdge = !above;
                var isTileBelowGroundLevel = !below;
                var isTileBelowSideEdge = !belowLeft || !belowRight;
                var isTileBelowEdge = isTileBelowGroundLevel || isTileBelowSideEdge;

                var isEdge = isCurrentTileSideEdge || isTileBelowEdge || isGrassGroundLevel || isTopEdge;
                if (!includeEdge && isEdge)
                    continue;

                validYs.Add(y);
            }

            if (validYs.Count == 0)
                return false;

            var idx = Random.Range(0, validYs.Count);
            position = grassMap.GetCellCenterWorld(new Vector3Int(cell.x, validYs[idx], 0));
            return true;
        }

        private bool IsWaterTile(Vector3 worldPos)
        {
            EnsureTilemaps();
            if (waterMap == null)
                return false;

            var cell = waterMap.WorldToCell(worldPos);
            return waterMap.HasTile(cell);
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
        /// <param name="worldX">The world X position of the spawn attempt.</param>
        /// <returns>A tuple containing the chosen WeightedSpawn and a boolean that is true if it's an enemy.</returns>
        private (WeightedSpawn entry, bool isEnemy, bool isWaterTask, bool isGrassTask) PickEntry(float worldX, bool allowWaterTasks, bool allowGrassTasks)
        {
            var enemyTotalWeight = 0f;
            foreach (var e in enemies)
                enemyTotalWeight += e.GetWeight(worldX);

            var otherTasksTotalWeight = 0f;
            foreach (var t in otherTasks)
                otherTasksTotalWeight += t.GetWeight(worldX);

            var waterTasksTotalWeight = 0f;
            if (allowWaterTasks)
                foreach (var w in waterTasks)
                    waterTasksTotalWeight += w.GetWeight(worldX);

            var grassTasksTotalWeight = 0f;
            if (allowGrassTasks)
                foreach (var g in grassTasks)
                    grassTasksTotalWeight += g.GetWeight(worldX);

            var totalWeight = enemyTotalWeight + otherTasksTotalWeight + waterTasksTotalWeight + grassTasksTotalWeight;
            if (totalWeight <= 0f)
                return (null, false, false, false);

            var r = Random.value * totalWeight;

            if (r < enemyTotalWeight)
            {
                foreach (var e in enemies)
                {
                    r -= e.GetWeight(worldX);
                    if (r <= 0f)
                        return (e, true, false, false);
                }
            }
            else
            {
                r -= enemyTotalWeight;

                if (allowWaterTasks && r < waterTasksTotalWeight)
                {
                    foreach (var w in waterTasks)
                    {
                        r -= w.GetWeight(worldX);
                        if (r <= 0f)
                            return (w, false, true, false);
                    }
                }
                else
                {
                    r -= waterTasksTotalWeight;

                    if (allowGrassTasks && r < grassTasksTotalWeight)
                    {
                        foreach (var g in grassTasks)
                        {
                            r -= g.GetWeight(worldX);
                            if (r <= 0f)
                                return (g, false, false, true);
                        }
                    }
                    else
                    {
                        r -= grassTasksTotalWeight;

                        foreach (var t in otherTasks)
                        {
                            r -= t.GetWeight(worldX);
                            if (r <= 0f)
                                return (t, false, false, false);
                        }
                    }
                }
            }

            return (null, false, false, false);
        }

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedSpawn
        {
            [Required] public GameObject prefab;

            [MinValue(0)] public float weight = 1f;

            public float minX;

            public float maxX = float.PositiveInfinity;

            public float GetWeight(float worldX)
            {
                if (prefab == null) return 0f;
                if (worldX < minX || worldX > maxX)
                    return 0f;
                return Mathf.Max(0f, weight);
            }
        }
    }
}