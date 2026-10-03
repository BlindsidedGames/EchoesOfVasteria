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
        private readonly Dictionary<SpriteRenderer, bool> hidden = new();
        private readonly Dictionary<Vector3Int, TileBase> changedPath = new();
        private readonly Dictionary<Vector3Int, Matrix4x4> pathMatrices = new();
        private readonly Dictionary<Vector3Int, TileFlags> pathFlags = new();
        private readonly Dictionary<Vector3Int, TileBase> changedDecor = new();
        private readonly List<GameObject> retainedTrees = new();
        private Tilemap path, decor;
        private Transform geometry;
        private FarmState renderedState;
        private int garden = -1, orchard = -1;
        private float nextRefresh;
        public int ClearedStumps { get; private set; }
        public int ClearedStandingTrees { get; private set; }
        public static readonly Vector2[] GardenAnchors = { new(-62,1), new(-58,1), new(-62,-9), new(-57,-9), new(-61,-27), new(-57,-27) };
        public static readonly Vector2[] OrchardAnchors = { new(-60,-13.5f), new(-56,-13.5f), new(-52,-13.5f), new(-60,-17), new(-56,-17), new(-52,-17) };
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
            path = town.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(t => t.name == "-2 Old Path");
            decor = town.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(t => t.name == "BG_Decor 0");
            for (int i=0;i<6;i++) CreatePlot(FarmCommands.GardenBeds[i], GardenAnchors[i], false);
            for (int i=0;i<6;i++) CreatePlot(FarmCommands.OrchardBeds[i], OrchardAnchors[i], true);
            Fence(-64,-1,-54,6,false,-61,true);
            Fence(-64,-11,-52,-4,true,-58,false);
            Fence(-63,-29,-53,-23,true,-58,false);
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
            if (!appearance || !town || !service) return;
            var state = service.Ready ? service.State : null;
            int capacity = Mathf.Clamp(state?.GardenCapacity ?? 0,0,6), trees = Mathf.Clamp(state?.OrchardCapacity ?? 0,0,6);
            if (!ReferenceEquals(renderedState,state) || garden != capacity || orchard != trees)
            {
                RestoreScenery(); renderedState = state; garden = capacity; orchard = trees;
                ApplyScenery(capacity >= 5 ? 3 : capacity >= 3 ? 2 : capacity >= 1 ? 1 : 0);
                for (int i=0;i<enclosures.Count;i++) enclosures[i].SetActive(capacity >= i*2+1);
            }
            // Quest visibility can reactivate legacy farmer decorations; hide only their renderers.
            if (capacity > 0) HideLegacyFarmArt();
            for (int i=0;i<plots.Count;i++)
            {
                var plot = plots[i]; plot.root.SetActive(plot.orchard ? i-6<trees : i<capacity);
                var bed = state?.Beds != null && state.Beds.TryGetValue(plot.id,out var saved) ? saved : null;
                var recipe = bed?.IsPlanted == true ? service.Content?.Recipe(bed.RecipeId) : null;
                float progress = bed?.ReadyAfterSeconds > 0 ? (float)(bed.ElapsedSeconds / bed.ReadyAfterSeconds) : 0;
                int stage = FarmView.GrowthStage(progress,bed?.IsReady == true);
                Sprite sprite = null;
                if (recipe != null)
                    sprite = plot.orchard && stage < 3 ? appearance.treeStages[stage] : recipe.stages?.Length == 4 ? recipe.stages[stage] : null;
                foreach(var wet in plot.wet) wet.enabled = bed?.IsPlanted == true && bed.Watered;
                foreach(var plant in plot.plants) { plant.sprite = sprite; plant.enabled = sprite; }
                // Retain the imported pivot relative to the ground sorting anchor.
                if (plot.orchard && sprite) plot.plants[0].transform.localPosition = new Vector3(sprite.pivot.x / sprite.pixelsPerUnit - 1,sprite.pivot.y / sprite.pixelsPerUnit - 1,0);
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
            root.SetActive(false);
        }
        private void Hide(SpriteRenderer sr)
        { if (!hidden.ContainsKey(sr)) hidden.Add(sr,sr.enabled); sr.enabled=false; }
        private void HideLegacyFarmArt()
        {
            foreach(var part in new[]{"Farmers/FarmingTasks","Farmers/Fence"})
            { var root=town.Find(part); if(root) foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>(true)) Hide(sr); }
            // Crops below the farmer property only: unrelated crop decoration elsewhere is retained.
            var farmers=town.Find("Farmers");
            if(farmers) foreach(var child in farmers.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Crops"))
                foreach(var sr in child.GetComponentsInChildren<SpriteRenderer>(true)) Hide(sr);
        }
        private void RestoreScenery()
        {
            foreach(var pair in hidden) if(pair.Key) pair.Key.enabled=pair.Value;
            hidden.Clear();
            if(path) foreach(var pair in changedPath)
            { path.SetTile(pair.Key,pair.Value); path.SetTileFlags(pair.Key,TileFlags.None); path.SetTransformMatrix(pair.Key,pathMatrices[pair.Key]); path.SetTileFlags(pair.Key,pathFlags[pair.Key]); }
            changedPath.Clear(); pathMatrices.Clear(); pathFlags.Clear();
            if(decor) foreach(var pair in changedDecor) decor.SetTile(pair.Key,pair.Value);
            changedDecor.Clear();
            foreach(var tree in retainedTrees) if(tree) Destroy(tree); retainedTrees.Clear();
            ClearedStumps=0; ClearedStandingTrees=0;
        }
        private void ClearDecor(Vector3Int cell)
        { if (!changedDecor.ContainsKey(cell)) changedDecor.Add(cell,decor.GetTile(cell)); decor.SetTile(cell,null); }
        private void ApplyScenery(int stage)
        {
            if(stage==0) return;
            foreach(var sr in town.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(!sr.sprite || sr.transform.IsChildOf(geometry)) continue;
                var p=sr.transform.position; var name=sr.sprite.name;
                if(stage>=2 && name.Contains("Stump") && p.x>=-66 && p.x<=-51 && p.y>=-17 && p.y<=-5) { Hide(sr); ClearedStumps++; }
                else if(stage>=3 && (name.Contains("Tree_Standing") || name.Contains("Tree_Stump")) && sr.bounds.Intersects(new Bounds(new Vector3(-58,-26,0),new Vector3(12,8,10)))) { Hide(sr); ClearedStandingTrees++; }
                else if((name.StartsWith("Outdoor_Decor_") || name.StartsWith("Crate_")) &&
                    (sr.bounds.Intersects(new Bounds(new Vector3(-59,2.5f,0),new Vector3(10.9f,7.9f,10))) ||
                    stage>=2 && sr.bounds.Intersects(new Bounds(new Vector3(-58,-7.5f,0),new Vector3(12.9f,7.9f,10))) ||
                    stage>=3 && sr.bounds.Intersects(new Bounds(new Vector3(-58,-26,0),new Vector3(10.9f,6.9f,10))))) Hide(sr);
            }
            if(decor)
            {
                // Promote exactly the two retained young trees before changing any enclosure ground clutter.
                foreach(var cell in new[]{new Vector3Int(13,-13,0),new Vector3Int(23,-5,0)})
                {
                    var sprite=decor.GetSprite(cell); if(!sprite || sprite.name!="FruitTree_Growth_YoungTree") continue;
                    var sr=Put(geometry,sprite,decor.GetCellCenterWorld(cell),3,"Retained young tree " + cell); retainedTrees.Add(sr.gameObject); ClearDecor(cell);
                }
                foreach(var cell in decor.cellBounds.allPositionsWithin)
                {
                    if(!decor.GetTile(cell)) continue; var p=decor.GetCellCenterWorld(cell);
                    if(p.x>-64&&p.x<-54&&p.y>-1&&p.y<6 || stage>=2&&p.x>-64&&p.x<-52&&p.y>-11&&p.y<-4 || stage>=3&&p.x>-65&&p.x<-50&&p.y>-33&&p.y<-23) ClearDecor(cell);
                }
            }
            ExtendPaths(stage);
        }
        private void ChangePath(Vector3Int cell,TileBase tile)
        {
            if(!changedPath.ContainsKey(cell)) { changedPath.Add(cell,path.GetTile(cell)); pathMatrices.Add(cell,path.GetTransformMatrix(cell)); pathFlags.Add(cell,path.GetTileFlags(cell)); }
            path.SetTile(cell,tile); path.SetTileFlags(cell,TileFlags.None); path.SetTransformMatrix(cell,Matrix4x4.identity);
        }
        private void ExtendPaths(int stage)
        {
            if(!path || appearance.pathTiles==null || appearance.pathTiles.Length==0) return;
            var changed=new HashSet<Vector3Int>();
            void Add(int x,int y) => changed.Add(path.WorldToCell(new Vector3(x,y,0)));
            if(stage>=2) { for(int x=-60;x<=-58;x++) Add(x,-3); for(int x=-59;x<=-58;x++) for(int y=-4;y<=-3;y++) Add(x,y); }
            if(stage>=3) { for(int x=-60;x<=-58;x++) Add(x,-22); for(int x=-59;x<=-58;x++) for(int y=-23;y<=-22;y++) Add(x,y); }
            foreach(var cell in changed) ChangePath(cell,appearance.pathTiles[0]);
            var affected=new HashSet<Vector3Int>(changed);
            foreach(var cell in changed) for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++) affected.Add(cell+new Vector3Int(x,y,0));
            foreach(var cell in affected)
            {
                if(!path.GetTile(cell)) continue;
                bool Occupied(int x,int y) => path.GetTile(cell+new Vector3Int(x,y,0));
                string join=""; if(Occupied(0,1))join+="N";if(Occupied(1,0))join+="E";if(Occupied(0,-1))join+="S";if(Occupied(-1,0))join+="W";
                var absent=new List<string>();
                if(!(Occupied(-1,0)&&Occupied(0,1)&&Occupied(-1,1)))absent.Add("NW");
                if(!(Occupied(1,0)&&Occupied(0,1)&&Occupied(1,1)))absent.Add("NE");
                if(!(Occupied(1,0)&&Occupied(0,-1)&&Occupied(1,-1)))absent.Add("SE");
                if(!(Occupied(-1,0)&&Occupied(0,-1)&&Occupied(-1,-1)))absent.Add("SW");
                string prefix="Farmland_WetOverlay_SoilJoin_"+(join==""?"None":join)+"_AbsentCorners_"+(absent.Count==0?"None":string.Join("_",absent))+"_Variant";
                var tile=Array.Find(appearance.pathTiles,t=>t&&t.name.StartsWith(prefix,StringComparison.Ordinal));
                if(tile) ChangePath(cell,tile);
            }
        }
        private void OnDestroy()
        { RestoreScenery(); if (geometry) Destroy(geometry.gameObject); }
    }
}
