using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Blindsided.SaveData;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles;
using Random = UnityEngine.Random;
using static Blindsided.Oracle;

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
        [SerializeField]
        private MapGenerationConfig config;
        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float minX;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float maxX = 990f;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float height = 18f;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float taskDensity = 0.1f;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float waterTaskDensity = 0.1f;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float sandTaskDensity = 0.1f;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float grassTaskDensity = 0.1f;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float enemyDensity = 0.1f;

        [TabGroup("Settings", "Generation")] [SerializeField]
        [HideInInspector]
        private LayerMask blockingMask;

        [TabGroup("Settings", "Generation")] [SerializeField] [MinValue(0)]
        [HideInInspector]
        private float otherTaskEdgeOffset = 1f;

        [TabGroup("Settings", "Generation")] [SerializeField]
        [HideInInspector]
        private List<WeightedSpawn> enemies = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        [HideInInspector]
        private List<WeightedTaskCategory> taskCategories = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        [HideInInspector]
        private List<MapGenerationConfig.ProceduralTaskSettings.NpcSpawnEntry> npcTasks = new();

        [TabGroup("Settings", "Generation")] [SerializeField] [MinValue(0f)]
        [HideInInspector]
        private float minTaskDistance = 1.5f;

        [TabGroup("Settings", "References")] [SerializeField]
        [HideInInspector]
        private Tilemap terrainMap;

        [TabGroup("Settings", "References")] [SerializeField]
        [HideInInspector]
        private BetterRuleTile waterTile;

        [TabGroup("Settings", "References")] [SerializeField]
        [HideInInspector]
        private BetterRuleTile sandTile;

        [TabGroup("Settings", "References")] [SerializeField]
        [HideInInspector]
        private BetterRuleTile grassTile;

        private readonly List<GameObject> generatedObjects = new();

        /// <summary>
        /// Optional parent for spawned task objects.
        /// </summary>
        public Transform SpawnParent { get; set; }

        /// <summary>
        /// Minimum world X value allowed for spawned tasks.
        /// </summary>
        public float MinX => minX;

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
            ApplyConfig();

            var chunk = GetComponent<TilemapChunkGenerator>();
            chunk?.AssignTilemaps(this);

            EnsureTilemaps();
        }

        private void ApplyConfig()
        {
            if (config == null) return;

            minX = config.taskGeneratorSettings.minX;
            height = config.taskGeneratorSettings.height;
            taskDensity = config.taskGeneratorSettings.taskDensity;
            waterTaskDensity = config.taskGeneratorSettings.waterTaskDensity;
            sandTaskDensity = config.taskGeneratorSettings.sandTaskDensity;
            grassTaskDensity = config.taskGeneratorSettings.grassTaskDensity;
            enemyDensity = config.taskGeneratorSettings.enemyDensity;
            blockingMask = config.taskGeneratorSettings.blockingMask;
            otherTaskEdgeOffset = config.taskGeneratorSettings.otherTaskEdgeOffset;
            enemies = config.taskGeneratorSettings.enemies;

            taskCategories = new List<WeightedTaskCategory>
            {
                config.taskGeneratorSettings.woodcutting,
                config.taskGeneratorSettings.mining,
                config.taskGeneratorSettings.farming,
                config.taskGeneratorSettings.fishing,
                config.taskGeneratorSettings.looting
            };

            npcTasks = config.taskGeneratorSettings.npcTasks;
            minTaskDistance = config.taskGeneratorSettings.minTaskDistance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + new Vector3((minX + maxX) * 0.5f, height * 0.5f, 0f);
            var size = new Vector3(maxX - minX, height, 0f);
            Gizmos.DrawWireCube(center, size);
        }

        internal void SetTilemapReferences(Tilemap map, BetterRuleTile water, BetterRuleTile sand, BetterRuleTile grass)
        {
            terrainMap = map;
            waterTile = water;
            sandTile = sand;
            grassTile = grass;
        }

        private void EnsureTilemaps()
        {
            if (terrainMap != null && waterTile != null && sandTile != null && grassTile != null)
                return;

            if (terrainMap == null)
            {
                var maps = GetComponentsInChildren<Tilemap>();
                foreach (var m in maps)
                    if (terrainMap == null && (m.gameObject.name.Contains("Terrain") || m.gameObject.name.Contains("Water")))
                        terrainMap = m;
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

            var baseTaskCount = Mathf.RoundToInt((localMaxX - localMinX) * taskDensity);
            var waterCount = Mathf.RoundToInt((localMaxX - localMinX) * waterTaskDensity);
            var sandCount = Mathf.RoundToInt((localMaxX - localMinX) * sandTaskDensity);
            var grassCount = Mathf.RoundToInt((localMaxX - localMinX) * grassTaskDensity);
            var enemyCount = Mathf.RoundToInt((localMaxX - localMinX) * enemyDensity);
            if (baseTaskCount + waterCount + sandCount + grassCount <= 0 && enemyCount <= 0)
                return;

            var spawnedTasks = new List<(Vector3 pos, MonoBehaviour obj)>();
            var taskPositions = new List<Vector3>();
            var taskMap = new Dictionary<Vector3, MonoBehaviour>();
            for (var i = 0; i < waterCount; i++)
            {
                for (var a = 0; a < 10; a++)
                    if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, true, false, false,
                            spawnedTasks, taskPositions, taskMap))
                        break;
            }

            for (var i = 0; i < sandCount; i++)
            {
                for (var a = 0; a < 10; a++)
                    if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, false, true, false,
                            spawnedTasks, taskPositions, taskMap))
                        break;
            }

            for (var i = 0; i < grassCount; i++)
            {
                for (var a = 0; a < 10; a++)
                    if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, false, false, true,
                            spawnedTasks, taskPositions, taskMap))
                        break;
            }

            for (var i = 0; i < baseTaskCount; i++)
            {
                for (var a = 0; a < 10; a++)
                    if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, false, false, false,
                            spawnedTasks, taskPositions, taskMap))
                        break;
            }

            for (var i = 0; i < enemyCount; i++)
            {
                var localX = Random.Range(localMinX, localMaxX);
                var worldX = transform.position.x + localX;

                var allowWater = TryGetWaterSpot(localX, out var waterPos);
                var allowSand = TryGetSandSpot(localX, 0, out var sandPos);
                var allowGrass = TryGetGrassPosition(localX, 0, out var _);
                var entry = PickEnemyEntry(worldX, allowWater, allowGrass, allowSand);
                if (entry == null || entry.prefab == null)
                    continue;

                Vector3 pos;
                if (entry.spawnOnWater && allowWater)
                {
                    pos = waterPos;
                }
                else
                {
                    Vector3 sandCandidate = Vector3.zero;
                    Vector3 grassCandidate = Vector3.zero;
                    var sandValid = entry.spawnOnSand && allowSand &&
                                   TryGetSandSpot(localX, entry.topBuffer, out sandCandidate);
                    var grassValid = entry.spawnOnGrass && allowGrass &&
                                     TryGetGrassPosition(localX, entry.topBuffer, out grassCandidate);

                    if (sandValid && grassValid)
                    {
                        pos = Random.value < 0.5f ? sandCandidate : grassCandidate;
                    }
                    else if (sandValid)
                    {
                        pos = sandCandidate;
                    }
                    else if (grassValid)
                    {
                        pos = grassCandidate;
                    }
                    else
                    {
                        pos = RandomPositionAtX(localX);
                    }
                }

                var attempts = 0;
                var positionIsValid = false;
                while (attempts < 5)
                {
                    if (entry.spawnOnWater || entry.spawnOnSand)
                    {
                        positionIsValid = true;
                        break;
                    }

                    var isWaterTile = !entry.spawnOnWater && IsWaterTile(pos);
                    var isObstructed = HasBlockingCollider(pos) || IsBlockedAhead(pos) || isWaterTile;
                    var onWaterEdge = allowWater && Mathf.Abs(pos.y - waterPos.y) < otherTaskEdgeOffset;

                    if (!isObstructed && !onWaterEdge)
                    {
                        positionIsValid = true;
                        break;
                    }
                    pos = RandomPositionAtX(localX);
                    attempts++;
                }

                if (!positionIsValid)
                    continue;

                var parentTf = parent != null ? parent : SpawnParent != null ? SpawnParent : transform;
                var obj = Instantiate(entry.prefab, pos, Quaternion.identity, parentTf);
                generatedObjects.Add(obj.gameObject);
            }

            foreach (var npc in npcTasks)
            {
                if (npc == null || npc.prefab == null) continue;
                if (npc.localX < localMinX || npc.localX >= localMaxX) continue;
                if (npc.spawnOnlyOnce && Blindsided.SaveData.StaticReferences.CompletedNpcTasks.Contains(npc.id))
                    continue;
                if (!string.IsNullOrEmpty(npc.id) && Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Contains(npc.id))
                    continue;

                Vector3 pos;
                if (npc.spawnOnWater && TryGetWaterSpot(npc.localX, out pos)) { }
                else if (npc.spawnOnSand && TryGetSandSpot(npc.localX, npc.topBuffer, out pos)) { }
                else if (npc.spawnOnGrass && TryGetGrassPosition(npc.localX, npc.topBuffer, out pos)) { }
                else
                    pos = RandomPositionAtX(npc.localX);

                for (int i = taskPositions.Count - 1; i >= 0; i--)
                {
                    var existing = taskPositions[i];
                    if (Vector3.Distance(existing, pos) < minTaskDistance)
                    {
                        if (taskMap.TryGetValue(existing, out var objToRemove))
                        {
                            if (clearExisting)
                                spawnedTasks.RemoveAll(t => t.obj == objToRemove);
                            else
                                controller.RemoveTaskObject(objToRemove);
                            generatedObjects.Remove(objToRemove.gameObject);
#if UNITY_EDITOR
                            if (!Application.isPlaying)
                                DestroyImmediate(objToRemove.gameObject);
                            else
#endif
                                Destroy(objToRemove.gameObject);
                            taskMap.Remove(existing);
                        }
                        taskPositions.RemoveAt(i);
                    }
                }

                var parentTf = parent != null ? parent : SpawnParent != null ? SpawnParent : transform;
                var obj = Instantiate(npc.prefab, pos, Quaternion.identity, parentTf);
                generatedObjects.Add(obj);
                var mono = obj.GetComponent<MonoBehaviour>();
                if (mono != null)
                {
                    if (clearExisting)
                        spawnedTasks.Add((pos, mono));
                    else
                        controller.AddRuntimeTaskObject(mono);
                    taskPositions.Add(pos);
                    taskMap[pos] = mono;
                }
            }

            if (clearExisting)
            {
                spawnedTasks.Sort((a, b) => a.pos.x.CompareTo(b.pos.x));
                foreach (var pair in spawnedTasks)
                    controller.AddTaskObject(pair.obj);

                controller.ResetTasks();
            }
        }

        private bool TrySpawnTask(float localX, Transform parent, bool clearExisting,
            bool requireWaterTask, bool requireSandTask, bool requireGrassTask,
            List<(Vector3 pos, MonoBehaviour obj)> spawnedTasks, List<Vector3> taskPositions,
            Dictionary<Vector3, MonoBehaviour> taskMap)
        {
            var worldX = transform.position.x + localX;

            var allowWater = TryGetWaterSpot(localX, out var waterPos);
            var allowSand = TryGetSandSpot(localX, 0, out var sandPos);
            var allowGrass = TryGetGrassPosition(localX, 0, out var _);
            var (entry, isWaterTask, isGrassTask, isSandTask) =
                PickTaskEntry(worldX, allowWater, allowGrass, allowSand);
            if (entry == null || entry.prefab == null)
                return false;

            if (requireWaterTask && !isWaterTask) return false;
            if (requireSandTask && !isSandTask) return false;
            if (requireGrassTask && !isGrassTask) return false;

            Vector3 pos;
            if (isWaterTask)
                pos = waterPos;
            else if (isSandTask)
            {
                if (!TryGetSandSpot(localX, entry.topBuffer, out pos))
                    return false;
            }
            else if (isGrassTask)
            {
                if (!TryGetGrassPosition(localX, entry.topBuffer, out pos))
                    return false;
            }
            else
                pos = RandomPositionAtX(localX);

            var attempts = 0;
            var positionIsValid = false;
            while (attempts < 5)
            {
                if (isWaterTask || isSandTask)
                {
                    positionIsValid = true;
                    break;
                }

                var isWaterTile = !isWaterTask && IsWaterTile(pos);
                var isObstructed = HasBlockingCollider(pos) || IsBlockedAhead(pos) || isWaterTile;
                var onWaterEdge = allowWater && Mathf.Abs(pos.y - waterPos.y) < otherTaskEdgeOffset;

                if (!isObstructed && !onWaterEdge)
                {
                    positionIsValid = true;
                    break;
                }
                if (isGrassTask)
                {
                    if (!TryGetGrassPosition(localX, entry.topBuffer, out pos))
                        break;
                }
                else
                {
                    pos = RandomPositionAtX(localX);
                }
                attempts++;
            }

            if (!positionIsValid)
                return false;

            foreach (var existing in taskPositions)
                if (Vector3.Distance(existing, pos) < minTaskDistance)
                    return false;

            var parentTf = parent != null ? parent : SpawnParent != null ? SpawnParent : transform;
            var obj = Instantiate(entry.prefab, pos, Quaternion.identity, parentTf);
            generatedObjects.Add(obj.gameObject);

            MonoBehaviour mono = obj;
            if (mono == null) return true;

            if (clearExisting)
                spawnedTasks.Add((pos, mono));
            else
                controller.AddRuntimeTaskObject(mono);
            taskPositions.Add(pos);
            taskMap[pos] = mono;
            return true;
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

        private bool TryGetWaterSpot(float localX, out Vector3 position)
        {
            position = Vector3.zero;
            EnsureTilemaps();
            if (terrainMap == null || waterTile == null)
                return false;

            var worldX = transform.position.x + localX;
            var cell = terrainMap.WorldToCell(new Vector3(worldX, transform.position.y, 0f));

            var maxY = terrainMap.cellBounds.yMax;
            var minY = terrainMap.cellBounds.yMin - 1;

            for (var y = maxY; y >= minY; y--)
            {
                if (terrainMap.GetTile(new Vector3Int(cell.x, y, 0)) != waterTile)
                    continue;

                if (terrainMap.GetTile(new Vector3Int(cell.x, y + 1, 0)) == waterTile)
                    continue;

                var candidateY = y - 1;
                if (candidateY < minY)
                    return false;
                if (terrainMap.GetTile(new Vector3Int(cell.x, candidateY, 0)) != waterTile)
                    return false;

                if (IsEdge(new Vector3Int(cell.x, candidateY, 0), waterTile))
                    return false;

                position = terrainMap.GetCellCenterWorld(new Vector3Int(cell.x, candidateY, 0));
                return true;
            }

            return false;
        }

        private bool TryGetSandSpot(float localX, int topBuffer, out Vector3 position)
        {
            position = Vector3.zero;
            EnsureTilemaps();
            if (terrainMap == null || sandTile == null)
                return false;

            var worldX = transform.position.x + localX;
            var cell = terrainMap.WorldToCell(new Vector3(worldX, transform.position.y, 0f));

            var maxY = Mathf.Clamp(terrainMap.cellBounds.yMax - topBuffer,
                                   terrainMap.cellBounds.yMin,
                                   terrainMap.cellBounds.yMax);
            var minY = terrainMap.cellBounds.yMin - 1;

            var validYs = new List<int>();
            for (var y = maxY; y >= minY; y--)
            {
                if (terrainMap.GetTile(new Vector3Int(cell.x, y, 0)) != sandTile)
                    continue;

                if (terrainMap.GetTile(new Vector3Int(cell.x, y + 1, 0)) == sandTile)
                    continue;

                for (var candidateY = y - 1; candidateY >= minY; candidateY--)
                {
                    if (terrainMap.GetTile(new Vector3Int(cell.x, candidateY, 0)) != sandTile)
                        break;

                    if (IsEdge(new Vector3Int(cell.x, candidateY, 0), sandTile))
                        continue;

                    validYs.Add(candidateY);
                }
            }

            if (validYs.Count == 0)
                return false;

            var idx = Random.Range(0, validYs.Count);
            position = terrainMap.GetCellCenterWorld(new Vector3Int(cell.x, validYs[idx], 0));
            return true;
        }

        /// <summary>
        ///     Attempt to find a grass tile at the given X coordinate.
        ///     Edge detection uses the presence of neighboring grass tiles
        ///     instead of checking the sand tilemap to avoid false positives
        ///     when sand extends behind the grass.
        /// </summary>
        /// <param name="localX">Local X position to sample.</param>
        /// <param name="topBuffer">Number of tiles to keep clear from the top.</param>
        /// <param name="position">The world position of a valid grass tile.</param>
        /// <returns>True if a valid tile was found.</returns>
        private bool TryGetGrassPosition(float localX, int topBuffer, out Vector3 position)
        {
            position = Vector3.zero;
            EnsureTilemaps();
            if (terrainMap == null || grassTile == null)
                return false;

            var worldX = transform.position.x + localX;
            var cell = terrainMap.WorldToCell(new Vector3(worldX, transform.position.y, 0f));

            var maxY = Mathf.Clamp(terrainMap.cellBounds.yMax - topBuffer, terrainMap.cellBounds.yMin, terrainMap.cellBounds.yMax);
            var minY = terrainMap.cellBounds.yMin - 1;

            var validYs = new List<int>();
            for (var y = maxY; y >= minY; y--)
            {
                if (terrainMap.GetTile(new Vector3Int(cell.x, y, 0)) != grassTile)
                    continue;

                validYs.Add(y);
            }

            if (validYs.Count == 0)
                return false;

            var idx = Random.Range(0, validYs.Count);
            position = terrainMap.GetCellCenterWorld(new Vector3Int(cell.x, validYs[idx], 0));
            return true;
        }

        private bool IsWaterTile(Vector3 worldPos)
        {
            EnsureTilemaps();
            if (terrainMap == null || waterTile == null)
                return false;

            var cell = terrainMap.WorldToCell(worldPos);
            return terrainMap.GetTile(cell) == waterTile;
        }

        private bool IsEdge(Vector3Int cell, TileBase tile)
        {
            var left = terrainMap.GetTile(cell + Vector3Int.left) == tile;
            var right = terrainMap.GetTile(cell + Vector3Int.right) == tile;
            var up = terrainMap.GetTile(cell + Vector3Int.up) == tile;
            var down = terrainMap.GetTile(cell + Vector3Int.down) == tile;
            var upLeft = terrainMap.GetTile(cell + Vector3Int.up + Vector3Int.left) == tile;
            var upRight = terrainMap.GetTile(cell + Vector3Int.up + Vector3Int.right) == tile;
            var downLeft = terrainMap.GetTile(cell + Vector3Int.down + Vector3Int.left) == tile;
            var downRight = terrainMap.GetTile(cell + Vector3Int.down + Vector3Int.right) == tile;
            return !(left && right && up && down && upLeft && upRight && downLeft && downRight);
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

        private static bool QuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(questId, out var rec) && rec.Completed;
        }

        private bool TaskAllowed(WeightedSpawn spawn, bool allowWater, bool allowGrass, bool allowSand)
        {
            var specific = spawn.spawnOnWater || spawn.spawnOnSand || spawn.spawnOnGrass;
            var permitted = (!spawn.spawnOnWater || allowWater) && (!spawn.spawnOnSand || allowSand) && (!spawn.spawnOnGrass || allowGrass);
            if (specific)
                return (spawn.spawnOnWater && allowWater) || (spawn.spawnOnSand && allowSand) || (spawn.spawnOnGrass && allowGrass);
            return permitted;
        }

        private bool TaskAllowed(WeightedTaskSpawn spawn, bool allowWater, bool allowGrass, bool allowSand)
        {
            var specific = spawn.spawnOnWater || spawn.spawnOnSand || spawn.spawnOnGrass;
            var permitted = (!spawn.spawnOnWater || allowWater) && (!spawn.spawnOnSand || allowSand) && (!spawn.spawnOnGrass || allowGrass);
            if (specific)
                return (spawn.spawnOnWater && allowWater) || (spawn.spawnOnSand && allowSand) || (spawn.spawnOnGrass && allowGrass);
            return permitted;
        }

        private (WeightedTaskSpawn entry, bool isWaterTask, bool isGrassTask, bool isSandTask) PickTaskFromCategory(
            WeightedTaskCategory category, float worldX, bool allowWaterTasks, bool allowGrassTasks, bool allowSandTasks)
        {
            var taskTotalWeight = 0f;
            foreach (var t in category.tasks)
            {
                if (!TaskAllowed(t, allowWaterTasks, allowGrassTasks, allowSandTasks))
                    continue;
                if (t.prefab != null && t.prefab is FarmingTask && !StaticReferences.CompletedNpcTasks.Contains("Witch1"))
                    continue;
                if (t.prefab != null)
                {
                    var data = t.prefab.taskData;
                    if (data != null && data.requiredQuest != null && !QuestCompleted(data.requiredQuest.questId))
                        continue;
                }
                taskTotalWeight += t.GetWeight(worldX);
            }

            if (taskTotalWeight <= 0f)
                return (null, false, false, false);

            var r = Random.value * taskTotalWeight;
            foreach (var t in category.tasks)
            {
                if (!TaskAllowed(t, allowWaterTasks, allowGrassTasks, allowSandTasks))
                    continue;
                if (t.prefab != null && t.prefab is FarmingTask && !StaticReferences.CompletedNpcTasks.Contains("Witch1"))
                    continue;
                if (t.prefab != null)
                {
                    var data = t.prefab.taskData;
                    if (data != null && data.requiredQuest != null && !QuestCompleted(data.requiredQuest.questId))
                        continue;
                }
                r -= t.GetWeight(worldX);
                if (r > 0f) continue;
                var isWater = t.spawnOnWater && allowWaterTasks;
                var isSand = !isWater && t.spawnOnSand && allowSandTasks;
                var isGrass = !isWater && !isSand && t.spawnOnGrass && allowGrassTasks;
                return (t, isWater, isGrass, isSand);
            }

            return (null, false, false, false);
        }

        private (WeightedTaskSpawn entry, bool isWaterTask, bool isGrassTask, bool isSandTask) PickTaskEntry(float worldX, bool allowWaterTasks, bool allowGrassTasks, bool allowSandTasks)
        {
            var categoryTotalWeight = 0f;
            foreach (var c in taskCategories)
                categoryTotalWeight += Mathf.Max(0f, c.weight);

            if (categoryTotalWeight <= 0f)
                return (null, false, false, false);

            var rCat = Random.value * categoryTotalWeight;
            WeightedTaskCategory chosen = null;
            foreach (var c in taskCategories)
            {
                rCat -= Mathf.Max(0f, c.weight);
                if (rCat <= 0f)
                {
                    chosen = c;
                    break;
                }
            }

            if (chosen == null)
                return (null, false, false, false);

            return PickTaskFromCategory(chosen, worldX, allowWaterTasks, allowGrassTasks, allowSandTasks);
        }

        private WeightedSpawn PickEnemyEntry(float worldX, bool allowWater, bool allowGrass, bool allowSand)
        {
            var enemyTotalWeight = 0f;
            foreach (var e in enemies)
            {
                if (!TaskAllowed(e, allowWater, allowGrass, allowSand))
                    continue;
                enemyTotalWeight += e.GetWeight(worldX);
            }

            if (enemyTotalWeight <= 0f)
                return null;

            var r = Random.value * enemyTotalWeight;
            foreach (var e in enemies)
            {
                if (!TaskAllowed(e, allowWater, allowGrass, allowSand))
                    continue;
                r -= e.GetWeight(worldX);
                if (r <= 0f)
                    return e;
            }

            return null;
        }

        /// <summary>
        ///     Picks an entry from the available lists and identifies if it's an enemy.
        /// </summary>
        /// <param name="worldX">The world X position of the spawn attempt.</param>
        /// <returns>A tuple containing the chosen WeightedSpawn and a boolean that is true if it's an enemy.</returns>
        private (object entry, bool isEnemy, bool isWaterTask, bool isGrassTask, bool isSandTask) PickEntry(float worldX, bool allowWaterTasks, bool allowGrassTasks, bool allowSandTasks)
        {
            var enemyTotalWeight = 0f;
            foreach (var e in enemies)
                enemyTotalWeight += e.GetWeight(worldX);

            var categoryWeight = 0f;
            foreach (var c in taskCategories)
                categoryWeight += Mathf.Max(0f, c.weight);

            var totalWeight = enemyTotalWeight + categoryWeight;
            if (totalWeight <= 0f)
                return (null, false, false, false, false);

            var r = Random.value * totalWeight;
            if (r < enemyTotalWeight)
            {
                foreach (var e in enemies)
                {
                    r -= e.GetWeight(worldX);
                    if (r <= 0f)
                        return (e, true, false, false, false);
                }
            }
            else
            {
                r -= enemyTotalWeight;
                var rCat = r;
                WeightedTaskCategory chosen = null;
                foreach (var c in taskCategories)
                {
                    rCat -= Mathf.Max(0f, c.weight);
                    if (rCat <= 0f)
                    {
                        chosen = c;
                        break;
                    }
                }

                if (chosen != null)
                {
                    var (entry, isWater, isGrass, isSand) =
                        PickTaskFromCategory(chosen, worldX, allowWaterTasks, allowGrassTasks, allowSandTasks);
                    if (entry != null)
                        return (entry, false, isWater, isGrass, isSand);
                }
            }

            return (null, false, false, false, false);
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

            [MinValue(0)]
            public int topBuffer = 0;

            public bool spawnOnWater;
            public bool spawnOnSand;
            public bool spawnOnGrass;

            public float GetWeight(float worldX)
            {
                if (prefab == null) return 0f;
                if (worldX < minX || worldX > maxX)
                    return 0f;
                return Mathf.Max(0f, weight);
            }
        }

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedTaskSpawn
        {
            [Required] public BaseTask prefab;

            [MinValue(0)] public float weight = 1f;

            [LabelText("Override Range")]
            public bool overrideRange;

            [ShowIf(nameof(overrideRange))]
            public float minX;

            [ShowIf(nameof(overrideRange))]
            public float maxX = float.PositiveInfinity;

            [MinValue(0)]
            public int topBuffer = 0;

            public bool spawnOnWater;
            public bool spawnOnSand;
            public bool spawnOnGrass;

            public float GetWeight(float worldX)
            {
                if (prefab == null || prefab.taskData == null) return 0f;
                var min = overrideRange ? minX : prefab.taskData.minX;
                var max = overrideRange ? maxX : prefab.taskData.maxX;
                var baseWeight = Mathf.Max(0f, weight);
                if (worldX < min)
                    return 0f;
                if (worldX > max)
                    return baseWeight * 0.1f;
                return baseWeight;
            }
        }

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedTaskCategory
        {
            public float weight = 1f;
            public List<WeightedTaskSpawn> tasks = new();
        }
    }
}