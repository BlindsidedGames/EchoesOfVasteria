using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Tilemaps;using UnityEditor;
public static class ValidateAllTiles {
 public class Plan{public Sheet[] sheets;public Asset[] assets;}
 public class Sheet{public string path;public Entry[] sprites;}
 public class Entry{public string guid,localId,name,old_name;public RectData rect;public Point pivot;public float pixelsPerUnit;}
 public class RectData{public float x,y,width,height;}
 public class Point{public float x,y;}
 public class Ref{public string property,guid,localId;}
 public class Asset{public string path,guid,localId,name,type;public Ref[] sprites;public string[] unresolvedProperties;}
 public static object Main(){
  var jt=Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json",true);var p=(Plan)jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText("docs/TerrainTiles/AllPacks/tiles.json"),typeof(Plan)});
  int spriteCount=0,refCount=0,missing=0;
  foreach(var sh in p.sheets){var sprites=AssetDatabase.LoadAllAssetsAtPath(sh.path).OfType<Sprite>().ToArray();if(sprites.Length!=sh.sprites.Length)throw new Exception("Sprite count changed: "+sh.path);
   foreach(var e in sh.sprites){var s=sprites.SingleOrDefault(x=>{string g;long id;return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(x,out g,out id)&&g==e.guid&&id.ToString()==e.localId;});if(s==null)throw new Exception("Missing identity: "+sh.path+" "+e.localId);
    if(s.name!=e.name)throw new Exception("Wrong name: "+s.name+" expected "+e.name);
    if(s.rect!=new Rect(e.rect.x,e.rect.y,e.rect.width,e.rect.height)||s.pivot!=new Vector2(e.pivot.x,e.pivot.y)||s.pixelsPerUnit!=e.pixelsPerUnit)throw new Exception("Geometry changed: "+e.name);
    if(e.old_name.StartsWith("Grass_")!=s.name.StartsWith("Grass_")||e.old_name.StartsWith("Water_Middle")!=s.name.StartsWith("Water_Middle"))throw new Exception("Habitat predicate changed");spriteCount++;
   }
  }
  foreach(var a in p.assets){if(AssetDatabase.GUIDToAssetPath(a.guid)!=a.path)throw new Exception("Tile GUID/path changed: "+a.path);var obj=AssetDatabase.LoadAllAssetsAtPath(a.path).OfType<TileBase>().SingleOrDefault(x=>{string g;long id;return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(x,out g,out id)&&g==a.guid&&id.ToString()==a.localId;});if(obj==null||obj.name!=a.name||obj.GetType().FullName!=a.type)throw new Exception("Tile identity/name/type changed: "+a.path);
   var refs=new Dictionary<string,string>();var unresolved=new List<string>();var it=new SerializedObject(obj).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference){if(it.objectReferenceValue is Sprite s){string g;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out g,out id);refs.Add(it.propertyPath,g+":"+id);refCount++;}else if(!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId))&&it.objectReferenceValue==null)unresolved.Add(it.propertyPath);}
   if(refs.Count!=a.sprites.Length||a.sprites.Any(r=>!refs.TryGetValue(r.property,out var actual)||actual!=r.guid+":"+r.localId))throw new Exception("Sprite references/frame order changed: "+a.path);
   if(!unresolved.OrderBy(x=>x).SequenceEqual(a.unresolvedProperties.OrderBy(x=>x)))throw new Exception("New missing reference: "+a.path);if(unresolved.Count>0)missing++;
  }
  var result=new{sourceSheets=p.sheets.Length,sprites=spriteCount,tileObjects=p.assets.Length,serializedSpriteReferences=refCount,existingUnresolvedTileObjects=missing,newMissingReferences=0,guidsLocalIdsGeometryAndFrameOrderPreserved=true,playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
  File.WriteAllText("docs/TerrainTiles/AllPacks/import-validation.json",(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
 }
}
