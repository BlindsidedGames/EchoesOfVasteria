using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
namespace Vasteria.StyleLab {
 public sealed class VasteriaStyleLab : EditorWindow {
  public const string Folder="Assets/Editor/StyleLab/", Output="docs/ui-style-study-2026-09-27/";
  [SerializeField] bool dark,phone,largeText,german,cauldron=true;
  [SerializeField] string fixture="mixed";
  StudyModel model; Study view; VisualElement stage,viewport;
  [MenuItem("Tools/Vasteria/Style study/Run HUD")] public static void OpenRun()=>Open(false);
  [MenuItem("Tools/Vasteria/Style study/Cauldron")] public static void OpenCauldron()=>Open(true);
  public static void Open(bool cauldron) { var w=GetWindow<VasteriaStyleLab>(); w.titleContent=new GUIContent("Vasteria · Style study"); w.minSize=new Vector2(700,460); w.cauldron=cauldron; w.BuildWindow(); w.Show(); }
  void OnEnable(){BuildWindow();}
  void OnDisable(){view?.Dispose();view=null;}
  void BuildWindow(){
   view?.Dispose();rootVisualElement.Clear(); model??=new StudyModel(fixture);
   var toolbar=new Toolbar(); toolbar.style.flexWrap=Wrap.Wrap; toolbar.style.height=StyleKeyword.Auto; toolbar.style.minHeight=28; rootVisualElement.Add(toolbar);
   toolbar.Add(new ToolbarButton(()=>{cauldron=false;BuildPreview();}){text="Run HUD"});
   toolbar.Add(new ToolbarButton(()=>{cauldron=true;BuildPreview();}){text="Cauldron"});
   var theme=new ToolbarToggle{text="Dark mode",value=dark};theme.RegisterValueChangedCallback(e=>{dark=e.newValue;view?.SetTheme(dark);});toolbar.Add(theme);
   var device=new ToolbarToggle{text="360 px",value=phone};device.RegisterValueChangedCallback(e=>{phone=e.newValue;BuildPreview();});toolbar.Add(device);
   var textSize=new ToolbarToggle{text="130% text",value=largeText};textSize.RegisterValueChangedCallback(e=>{largeText=e.newValue;BuildPreview();});toolbar.Add(textSize);
   var language=new ToolbarToggle{text="Deutsch",value=german};language.RegisterValueChangedCallback(e=>{german=e.newValue;BuildPreview();});toolbar.Add(language);
   var state=new DropdownField(new System.Collections.Generic.List<string>{"mixed","maxed","empty"},fixture);state.style.width=90;state.RegisterValueChangedCallback(e=>{fixture=e.newValue;model.Reset(fixture);BuildPreview();});toolbar.Add(state);
   toolbar.Add(new ToolbarButton(()=>{model.Reset(fixture);BuildPreview();}){text="Reset sample"});
   var note=new Label("Isolated preview · sample data");note.style.marginLeft=12;note.style.unityTextAlign=TextAnchor.MiddleLeft;toolbar.Add(note);
   viewport=new VisualElement(); viewport.style.flexGrow=1;viewport.style.overflow=Overflow.Hidden; viewport.style.backgroundColor=new Color(.1f,.1f,.1f);rootVisualElement.Add(viewport);
   viewport.RegisterCallback<GeometryChangedEvent>(_=>Fit()); BuildPreview();
  }
  void BuildPreview(){
   if(viewport==null)return;view?.Dispose();viewport.Clear();stage=new VisualElement();stage.style.position=Position.Absolute;stage.style.width=phone?360:1440;stage.style.height=phone?800:900;stage.style.transformOrigin=new TransformOrigin(0,0);viewport.Add(stage);
   view=new Study(stage,model,cauldron,dark,largeText?1.3f:1,german?"de":"en");Fit();
  }
  void Fit(){if(stage==null)return;float width=phone?360:1440,height=phone?800:900;float scale=Mathf.Min(viewport.resolvedStyle.width/width,viewport.resolvedStyle.height/height);if(float.IsNaN(scale)||scale<=0)return;stage.style.scale=new Scale(new Vector3(scale,scale,1));stage.style.left=(viewport.resolvedStyle.width-width*scale)/2;stage.style.top=(viewport.resolvedStyle.height-height*scale)/2;}
  public static string Capture(bool cauldron,int width=1920,int height=1080)=>CaptureView(cauldron,width,height,false);
  public static string CaptureView(bool cauldron,int width,int height,bool dark,float textScale=1,string locale="en",string fixture="mixed",string tab="resources",string variant=""){
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");Directory.CreateDirectory(Output+"polished");
   var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Style study capture"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(go,scene);
   var panel=Instantiate(Resources.Load<PanelSettings>("UI/ToolkitPanel"));panel.hideFlags=HideFlags.HideAndDontSave;
   var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();panel.targetTexture=target;panel.scaleMode=PanelScaleMode.ConstantPixelSize;panel.scale=width<=600?1:width/1440f;panel.clearColor=true;panel.colorClearValue=Color.black;
   var doc=go.AddComponent<UIDocument>();doc.panelSettings=panel;var root=doc.rootVisualElement;root.style.width=width/panel.scale;root.style.height=height/panel.scale;
   var study=new Study(root,new StudyModel(fixture),cauldron,dark,textScale,locale);study.CollectionTab=tab;study.Rebuild();study.ApplyVariant(variant);
   double due=EditorApplication.timeSinceStartup+1;int frame=0;EditorApplication.CallbackFunction tick=null;bool cleaned=false;
   void Cleanup(){if(cleaned)return;cleaned=true;EditorApplication.update-=tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;EditorApplication.quitting-=Cleanup;study.Dispose();DestroyImmediate(go);DestroyImmediate(panel);target.Release();DestroyImmediate(target);EditorSceneManager.ClosePreviewScene(scene);}
   tick=()=>{try{
    var runtime=typeof(PanelSettings).GetProperty("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel);
    foreach(var name in new[]{"Update","Repaint","Render"})runtime.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null)?.Invoke(runtime,null);
    EditorApplication.QueuePlayerLoopUpdate();if(++frame<10||EditorApplication.timeSinceStartup<due)return;
    string file=(cauldron?"cauldron":"run")+"-"+(dark?"dark":"light")+"-"+width+"x"+height+"-"+locale+"-"+(textScale>1?"130":"100")+"-"+fixture+(tab!="resources"?"-"+tab:"")+(variant!=""?"-"+variant:"");
    var previous=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(width,height,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();RenderTexture.active=previous;File.WriteAllBytes(Output+"polished/"+file+".png",png.EncodeToPNG());DestroyImmediate(png);
    File.WriteAllText(Output+"polished/"+file+".txt",study.Validate());Cleanup();
   }catch(Exception ex){File.WriteAllText(Output+"polished/capture-error.txt",ex.ToString());Cleanup();}};
   AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;EditorApplication.quitting+=Cleanup;EditorApplication.update+=tick;return "Queued isolated native panel capture.";
  }
 }
}
