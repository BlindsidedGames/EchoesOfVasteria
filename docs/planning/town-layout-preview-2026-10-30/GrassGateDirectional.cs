using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.Rendering;using Blindsided;using Blindsided.SaveData;using TimelessEchoes;using TimelessEchoes.Farming;using TimelessEchoes.Editor.Capture;
public static class GrassGateDirectional {
 public static object Main(){
  if(!ScreenshotSession.Ready)throw new Exception("Private session required");
  var root="Artifacts/GrassGateDirectional20261030/";Directory.CreateDirectory(root);var d=Oracle.oracle.saveData;var fields=UnityEngine.Object.FindAnyObjectByType<FieldsWorldView>();var roads=UnityEngine.Object.FindAnyObjectByType<TownPathNetwork>();
  d.Farm.GardenCapacity=6;d.Farm.OrchardCapacity=6;d.Farm.Projects=(FarmProject)7;fields.Refresh();d.Quests[TownPathProgression.DirtQuest]=new GameData.QuestRecord();d.Quests[TownPathProgression.PavingQuest]=new GameData.QuestRecord();roads.Refresh();Time.timeScale=0;
  var go=new GameObject("Entrance preview camera");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.49f,.59f,.24f);cam.nearClipPlane=.1f;cam.farClipPlane=100;cam.allowHDR=false;cam.allowMSAA=false;
  void Shot(string name,Vector2 point,float size,int w=1280,int h=768){SortingGroup.UpdateAllSortingGroups();cam.orthographicSize=size;cam.aspect=(float)w/h;cam.transform.position=new Vector3(point.x,point.y,-10);var rt=RenderTexture.GetTemporary(w,h,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(root+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);cam.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
  var scenery=new System.Collections.Generic.List<(SpriteRenderer sr,bool enabled)>();var removedTiles=new System.Collections.Generic.List<(UnityEngine.Tilemaps.Tilemap map,Vector3Int cell,UnityEngine.Tilemaps.TileBase tile)>();var owned=new System.Collections.Generic.List<UnityEngine.Object>();var moved=new System.Collections.Generic.List<(Transform t,Vector3 pos)>();var originals=new System.Collections.Generic.List<SpriteRenderer>();var roots=new System.Collections.Generic.List<GameObject>();
  var at=new[]{new Vector2(-57,-.75f),new Vector2(-54,-5.25f),new Vector2(-54,-23.25f)};
  try{
   for(int i=0;i<3;i++)Shot("before-"+(i+1),at[i],3);
   var def=Resources.Load<TownPathDefinition>("Farming/TownPaths");
   var fieldMaps=new System.Collections.Generic.List<(UnityEngine.Tilemaps.Tilemap map,System.Collections.Generic.HashSet<Vector3Int> cells,Vector2 origin)>();
   var origins=new[]{new Vector2(-59,0),new Vector2(-57,-10),new Vector2(-58,-28)};
   void Move(Transform t){moved.Add((t,t.position));t.position+=new Vector3(.5f,.5f,0);}
   foreach(var id in FarmCommands.GardenBeds)Move(GameObject.Find(id).transform);
   var well=GameObject.Find("Hometown").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Well_0");moved.Add((well,well.position));well.position+=Vector3.up*.5f;
   for(int i=1;i<=3;i++)Move(GameObject.Find("Fields enclosure "+i).transform);
   for(int i=0;i<3;i++){
    var sr=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).Single(s=>s.name=="Field clearing "+(i+1));originals.Add(sr);sr.enabled=false;
    var field=new GameObject("Full tile field "+i);roots.Add(field);field.transform.position=origins[i];field.AddComponent<Grid>();
    var obj=new GameObject("Ground tiles");obj.transform.SetParent(field.transform,false);var map=obj.AddComponent<UnityEngine.Tilemaps.Tilemap>();var renderer=obj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();renderer.sharedMaterial=sr.sharedMaterial;renderer.sortingLayerName="Background";renderer.sortingOrder=0;
    var cells=new System.Collections.Generic.HashSet<Vector3Int>();for(int y=0;y<5;y++)for(int x=0;x<9;x++)cells.Add(new Vector3Int(x,y,0));
    cells.Add(new Vector3Int(i+2,i==0?-1:5,0));
    fieldMaps.Add((map,cells,origins[i]));
   }
   var appearance=Resources.Load<FieldsWorldAppearance>("Farming/FieldsWorldAppearance");
   var fencePiece=new GameObject("Missing west fence piece");roots.Add(fencePiece);fencePiece.transform.position=new Vector3(-59.5f,4.5f,0);var fenceRenderer=fencePiece.AddComponent<SpriteRenderer>();fenceRenderer.sprite=appearance.fence[1];fenceRenderer.sharedMaterial=appearance.material;fenceRenderer.sortingLayerName="Default";fenceRenderer.sortingOrder=-1;
   foreach(var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).Where(s=>s.name.StartsWith("Orchard clearing "))){if(sr.enabled){originals.Add(sr);sr.enabled=false;}}
   var orchardIds=FarmCommands.OrchardBeds.ToArray();for(int i=0;i<orchardIds.Length;i++){var t=GameObject.Find(orchardIds[i]).transform;moved.Add((t,t.position));t.position+=new Vector3(i%3==0?1f:i%3==1?.5f:0,.75f,0);}
   var orchardRoot=new GameObject("Narrower orchard");roots.Add(orchardRoot);orchardRoot.transform.position=new Vector3(-57,-17,0);orchardRoot.AddComponent<Grid>();var orchardObj=new GameObject("Orchard terrain");orchardObj.transform.SetParent(orchardRoot.transform,false);var orchardMap=orchardObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();var orchardRenderer=orchardObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();orchardRenderer.sharedMaterial=appearance.material;orchardRenderer.sortingLayerName="Background";orchardRenderer.sortingOrder=0;
   var orchardCells=new System.Collections.Generic.HashSet<Vector3Int>();for(int y=0;y<6;y++)for(int x=0;x<9;x++)orchardCells.Add(new Vector3Int(x,y,0));foreach(var cell in orchardCells)orchardMap.SetTile(cell,def.dirt[TownPathDefinition.Neighbours(orchardCells,cell)]);
   var grid=new GameObject("World grid overlay");roots.Add(grid);var white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),Texture2D.whiteTexture.width);
   void Line(Vector2 a,Vector2 b){var obj=new GameObject("16px line");obj.transform.SetParent(grid.transform,false);obj.transform.position=(a+b)*.5f;obj.transform.localScale=new Vector3(Math.Abs(b.x-a.x)<.001f?.05f:Math.Abs(b.x-a.x),Math.Abs(b.y-a.y)<.001f?.05f:Math.Abs(b.y-a.y),1);var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=white;sr.sharedMaterial=appearance.material;sr.color=new Color(.1f,.85f,1f,.6f);sr.sortingLayerName="Default";sr.sortingOrder=32000;}
   for(int x=-78;x<=-45;x++)Line(new Vector2(x,-31),new Vector2(x,7));for(int y=-31;y<=7;y++)Line(new Vector2(-78,y),new Vector2(-45,y));
   var surface=roads.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Single(t=>t.name=="Walking surfaces");var edging=roads.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Single(t=>t.name=="Paving borders");
   var fieldWorld=new System.Collections.Generic.HashSet<Vector3Int>();foreach(var f in fieldMaps)foreach(var c in f.cells)fieldWorld.Add(c+Vector3Int.RoundToInt(f.origin));
   var gates=new System.Collections.Generic.HashSet<Vector3Int>{new Vector3Int(-57,-1,0),new Vector3Int(-54,-5,0),new Vector3Int(-54,-23,0)};
   var borderThresholds=new System.Collections.Generic.HashSet<Vector3Int>();
   foreach(var end in new[]{new Vector3Int(-57,0,0),new Vector3Int(-54,-6,0),new Vector3Int(-54,-24,0)}){borderThresholds.Add(end);borderThresholds.Add(end+Vector3Int.left);borderThresholds.Add(end+Vector3Int.right);}
   var centreEnds=new System.Collections.Generic.HashSet<Vector3Int>{new Vector3Int(-57,0,0),new Vector3Int(-54,-6,0),new Vector3Int(-54,-24,0)};
   var orchardOriginal=orchardRoot.transform.position;var treeTransforms=orchardIds.Select(id=>GameObject.Find(id).transform).ToArray();var treePositions=treeTransforms.Select(t=>t.position).ToArray();
   var alternateBounds=new Bounds(new Vector3(-62.5f,-8,0),new Vector3(10,8,1));
   var town=GameObject.Find("Hometown");
   foreach(var sr in town.GetComponentsInChildren<SpriteRenderer>(true)){
    if(!sr.sprite||sr.name.StartsWith("Orchard clearing")||sr.transform.IsChildOf(fields.transform))continue;
    var name=sr.sprite.name;
    if((name.Contains("Tree")||name.Contains("Stump")||name.Contains("Log"))&&alternateBounds.Contains(new Vector3(sr.bounds.center.x,sr.bounds.center.y,0)))scenery.Add((sr,sr.enabled));
   }
   foreach(var map in town.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)){
    if(map.name!="BG_Decor 0")continue;
    foreach(var c in map.cellBounds.allPositionsWithin){var sprite=map.GetSprite(c);if(!sprite)continue;var pos=map.GetCellCenterWorld(c);if(pos.x>=-67.75f&&pos.x<-57.25f&&pos.y>=-11.75f&&pos.y<-4.25f&&(sprite.name.Contains("Tree")||sprite.name.Contains("Stump")||sprite.name.Contains("Log")))removedTiles.Add((map,c,map.GetTile(c)));}
   }
   var fenceRoot=new GameObject("Orchard preview fence");roots.Add(fenceRoot);
   void FencePart(int sprite,float x,float y){var o=new GameObject("Orchard fence");o.transform.SetParent(fenceRoot.transform,false);o.transform.position=new Vector3(x,y,0);var sr=o.AddComponent<SpriteRenderer>();sr.sprite=appearance.fence[sprite];sr.sharedMaterial=appearance.material;sr.sortingLayerName="Default";sr.sortingOrder=-1;}
   float x0=-57.5f,x1=-47.5f,y0=-18.5f,y1=-10.5f;
   FencePart(4,x0,y0);FencePart(5,x1,y0);
   for(float x=x0+1;x<x1;x++){FencePart(0,x,y0);}
   for(float y=y0+1;y<y1;y++){FencePart(1,x1,y);if(y<=-17||y>=-14)FencePart(1,x0,y);}
   var shadowRoot=new GameObject("Paving shadow grid");roots.Add(shadowRoot);shadowRoot.transform.position=new Vector3(1f/16,-1f/16,0);shadowRoot.AddComponent<Grid>();var shadowObj=new GameObject("Paving shadow");shadowObj.transform.SetParent(shadowRoot.transform,false);var shadow=shadowObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();shadow.color=new Color(.15f,.17f,.10f,.7f);var shadowRenderer=shadowObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();shadowRenderer.sharedMaterial=appearance.material;shadowRenderer.sortingLayerName="Background";shadowRenderer.sortingOrder=1;
   var retainedTree=GameObject.Find("Retained young tree (23, -5, 0)");if(retainedTree)retainedTree.GetComponent<SpriteRenderer>().sortingOrder=-2;
   var readable=new System.Collections.Generic.Dictionary<string,Texture2D>();
   Color32[] Pixels(Sprite sprite){var file=UnityEditor.AssetDatabase.GetAssetPath(sprite);if(!file.EndsWith(".png",StringComparison.OrdinalIgnoreCase))file=UnityEditor.AssetDatabase.GetAssetPath(sprite.texture);if(!readable.TryGetValue(file,out var texture)){texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(file));readable[file]=texture;owned.Add(texture);}var r=sprite.rect;return texture.GetPixels((int)r.x,(int)r.y,16,16).Select(c=>(Color32)c).ToArray();}
   var normalPath=Pixels(def.grass[17].sprite);
   UnityEngine.Tilemaps.Tile Transition(int baseMask,int direction,string name){var soil=Pixels(def.dirt[baseMask].sprite);var pixels=(Color32[])soil.Clone();for(int y=0;y<16;y++)for(int x=0;x<16;x++){
    int forward=direction==0?15-y:direction==1?y:x;int across=direction==2?y:x;var path=normalPath[(15-forward)*16+across];
    bool isPath=(path.r==98&&path.g==123&&path.b==38)||(path.r==94&&path.g==113&&path.b==46)||(path.r==73&&path.g==87&&path.b==38);
    if(!isPath)continue;float reach=4+2*Mathf.Clamp01(1-Mathf.Abs(across-7.5f)/4.5f);float mix=Mathf.Clamp01((reach+1-forward)/2f);if(mix>0)pixels[y*16+x]=(Color32)Color.Lerp(soil[y*16+x],path,mix);
   }
   var tex=new Texture2D(16,16,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;tex.wrapMode=TextureWrapMode.Clamp;tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(root+name+".png",tex.EncodeToPNG());var sprite=Sprite.Create(tex,new Rect(0,0,16,16),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);var tile=ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();tile.sprite=sprite;owned.Add(tile);owned.Add(sprite);owned.Add(tex);return tile;}
   var fromNorth=Transition(125,0,"transition-from-north");var fromSouth=Transition(215,1,"transition-from-south");var fromWest=Transition(95,2,"transition-from-west");
   int captures=0;
   for(int layout=1;layout<2;layout++){
    fenceRoot.SetActive(layout==2);bool alternate=false;var delta=layout>0?Vector3.down:Vector3.zero;orchardRoot.transform.position=orchardOriginal+delta;for(int i=0;i<treeTransforms.Length;i++)treeTransforms[i].position=treePositions[i]+delta;
    foreach(var item in scenery)item.sr.enabled=alternate?false:item.enabled;foreach(var item in removedTiles)item.map.SetTile(item.cell,alternate?null:item.tile);
    var orchardOrigin=Vector3Int.RoundToInt(orchardRoot.transform.position);var orchardWorld=new System.Collections.Generic.HashSet<Vector3Int>(orchardCells.Select(c=>c+orchardOrigin));
    for(int tier=0;tier<1;tier++)for(int cap=1;cap<2;cap++){
     d.Quests[TownPathProgression.DirtQuest]=new GameData.QuestRecord{Completed=tier>=1};d.Quests[TownPathProgression.PavingQuest]=new GameData.QuestRecord{Completed=tier==2};roads.Refresh();
     var cells=def.Cells(6,true);cells.Remove(new Vector3Int(-58,-2,0));cells.Remove(new Vector3Int(-58,-1,0));for(int y=-6;y<=-4;y++)cells.Remove(new Vector3Int(-55,y,0));for(int y=-24;y<=-23;y++)cells.Remove(new Vector3Int(-55,y,0));if(tier!=0)cells.Add(new Vector3Int(-57,0,0));
     var threshold=new System.Collections.Generic.HashSet<Vector3Int>(borderThresholds);
     if(layout>0){for(int y=-15;y<=-3;y++)cells.Add(new Vector3Int(-60,y,0));for(int x=-59;x<=-58;x++)cells.Add(new Vector3Int(x,-15,0));foreach(int y in new[]{-16,-15,-14})threshold.Add(new Vector3Int(-57,y,0));}

     if(tier==2)cells.RemoveWhere(c=>(fieldWorld.Contains(c)&&!gates.Contains(c))||orchardWorld.Contains(c));
     var allTerrain=new System.Collections.Generic.HashSet<Vector3Int>(cells);allTerrain.UnionWith(fieldWorld);allTerrain.UnionWith(orchardWorld);
     foreach(var f in fieldMaps)foreach(var c in f.cells){var world=c+Vector3Int.RoundToInt(f.origin);var mask=TownPathDefinition.Neighbours(allTerrain,world);f.map.SetTile(c,tier!=1&&gates.Contains(world)?null:def.dirt[mask]);}
     foreach(var c in orchardCells){var mask=TownPathDefinition.Neighbours(allTerrain,c+orchardOrigin);orchardMap.SetTile(c,def.dirt[mask]);}
     fieldMaps[0].map.SetTile(new Vector3Int(2,0,0),fromSouth);fieldMaps[1].map.SetTile(new Vector3Int(3,4,0),fromNorth);fieldMaps[2].map.SetTile(new Vector3Int(4,4,0),fromNorth);orchardMap.SetTile(new Vector3Int(0,3,0),fromWest);
     surface.ClearAllTiles();edging.ClearAllTiles();shadow.ClearAllTiles();edging.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder=tier==2?3:-1;surface.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder=tier==2?2:-1;
     foreach(var c in cells){var mask=TownPathDefinition.Neighbours(allTerrain,c);surface.SetTile(c,tier==0?def.grass[mask]:tier==1?def.dirt[mask]:def.pavingFill[((c.x%2+2)%2)+2*((c.y%2+2)%2)]);}
     surface.SetTile(new Vector3Int(-57,-1,0),def.grass[17]);surface.SetTile(new Vector3Int(-54,-5,0),def.grass[17]);surface.SetTile(new Vector3Int(-54,-23,0),def.grass[17]);surface.SetTile(new Vector3Int(-58,-15,0),def.grass[68]);
     if(tier==2){var edges=new System.Collections.Generic.HashSet<Vector3Int>();foreach(var c in cells)foreach(var off in TownPathDefinition.Offsets){var q=c+off;if(!cells.Contains(q)&&((!fieldWorld.Contains(q)&&!orchardWorld.Contains(q))||threshold.Contains(q)))edges.Add(q);}foreach(var c in edges){if(cap==0&&centreEnds.Contains(c))continue;edging.SetTile(c,def.pavingEdge[TownPathDefinition.Neighbours(cells,c)]);}}
     if(tier==2){foreach(var c in cells)shadow.SetTile(c,surface.GetTile(c));foreach(var c in edging.cellBounds.allPositionsWithin){var t=edging.GetTile(c);if(t!=null)shadow.SetTile(c,t);}}
     for(int mode=0;mode<2;mode++){grid.SetActive(mode==1);var key="layout-"+layout+"-tier-"+tier+"-cap-"+cap+"-grid-"+mode;for(int i=0;i<3;i++)Shot(key+"-field-"+(i+1),origins[i]+new Vector2(4.5f,2.5f),4.5f,1536,1152);Shot(key+"-orchard",(Vector2)orchardRoot.transform.position+new Vector2(4.5f,3),4.5f,1280,1152);Shot(key+"-all",new Vector2(-60,-12),20,1440,1600);captures+=5;}
    }
   }
   foreach(var item in scenery)item.sr.enabled=item.enabled;foreach(var item in removedTiles)item.map.SetTile(item.cell,item.tile);
   UnityEngine.Object.DestroyImmediate(white);
   return "10 grass-only renders with ordinary edged approach tiles and three oriented transition tiles";
  }finally{foreach(var item in scenery)if(item.sr)item.sr.enabled=item.enabled;foreach(var item in removedTiles)if(item.map)item.map.SetTile(item.cell,item.tile);foreach(var o in owned)UnityEngine.Object.DestroyImmediate(o);foreach(var sr in originals)sr.enabled=true;foreach(var p in moved)p.t.position=p.pos;foreach(var rootObj in roots)UnityEngine.Object.DestroyImmediate(rootObj);UnityEngine.Object.DestroyImmediate(go);}
 }
}
