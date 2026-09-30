using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class TownZoomQA
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
  Action<string,bool> check=(name,ok)=>rows.Add(name+" PASS="+ok);
  Action<float> wheel=v=>InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(550,300),scroll=new Vector2(0,v)});
  Action<string> capture=name=>{cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("output/scene-review/"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;};
  steps.Enqueue(()=>check("Standard size 18",Mathf.Abs(cam.orthographicSize-18)<.01));
  steps.Enqueue(()=>wheel(1));
  steps.Enqueue(()=>check("One normalized wheel notch changes size by 14 percent",Mathf.Abs(cam.orthographicSize-18*Mathf.Exp(-.15f))<.02));
  steps.Enqueue(()=>wheel(8));
  steps.Enqueue(()=>check("Closest size 9",Mathf.Abs(cam.orthographicSize-9)<.01));
  steps.Enqueue(()=>wheel(-8));
  steps.Enqueue(()=>check("16:9 farthest size 27",Mathf.Abs(cam.orthographicSize-27)<.01));
  for(int a=0;a<2;a++) {int mode=a;
   steps.Enqueue(()=>{rt.Release();rt.width=mode==0?1024:2048;rt.Create();cam.aspect=rt.width/(float)rt.height;wheel(-8);});
   for(int i=0;i<4;i++){int corner=i;
    steps.Enqueue(()=>target.position=new Vector3(corner%2==0?-1000:1000,corner<2?-1000:1000,0));
    steps.Enqueue(()=>{var lo=cam.ViewportToWorldPoint(new Vector3(0,0,10));var hi=cam.ViewportToWorldPoint(new Vector3(1,1,10));check("Far zoom aspect "+cam.aspect+" corner "+corner,lo.x>=bounds.min.x-.01&&lo.y>=bounds.min.y-.01&&hi.x<=bounds.max.x+.01&&hi.y<=bounds.max.y+.01);if(corner==3)capture("town-zoom-out-"+mode);});
   }
  }
  steps.Enqueue(()=>{rt.Release();rt.width=1024;rt.Create();cam.aspect=16f/9;target.position=oldPos;wheel(8);});
  foreach(var key in new[]{Key.W,Key.RightArrow}){var k=key;steps.Enqueue(()=>{before=target.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(k));});steps.Enqueue(()=>{var d=target.position-before;check("Movement "+k,k==Key.W?d.y>.1f:d.x>.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());});}
  steps.Enqueue(()=>{before=target.position;InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=new Vector2(-.7f,0)});});
  steps.Enqueue(()=>{check("Analog stick left",target.position.x<before.x-.1f);InputSystem.QueueStateEvent(gamepad,new GamepadState());});
  steps.Enqueue(()=>{before=target.position;InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.DpadDown));});
  steps.Enqueue(()=>{check("D-pad down",target.position.y<before.y-.1f);InputSystem.QueueStateEvent(gamepad,new GamepadState());});
  steps.Enqueue(()=>{priorSize=cam.orthographicSize;InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(350,300)});InputSystem.QueueStateEvent(touch,new TouchState{touchId=2,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(650,300)});});
  steps.Enqueue(()=>{InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=new Vector2(425,300)});InputSystem.QueueStateEvent(touch,new TouchState{touchId=2,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=new Vector2(575,300)});});
  steps.Enqueue(()=>{check("Pinch doubles visible span",Mathf.Abs(cam.orthographicSize-priorSize*2)<.05);});
  EditorApplication.CallbackFunction tick=null;double next=EditorApplication.timeSinceStartup+1;
  Action clean=()=>{EditorApplication.update-=tick;InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(gamepad);InputSystem.QueueStateEvent(keyboard,new KeyboardState());cam.targetTexture=oldRT;cam.rect=oldRect;cam.ResetAspect();aspect.enabled=enabled;target.position=oldPos;rt.Release();UnityEngine.Object.DestroyImmediate(rt);File.WriteAllLines("output/scene-review/town-zoom-qa.txt",rows);};
  tick=()=>{if(EditorApplication.timeSinceStartup<next)return;try{if(steps.Count==0){clean();return;}steps.Dequeue()();next=EditorApplication.timeSinceStartup+.7;}catch(Exception e){rows.Add("FAIL "+e);clean();}};EditorApplication.update+=tick;return "Zoom and shared input QA scheduled";
 }
}

