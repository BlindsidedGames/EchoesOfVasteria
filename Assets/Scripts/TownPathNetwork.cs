using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Farming;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes
{
    /// <summary>One owner for the town's path surfaces and construction-gated branches.</summary>
    public sealed class TownPathNetwork : MonoBehaviour
    {
        [SerializeField] private TownPathDefinition definition;
        [SerializeField] private Material material;
        [SerializeField] private SpriteRenderer mineEntrance;
        [SerializeField] private Sprite minePathEntrance;
        [SerializeField] private SpriteRenderer pavingStump;
        [SerializeField] private Transform pavingBarkley;
        [SerializeField] private Vector2 pavingBarkleyPosition;
        private Sprite originalMineEntrance;
        private bool originalStumpEnabled;
        private Vector3 originalBarkleyPosition;
        private bool initialized;
        private Tilemap surface, edging;
        private bool held;
        private int signature = int.MinValue;
        private Dictionary<Vector3Int, TileBase> rendered = new();
        public int Tier { get; private set; }
        public int CellCount { get; private set; }

        private void Awake()
        {
            initialized = true;
            originalMineEntrance = mineEntrance ? mineEntrance.sprite : null;
            originalStumpEnabled = pavingStump && pavingStump.enabled;
            if (pavingBarkley) originalBarkleyPosition = pavingBarkley.position;
            var root = new GameObject("Path tile grid");
            root.transform.SetParent(transform, false);
            root.transform.position = Vector3.zero;
            root.AddComponent<Grid>();
            surface = MakeMap(root.transform, "Walking surfaces", -1);
            edging = MakeMap(root.transform, "Paving borders", -1);
        }

        private Tilemap MakeMap(Transform parent, string name, int order)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            var map = obj.AddComponent<Tilemap>();
            var renderer = obj.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Background"; renderer.sortingOrder = order;
            renderer.sharedMaterial = material;
            return map;
        }

        private void OnEnable()
        {
            Blindsided.EventHandler.OnLoadData += Refresh;
            Blindsided.EventHandler.OnQuestHandin += HandedIn;
        }
        private void OnDisable()
        {
            Blindsided.EventHandler.OnLoadData -= Refresh;
            Blindsided.EventHandler.OnQuestHandin -= HandedIn;
        }
        private void HandedIn(string _) => Refresh();
        private void Start() => Refresh();
        private void Update()
        {
            var data = Oracle.oracle?.saveData;
            if (data == null || !definition || !surface) return;
            var next = HashCode.Combine(data, data.Farm?.GardenCapacity ?? 0,
                data.Farm?.Projects ?? FarmProject.None, TownPathProgression.Tier(data));
            if (next != signature && !held) Refresh();
        }

        private Dictionary<Vector3Int, TileBase> Desired(out HashSet<Vector3Int> cells, out int tier)
        {
            var data = Oracle.oracle?.saveData;
            tier = TownPathProgression.Tier(data);
            cells = definition.Cells(data?.Farm?.GardenCapacity ?? 0,
                FarmProjects.Complete(data?.Farm, FarmProject.MillBody));
            var result = new Dictionary<Vector3Int, TileBase>();
            foreach (var cell in cells)
            {
                // Keep the existing rock stairs above the first two surfaces.
                // Their cells still participate in the connected path shape.
                if (tier < 2 && Enumerable.Range(0, definition.pavingStairs.Length)
                        .Any(i => cell == definition.stairTop + Vector3Int.down * i)) continue;
                var mask = TownPathDefinition.Neighbours(cells, cell);
                var tile = tier == 0 ? definition.grass[mask] : tier == 1 ? definition.dirt[mask] :
                    definition.pavingFill[((cell.x % 2 + 2) % 2) + 2 * ((cell.y % 2 + 2) % 2)];
                if (tier == 2)
                    for (int i = 0; i < definition.pavingStairs.Length; i++)
                        if (cell == definition.stairTop + Vector3Int.down * i) tile = definition.pavingStairs[i];
                if (tile) result[cell] = tile;
            }
            return result;
        }

        public void Refresh()
        {
            if (held || !definition || !surface || Oracle.oracle?.saveData == null) return;
            rendered = Desired(out var cells, out var tier);
            surface.ClearAllTiles(); edging.ClearAllTiles();
            foreach (var pair in rendered) surface.SetTile(pair.Key, pair.Value);
            if (tier == 2)
            {
                var neighbours = new HashSet<Vector3Int>();
                foreach (var cell in cells) foreach (var offset in TownPathDefinition.Offsets)
                    if (!cells.Contains(cell + offset)) neighbours.Add(cell + offset);
                foreach (var cell in neighbours)
                {
                    var tile = definition.pavingEdge[TownPathDefinition.Neighbours(cells, cell)];
                    if (tile) edging.SetTile(cell, tile);
                }
            }
            Tier = tier; CellCount = cells.Count;
            if (mineEntrance && minePathEntrance) mineEntrance.sprite = minePathEntrance;
            if (pavingStump) pavingStump.enabled = originalStumpEnabled && tier < 2;
            if (pavingBarkley) pavingBarkley.position = tier == 2
                ? new Vector3(pavingBarkleyPosition.x, pavingBarkleyPosition.y, originalBarkleyPosition.z)
                : originalBarkleyPosition;
            var data = Oracle.oracle.saveData;
            signature = HashCode.Combine(data, data.Farm?.GardenCapacity ?? 0,
                data.Farm?.Projects ?? FarmProject.None, TownPathProgression.Tier(data));
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            if (mineEntrance && originalMineEntrance) mineEntrance.sprite = originalMineEntrance;
            if (pavingStump) pavingStump.enabled = originalStumpEnabled;
            if (pavingBarkley) pavingBarkley.position = originalBarkleyPosition;
        }

        public void HoldPresentation() => held = true;
        public void ReleasePresentation() { held = false; Refresh(); }
        public TownVisualChange? PendingPresentationChange()
        {
            if (!definition || !surface) return null;
            var desired = Desired(out _, out _);
            Bounds? bounds = null;
            foreach (var cell in rendered.Keys.Concat(desired.Keys).Distinct())
            {
                rendered.TryGetValue(cell, out var old); desired.TryGetValue(cell, out var next);
                if (old == next) continue;
                var addition = new Bounds((Vector3)cell + new Vector3(.5f,.5f), Vector3.one * 2);
                if (bounds.HasValue) { var b = bounds.Value; b.Encapsulate(addition); bounds = b; }
                else bounds = addition;
            }
            if (!bounds.HasValue) return null;
            return new TownVisualChange(bounds.Value, () => { var wasHeld = held; held = false; Refresh(); held = wasHeld; });
        }
    }
}
