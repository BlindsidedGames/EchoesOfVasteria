using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    // Approved StyleLab primitives, in the game's 768x432 reference coordinate system.
    public static class ToolkitGameplay
    {
        public static void Apply(VisualElement root, ToolkitTheme theme) { root.AddToClassList("gameplay");root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Gameplay")); root.style.unityFontDefinition=FontDefinition.FromFont(theme.gameplayFont ? theme.gameplayFont : theme.font);root.style.color=StyleKeyword.Null;root.AddToClassList("dark");root.RegisterCallback<PointerDownEvent>(_=>root.RemoveFromClassList("keyboard"),TrickleDown.TrickleDown);root.RegisterCallback<KeyDownEvent>(_=>root.AddToClassList("keyboard"),TrickleDown.TrickleDown); }
        public static VisualElement E(VisualElement parent,string classes){var e=new VisualElement();foreach(var c in classes.Split(' '))if(c.Length>0)e.AddToClassList(c);parent.Add(e);return e;}
        public static Label L(VisualElement parent,string text,string classes=""){var e=new Label(text);foreach(var c in classes.Split(' '))if(c.Length>0)e.AddToClassList(c);parent.Add(e);return e;}
        public static Button B(VisualElement parent,string text,Action action,string classes=""){var b=new Button(action){text=text};b.AddToClassList("button");foreach(var c in classes.Split(' '))if(c.Length>0)b.AddToClassList(c);b.RegisterCallback<ClickEvent>(_=>Audio.AudioManager.Instance?.PlayUIButtonClick());parent.Add(b);return b;}
        public static Image Icon(VisualElement parent,Sprite sprite,float size){var i=new Image{sprite=sprite,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};i.AddToClassList("icon");i.style.width=size;i.style.height=size;parent.Add(i);return i;}
        public static VisualElement Bar(VisualElement parent,string classes=""){var track=E(parent,"track "+classes);return E(track,"fill");}
        public static void SetToggle(Image image, bool value)
        {
            image.style.display = DisplayStyle.None;
            SetToggle((Button)image.parent, value, image.parent.Query<Label>().ToList().Count == 0);
        }
        public static void SetToggle(Button button, bool value, bool standalone = true)
        {
            button.text = string.Empty;
            button.RemoveFromClassList("active");
            button.AddToClassList("check-control");
            button.EnableInClassList("check-control--standalone", standalone);
            var mark = button.Q<ToolkitCheckMark>();
            if (mark == null) { mark = new ToolkitCheckMark(); button.Insert(0, mark); }
            mark.Value = value;
            button.tooltip = value ? "Enabled" : "Disabled";
        }
        public static void StyleSlider(Slider slider)
        {
            slider.AddToClassList("gameplay-slider");
            foreach(var name in new[]{"unity-tracker","unity-dragger","value-fill"})
            {
                var part=slider.Q(name);if(part!=null)part.style.backgroundImage=StyleKeyword.None;
            }
        }
        public static void StyleScroll(ScrollView scroll)
        {
            scroll.AddToClassList("gameplay-scroll");
            scroll.verticalScroller.lowButton.style.display=DisplayStyle.None;
            scroll.verticalScroller.highButton.style.display=DisplayStyle.None;
            scroll.verticalScroller.style.width=3;
            scroll.verticalScroller.style.minWidth=3;
            foreach(var name in new[]{"unity-tracker","unity-dragger"})
            {
                var part=scroll.verticalScroller.slider.Q(name);if(part!=null)part.style.backgroundImage=StyleKeyword.None;
            }
        }
        public static ScrollView Scroll(VisualElement parent,string name){var s=new ScrollView(ScrollViewMode.Vertical){name=name,horizontalScrollerVisibility=ScrollerVisibility.Hidden,verticalScrollerVisibility=ScrollerVisibility.Hidden};s.AddToClassList("scroll");parent.Add(s);return s;}
    }
    public sealed class ToolkitCheckMark : VisualElement
    {
        private bool value;
        public bool Value { get => value; set { if(this.value == value)return; this.value = value; EnableInClassList("checked", value); MarkDirtyRepaint(); } }
        public ToolkitCheckMark()
        {
            AddToClassList("check-mark"); pickingMode = PickingMode.Ignore;
            generateVisualContent += ctx =>
            {
                if (!value) return;
                var p = ctx.painter2D; p.strokeColor = new Color32(48,33,38,255); p.lineWidth = 1.4f;
                // Painter coordinates include the border; contentRect starts inside it.
                var rect = contentRect;
                var origin = rect.position;
                var w = rect.width; var h = rect.height;
                p.BeginPath();p.MoveTo(origin + new Vector2(w*.2f,h*.5f));p.LineTo(origin + new Vector2(w*.43f,h*.73f));p.LineTo(origin + new Vector2(w*.81f,h*.25f));p.Stroke();
            };
        }
    }
    public sealed class ToolkitBorderProgress : VisualElement
    {
        static readonly CustomStyleProperty<Color> Accent=new("--ring-color"),Track=new("--ring-track");Color accent,track;float value;
        public float Value {get=>value;set{if(Mathf.Approximately(this.value,value))return;this.value=Mathf.Clamp01(value);MarkDirtyRepaint();}}
        public ToolkitBorderProgress(){AddToClassList("border-progress");pickingMode=PickingMode.Ignore;RegisterCallback<CustomStyleResolvedEvent>(e=>{e.customStyle.TryGetValue(Accent,out accent);e.customStyle.TryGetValue(Track,out track);MarkDirtyRepaint();});generateVisualContent+=Draw;}
        void Draw(MeshGenerationContext ctx){float w=contentRect.width-1,h=contentRect.height-1;if(w<=0||h<=0)return;var pts=new[]{new Vector2(.5f+w/2,.5f),new Vector2(.5f+w,.5f),new Vector2(.5f+w,.5f+h),new Vector2(.5f,.5f+h),new Vector2(.5f,.5f),new Vector2(.5f+w/2,.5f)};var p=ctx.painter2D;p.lineWidth=1;Stroke(p,pts,2*(w+h),track);Stroke(p,pts,2*(w+h)*value,accent);}
        static void Stroke(Painter2D p,Vector2[] pts,float remaining,Color color){p.strokeColor=color;p.BeginPath();p.MoveTo(pts[0]);for(int i=1;i<pts.Length&&remaining>0;i++){float length=Vector2.Distance(pts[i-1],pts[i]);p.LineTo(Vector2.Lerp(pts[i-1],pts[i],Mathf.Min(remaining/length,1)));remaining-=length;}p.Stroke();}
    }
}
