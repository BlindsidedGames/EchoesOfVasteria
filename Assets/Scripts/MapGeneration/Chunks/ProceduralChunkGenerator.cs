using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace TimelessEchoes.MapGeneration.Chunks
{
    [Serializable]
    public class DecorativeTileEntry
    {
        [HorizontalGroup("Entry", 175, LabelWidth = 45)] public TileBase Tile;
        [HorizontalGroup("Entry"), MinValue(1)] public int Weight = 1;
        [HorizontalGroup("Entry", Width = 160), LabelWidth(125)] public bool AllowRotation;
    }

    /// <summary>
    /// Generates a terrain chunk and procedural tasks.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class ProceduralChunkGenerator : MonoBehaviour
    {
        [Header("Tilemaps"), TabGroup("References")]
        [SerializeField] private Tilemap waterMap;
        [TabGroup("References"), SerializeField] private Tilemap sandMap;
        [TabGroup("References"), SerializeField] private Tilemap grassMap;
        [TabGroup("References"), SerializeField] private Tilemap decorationMap;

        [TabGroup("References"), SerializeField] private Transform spawnRoot;

        public void SetSpawnRoot(Transform root)
        {
            spawnRoot = root;
        }

        public void SetTilemaps(Tilemap water, Tilemap sand, Tilemap grass, Tilemap decor)
        {
            waterMap = water;
            sandMap = sand;
            grassMap = grass;
            decorationMap = decor;
        }

        [Header("Tiles"), TabGroup("References")]
        [SerializeField] private TileBase waterTile;
        [TabGroup("References"), SerializeField] private TileBase sandRuleTile;
        [TabGroup("References"), SerializeField] private TileBase grassRuleTile;

        [Header("Decorative Tiles"), TabGroup("References")]
        [SerializeField] private DecorativeTileEntry[] waterDecorativeTiles;
        [TabGroup("References"), SerializeField] private DecorativeTileEntry[] sandDecorativeTiles;
        [TabGroup("References"), SerializeField] private DecorativeTileEntry[] grassDecorativeTiles;

        [Header("Decoration Density"), TabGroup("Settings"), SerializeField, Range(0f,1f)]
        private float waterDecorationDensity = 0.05f;
        [TabGroup("Settings"), SerializeField, Range(0f,1f)]
        private float sandDecorationDensity = 0.05f;
        [TabGroup("Settings"), SerializeField, Range(0f,1f)]
        private float grassDecorationDensity = 0.05f;

        [Header("Dimensions"), TabGroup("Settings"), SerializeField]
        private Vector2Int size = new(64,18);

        [Header("Generation"), TabGroup("Settings"), SerializeField, MinValue(1)]
        private int minAreaWidth = 2;
        [TabGroup("Settings"), SerializeField, MinValue(0)]
        private int edgeWaviness = 1;
        [TabGroup("Settings"), SerializeField]
        private Vector2Int sandDepthRange = new(2,6);
        [TabGroup("Settings"), SerializeField]
        private Vector2Int grassDepthRange = new(2,6);

        [Header("Random Seed"), TabGroup("Settings"), SerializeField]
        private int seed;
        [TabGroup("Settings"), SerializeField] private bool randomizeSeed = true;

        [Header("Task Area"), TabGroup("Task Settings"), SerializeField]
        private float taskMinX;
        [TabGroup("Task Settings"), SerializeField] private float taskDensity = 0.1f;
        [TabGroup("Task Settings"), SerializeField] private LayerMask blockingMask;
        [TabGroup("Task Settings"), SerializeField, MinValue(0)] private float otherTaskEdgeOffset = 1f;
        [TabGroup("Task Settings"), SerializeField] private List<WeightedSpawn> enemies = new();
        [TabGroup("Task Settings"), SerializeField] private List<WeightedSpawn> otherTasks = new();
        [TabGroup("Task Settings"), SerializeField] private List<WeightedSpawn> waterTasks = new();
        [TabGroup("Task Settings"), SerializeField] private List<WeightedSpawn> grassTasks = new();
        [TabGroup("Task Settings"), SerializeField] private bool allowGrassEdge = false;
        [TabGroup("Task Settings"), SerializeField, MinValue(0)] private int grassTopBuffer = 2;

        private readonly List<GameObject> generatedObjects = new();
        private TaskController controller;
        private System.Random rng;
        private int endSandDepth;
        private int endGrassDepth;

        public int EndSandDepth => endSandDepth;
        public int EndGrassDepth => endGrassDepth;

        private void Awake()
        {
            rng = randomizeSeed ? new System.Random() : new System.Random(seed);
        }

        public void Generate(TaskController taskController, int startSandDepth, int startGrassDepth)
        {
            controller = taskController;
            rng = randomizeSeed ? new System.Random() : new System.Random(seed);

            ClearSpawnedObjects();
            GenerateTerrain(startSandDepth, startGrassDepth);
            GenerateTasks();
        }

        public void Clear()
        {
            ClearMaps();
            ClearSpawnedObjects();
        }

        private void GenerateTerrain(int startSandDepth, int startGrassDepth)
        {
            var sandDepths = new int[size.x];
            var grassDepths = new int[size.x];

            var currentSand = Mathf.Clamp(startSandDepth, sandDepthRange.x, sandDepthRange.y);
            var currentGrass = Mathf.Clamp(startGrassDepth, grassDepthRange.x, grassDepthRange.y);

            for (var x = 0; x < size.x; )
            {
                for (var segX = 0; segX < minAreaWidth && x < size.x; segX++, x++)
                {
                    sandDepths[x] = currentSand;
                    grassDepths[x] = currentGrass;
                }

                var sandDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);
                var grassDelta = RandomRange(-edgeWaviness, edgeWaviness + 1);
                currentSand = Mathf.Clamp(currentSand + sandDelta, sandDepthRange.x, sandDepthRange.y);
                currentGrass = Mathf.Clamp(currentGrass + grassDelta, grassDepthRange.x, grassDepthRange.y);
                if (currentSand + currentGrass > size.y)
                    currentGrass = Mathf.Clamp(size.y - currentSand, grassDepthRange.x, grassDepthRange.y);
            }

            endSandDepth = currentSand;
            endGrassDepth = currentGrass;

            var worldOffsetX = Mathf.RoundToInt(transform.position.x);
            var worldOffsetY = Mathf.RoundToInt(transform.position.y);

            for (var x = 0; x < size.x; x++)
            {
                var sandDepth = sandDepths[x];
                var grassDepth = grassDepths[x];
                var waterDepth = Mathf.Max(0, size.y - sandDepth - grassDepth);

                for (var y = 0; y < waterDepth; y++)
                    waterMap.SetTile(new Vector3Int(worldOffsetX + x, worldOffsetY + y, 0), waterTile);
                for (var y = waterDepth; y < size.y; y++)
                    sandMap.SetTile(new Vector3Int(worldOffsetX + x, worldOffsetY + y, 0), sandRuleTile);
                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth && y < size.y; y++)
                    grassMap.SetTile(new Vector3Int(worldOffsetX + x, worldOffsetY + y, 0), grassRuleTile);

                for (var y = 0; y < waterDepth; y++)
                {
                    var isEdge = y == 0 || y == waterDepth - 1;
                    if (!isEdge && waterDecorativeTiles != null && waterDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < waterDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(worldOffsetX + x, worldOffsetY + y, 0), waterDecorativeTiles);
                }

                for (var y = waterDepth + 1; y < waterDepth + sandDepth; y++)
                {
                    var leftWaterLvl = x > 0 ? size.y - sandDepths[x - 1] - grassDepths[x - 1] : waterDepth;
                    var rightWaterLvl = x < size.x - 1 ? size.y - sandDepths[x + 1] - grassDepths[x + 1] : waterDepth;
                    var isCurrentTileSideEdge = y < leftWaterLvl || y < rightWaterLvl;
                    var isTileBelowGroundLevel = y - 1 == waterDepth;
                    var isTileBelowSideEdge = y - 1 < leftWaterLvl || y - 1 < rightWaterLvl;
                    var isTileBelowEdge = isTileBelowGroundLevel || isTileBelowSideEdge;
                    if (isCurrentTileSideEdge || isTileBelowEdge) continue;

                    var isTopmostSandLayer = y == waterDepth + sandDepth - 1;
                    var isGrassAbove = grassDepth > 0;
                    var canSpawn = !isTopmostSandLayer || !isGrassAbove;
                    if (canSpawn && sandDecorativeTiles != null && sandDecorativeTiles.Length > 0 &&
                        rng.NextDouble() < sandDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(worldOffsetX + x, worldOffsetY + y, 0), sandDecorativeTiles);
                }

                for (var y = waterDepth + sandDepth; y < waterDepth + sandDepth + grassDepth; y++)
                {
                    var isGrassGroundLevel = y == waterDepth + sandDepth;
                    var isTopEdge = y == waterDepth + sandDepth + grassDepth - 1;
                    if (!isGrassGroundLevel && !isTopEdge && grassDecorativeTiles != null &&
                        grassDecorativeTiles.Length > 0 && rng.NextDouble() < grassDecorationDensity)
                        PlaceDecorativeTile(new Vector3Int(worldOffsetX + x, worldOffsetY + y, 0), grassDecorativeTiles);
                }
            }
        }

        private void GenerateTasks()
        {
            ClearSpawnedObjects();
            if (controller == null)
                return;

            var startOffset = Mathf.Max(0f, taskMinX - transform.position.x);
            var count = Mathf.RoundToInt((size.x - startOffset) * taskDensity);
            if (count <= 0)
                return;

            var spawnedTasks = new List<(float x, MonoBehaviour obj)>();
            for (var i = 0; i < count; i++)
            {
                var localX = Random.Range(startOffset, size.x);
                var worldX = transform.position.x + localX;
                if (worldX < taskMinX)
                    continue;
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
                    var isObstructed = HasBlockingCollider(pos) || IsBlockedAhead(pos);
                    var onWaterEdge = !isEnemy && allowWater && Mathf.Abs(pos.y - waterPos.y) < otherTaskEdgeOffset;
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

                var parent = spawnRoot != null ? spawnRoot : transform;
                var obj = Instantiate(entry.prefab, pos, Quaternion.identity, parent);
                generatedObjects.Add(obj);

                if (!isEnemy)
                {
                    var mono = obj.GetComponent<MonoBehaviour>();
                    if (mono != null)
                        spawnedTasks.Add((pos.x, mono));
                }
            }

            spawnedTasks.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (var pair in spawnedTasks)
                controller.AddRuntimeTaskObject(pair.obj);
        }

        private Vector3 RandomPositionAtX(float localX)
        {
            var y = Random.Range(0f, size.y);
            var worldX = transform.position.x + localX;
            var worldY = transform.position.y + y;
            return new Vector3(worldX, worldY, 0f);
        }

        private bool TryGetWaterEdge(float localX, out Vector3 position)
        {
            position = Vector3.zero;
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

        private bool TryGetGrassPosition(float localX, bool includeEdge, out Vector3 position)
        {
            position = Vector3.zero;
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
                var sandBelow = sandMap != null && sandMap.HasTile(new Vector3Int(cell.x, y - 1, 0));
                var leftEmpty = !grassMap.HasTile(new Vector3Int(cell.x - 1, y, 0));
                var rightEmpty = !grassMap.HasTile(new Vector3Int(cell.x + 1, y, 0));
                var isEdge = sandBelow || leftEmpty || rightEmpty;
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

        private bool HasBlockingCollider(Vector2 point)
        {
            return Physics2D.OverlapPoint(point, blockingMask) != null;
        }

        private bool IsBlockedAhead(Vector3 pos)
        {
            const float checkRadius = 0.4f;
            var hits = Physics2D.OverlapCircleAll(pos, checkRadius, blockingMask);
            foreach (var h in hits)
                if (h.bounds.min.y > pos.y - 0.1f)
                    return true;
            return false;
        }

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

        private void PlaceDecorativeTile(Vector3Int position, DecorativeTileEntry[] decorations)
        {
            var entry = GetWeightedRandomEntry(decorations);
            if (entry == null || entry.Tile == null) return;
            decorationMap.SetTile(position, entry.Tile);
            if (entry.AllowRotation)
            {
                var rotationAngle = rng.Next(0, 4) * 90f;
                var pivot = new Vector3(0.5f, 0.5f, 0);
                var matrix = Matrix4x4.TRS(pivot, Quaternion.Euler(0, 0, rotationAngle), Vector3.one) *
                             Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
                decorationMap.SetTransformMatrix(position, matrix);
            }
        }

        private DecorativeTileEntry GetWeightedRandomEntry(DecorativeTileEntry[] decorations)
        {
            if (decorations == null || decorations.Length == 0) return null;
            var totalWeight = 0;
            foreach (var entry in decorations)
                totalWeight += entry.Weight;
            if (totalWeight <= 0) return null;
            var randomValue = rng.Next(0, totalWeight);
            foreach (var entry in decorations)
            {
                if (randomValue < entry.Weight) return entry;
                randomValue -= entry.Weight;
            }
            return null;
        }

        private int RandomRange(int minInclusive, int maxExclusive)
        {
            if (rng == null)
                rng = randomizeSeed ? new System.Random() : new System.Random(seed);
            return rng.Next(minInclusive, maxExclusive);
        }

        private void ClearMaps()
        {
            waterMap.ClearAllTiles();
            sandMap.ClearAllTiles();
            grassMap.ClearAllTiles();
            decorationMap.ClearAllTiles();
        }

        private void ClearSpawnedObjects()
        {
            foreach (var obj in generatedObjects)
            {
                if (obj == null) continue;
                Destroy(obj);
            }
            generatedObjects.Clear();
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
