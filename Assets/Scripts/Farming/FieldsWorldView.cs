using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;

namespace TimelessEchoes.Farming
{
    /// <summary>Fields-owned visual layer. Existing adventure task behaviours and colliders remain intact.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class FieldsWorldView : MonoBehaviour
    {
        [SerializeField] private FarmService service;
        [SerializeField] private FieldsWorldAppearance appearance;
        [SerializeField] private Transform town;
        private readonly List<Plot> plots = new();
        private readonly List<GameObject> enclosures = new();
        private readonly List<GameObject> gardenGround = new(), orchardGround = new();
        private readonly Dictionary<SpriteRenderer, bool> hidden = new();
        private readonly List<SpriteRenderer> legacyFarmArt = new();
        private bool legacyFarmArtCached;
        private readonly Dictionary<Vector3Int, TileBase> changedDecor = new();
        private readonly Dictionary<Vector3Int, Matrix4x4> decorMatrices = new();
        private readonly Dictionary<Vector3Int, TileFlags> decorFlags = new();
        private readonly List<GameObject> retainedTrees = new();
        private Tilemap decor;
        private Transform geometry;
        private GameObject firstFencePosts;
        private SpriteRenderer millConstruction;
        private SpriteRenderer[] originalMill, originalWell;
        private Bounds millBounds, wellBounds;
        private FarmProject renderedProjects = (FarmProject)(-1);
        private Blindsided.SaveData.GameData renderedOwner;
        private int garden = -1, orchard = -1;
        private float nextRefresh;
        private bool presentationHeld;
        public void HoldPresentation() => presentationHeld = true;
        public void ReleasePresentation() { presentationHeld = false; Refresh(); }
        public TownVisualChange? PendingPresentationChange()
        {
            if (!service || !service.Ready || garden < 0 || orchard < 0) return null;
            var nextGarden = Mathf.Clamp(service.State.GardenCapacity, 0, 6);
            var nextOrchard = Mathf.Clamp(service.State.OrchardCapacity, 0, 6);
            Bounds? bounds = null;
            void Include(Bounds addition) { if (bounds.HasValue) { var current = bounds.Value; current.Encapsulate(addition); bounds = current; } else bounds = addition; }
            for (var i = garden; i < nextGarden; i++) Include(new Bounds(GardenAnchors[i] + Vector2.one, new Vector3(5, 5, 1)));
            for (var i = orchard; i < nextOrchard; i++) Include(new Bounds(OrchardAnchors[i], new Vector3(4, 4, 1)));
            for (var i = 0; i < EnclosureBounds.Length; i++) if (garden < i * 2 + 1 && nextGarden >= i * 2 + 1) Include(EnclosureBounds[i]);
            var projects = service.State.Projects;
            if ((projects & (FarmProject.MillBody | FarmProject.MillSails)) != (renderedProjects & (FarmProject.MillBody | FarmProject.MillSails)))
            { Include(millBounds); if (nextGarden > 0) Include(EnclosureBounds[0]); }
            if ((projects & FarmProject.Well) != (renderedProjects & FarmProject.Well)) Include(wellBounds);
            if (!bounds.HasValue) return null;
            return new TownVisualChange(bounds.Value, () => { var held = presentationHeld; presentationHeld = false; Refresh(); presentationHeld = held; });
        }
        public int ClearedStumps { get; private set; }
        public int ClearedStandingTrees { get; private set; }
        public static readonly Vector2[] GardenAnchors = { new(-58,1), new(-54,1), new(-56,-9), new(-52,-9), new(-57,-27), new(-53,-27) };
        public static readonly Vector2[] OrchardAnchors = { new(-56,-13.5f), new(-53,-13.5f), new(-50,-13.5f), new(-56,-16), new(-53,-16), new(-50,-16) };
        private static readonly Bounds[] EnclosureBounds = {
            new(new Vector3(-55,2),new Vector3(11,7,10)),
            new(new Vector3(-53,-8),new Vector3(11,7,10)),
            new(new Vector3(-54,-26),new Vector3(11,7,10))
        };
        private static readonly Vector2[] GardenGroundOrigins = { new(-59.5f,-.5f), new(-57.5f,-10.5f), new(-58.5f,-28.5f) };
        private static readonly Vector2[] OrchardGroundOrigins = { new(-58,-15.5f), new(-58,-18), new(-58,-18) };
        private sealed class Plot
        {
            public string id;
            public GameObject root;
            public SpriteRenderer[] wet, plants;
            public bool orchard;
        }
        private void Start()
        {
            service ??= FarmService.Instance;
            if (!town) town = GameObject.Find("Hometown")?.transform;
            if (!appearance || !town || !service) { enabled = false; return; }
            geometry = new GameObject("Fields world").transform;
            geometry.SetParent(town,false);
            decor = town.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(t => t.name == "BG_Decor 0");
            for (int i=0;i<6;i++) CreatePlot(FarmCommands.GardenBeds[i], GardenAnchors[i], false);
            for (int i=0;i<6;i++) CreatePlot(FarmCommands.OrchardBeds[i], OrchardAnchors[i], true);
            Fence(-60,-1,-50,5,false,-57,true);
            Fence(-58,-11,-48,-5,true,-54,false);
            Fence(-59,-29,-49,-23,true,-54,false);
            CreateGround();
            CreateProjects();
            ConfigureScreen();
            Refresh();
        }
        private void ConfigureScreen()
        {
            var manager = TownWindowManager.Instance;
            if (!manager || !appearance.theme || !appearance.runtimeTheme || !appearance.textSettings) return;
            var screen = GetComponent<ToolkitFarmScreen>();
            if (!screen) screen = gameObject.AddComponent<ToolkitFarmScreen>();
            var recipe = service.Content?.recipes.FirstOrDefault(r => r != null && !r.orchard);
            screen.Configure(service, appearance.theme, appearance.runtimeTheme, appearance.textSettings, recipe?.packIcon, recipe?.unknownIcon);
            manager.ConfigureFarm(screen);
        }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            Refresh();
        }
        public void Refresh()
        {
            if (presentationHeld) return;
            if (!appearance || !town || !service) return;
            var state = service.Ready ? service.State : null;
            var owner = service.Ready ? Blindsided.Oracle.oracle?.saveData : null;
            int capacity = Mathf.Clamp(state?.GardenCapacity ?? 0,0,6), trees = Mathf.Clamp(state?.OrchardCapacity ?? 0,0,6);
            var projects = state?.Projects ?? FarmProject.None;
            // Credits and harvests replace the farm candidate, but do not change
            // the scenery. Rebuild only for a new save owner or plot layout.
            if (!ReferenceEquals(renderedOwner,owner) || garden != capacity || orchard != trees || renderedProjects != projects)
            {
                RestoreScenery(); renderedOwner = owner; garden = capacity; orchard = trees; renderedProjects = projects;
                ApplyScenery(capacity >= 5 ? 3 : capacity >= 3 ? 2 : capacity >= 1 ? 1 : 0, trees);
                for (int i=0;i<enclosures.Count;i++) enclosures[i].SetActive(capacity >= i*2+1 && (i > 0 || FarmProjects.Complete(state,FarmProject.MillBody)));
                RefreshProjects(state);
                for (int i=0;i<gardenGround.Count;i++) gardenGround[i].SetActive(capacity >= i*2+1);
                var orchardStage = (trees + 1) / 2;
                for (int i=0;i<orchardGround.Count;i++) orchardGround[i].SetActive(orchardStage == i+1);
            }
            // Quest visibility can reactivate legacy farmer decorations; hide only their renderers.
            HideLegacyFarmArt(); // Legacy planted art is never a valid Fields state, including an unbuilt fresh slot.
            for (int i=0;i<plots.Count;i++)
            {
                var plot = plots[i]; plot.root.SetActive(plot.orchard ? i-6<trees : i<capacity);
                var bed = state?.Beds != null && state.Beds.TryGetValue(plot.id,out var saved) ? saved : null;
                var recipe = bed?.IsPlanted == true ? service.Content?.Recipe(bed.RecipeId) : null;
                float progress = bed?.ReadyAfterSeconds > 0 ? (float)(bed.ElapsedSeconds / bed.ReadyAfterSeconds) : 0;
                int stage = FarmView.GrowthStage(progress,bed?.IsReady == true);
                Sprite sprite = null;
                if (recipe != null)
                    sprite = recipe.GrowthSprite(progress, bed.IsReady, bed.FullGrowth) ?? (plot.orchard && stage < 3 ? appearance.treeStages[stage] : null);
                foreach(var wet in plot.wet) wet.enabled = bed?.IsPlanted == true && bed.Watered;
                foreach(var plant in plot.plants) { plant.sprite = sprite; plant.enabled = sprite; }
                // Retain the imported pivot relative to the ground sorting anchor.
                if (plot.orchard && sprite) plot.plants[0].transform.localPosition = sprite == recipe.fullGrownSprite
                    ? Vector3.zero
                    : new Vector3(sprite.pivot.x / sprite.pixelsPerUnit - 1,sprite.pivot.y / sprite.pixelsPerUnit - 1,0);
            }
        }
        private SpriteRenderer Put(Transform parent, Sprite sprite, Vector2 position, int order, string name, bool flipX=false, bool flipY=false)
        {
            var go = new GameObject(name); go.transform.SetParent(parent,false); go.transform.position = new Vector3(position.x,position.y,0);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = appearance.material;
            renderer.sortingLayerName = "Default"; renderer.sortingOrder = order; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.flipX = flipX; renderer.flipY = flipY; return renderer;
        }
        private void CreatePlot(string id,Vector2 anchor,bool isOrchard)
        {
            var root = new GameObject(id); root.transform.SetParent(geometry,false);
            var wet = new List<SpriteRenderer>(); var plants = new List<SpriteRenderer>(); int size = isOrchard ? 2 : 3;
            for (int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                bool corner = (x==0 || x==size-1) && (y==0 || y==size-1);
                int tile = corner ? 0 : y==0 || y==size-1 ? 1 : x==0 || x==size-1 ? 2 : 3;
                bool flipX = tile==0 && x==size-1 || tile==2 && x==size-1;
                bool flipY = (tile==0 || tile==1) && y==0;
                var center = isOrchard ? anchor + new Vector2(x-.5f,y-.5f) : anchor + new Vector2(x,y);
                Put(root.transform,appearance.dry[tile],center,1,"Rounded dry soil",flipX,flipY);
                wet.Add(Put(root.transform,appearance.wet[tile],center,2,"Matched watered overlay",flipX,flipY));
                if (!isOrchard) plants.Add(Put(root.transform,null,center,3,"Crop growth"));
            }
            if (isOrchard)
            {
                var tree = new GameObject("Tree ground anchor"); tree.transform.SetParent(root.transform,false); tree.transform.position=anchor;
                var sorting = tree.AddComponent<SortingGroup>(); sorting.sortingLayerName="Default"; sorting.sortingOrder=3;
                plants.Add(Put(tree.transform,null,anchor,0,"Finite orchard growth"));
            }
            plots.Add(new Plot { id=id,root=root,wet=wet.ToArray(),plants=plants.ToArray(),orchard=isOrchard });
            root.SetActive(false);
        }
        private void CreateGround()
        {
            void Add(Sprite[] sprites, Vector2[] origins, List<GameObject> roots, string name)
            {
                for (var i=0;i<origins.Length;i++)
                {
                    var sprite = sprites != null && i < sprites.Length ? sprites[i] : null;
                    // Clearing artwork covers the path's end cap inside the field.
                    // Its exterior remains below the Default-layer fence and actors.
                    var sr = Put(geometry,sprite,origins[i],0,name + " " + (i+1));
                    sr.sortingLayerName = "Background";
                    roots.Add(sr.gameObject); sr.gameObject.SetActive(false);
                }
            }
            Add(appearance.gardenGround,GardenGroundOrigins,gardenGround,"Field clearing");
            Add(appearance.orchardGround,OrchardGroundOrigins,orchardGround,"Orchard clearing");
        }

