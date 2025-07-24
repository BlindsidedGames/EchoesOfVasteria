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
        private int topBuffer;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private int bottomBuffer;

        [TabGroup("Settings", "Area")] [SerializeField]
        [HideInInspector]
        private float taskDensity = 0.1f;


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
        private TerrainSettings bottomTerrain;

        [TabGroup("Settings", "References")] [SerializeField]
        [HideInInspector]
        private TerrainSettings middleTerrain;

        [TabGroup("Settings", "References")] [SerializeField]
        [HideInInspector]
        private TerrainSettings topTerrain;

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
            enemyDensity = config.taskGeneratorSettings.enemyDensity;
            topBuffer = config.taskGeneratorSettings.topBuffer;
            bottomBuffer = config.taskGeneratorSettings.bottomBuffer;
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

        internal void SetTilemapReferences(Tilemap map, TerrainSettings bottom, TerrainSettings middle, TerrainSettings top)
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
            var bottomCount = Mathf.RoundToInt((localMaxX - localMinX) * (bottomTerrain?.taskSettings.taskDensity ?? 0f));
            var middleCount = Mathf.RoundToInt((localMaxX - localMinX) * (middleTerrain?.taskSettings.taskDensity ?? 0f));
            var topCount = Mathf.RoundToInt((localMaxX - localMinX) * (topTerrain?.taskSettings.taskDensity ?? 0f));
            var enemyCount = Mathf.RoundToInt((localMaxX - localMinX) * enemyDensity);
            if (baseTaskCount + bottomCount + middleCount + topCount <= 0 && enemyCount <= 0)
                return;

            var spawnedTasks = new List<(Vector3 pos, MonoBehaviour obj)>();
            var taskPositions = new List<Vector3>();
            var taskMap = new Dictionary<Vector3, MonoBehaviour>();
            for (var i = 0; i < bottomCount; i++)
            {
                for (var a = 0; a < 10; a++)
                    if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, true, false, false,
                            spawnedTasks, taskPositions, taskMap))
                        break;
            }

            for (var i = 0; i < middleCount; i++)
            {
                for (var a = 0; a < 10; a++)
                    if (TrySpawnTask(Random.Range(localMinX, localMaxX), parent, clearExisting, false, true, false,
                            spawnedTasks, taskPositions, taskMap))
                        break;
            }

            for (var i = 0; i < topCount; i++)
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

                var entry = PickEnemyEntry(worldX, true, true, true);
                if (entry == null || entry.prefab == null)
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
                if (!TryGetTerrainSpot(npc.localX, npc.spawnTerrains, out pos))
                    pos = RandomPositionAtX(npc.localX);

                var npcTerrain = GetTerrainAt(pos);
                if (npcTerrain == null ||
                    (npc.spawnTerrains != null && npc.spawnTerrains.Count > 0 && !npc.spawnTerrains.Contains(npcTerrain)) ||
                    !ValidateTerrainRules(npcTerrain, terrainMap.WorldToCell(pos)))
                    continue;

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
            var obj = Instantiate(data.taskPrefab, pos, Quaternion.identity, parentTf);
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

        private bool IsEdge(Vector3Int cell, TileBase tile, int offset = 0)
        {
            var range = offset;
            for (var dx = -range; dx <= range; dx++)
            for (var dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (terrainMap.GetTile(cell + new Vector3Int(dx, dy, 0)) != tile)
                    return true;
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
            var isEdge = IsEdge(cell, settings.tile, settings.taskSettings.innerEdgeOffset);
            if (settings.taskSettings.edgeOnly)
            {
                var areaBottom = terrainMap.WorldToCell(transform.position).y;
                var bottomLimit = Mathf.Max(areaBottom + bottomBuffer, terrainMap.cellBounds.yMin);
                if (cell.y < bottomLimit)
                    return false;

                var nearOuter = IsEdge(cell, settings.tile, settings.taskSettings.innerEdgeOffset +
                                               settings.taskSettings.outerEdgeOffset);
                var beyondInner = !IsEdge(cell, settings.tile, settings.taskSettings.innerEdgeOffset - 1);
                return nearOuter && beyondInner;
            }

            if (settings.taskSettings.taskEdgeAvoidance > 0)
            {
                var dist = settings.taskSettings.taskEdgeAvoidance;
                for (var dx = -dist; dx <= dist; dx++)
                for (var dy = -dist; dy <= dist; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (IsEdge(cell + new Vector3Int(dx, dy, 0), settings.tile))
                        return false;
                }
            }
            return true;
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

        private bool TaskAllowed(WeightedSpawn spawn, bool allowBottom, bool allowTop, bool allowMiddle)
        {
            var specific = spawn.spawnTerrains != null && spawn.spawnTerrains.Count > 0;
            var permitted = (!specific) ||
                            (allowBottom && spawn.spawnTerrains.Contains(bottomTerrain)) ||
                            (allowMiddle && spawn.spawnTerrains.Contains(middleTerrain)) ||
                            (allowTop && spawn.spawnTerrains.Contains(topTerrain));
            return permitted;
        }

        private bool TaskAllowed(TaskData data, bool allowBottom, bool allowTop, bool allowMiddle)
        {
            var terrains = data != null ? data.spawnTerrains : null;
            var specific = terrains != null && terrains.Count > 0;
            var permitted = (!specific) ||
                            (allowBottom && terrains.Contains(bottomTerrain)) ||
                            (allowMiddle && terrains.Contains(middleTerrain)) ||
                            (allowTop && terrains.Contains(topTerrain));
            return permitted;
        }

        private TaskData PickTaskFromCategory(WeightedTaskCategory category, float worldX)
        {
            var taskTotalWeight = 0f;
            foreach (var t in category.tasks)
            {
                if (!TaskAllowed(t, true, true, true))
                    continue;
                if (t != null && t.taskPrefab != null && t.taskPrefab is FarmingTask && !StaticReferences.CompletedNpcTasks.Contains("Witch1"))
                    continue;
                if (t != null)
                {
                    if (t.requiredQuest != null && !QuestCompleted(t.requiredQuest.questId))
                        continue;
                }
                taskTotalWeight += t.GetWeight(worldX);
            }

            if (taskTotalWeight <= 0f)
                return null;

            var r = Random.value * taskTotalWeight;
            foreach (var t in category.tasks)
            {
                if (!TaskAllowed(t, true, true, true))
                    continue;
                if (t != null && t.taskPrefab != null && t.taskPrefab is FarmingTask && !StaticReferences.CompletedNpcTasks.Contains("Witch1"))
                    continue;
                if (t != null)
                {
                    if (t.requiredQuest != null && !QuestCompleted(t.requiredQuest.questId))
                        continue;
                }
                r -= t.GetWeight(worldX);
                if (r > 0f) continue;
                return t;
            }

            return null;
        }

        private TaskData PickTaskEntry(float worldX)
        {
            var categoryTotalWeight = 0f;
            foreach (var c in taskCategories)
                categoryTotalWeight += Mathf.Max(0f, c.weight);

            if (categoryTotalWeight <= 0f)
                return null;

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
                return null;

            return PickTaskFromCategory(chosen, worldX);
        }

        private WeightedSpawn PickEnemyEntry(float worldX, bool allowBottom, bool allowTop, bool allowMiddle)
        {
            var enemyTotalWeight = 0f;
            foreach (var e in enemies)
            {
                if (!TaskAllowed(e, allowBottom, allowTop, allowMiddle))
                    continue;
                enemyTotalWeight += e.GetWeight(worldX);
            }

            if (enemyTotalWeight <= 0f)
                return null;

            var r = Random.value * enemyTotalWeight;
            foreach (var e in enemies)
            {
                if (!TaskAllowed(e, allowBottom, allowTop, allowMiddle))
                    continue;
                r -= e.GetWeight(worldX);
                if (r <= 0f)
                    return e;
            }

            return null;
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


            // Terrains this entry can spawn on.
            public List<TerrainSettings> spawnTerrains = new();

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
        public class WeightedTaskCategory
        {
            public float weight = 1f;
            public List<TaskData> tasks = new();
        }
    }
}