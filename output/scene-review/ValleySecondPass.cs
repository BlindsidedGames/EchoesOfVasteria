using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Tilemaps;using UnityEditor;using UnityEditor.SceneManagement;
public static class ValleySecondPass {
 public static object Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying)throw new Exception("Clean edit scene required");
 
 var home=GameObject.Find("Hometown").transform;var existing=home.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.sprite!=null).ToArray();
 var maps=home.GetComponentsInChildren<Tilemap>();var bg=maps.Single(t=>t.name=="BG -5");var walk=maps.Single(t=>t.name=="BG_Walkable -4");
 var root=new GameObject("Lower Valley Cliff Clusters");root.transform.SetParent(home,false);Undo.RegisterCreatedObjectUndo(root,"Dress lower valley");
 var rng=new System.Random(8261);var placed=new List<Bounds>();int count=0;
 Func<float,float,float> random=(a,b)=>a+(float)rng.NextDouble()*(b-a);
 bool Valid(SpriteRenderer r){var b=r.bounds; var local=home.InverseTransformPoint(b.center);if(local.y> -18||local.y< -52||local.x < -52||local.x>10)return false;
  // Keep every sprite off cliff faces, water and shoreline transition tiles.
  for(float x=b.min.x;x<=b.max.x+.2f;x+=.4f)for(float y=b.min.y;y<=b.max.y+.2f;y+=.4f){var p=new Vector3(x,y);var cell=bg.WorldToCell(p);if(bg.GetTile(cell)?.name!="Grass_3_Middle"||walk.HasTile(walk.WorldToCell(p)))return false;}
  var foot=new Bounds(new Vector3(b.center.x,b.min.y+.25f),new Vector3(Mathf.Max(.5f,b.size.x*.65f),Mathf.Min(1.3f,b.size.y),1));
  if(existing.Any(e=>e.bounds.Intersects(foot))||placed.Any(e=>e.Intersects(foot)))return false;placed.Add(foot);return true;}
 bool Add(string name,Vector2 p,string spriteName=null,string prefab=null){GameObject go;if(prefab!=null)go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/AnimatedDecor/Bank/"+prefab+".prefab"));else{var template=existing.First(r=>r.sprite.name==spriteName);go=new GameObject(name);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=template.sprite;sr.sharedMaterial=template.sharedMaterial;sr.sortingLayerID=template.sortingLayerID;}
  go.name=name;go.transform.SetParent(root.transform,false);go.transform.position=home.TransformPoint(new Vector3(Mathf.Round(p.x*16)/16,Mathf.Round(p.y*16)/16,0));var renderer=go.GetComponent<SpriteRenderer>();
  if(!Valid(renderer)){UnityEngine.Object.DestroyImmediate(go);return false;}renderer.sortingOrder=Mathf.RoundToInt(-renderer.bounds.min.y*16);count++;return true;}
 string[] trees={"Medium_Oak_Tree_1","Small_Oak_Tree_1","Medium_Birch_Tree_1","Big_Oak_Tree_1"};
 Vector2[] groves={new(-41,-28),new(-45,-38),new(-48,-49),new(-31,-46),new(-30,-51),new(-7,-22),new(2,-30),new(5,-40),new(1,-49)};
 foreach(var c in groves){for(int i=0;i<0;i++)Add("Valley grove",c+new Vector2(random(-3,3),random(-2.5f,2.5f)),trees[rng.Next(trees.Length)]);}
 Vector2[] clusters={new(-42,-27),new(-42,-33),new(-47,-36),new(-50,-42),new(-51,-48),new(-43,-51),new(-35,-48),new(-30,-42),new(-30,-35),new(-35,-26),new(-9,-20),new(-3,-24),new(3,-29),new(3,-35),new(7,-39),new(7,-44),new(2,-48),new(-3,-52),new(-18,-23),new(-10,-32)};
 foreach(var c in clusters){for(int i=0;i<3;i++)Add("Valley weathered stone",c+new Vector2(random(-2,2),random(-1.5f,1.5f)),null,"Rocks/Rock_"+new[]{11,12,13,14}[rng.Next(4)]+"_Anim");
 for(int i=0;i<2;i++)Add("Valley undergrowth",c+new Vector2(random(-3,3),random(-2.5f,2.5f)),"Outdoor_Decor_"+new[]{83,84,126,127}[rng.Next(4)]);
 for(int i=0;i<3;i++)Add("Valley meadow plants",c+new Vector2(random(-4,4),random(-3,3)),null,i%3==0?"Grass/Flower_Grass_"+new[]{2,9,11}[rng.Next(3)]+"_Anim":"Grass/Grass_"+rng.Next(1,4)+"_Anim");}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,"output/scene-review/Main.after-valley-live.unity",true);return new{added=count};
 }
}