        private void CreateProjects()
        {
            var mill = town.Find("Farmers/Windmill_0");
            var well = town.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Well_0");
            originalMill = mill ? mill.GetComponentsInChildren<SpriteRenderer>(true) : Array.Empty<SpriteRenderer>();
            originalWell = well ? well.GetComponentsInChildren<SpriteRenderer>(true) : Array.Empty<SpriteRenderer>();
            millBounds = mill ? TownQuestReveal.SpriteBounds(mill.gameObject) ?? new Bounds(mill.position, new Vector3(5,8,0)) : default;
            wellBounds = well ? TownQuestReveal.SpriteBounds(well.gameObject) ?? new Bounds(well.position, new Vector3(3,4,0)) : default;
            if (mill) millConstruction = Put(geometry, appearance.millPosts, mill.position, 0, "Mill construction");
            firstFencePosts = new GameObject("First field boundary posts"); firstFencePosts.transform.SetParent(geometry,false);
            foreach(var point in new [] {new Vector2(-60,5),new Vector2(-50,5),new Vector2(-50,-1),new Vector2(-58,-1),new Vector2(-56,-1)})
                Put(firstFencePosts.transform,appearance.fence[1],point,-1,"Boundary post");
        }
        private void RefreshProjects(FarmState state)
        {
            var body = FarmProjects.Complete(state,FarmProject.MillBody);
            var sails = FarmProjects.AutoReplant(state);
            foreach(var sr in originalMill ?? Array.Empty<SpriteRenderer>()) if(sr && !sails) Hide(sr);
            foreach(var sr in originalWell ?? Array.Empty<SpriteRenderer>()) if(sr && !FarmProjects.AutoWater(state)) Hide(sr);
            if(millConstruction) { millConstruction.sprite = body ? appearance.millBody : appearance.millPosts; millConstruction.enabled = !sails; }
            if(firstFencePosts) firstFencePosts.SetActive(garden>0 && !body);
        }

