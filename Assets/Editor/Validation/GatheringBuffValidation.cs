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
        foreach(var name in new[]{"Prospector","Windfall","Collectors Instinct","Echo Resonance"}) {
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
            Check(manager.GetGatheringWeightMultiplier(task)==3,"Additive spawn stacking");
            Call(manager,"LoadSlots");
            Check(buffs.Count==0,"Profile load clears gathering activations and their pending rewards");
            Check(manager.GetGatheringWeightMultiplier(task)==1,"Spawn weighting restored after profile load");
            var prospector=Resources.Load<BuffRecipe>("Buffs/Prospector");oracle.saveData.ProspectorTaskId=-1;
            Check(!manager.CanActivate(prospector),"Unconfigured Prospector cannot cast");
            Check(Time.timeScale==scale,"Gathering changed time scale");
            canvasObject=new GameObject("Validation Canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject,scene);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.pixelPerfect=true;
            canvas.referencePixelsPerUnit=16;
            var scaler=canvasObject.AddComponent<CanvasScaler>();
            scaler.referencePixelsPerUnit=16;scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,432);scaler.matchWidthOrHeight=1;
            cameraObject=new GameObject("Validation Camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.GetComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.13f);camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            canvas.worldCamera=camera;canvas.planeDistance=1;
            var ui=UnityEngine.Object.Instantiate(Resources.Load<ProspectorPicker>("UI/ProspectorPicker"),canvas.transform);
            var recipe=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BuffRecipe.prefab").GetComponent<References.UI.BuffRecipeUIReferences>();
            Check(ui.rowPrefab.image.color==recipe.purchaseButton.image.color,"Picker uses native button tint");
            Check(ui.selectedText.color==recipe.purchaseButton.GetComponentInChildren<TMPro.TMP_Text>().color,"Picker uses native text color");
            Check(recipe.targetButton.image.color==recipe.purchaseButton.image.color,"Target row uses native button tint");
            Check(recipe.targetText.color==recipe.purchaseButton.GetComponentInChildren<TMPro.TMP_Text>().color,"Target row uses native text color");
            Call(ui,"Awake");
            var tasks=Resources.LoadAll<TaskData>("Tasks").OrderBy(t=>t.taskName).ToArray();
            Set(ui,"tasks",tasks);Set(ui,"skills",new[]{"All skills"});Set(ui,"owner",canvasObject);
            ui.gameObject.SetActive(true);Call(ui,"Refresh");
            var rows=(List<Button>)typeof(ProspectorPicker).GetField("rows",Private).GetValue(ui);
            Check(rows.Count==tasks.Length,"Picker full task inventory");
            Check(ui.scroll.transform.parent.GetComponent<Image>().sprite.name=="UI_Frames_1","Picker uses recessed boosts frame");
            Check(ui.panel.Find("Title")==null,"Picker title removed");
            Check(ui.scroll.viewport.GetComponent<Mask>()!=null&&!ui.scroll.viewport.GetComponent<Mask>().showMaskGraphic,"Picker uses hidden Image + Mask like native scroll view");
            foreach(var name in new[]{"Corn","Medium Oak Tree","Large Oak Tree"}) {
                int sample=Array.FindIndex(tasks,t=>t.taskName==name);
                Check(sample>=0,"Non-square icon sample exists: "+name);
                var icon=rows[sample].transform.Find("IconFrame/Icon").GetComponent<Image>();
                Check(icon.type==Image.Type.Simple&&icon.preserveAspect,"Native icon rendering: "+name);
                Check(Mathf.Abs(icon.rectTransform.rect.width/icon.rectTransform.rect.height-icon.sprite.rect.width/icon.sprite.rect.height)<.001f,"Native icon aspect ratio: "+name);
                Check(icon.GetComponentInParent<Mask>()!=null,"Oversized icon masked: "+name);
            }
            Check(!ui.confirm.interactable,"Confirm disabled before selection");
            rows[0].onClick.Invoke();Check(ui.confirm.interactable==TaskWeightService.IsTaskUnlocked(tasks[0]),"Selection validates skill gate");
            int childCount=ui.content.childCount;for(int i=0;i<10;i++) Call(ui,"Refresh");
            Check(ui.content.childCount==childCount,"Picker reuses rows");
            var unlocked=tasks.First(t=>TaskWeightService.IsTaskUnlocked(t));int idx=Array.IndexOf(tasks,unlocked);
            rows[idx].onClick.Invoke();ui.confirm.onClick.Invoke();
            Check(oracle.saveData.ProspectorTaskId==unlocked.taskID,"Confirm saves selected task ID");
            Check(!ui.gameObject.activeSelf,"Confirm closes picker");
            ui.gameObject.SetActive(true);Set(ui,"selected",tasks.Last());ui.cancel.onClick.Invoke();
            Check(oracle.saveData.ProspectorTaskId==unlocked.taskID,"Cancel preserves previous target");
            ui.gameObject.SetActive(true);Set(ui,"selected",unlocked);Call(ui,"Refresh");
            Capture(ui,canvas,camera,1920,1080,1,"desktop");
            Capture(ui,canvas,camera,1920,1080,1,"desktop-scrolled",false,.5f);
            Capture(ui,canvas,camera,1920,1080,1,"desktop-scroll-bottom",false,0);
            Capture(ui,canvas,camera,390,844,1,"phone-portrait");
            Capture(ui,canvas,camera,844,390,1,"phone-landscape");
            Capture(ui,canvas,camera,2532,1170,3,"phone-landscape-3x");
            Capture(ui,canvas,camera,844,390,1,"phone-landscape-safe-area",true);
            // Render an actual recipe card alongside the popup so palette and border scale
            // can be compared directly, rather than reviewing the popup on its own.
            var comparison=UnityEngine.Object.Instantiate(recipe,canvas.transform);
            var comparisonRect=(RectTransform)comparison.transform;
            comparisonRect.anchorMin=new Vector2(0,1);comparisonRect.anchorMax=Vector2.one;
            comparisonRect.pivot=new Vector2(.5f,1);comparisonRect.offsetMin=new Vector2(8,-58);comparisonRect.offsetMax=new Vector2(-8,-8);
            comparison.nameText.text="Prospector";
            comparison.descriptionText.text="Choose an unlocked task in town.";
            comparison.durationText.text="Target spawn weight +100% (new terrain)";
            comparison.iconImage.sprite=prospector.buffIcon;
            comparison.purchaseButton.GetComponentInChildren<TMPro.TMP_Text>().text="Assign";
            comparison.costGridLayoutParent.SetActive(false);
            comparison.targetButton.gameObject.SetActive(true);comparison.targetText.text="Choose target";
            comparison.targetIcon.gameObject.SetActive(false);
            var backdrop=ui.GetComponent<Image>();var backdropColor=backdrop.color;backdrop.color=Color.clear;
            Capture(ui,canvas,camera,1920,1080,1,"desktop-style-comparison");
            backdrop.color=backdropColor;UnityEngine.Object.DestroyImmediate(comparison.gameObject);
            ui.Close();
            ui.Open(canvasObject, null);
            var filtered=(TaskData[])typeof(ProspectorPicker).GetField("tasks",Private).GetValue(ui);
            Check(filtered.All(TaskWeightService.IsTaskUnlocked),"Open excludes locked tasks");
            int rowsBefore=ui.content.childCount;
            ui.nextSkill.onClick.Invoke();ui.previousSkill.onClick.Invoke();
            Check(ui.content.childCount==rowsBefore,"Filter navigation reuses rows");
            ui.Close();
            Check(!ProspectorPicker.IsOpen,"Modal flag reset on close");
            Check(EditorApplication.isPlaying==false,"Test entered Play mode");
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
    static void Capture(ProspectorPicker ui,Canvas canvas,Camera camera,int w,int h,float density,string name,bool inset=false,float scrollPosition=1)
    {
        var rt=new RenderTexture(w,h,24);camera.targetTexture=rt;var old=RenderTexture.active;
        try {
            canvas.pixelRect.Set(0,0,w,h);
            ui.safeArea.anchorMin=inset?new Vector2(.05f,.04f):Vector2.zero;ui.safeArea.anchorMax=inset?new Vector2(.95f,.96f):Vector2.one;ui.safeArea.offsetMin=ui.safeArea.offsetMax=Vector2.zero;
            canvas.scaleFactor=h/432f;
            Canvas.ForceUpdateCanvases();ui.ApplyLayout(new Vector2(w*(inset?.9f:1f),h*(inset?.92f:1f))/canvas.scaleFactor,density/canvas.scaleFactor);Canvas.ForceUpdateCanvases();
            float nativePixel=canvas.referencePixelsPerUnit/ui.rowPrefab.image.sprite.pixelsPerUnit;
            float pickerPixel=ui.panel.localScale.x*canvas.referencePixelsPerUnit/(ui.rowPrefab.image.sprite.pixelsPerUnit*ui.rowPrefab.image.pixelsPerUnitMultiplier);
            Check(Mathf.Abs(nativePixel-pickerPixel)<.001f,"Picker border pixels match native canvas: "+name);
            var inner=(RectTransform)ui.scroll.transform;
            Check(Vector2.Distance(inner.offsetMin*ui.panel.localScale.x,new Vector2(2,1))<.001f&&Vector2.Distance(inner.offsetMax*ui.panel.localScale.x,new Vector2(-2,-2))<.001f,"Scroll padding matches native 2/2/2/1: "+name);
            Check(ui.scroll.viewport.offsetMin==Vector2.zero&&ui.scroll.viewport.offsetMax==Vector2.zero,"Viewport fills native inner scroll area: "+name);
            Check(ui.scroll.viewport.rect.height>60,"Viewport collapsed: "+name);
            Check(ui.panel.rect.width>=280,"Panel too narrow: "+name);
            Check(ui.confirm.GetComponent<RectTransform>().rect.height>=44,"Confirm touch target: "+name);
            Check(ui.panel.rect.width*density<=w*(inset?.9f:1f),"Panel safe width: "+name);
            Check(ui.panel.rect.height*density<=h*(inset?.92f:1f),"Panel safe height: "+name);
            camera.Render();
            var raycaster=canvas.GetComponent<GraphicRaycaster>();
            var pointer=new PointerEventData(null){position=new Vector2(4,4)};
            var hits=new List<RaycastResult>();raycaster.Raycast(pointer,hits);
            Check(hits.Any(hit=>hit.gameObject==ui.gameObject),"Modal backdrop blocks clicks: "+name);
            ui.scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();
            float oldY=ui.content.anchoredPosition.y;
            pointer.scrollDelta=new Vector2(0,-20);ui.scroll.OnScroll(pointer);
            Check(ui.content.anchoredPosition.y>oldY,"Scroll input moves list: "+name);
            ui.scroll.verticalNormalizedPosition=scrollPosition;Canvas.ForceUpdateCanvases();
            camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();
            System.IO.File.WriteAllBytes("Library/BuffReplacement/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        } finally {camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
