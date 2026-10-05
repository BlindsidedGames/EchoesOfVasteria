using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public static class InventoryAllTiles
{
    public class Scope { public Candidate[] additionalCandidates; }
    public class Candidate { public string path; }
    public static object Main()
    {
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")).ToArray();
        var tiles=new List<object>(); var sources=new HashSet<string>();
        foreach(var path in paths.Where(p=>p.EndsWith(".asset")))
        foreach(var tile in AssetDatabase.LoadAllAssetsAtPath(path).OfType<TileBase>())
        {
            string guid; long id; AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tile,out guid,out id);
            var refs=new List<object>();var missing=new List<string>(); var so=new SerializedObject(tile);var it=so.GetIterator();
            while(it.Next(true)) if(it.propertyType==SerializedPropertyType.ObjectReference && it.objectReferenceValue is Sprite s)
            {
                string sg;long sid;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out sg,out sid);
                var sp=AssetDatabase.GetAssetPath(s);sources.Add(sp);
                refs.Add(new{property=it.propertyPath,path=sp,name=s.name,guid=sg,localId=sid.ToString()});
            }
            it=so.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference && !it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)) && it.objectReferenceValue==null)missing.Add(it.propertyPath);
            tiles.Add(new{path,name=tile.name,type=tile.GetType().FullName,guid,localId=id.ToString(),sprites=refs,unresolvedProperties=missing});
        }
        var explicitSheets=paths.Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase) &&
            (p.Contains("/Tiles/")||p.Contains("/Tilemaps/")||p.Contains("/Tilesets/"))).ToArray();
        foreach(var p in explicitSheets)sources.Add(p);
        var jt=Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json",true);
        var extra=(Scope)jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText("output/scene-review/all-tiles-independent-scope-audit.json"),typeof(Scope)});
        foreach(var c in extra.additionalCandidates)sources.Add(c.path);
        var sheets=new List<object>();
        foreach(var path in sources.OrderBy(p=>p))
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(s=>{
                string guid;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out guid,out id);
                return new{name=s.name,guid,localId=id.ToString(),rect=new{x=s.rect.x,y=s.rect.y,width=s.rect.width,height=s.rect.height},pivot=new{x=s.pivot.x,y=s.pivot.y},pixelsPerUnit=s.pixelsPerUnit};
            }).ToArray();
            sheets.Add(new{path,explicitTileDirectory=explicitSheets.Contains(path),sprites});
        }
        var jsonType=Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json",true);
        var payload=new{generatedUtc=DateTime.UtcNow.ToString("o"),tiles,sheets,otherPackTextures=paths.Where(p=>p.StartsWith("Assets/Art/Packs/")&&p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&!sources.Contains(p)).ToArray()};
        var json=(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{payload});
        Directory.CreateDirectory("docs/TerrainTiles/AllPacks");
        File.WriteAllText("docs/TerrainTiles/AllPacks/inventory.json",json);
        return new{tileObjects=tiles.Count,sourceSheets=sheets.Count,explicitTileSheets=explicitSheets.Length,playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling};
    }
}
