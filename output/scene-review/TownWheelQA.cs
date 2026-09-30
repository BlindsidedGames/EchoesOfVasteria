using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class TownWheelQA
{
 public static object Main()
 {
  var cam=Camera.main; var target=GameObject.Find("Hometown/Town Camera Pan Target").transform;
  var bounds=GameObject.Find("Hometown/Town Camera Land Bounds").GetComponent<BoxCollider2D>().bounds;
  var aspect=cam.GetComponent<TimelessEchoes.AspectRatioBox>(); bool enabled=aspect.enabled;
  var oldRT=cam.targetTexture; var oldRect=cam.rect; var oldPos=target.position;
  var rt=new RenderTexture(1024,576,24); rt.Create(); aspect.enabled=false; cam.rect=new Rect(0,0,1,1);cam.targetTexture=rt;cam.aspect=16f/9;
  var mouse=Mouse.current; var keyboard=Keyboard.current; var gamepad=InputSystem.AddDevice<Gamepad>(); var touch=InputSystem.AddDevice<Touchscreen>();
  var steps=new Queue<Action>();var rows=new List<string>();Vector3 before=default;float priorSize=0;
  Action<string,bool> check=(name,ok)=>rows.Add(name+" PASS="+ok+" size="+cam.orthographicSize);
  Action<float> wheel=v=>InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(550,300),scroll=new Vector2(0,v)});
  Action<string> capture=name=>{cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("output/scene-review/"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;};
  steps.Enqueue(()=>wheel(20)); steps.Enqueue(()=>wheel(-2.70465f));
  steps.Enqueue(()=>wheel(1));
  steps.Enqueue(()=>check("One normalized wheel notch changes size by 14 percent",Mathf.Abs(cam.orthographicSize-18*Mathf.Exp(-.15f))<.02));
  steps.Enqueue(()=>wheel(8));
  steps.Enqueue(()=>check("Closest size 9",Mathf.Abs(cam.orthographicSize-9)<.01));
  steps.Enqueue(()=>wheel(-8));
  steps.Enqueue(()=>check("16:9 farthest size 27",Mathf.Abs(cam.orthographicSize-27)<.01));
  EditorApplication.CallbackFunction tick=null;double next=EditorApplication.timeSinceStartup+1;
  Action clean=()=>{EditorApplication.update-=tick;InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(gamepad);InputSystem.QueueStateEvent(keyboard,new KeyboardState());cam.targetTexture=oldRT;cam.rect=oldRect;cam.ResetAspect();aspect.enabled=enabled;target.position=oldPos;rt.Release();UnityEngine.Object.DestroyImmediate(rt);File.WriteAllLines("output/scene-review/town-wheel-qa.txt",rows);};
  tick=()=>{if(EditorApplication.timeSinceStartup<next)return;try{if(steps.Count==0){clean();return;}steps.Dequeue()();next=EditorApplication.timeSinceStartup+.7;}catch(Exception e){rows.Add("FAIL "+e);clean();}};EditorApplication.update+=tick;return "Zoom and shared input QA scheduled";
 }
}



