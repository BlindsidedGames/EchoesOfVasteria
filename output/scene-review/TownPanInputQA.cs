using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class TownPanInputQA
{
    public static object Main()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play mode required");
        var target = GameObject.Find("Hometown/Town Camera Pan Target").transform;
        var original = target.position;
        var mouse = Mouse.current;
        var touch = InputSystem.AddDevice<Touchscreen>();
        var rows = new List<string>();
        var steps = new Queue<Action>();
        Vector3 before = default;
        Action<Vector2, bool> move = (p, down) => InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = (ushort)(down ? 1 : 0) });
        Action<string, bool> check = (name, shouldMove) => {
            bool moved = Vector3.Distance(before, target.position) > .1f;
            rows.Add(name + " PASS=" + (moved == shouldMove) + " " + before + " -> " + target.position);
        };
        steps.Enqueue(() => move(new Vector2(1400,1100), false));
        steps.Enqueue(() => {before=target.position; move(new Vector2(1400,1100),true);});
        steps.Enqueue(() => move(new Vector2(1550,1200),true));
        steps.Enqueue(() => {check("Mouse world drag",true); move(new Vector2(1550,1200),false);});
        steps.Enqueue(() => {before=target.position; move(new Vector2(450,1640),true);});
        steps.Enqueue(() => move(new Vector2(1400,1100),true));
        steps.Enqueue(() => {check("UI-origin drag blocked",false); move(new Vector2(1400,1100),false);});
        steps.Enqueue(() => {before=target.position; InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(1400,1100)});});
        steps.Enqueue(() => InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=new Vector2(1250,1000)}));
        steps.Enqueue(() => {check("Single touch drag",true); InputSystem.QueueStateEvent(touch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=new Vector2(1250,1000)});});
        EditorApplication.CallbackFunction tick=null;
        double next=0;
        Action clean=()=>{EditorApplication.update-=tick;InputSystem.RemoveDevice(touch);target.position=original;File.WriteAllLines("output/scene-review/town-pan-input-qa.txt",rows);};
        tick=()=>{if(EditorApplication.timeSinceStartup<next)return;try{if(steps.Count==0){clean();return;}steps.Dequeue()();next=EditorApplication.timeSinceStartup+.35;}catch(Exception e){rows.Add("FAIL "+e);clean();}};
        EditorApplication.update+=tick;
        return "Input System gesture checks scheduled";
    }
}
