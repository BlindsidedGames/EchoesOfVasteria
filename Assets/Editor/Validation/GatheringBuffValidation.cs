using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TimelessEchoes.Buffs;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using TimelessEchoes.Utilities;

public static class GatheringBuffValidation
{
    static int passed;
    static readonly BindingFlags Private = BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message) { if(!value) throw new Exception(message); passed++; }
    static void Set(object obj,string field,object value) => obj.GetType().GetField(field,Private).SetValue(obj,value);
    static object Call(object obj,string method,params object[] args) => obj.GetType().GetMethod(method,Private).Invoke(obj,args);
    [MenuItem("Tools/Validation/Validate Gathering Buffs")]
    public static void RunFromMenu() => Debug.Log(Main());

    public static string Main()
    {
        if(EditorApplication.isPlaying) throw new Exception("Requires Edit mode; no real save is loaded.");
        System.IO.Directory.CreateDirectory("Library/BuffReplacement");
        passed=0;
        var migration=new Migration_GatheringBuffs();
        var data=new GameData{SchemaVersion=2,ProspectorTaskId=0};data.BuffSlots[0]="Slipstream";
        data.CauldronCardCounts["BUFF:Slipstream"]=120;
        migration.Apply(data);migration.Apply(data);
        Check(data.BuffSlots[0]=="Prospector","Slot migration");
        Check(data.CauldronCardCounts["BUFF:Prospector"]==120&&!data.CauldronCardCounts.ContainsKey("BUFF:Slipstream"),"Card migration idempotent");
        Check(data.ProspectorTaskId==-1,"No synthetic target assigned on migration");
        Check(Math.Abs(BuffManager.CollectorBonus(100,0)-1)<.001,"Collector new task");
        Check(Math.Abs(BuffManager.CollectorBonus(100,100)-.5)<.001,"Collector 100 completions");
        Check(BuffManager.CollectorBonus(100,10000)<.01,"Collector familiar tasks");
        Check(BuffManager.CollectorBonus(100000,0)==2,"Collector power cap");
        Check(AssetDatabase.LoadAssetAtPath<BuffRecipe>("Assets/Resources/Buffs/Slipstream.asset")==null,"Slipstream asset removed");
        foreach(var name in new[]{"Windfall","Collectors Instinct","Echo Resonance"}) {
            var b=Resources.Load<BuffRecipe>("Buffs/"+name);
            Check(b!=null&&b.buffIcon!=null,"Buff/icon missing: "+name);
            Check(!b.baseEffects.Exists(e=>e.type==BuffEffectType.TimeScalePercent),"Time scaling remains");
            Check(b.baseDuration>0&&b.requiredQuest!=null,"Buff progression missing");
        }
        var legacy=new GameData{SchemaVersion=2,LastGameVersion=Application.version};
        legacy.BuffSlots[0]="Slipstream";legacy.CauldronCardCounts["BUFF:Slipstream"]=80;
        var migrated=SaveMigrationRunner.TryMigrate(legacy,Application.version);
        Check(migrated.Succeeded,"Full migration pipeline succeeds: "+migrated.Error);
        Check(migrated.Data.SchemaVersion==3&&migrated.Data.BuffSlots[0]=="Prospector","Full pipeline upgrades schema and slots");
        Check(legacy.SchemaVersion==2&&legacy.BuffSlots[0]=="Slipstream","Migration leaves source data untouched");
        Check(migrated.Data.CauldronCardCounts["BUFF:Prospector"]==80,"Full pipeline preserves cards");
        Check(BuffRecipe.LoadAvailable().All(b=>!b.IsRetired),"All playable buff pools exclude removed recipes");
        Check(Resources.Load<GameObject>("UI/ProspectorPicker")==null,"Picker removed from runtime Resources");
        var milestone=Resources.Load<TimelessEchoes.Quests.QuestData>("Quests/BuffS/Slipstream20");
        foreach(string name in new[]{"Windfall","Collectors Instinct","Echo Resonance"})
            Check(Resources.Load<BuffRecipe>("Buffs/"+name).requiredQuest==milestone,"Existing gathering milestone unlocks "+name);
        var originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var stagingScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(stagingScene);
        var scene=EditorSceneManager.NewPreviewScene();
        var previousOracle=Oracle.oracle;
        var singleton=typeof(Singleton<BuffManager>).GetProperty("Instance").GetSetMethod(true);
        var previousManager=BuffManager.Instance;
        var previousSkills=TimelessEchoes.Skills.SkillController.Instance;
        var skillSetter=typeof(TimelessEchoes.Skills.SkillController).GetProperty("Instance").GetSetMethod(true);
        float scale=Time.timeScale, fixedDelta=Time.fixedDeltaTime;
        GameObject host=null,canvasObject=null,cameraObject=null;
        Resource resource=null;TaskData task=null;
        try {
            host=new GameObject("Disposable gathering validation");host.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host,scene);
            var oracle=host.AddComponent<Oracle>();oracle.saveData=new GameData();Oracle.oracle=oracle;
            var manager=host.AddComponent<BuffManager>();singleton.Invoke(null,new object[]{manager});
            skillSetter.Invoke(null,new object[]{host.AddComponent<TimelessEchoes.Skills.SkillController>()});
            task=ScriptableObject.CreateInstance<TaskData>();task.taskID=999999;task.weight=1;
            var buffs=(List<BuffManager.ActiveBuff>)typeof(BuffManager).GetField("activeBuffs",Private).GetValue(manager);
            var active=new BuffManager.ActiveBuff{remaining=120,effects=new List<BuffEffect>{new BuffEffect{type=BuffEffectType.EchoResonanceRewardPercent,value=25}}};
            buffs.Add(active);active.gathering.echoCompletions[task.taskID]=Time.time;
            Check(manager.GetResonanceBonus(task,false,100)==25,"Hero resonance payout");
            Check(manager.GetResonanceBonus(task,true,100)==0,"Echo cannot follow itself");
            manager.RecordGatheringCompletion(task,false,new Dictionary<Resource,double>(),new List<Resource>(),null);
            Check(manager.GetResonanceBonus(task,false,100)==0,"Resonance consumed once");
            active.gathering.echoCompletions[task.taskID]=Time.time-11;
            Check(manager.GetResonanceBonus(task,false,100)==0,"Resonance expiry");
            active.remaining=0;active.gathering.echoCompletions[task.taskID]=Time.time;
            Check(manager.GetResonanceBonus(task,false,100)==0,"Inactive resonance");buffs.Clear();
            resource=ScriptableObject.CreateInstance<Resource>();
            var state=new GatheringBuffState();
            for(int i=0;i<9;i++) Check(!state.AccumulateWindfall(new Dictionary<Resource,double>{{resource,5}}),"Early Windfall");
            Check(state.AccumulateWindfall(new Dictionary<Resource,double>{{resource,5}}),"Tenth Windfall");
            Check(state.bundle[resource]==50,"Windfall records all ten rewards");
            var resources=host.AddComponent<ResourceManager>();resource.DisableAlterEcho=true;
            var windfall=new BuffManager.ActiveBuff{remaining=120,effects=new List<BuffEffect>{new BuffEffect{type=BuffEffectType.WindfallRewardPercent,value=20}}};
            buffs.Add(windfall);
            resources.BeginBatch();
            for(int i=0;i<10;i++) manager.RecordGatheringCompletion(task,i%2==0,new Dictionary<Resource,double>{{resource,5}},new List<Resource>{resource},resources);
            resources.EndBatch();
            Check(resources.GetAmount(resource)==10,"Windfall pays actual inventory once at ten completions");
            Check(windfall.gathering.completed==0&&windfall.gathering.bundle.Count==0,"Windfall resets after payout");
            buffs.Clear();
            var a=new BuffManager.ActiveBuff{remaining=120,effects=new List<BuffEffect>{new BuffEffect{type=BuffEffectType.ProspectorWeightPercent,value=100},new BuffEffect{type=BuffEffectType.CollectorWeightPercent,value=100}}};
            oracle.saveData.ProspectorTaskId=task.taskID;buffs.Add(a);
            Check(manager.GetGatheringWeightMultiplier(task)==2,"Retired Prospector contributes no spawn weight");
            oracle.saveData.BuffSlots[0]="Prospector";oracle.saveData.AutoBuffSlots[0]=true;
            oracle.saveData.CauldronCardCounts["BUFF:Prospector"]=91;
            Call(manager,"LoadSlots");
            Check(oracle.saveData.BuffSlots[0]==null&&!oracle.saveData.AutoBuffSlots[0],"Removed buff assignment cleared");
            Check(oracle.saveData.CauldronCardCounts["BUFF:Prospector"]==91,"Unused saved cards left untouched");
            Check(buffs.Count==0,"Profile load clears gathering activations and their pending rewards");
            Check(manager.GetGatheringWeightMultiplier(task)==1,"Spawn weighting restored after profile load");
            var prospector=ScriptableObject.CreateInstance<BuffRecipe>();prospector.name="Prospector";
            Check(!manager.CanActivate(prospector),"Removed Prospector cannot cast");UnityEngine.Object.DestroyImmediate(prospector);
            Check(Time.timeScale==scale,"Gathering changed time scale");

        }
        finally {
            Oracle.oracle=previousOracle;
            if(canvasObject!=null) UnityEngine.Object.DestroyImmediate(canvasObject);
            if(cameraObject!=null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if(host!=null) UnityEngine.Object.DestroyImmediate(host);
            singleton.Invoke(null,new object[]{previousManager});
            skillSetter.Invoke(null,new object[]{previousSkills});
            if(resource!=null) UnityEngine.Object.DestroyImmediate(resource);
            if(task!=null) UnityEngine.Object.DestroyImmediate(task);
            Time.timeScale=scale;Time.fixedDeltaTime=fixedDelta;
            EditorSceneManager.ClosePreviewScene(scene);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(originalScene);
            EditorSceneManager.CloseScene(stagingScene,true);
        }
        string report=passed+" assertions passed. Isolated Edit-mode objects and in-memory save only; no save or cloud APIs called.";
        System.IO.File.WriteAllText("Library/BuffReplacement/validation.txt",report);
        return report;
    }
}
