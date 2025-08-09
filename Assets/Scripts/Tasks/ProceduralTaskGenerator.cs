using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Enemies;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using static Blindsided.Oracle;
using static TimelessEchoes.Quests.QuestUtils;

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
        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private float minX;

        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private float maxX = 990f;

        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private float height = 18f;

        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private int topBuffer;

        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private int bottomBuffer;


        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private float enemyDensity = 0.1f;

        [TabGroup("Settings", "Area")] [SerializeField] [HideInInspector]
        private float enemySpawnXOffset;

        [TabGroup("Settings", "Generation")] [SerializeField] [HideInInspector]
        private LayerMask blockingMask;

        [TabGroup("Settings", "Generation")] [SerializeField] [MinValue(0)] [HideInInspector]
        private float otherTaskEdgeOffset = 1f;

        [TabGroup("Settings", "Generation")] [SerializeField] [HideInInspector]
        private List<WeightedSpawn> enemies = new();

        [TabGroup("Settings", "Generation")] [SerializeField] [HideInInspector]
        private List<WeightedTaskCategory> taskCategories = new();

        [TabGroup("Settings", "Generation")] [SerializeField] [HideInInspector]
        private List<MapGenerationConfig.ProceduralTaskSettings.NpcSpawnEntry> npcTasks = new();

        [TabGroup("Settings", "Generation")] [SerializeField] [MinValue(0f)] [HideInInspector]
        private float minTaskDistance = 1.5f;

        [TabGroup("Settings", "References")] [SerializeField] [HideInInspector]
        private Tilemap terrainMap;

        [TabGroup("Settings", "References")] [SerializeField] [HideInInspector]
        private TerrainSettings bottomTerrain;

        [TabGroup("Settings", "References")] [SerializeField] [HideInInspector]
        private TerrainSettings middleTerrain;

        [TabGroup("Settings", "References")] [SerializeField] [HideInInspector]
        private TerrainSettings topTerrain;

        private readonly List<GameObject> generatedObjects = new();

        /// <summary>
        ///     Optional parent for spawned task objects.
        /// </summary>
        public Transform SpawnParent { get; set; }

        /// <summary>
        ///     Minimum world X value allowed for spawned tasks.
        /// </summary>
        public float MinX => minX;

        /// <summary>
        ///     Optional externally assigned TaskController. If null the component
        ///     will attempt to locate one on the same GameObject.
        /// </summary>
        public TaskController Controller { get; set; }

        private void Awake()
        {
            if (Controller == null)
                Controller = GetComponent<TaskController>();
            ApplyConfig(GameManager.CurrentGenerationConfig);

            var chunk = GetComponent<TilemapChunkGenerator>();
            chunk?.AssignTilemaps(this);

            EnsureTilemaps();
        }

        private void ApplyConfig(MapGenerationConfig cfg)
        {
            if (cfg == null) return;
            minX = cfg.taskGeneratorSettings.minX;
            height = cfg.taskGeneratorSettings.height;
            enemyDensity = cfg.taskGeneratorSettings.enemyDensity;
            enemySpawnXOffset = cfg.taskGeneratorSettings.enemySpawnXOffset;
            topBuffer = cfg.taskGeneratorSettings.topBuffer;
            bottomBuffer = cfg.taskGeneratorSettings.bottomBuffer;
            blockingMask = cfg.taskGeneratorSettings.blockingMask;
            otherTaskEdgeOffset = cfg.taskGeneratorSettings.otherTaskEdgeOffset;

            enemies = new List<WeightedSpawn>();
            foreach (var spawn in cfg.taskGeneratorSettings.enemies)
            {
                if (spawn == null) continue;
                var copy = new WeightedSpawn
                {
                    data = spawn,
                    minX = spawn.minX + enemySpawnXOffset,
                    maxX = spawn.maxX + enemySpawnXOffset
                };
                enemies.Add(copy);
            }

            taskCategories = new List<WeightedTaskCategory>
            {
                cfg.taskGeneratorSettings.woodcutting,
                cfg.taskGeneratorSettings.mining,
                cfg.taskGeneratorSettings.farming,
                cfg.taskGeneratorSettings.fishing,
                cfg.taskGeneratorSettings.looting
            };

            npcTasks = cfg.taskGeneratorSettings.npcTasks;
            minTaskDistance = cfg.taskGeneratorSettings.minTaskDistance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + new Vector3((minX + maxX) * 0.5f, height * 0.5f, 0f);
            var size = new Vector3(maxX - minX, height, 0f);
            Gizmos.DrawWireCube(center, size);
#if UNITY_EDITOR
            DrawValidationGizmos();
#endif
        }

