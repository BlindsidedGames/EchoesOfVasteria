using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class ValidateRegularTerrain
{
    public class Plan { public Sheet[] sheets; public Asset[] assets; }
    public class Sheet { public string path,guid; public Entry[] sprites; }
    public class Entry { public string name,old_name,local_id; public RectData rect; public Point pivot; }
    public class RectData { public float x,y,width,height; }
    public class Point { public float x,y; }
    public class Asset { public string path,guid; }
    public static object Main()
    {
        var jt=Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json",true);
        var p=(Plan)jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText("docs/TerrainTiles/LightGreenTerrain.tiles.json"),typeof(Plan)});
        int count=0;int prefixChecks=0;
        foreach(var sheet in p.sheets)
        {
            if(AssetDatabase.GUIDToAssetPath(sheet.guid)!=sheet.path)throw new Exception("Source GUID/path mismatch");
            var sprites=AssetDatabase.LoadAllAssetsAtPath(sheet.path).OfType<Sprite>().ToArray();
            if(sprites.Length!=sheet.sprites.Length)throw new Exception("Missing/extra sprites: "+sheet.path);
            foreach(var e in sheet.sprites)
            {
                var s=sprites.Single(x=>x.name==e.name);string guid;long id;
                if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out guid,out id)||guid!=sheet.guid||id.ToString()!=e.local_id)throw new Exception("Sprite identity changed: "+e.name);
                if(s.rect!=new Rect(e.rect.x,e.rect.y,e.rect.width,e.rect.height)||s.pivot!=new Vector2(e.pivot.x,e.pivot.y))throw new Exception("Sprite geometry changed: "+e.name);
                if(e.old_name.StartsWith("Grass_")!=e.name.StartsWith("Grass_")||e.old_name.StartsWith("Water_Middle")!=e.name.StartsWith("Water_Middle"))throw new Exception("Habitat classification changed: "+e.name);
                count++;prefixChecks++;
            }
        }
        foreach(var a in p.assets)
        {
            if(AssetDatabase.GUIDToAssetPath(a.guid)!=a.path)throw new Exception("Asset GUID/path mismatch: "+a.path);
            foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(a.path))
            {
                var so=new SerializedObject(obj);var it=so.GetIterator();
                while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId))&&it.objectReferenceValue==null)throw new Exception("Missing reference: "+a.path+" "+it.propertyPath);
            }
        }
        return new{sourceSheets=p.sheets.Length,sprites=count,assetFiles=p.assets.Length,habitatPrefixChecks=prefixChecks,
                   guidsAndLocalIdsPreserved=true,rectanglesAndPivotsPreserved=true,missingReferences=0,
                   playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
    }
}