        private void Fence(int x0,int y0,int x1,int y1,bool northGate,int gateX,bool original)
        {
            var root = new GameObject("Fields enclosure " + (enclosures.Count+1)); root.transform.SetParent(geometry,false); enclosures.Add(root);
            var f = appearance.fence;
            Put(root.transform,f[2],new(x0,y1),3,"NW corner"); Put(root.transform,f[3],new(x1,y1),3,"NE corner");
            Put(root.transform,f[4],new(x0,y0),original?-1:3,"SW corner"); Put(root.transform,f[5],new(x1,y0),original?-1:3,"SE corner");
            for(int x=x0+1;x<x1;x++)
            {
                if(!northGate || Math.Abs(x-gateX)>1) Put(root.transform,f[0],new(x,y1),3,"North fence");
                if(northGate || Math.Abs(x-gateX)>1) Put(root.transform,f[0],new(x,y0),original?-1:3,"South fence");
            }
            for(int y=y0+1;y<y1;y++)
            { if(!original || y>=5) Put(root.transform,f[1],new(x0,y),original?-1:3,"West fence"); Put(root.transform,f[1],new(x1,y),3,"East fence"); }
            Put(root.transform,f[northGate||original?6:7],new(gateX,northGate?y1+.5f:original?y0+.5f:y0-.5f),original?-1:3,"Open gate");
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>()) renderer.sortingOrder = -1;
            root.SetActive(false);
        }
        private void Hide(SpriteRenderer sr)
        { if (!hidden.ContainsKey(sr)) hidden.Add(sr,sr.enabled); sr.enabled=false; }
        private void HideLegacyFarmArt()
        {
            if (!legacyFarmArtCached)
            {
                // These are authored town objects, including inactive quest stages.
                // Discover them once; quest visibility may re-enable their renderers,
                // but does not change which objects belong to the legacy farm.
                var unique = new HashSet<SpriteRenderer>();
                foreach (var part in new[] { "Farmers/FarmingTasks", "Farmers/Fence" })
                {
                    var root = town.Find(part);
                    if (root) foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true)) unique.Add(sr);
                }
                var farmers = town.Find("Farmers");
                if (farmers) foreach (var child in farmers.GetComponentsInChildren<Transform>(true))
                    if (child.name == "Crops")
                        foreach (var sr in child.GetComponentsInChildren<SpriteRenderer>(true)) unique.Add(sr);
                legacyFarmArt.AddRange(unique);
                legacyFarmArtCached = true;
            }
            foreach (var sr in legacyFarmArt) if (sr) Hide(sr);
        }
        private void RestoreScenery()
        {
            foreach(var pair in hidden) if(pair.Key) pair.Key.enabled=pair.Value;
            hidden.Clear();
            if(decor) foreach(var pair in changedDecor)
            {
                decor.SetTile(pair.Key,pair.Value); decor.SetTileFlags(pair.Key,TileFlags.None);
                decor.SetTransformMatrix(pair.Key,decorMatrices[pair.Key]); decor.SetTileFlags(pair.Key,decorFlags[pair.Key]);
            }
            changedDecor.Clear(); decorMatrices.Clear(); decorFlags.Clear();
            foreach(var tree in retainedTrees) if(tree) Destroy(tree); retainedTrees.Clear();
            ClearedStumps=0; ClearedStandingTrees=0;
        }
        private void ClearDecor(Vector3Int cell)
        {
            if (!changedDecor.ContainsKey(cell))
            {
                changedDecor.Add(cell,decor.GetTile(cell)); decorMatrices.Add(cell,decor.GetTransformMatrix(cell)); decorFlags.Add(cell,decor.GetTileFlags(cell));
            }
            decor.SetTile(cell,null);
        }
        private void ApplyScenery(int stage, int trees)
        {
            if(stage==0 && trees==0) return;
            bool BuiltArea(Bounds bounds)
            {
                for(var i=0;i<stage;i++) if(bounds.Intersects(EnclosureBounds[i])) return true;
                for(var i=0;i<trees;i++) if(bounds.Intersects(new Bounds(OrchardAnchors[i],new Vector3(4,4,10)))) return true;
                return false;
            }
            var farmers = town.Find("Farmers");
            foreach(var sr in town.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(!sr.sprite || sr.transform.IsChildOf(geometry)) continue;
                if(farmers && sr.transform.IsChildOf(farmers)) continue;
                var name=sr.sprite.name;
                if(!BuiltArea(sr.bounds)) continue;
                if(name.Contains("Stump")) { Hide(sr); ClearedStumps++; }
                else if(name.Contains("Tree")) { Hide(sr); ClearedStandingTrees++; }
                else if(name.StartsWith("Outdoor_Decor_") || name.StartsWith("Crate_") || name.Contains("Rock")) Hide(sr);
            }
            if(decor)
            {
                // Promote exactly the two retained young trees before changing any enclosure ground clutter.
                foreach(var cell in new[]{new Vector3Int(13,-13,0),new Vector3Int(23,-5,0)})
                {
                    var sprite=decor.GetSprite(cell); if(!sprite || sprite.name!="FruitTree_Growth_YoungTree") continue;
                    var order=cell==new Vector3Int(23,-5,0)?-2:3;
                    var sr=Put(geometry,sprite,decor.GetCellCenterWorld(cell),order,"Retained young tree " + cell); retainedTrees.Add(sr.gameObject); ClearDecor(cell);
                }
                foreach(var cell in decor.cellBounds.allPositionsWithin)
                {
                    if(!decor.GetTile(cell)) continue; var p=decor.GetCellCenterWorld(cell);
                    if(BuiltArea(new Bounds(p,Vector3.one * .1f))) ClearDecor(cell);
                }
            }
        }
        private void OnDestroy()
        { RestoreScenery(); if (geometry) Destroy(geometry.gameObject); }
    }
}