#if UNITY_EDITOR
        private void DrawValidationGizmos()
        {
            EnsureTilemaps();
            if (terrainMap == null)
                return;

            foreach (var cell in terrainMap.cellBounds.allPositionsWithin)
            {
                var tile = terrainMap.GetTile(cell);
                TerrainSettings settings = null;
                if (bottomTerrain != null && bottomTerrain.showValidationGizmos && tile == bottomTerrain.tile)
                    settings = bottomTerrain;
                else if (middleTerrain != null && middleTerrain.showValidationGizmos && tile == middleTerrain.tile)
                    settings = middleTerrain;
                else if (topTerrain != null && topTerrain.showValidationGizmos && tile == topTerrain.tile)
                    settings = topTerrain;

                if (settings == null)
                    continue;

                var valid = ValidateTerrainRules(settings, cell);
                Gizmos.color = valid ? Color.green : Color.red;
                var world = terrainMap.GetCellCenterWorld(cell);
                Gizmos.DrawCube(world, Vector3.one * 0.2f);
            }
        }
#endif

        internal void SetTilemapReferences(Tilemap map, TerrainSettings bottom, TerrainSettings middle,
            TerrainSettings top)
        {
            terrainMap = map;
            bottomTerrain = bottom;
            middleTerrain = middle;
            topTerrain = top;
        }

        private void EnsureTilemaps()
        {
            if (terrainMap != null && bottomTerrain != null && middleTerrain != null && topTerrain != null)
                return;

            if (terrainMap == null)
            {
                var maps = GetComponentsInChildren<Tilemap>();
                foreach (var m in maps)
                    if (terrainMap == null &&
                        (m.gameObject.name.Contains("Terrain") || m.gameObject.name.Contains("Water")))
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
                {
                    Destroy(obj);
                }
            }

            generatedObjects.Clear();
        }

        /// <summary>
        ///     Clear all spawned task objects and remove them from the controller.
        /// </summary>
        public void Clear()
        {
            if (Controller == null)
                Controller = GetComponent<TaskController>();

            ClearSpawnedObjects();
            Controller?.ClearTaskObjects();
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
            if (Controller == null)
                Controller = GetComponent<TaskController>();
            if (Controller == null)
                return;

            if (clearExisting)
            {
                ClearSpawnedObjects();
                Controller.ClearTaskObjects();
            }

            var bottomCount =
                Mathf.RoundToInt((localMaxX - localMinX) * (bottomTerrain?.taskSettings.taskDensity ?? 0f));
            var middleCount =
                Mathf.RoundToInt((localMaxX - localMinX) * (middleTerrain?.taskSettings.taskDensity ?? 0f));
            var topCount = Mathf.RoundToInt((localMaxX - localMinX) * (topTerrain?.taskSettings.taskDensity ?? 0f));
            var enemyCount = Mathf.RoundToInt((localMaxX - localMinX) * enemyDensity);
            if (bottomCount + middleCount + topCount <= 0 && enemyCount <= 0)
                return;

            var spawnedTasks = new List<(Vector3 pos, MonoBehaviour obj)>();
            var taskPositions = new List<Vector3>();
            var taskMap = new Dictionary<Vector3, MonoBehaviour>();
            SpawnTasks(bottomCount, localMinX, localMaxX, parent, clearExisting, true, false, false, spawnedTasks,
                taskPositions, taskMap);
            SpawnTasks(middleCount, localMinX, localMaxX, parent, clearExisting, false, true, false, spawnedTasks,
                taskPositions, taskMap);
            SpawnTasks(topCount, localMinX, localMaxX, parent, clearExisting, false, false, true, spawnedTasks,
                taskPositions, taskMap);


            for (var i = 0; i < enemyCount; i++)
            {
                var localX = Random.Range(localMinX, localMaxX);
                var worldX = transform.position.x + localX;

                var entry = PickEntry(enemies, worldX, e => TaskAllowed(e, true, true, true));
                if (entry == null || entry.data == null || entry.data.prefab == null)
                    continue;

                if (!TryGetTerrainSpot(localX, entry.spawnTerrains, out var pos))
                    continue;

                var attempts = 0;
                var positionIsValid = false;
                while (attempts < 5)
                {
                    var terrain = GetTerrainAt(pos);
                    var allowedTerrain = terrain != null &&
                                         (entry.spawnTerrains == null || entry.spawnTerrains.Count == 0 ||
                                          entry.spawnTerrains.Contains(terrain));
                    var cell = terrainMap.WorldToCell(pos);
                    var isObstructed = HasBlockingCollider(pos) || IsBlockedAhead(pos);

                    if (allowedTerrain && !isObstructed && ValidateTerrainRules(terrain, cell))
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
                var obj = Instantiate(entry.data.prefab, pos, Quaternion.identity, parentTf);
                if (clearExisting)
                    generatedObjects.Add(obj.gameObject);
            }

            foreach (var npc in npcTasks)
            {
                if (npc == null || npc.prefab == null) continue;
                if (npc.localX < localMinX || npc.localX >= localMaxX) continue;
                if (npc.spawnOnlyOnce && StaticReferences.CompletedNpcTasks.Contains(npc.id))
                    continue;
                if (!string.IsNullOrEmpty(npc.id) && StaticReferences.ActiveNpcMeetings.Contains(npc.id))
                    continue;

                Vector3 pos;
                if (!TryGetTerrainSpot(npc.localX, npc.spawnTerrains, out pos))
                    pos = RandomPositionAtX(npc.localX);

                var npcTerrain = GetTerrainAt(pos);
                if (npcTerrain == null ||
                    (npc.spawnTerrains != null && npc.spawnTerrains.Count > 0 &&
                     !npc.spawnTerrains.Contains(npcTerrain)) ||
                    !ValidateTerrainRules(npcTerrain, terrainMap.WorldToCell(pos)))
                    continue;

                for (var i = taskPositions.Count - 1; i >= 0; i--)
                {
                    var existing = taskPositions[i];
                    if (Vector3.Distance(existing, pos) < minTaskDistance)
                    {
                        if (taskMap.TryGetValue(existing, out var objToRemove))
                        {
                            if (clearExisting)
                                spawnedTasks.RemoveAll(t => t.obj == objToRemove);
                            else
                                Controller.RemoveTaskObject(objToRemove);
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
                if (clearExisting)
                    generatedObjects.Add(obj);
                var mono = obj.GetComponent<MonoBehaviour>();
                if (mono != null)
                {
                    if (clearExisting)
                        spawnedTasks.Add((pos, mono));
                    else
                        Controller.AddRuntimeTaskObject(mono);
                    taskPositions.Add(pos);
                    taskMap[pos] = mono;
                }
            }

            if (clearExisting)
            {
                spawnedTasks.Sort((a, b) => a.pos.x.CompareTo(b.pos.x));
                foreach (var pair in spawnedTasks)
                    Controller.AddTaskObject(pair.obj);

                Controller.ResetTasks();
            }
        }

        private void SpawnTasks(int count, float localMinX, float localMaxX, Transform parent, bool clearExisting,
            bool requireBottomTask, bool requireMiddleTask, bool requireTopTask,
            List<(Vector3 pos, MonoBehaviour obj)> spawnedTasks, List<Vector3> taskPositions,
            Dictionary<Vector3, MonoBehaviour> taskMap)
        {
            for (var i = 0; i < count; i++)
            for (var a = 0; a < 10; a++)
                if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, requireBottomTask,
                        requireMiddleTask, requireTopTask,
                        spawnedTasks, taskPositions, taskMap))
                    break;
        }

        private bool TrySpawnTask(float localX, Transform parent, bool clearExisting,
            bool requireBottomTask, bool requireMiddleTask, bool requireTopTask,
            List<(Vector3 pos, MonoBehaviour obj)> spawnedTasks, List<Vector3> taskPositions,
            Dictionary<Vector3, MonoBehaviour> taskMap)
        {
            var worldX = transform.position.x + localX;

            var data = PickTaskEntry(worldX);
            if (data == null || data.taskPrefab == null)
                return false;

            if (!TryGetTerrainSpot(localX, data.spawnTerrains, out var pos))
                return false;

            var chosenTerrain = GetTerrainAt(pos);
            if (requireBottomTask && chosenTerrain != bottomTerrain) return false;
            if (requireMiddleTask && chosenTerrain != middleTerrain) return false;
            if (requireTopTask && chosenTerrain != topTerrain) return false;

            var attempts = 0;
            var positionIsValid = false;
            while (attempts < 5)
            {
                var terrain = GetTerrainAt(pos);
                var terrains = data != null ? data.spawnTerrains : null;
                var allowedTerrain = terrain != null &&
                                     (terrains == null || terrains.Count == 0 ||
                                      terrains.Contains(terrain));
                var cell = terrainMap.WorldToCell(pos);
                var isObstructed = HasBlockingCollider(pos) || IsBlockedAhead(pos);

                if (allowedTerrain && !isObstructed && ValidateTerrainRules(terrain, cell))
                {
                    positionIsValid = true;
                    break;
                }

                if (!TryGetTerrainSpot(localX, data.spawnTerrains, out pos))
                    break;
                attempts++;
            }

            if (!positionIsValid)
                return false;

            foreach (var existing in taskPositions)
                if (Vector3.Distance(existing, pos) < minTaskDistance)
                    return false;

            var parentTf = parent != null ? parent : SpawnParent != null ? SpawnParent : transform;

            GameObject spawned = Instantiate(data.taskPrefab.gameObject, pos, Quaternion.identity, parentTf);

            generatedObjects.Add(spawned);

            var mono = spawned.GetComponent<BaseTask>();
            if (mono == null) return true;

            var addToList = chosenTerrain == null || chosenTerrain.taskSettings.addToTaskList;

            if (addToList)
            {
                if (clearExisting)
                    spawnedTasks.Add((pos, mono));
                else
                    Controller.AddRuntimeTaskObject(mono);
            }

            taskPositions.Add(pos);
            taskMap[pos] = mono;
            return true;
        }

        private Vector3 RandomPositionAtX(float localX)
        {
            var y = Random.Range(bottomBuffer, height - topBuffer);
            var worldX = transform.position.x + localX;
            var worldY = transform.position.y + y;
            return new Vector3(worldX, worldY, 0f);
        }

        private bool TryGetTerrainSpot(float localX, TerrainSettings settings, out Vector3 position)
        {
            position = Vector3.zero;
            EnsureTilemaps();
            if (terrainMap == null || settings == null)
                return false;

            var worldX = transform.position.x + localX;
            var baseCell = terrainMap.WorldToCell(new Vector3(worldX, transform.position.y, 0f));

            var areaBottom = terrainMap.WorldToCell(transform.position).y;
            var minY = Mathf.Clamp(areaBottom + bottomBuffer,
                terrainMap.cellBounds.yMin,
                terrainMap.cellBounds.yMax);
            var maxY = Mathf.Clamp(areaBottom + Mathf.RoundToInt(height) - topBuffer,
                terrainMap.cellBounds.yMin,
                terrainMap.cellBounds.yMax);

            var validYs = new List<int>();
            for (var y = maxY; y >= minY; y--)
            {
                var cell = new Vector3Int(baseCell.x, y, 0);
                if (ValidateTerrainRules(settings, cell))
                    validYs.Add(y);
            }

            if (validYs.Count == 0)
                return false;

            var idx = Random.Range(0, validYs.Count);
            position = terrainMap.GetCellCenterWorld(new Vector3Int(baseCell.x, validYs[idx], 0));
            return true;
        }

        private bool TryGetTerrainSpot(float localX, List<TerrainSettings> terrains, out Vector3 position)
        {
            position = Vector3.zero;
            if (terrains == null || terrains.Count == 0)
            {
                position = RandomPositionAtX(localX);
                return true;
            }

            var candidates = new List<Vector3>();
            foreach (var t in terrains)
                if (TryGetTerrainSpot(localX, t, out var pos))
                    candidates.Add(pos);

            if (candidates.Count == 0)
                return false;

            position = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        private int CountSame(Vector3Int start, Vector3Int dir, TileBase tile)
        {
            var count = 0;
            var pos = start;
            while (terrainMap.GetTile(pos) == tile)
            {
                count++;
                pos += dir;
            }

            return count;
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

        private TerrainSettings GetTerrainAt(Vector3 worldPos)
        {
            EnsureTilemaps();
            if (terrainMap == null) return null;

            var cell = terrainMap.WorldToCell(worldPos);
            var tile = terrainMap.GetTile(cell);
            if (bottomTerrain != null && tile == bottomTerrain.tile) return bottomTerrain;
            if (middleTerrain != null && tile == middleTerrain.tile) return middleTerrain;
            if (topTerrain != null && tile == topTerrain.tile) return topTerrain;
            return null;
        }

        private bool ValidateTerrainRules(TerrainSettings settings, Vector3Int cell)
        {
            if (settings == null) return false;
            var tile = terrainMap.GetTile(cell);
            if (tile != settings.tile) return false;

            if (!settings.taskSettings.borderOnly) return IsInCore(cell, settings, tile, 0);

            return IsBorderCell(cell, settings, tile);
        }

        private bool IsBorderCell(Vector3Int cell, TerrainSettings settings, TileBase tile)
        {
            var inCore = IsInCore(cell, settings, tile, 0);
            var inInnerCore = IsInCore(cell, settings, tile, 1);
            if (!inCore || inInnerCore) return false;

            var upDist = CountSame(cell + Vector3Int.up, Vector3Int.up, tile);
            var downDist = CountSame(cell + Vector3Int.down, Vector3Int.down, tile);
            var leftDist = CountSame(cell + Vector3Int.left, Vector3Int.left, tile);
            var rightDist = CountSame(cell + Vector3Int.right, Vector3Int.right, tile);

            var ts = settings.taskSettings;
            if (ts.topBorderOffset < 0 && upDist == 0) return false;
            if (ts.bottomBorderOffset < 0 && downDist == 0) return false;
            if (ts.leftBorderOffset < 0 && leftDist == 0) return false;
            if (ts.rightBorderOffset < 0 && rightDist == 0) return false;

            var touchesTop = ts.topBorderOffset >= 0 &&
                             upDist == Mathf.Max(0, ts.topBorderOffset);
            var touchesBottom = ts.bottomBorderOffset >= 0 &&
                                downDist == Mathf.Max(0, ts.bottomBorderOffset);
            var touchesLeft = ts.leftBorderOffset >= 0 &&
                              leftDist == Mathf.Max(0, ts.leftBorderOffset);
            var touchesRight = ts.rightBorderOffset >= 0 &&
                               rightDist == Mathf.Max(0, ts.rightBorderOffset);

            var sideCount = 0;
            if (touchesTop) sideCount++;
            if (touchesBottom) sideCount++;
            if (touchesLeft) sideCount++;
            if (touchesRight) sideCount++;

            return sideCount == 1;
        }

        private bool IsInCore(Vector3Int cell, TerrainSettings settings, TileBase tile, int extraOffset)
        {
            if (terrainMap.GetTile(cell) != tile) return false;

            var upDist = CountSame(cell + Vector3Int.up, Vector3Int.up, tile);
            var downDist = CountSame(cell + Vector3Int.down, Vector3Int.down, tile);
            var leftDist = CountSame(cell + Vector3Int.left, Vector3Int.left, tile);
            var rightDist = CountSame(cell + Vector3Int.right, Vector3Int.right, tile);

            var topRaw = settings.taskSettings.topBorderOffset;
            if (topRaw < 0 && upDist == 0) return false;
            var bottomRaw = settings.taskSettings.bottomBorderOffset;
            if (bottomRaw < 0 && downDist == 0) return false;
            var leftRaw = settings.taskSettings.leftBorderOffset;
            if (leftRaw < 0 && leftDist == 0) return false;
            var rightRaw = settings.taskSettings.rightBorderOffset;
            if (rightRaw < 0 && rightDist == 0) return false;

            var topOffset = Mathf.Max(0, topRaw) + extraOffset;
            var bottomOffset = Mathf.Max(0, bottomRaw) + extraOffset;
            var leftOffset = Mathf.Max(0, leftRaw) + extraOffset;
            var rightOffset = Mathf.Max(0, rightRaw) + extraOffset;

            if (upDist < topOffset) return false;
            if (downDist < bottomOffset) return false;
            if (leftDist < leftOffset) return false;
            if (rightDist < rightOffset) return false;

            for (var dx = -leftOffset; dx <= rightOffset; dx++)
            for (var dy = -bottomOffset; dy <= topOffset; dy++)
            {
                if (dx == 0 || dy == 0) continue;
                var checkPos = cell + new Vector3Int(dx, dy, 0);
                if (terrainMap.GetTile(checkPos) != tile)
                    return false;
            }

            return true;
        }


        private bool TaskAllowed(WeightedSpawn spawn, bool allowBottom, bool allowTop, bool allowMiddle)
        {
            var specific = spawn.spawnTerrains != null && spawn.spawnTerrains.Count > 0;
            var permitted = !specific ||
                            (allowBottom && spawn.spawnTerrains.Contains(bottomTerrain)) ||
                            (allowMiddle && spawn.spawnTerrains.Contains(middleTerrain)) ||
                            (allowTop && spawn.spawnTerrains.Contains(topTerrain));
            return permitted;
        }

        private bool TaskAllowed(TaskData data, bool allowBottom, bool allowTop, bool allowMiddle)
        {
            var terrains = data != null ? data.spawnTerrains : null;
            var specific = terrains != null && terrains.Count > 0;
            var permitted = !specific ||
                            (allowBottom && terrains.Contains(bottomTerrain)) ||
                            (allowMiddle && terrains.Contains(middleTerrain)) ||
                            (allowTop && terrains.Contains(topTerrain));
            return permitted;
        }

        private TaskData PickTaskFromCategory(WeightedTaskCategory category, float worldX)
        {
            return PickEntry(category.tasks, worldX, t =>
            {
                if (!TaskAllowed(t, true, true, true))
                    return false;
                if (t != null)
                    if (t.requiredQuest != null && !QuestCompleted(t.requiredQuest.questId))
                        return false;
                return true;
            });
        }

        private TaskData PickTaskEntry(float worldX)
        {
            var chosen = PickEntry(taskCategories, worldX, _ => true);
            if (chosen == null)
                return null;
            return PickTaskFromCategory(chosen, worldX);
        }

        private T PickEntry<T>(List<T> entries, float worldX, Predicate<T> filter) where T : IWeighted
        {
            var totalWeight = 0f;
            foreach (var e in entries)
                if (filter(e))
                    totalWeight += e.GetWeight(worldX);

            if (totalWeight <= 0f)
                return default;

            var r = Random.value * totalWeight;
            foreach (var e in entries)
            {
                if (!filter(e))
                    continue;
                r -= e.GetWeight(worldX);
                if (r <= 0f)
                    return e;
            }

            return default;
        }

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedSpawn : IWeighted
        {
            [Required] public EnemyData data;

            public float minX;

            public float maxX = float.PositiveInfinity;

            // Terrains this entry can spawn on.
            public List<TerrainSettings> spawnTerrains => data != null ? data.spawnTerrains : null;

            public float GetWeight(float worldX)
            {
                if (data == null || data.prefab == null) return 0f;
                if (worldX < minX || worldX > maxX)
                    return 0f;
                return Mathf.Max(0f, data.weight);
            }
        }

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedTaskCategory : IWeighted
        {
            public float weight = 1f;
            public List<TaskData> tasks = new();

            public float GetWeight(float worldX)
            {
                return Mathf.Max(0f, weight);
            }
        }
    }
}