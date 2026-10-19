using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.UIElements;
using Blindsided.Utilities;
namespace TimelessEchoes.UI.Toolkit
{
    public static class BuffIntroductionGuidance
    {
        public static int Stage => Blindsided.Oracle.oracle?.saveData?.General.BuffTutorialStage ?? 0;
        public static bool Eligible => Stage == 0 && QuestUtils.QuestCompleted("Mildred") &&
            !(QuestUtils.QuestCompleted("Into The Multiverse") && Blindsided.Oracle.oracle.saveData.BuffSlots?.Any(s => s == "Echo Tasks" || s == "Echo Combat") == true);
        public static bool Active => Stage == 1 || Stage == 2;
        public static bool IsFirstEcho(BuffRecipe recipe) => recipe && (recipe.name == "Echo Tasks" || recipe.name == "Echo Combat");
    }
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitBuffsScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitBuffsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings panel;
        private VisualElement root;
        private ScrollView scroll;
        private Label instruction;
        private readonly Button[] slots=new Button[5];
        private readonly Image[] icons=new Image[5],autoIcons=new Image[5];
        private readonly Label[] locked=new Label[5];
        private readonly List<(BuffRecipe recipe,VisualElement root,Button assign,Label effects,Label timing)> rows=new();
        private BuffRecipe selected;
        private bool inRun,rebuild;
        private float nextRefresh;
        private Image lessonHand;
        public bool IsOpen=>root!=null;
        public bool IsConfigured=>definition&&theme&&runtimeTheme&&textSettings;
        public bool Show()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
            if(IsOpen)return true;if(!IsConfigured||!BuffManager.Instance)return false;
            if(!panel){panel=ToolkitPanel.CreateSettings(runtimeTheme,textSettings);panel.sortingOrder=100;}
            var doc=GetComponent<UIDocument>();doc.panelSettings=panel;doc.rootVisualElement.pickingMode=PickingMode.Ignore;
            root=new VisualElement{name="buffs"};root.AddToClassList("eov-buffs");root.AddToClassList("buffs-reviewed");theme.Apply(root);ToolkitGameplay.Apply(root,theme);doc.rootVisualElement.Add(root);
            var header=new VisualElement();header.AddToClassList("eov-buffs-header");root.Add(header);
            var slotRow=new VisualElement{name="assignment-slots"};slotRow.AddToClassList("eov-buffs-slots");header.Add(slotRow);
            for(int i=0;i<5;i++)
            {
                var index=i;
                var button=MakeButton("assign-slot-"+i,()=>AssignSlot(index),definition.slot);button.AddToClassList("eov-buffs-slot");button.AddToClassList("button");slotRow.Add(button);slots[i]=button;
                icons[i]=new Image{pickingMode=PickingMode.Ignore,scaleMode=ScaleMode.ScaleToFit};icons[i].AddToClassList("eov-buffs-slot-icon");button.Add(icons[i]);
                locked[i]=Label("",5);locked[i].AddToClassList("eov-buffs-slot-locked");button.Add(locked[i]);
                autoIcons[i]=new Image{sprite=definition.autoCast,scaleMode=ScaleMode.ScaleToFit,tintColor=definition.autoCastTint,pickingMode=PickingMode.Ignore};autoIcons[i].AddToClassList("eov-buffs-auto");button.Add(autoIcons[i]);
            }
            var instructions=new VisualElement();instructions.AddToClassList("eov-buffs-instructions");header.Add(instructions);
            lessonHand = new Image { sprite=definition.tutorialHand,pickingMode=PickingMode.Ignore };
            lessonHand.style.position=Position.Absolute;lessonHand.style.width=lessonHand.style.height=24;root.Add(lessonHand);
            instruction=Label("",6.7f);instruction.style.unityFontStyleAndWeight=FontStyle.Normal;instruction.style.letterSpacing=.134f;instructions.Add(instruction);
            scroll=ToolkitControls.RecessedScroll(root,"buff-recipes",theme,definition.inset);scroll.parent.AddToClassList("buff-list-frame");scroll.contentContainer.AddToClassList("buff-list-content");scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_=>SizeRows());
            inRun=GameplayStatTracker.Instance?.RunInProgress==true;
            ToolkitLocalization.Changed+=LocalizationChanged;
            Blindsided.EventHandler.OnLoadData+=Loaded;Blindsided.EventHandler.OnQuestHandin+=QuestChanged;Blindsided.EventHandler.OnRunStarted+=RunStarted;Blindsided.EventHandler.OnRunEnded+=RunEnded;
            BuildRows();Layout();Refresh();return true;
        }
        internal static Label Label(string text,float size){var label=new Label(text){pickingMode=PickingMode.Ignore};label.AddToClassList("eov-buffs-label");label.style.fontSize=Mathf.Max(7,size);return label;}
        internal static Button MakeButton(string name,System.Action click,Sprite sprite){var button=ToolkitControls.Button(name,click,sprite);button.AddToClassList("eov-buffs-button");button.style.backgroundImage=StyleKeyword.None;return button;}
        private static Image NativeIcon(VisualElement parent,Sprite sprite)
        {
            var mask=new VisualElement{pickingMode=PickingMode.Ignore};mask.AddToClassList("buff-icon-mask");parent.Add(mask);
            var image=new Image{scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};mask.Add(image);SetIcon(image,sprite);return image;
        }
        private static void SetIcon(Image image,Sprite sprite){image.sprite=sprite;image.style.width=sprite?sprite.rect.width*16/sprite.pixelsPerUnit:0;image.style.height=sprite?sprite.rect.height*16/sprite.pixelsPerUnit:0;image.style.flexShrink=0;}
        private void BuildRows()
        {
            var offset=scroll.scrollOffset;scroll.Clear();rows.Clear();var manager=BuffManager.Instance;if(!manager)return;
            // Keep the introductory Echo choices first; preserve the remaining recipe order.
            foreach(var recipe in manager.Recipes.OrderBy(recipe => !recipe ? 3 : recipe.name == "Echo Tasks" ? 0 : recipe.name == "Echo Combat" ? 1 : 2))
            {
                if(!recipe||!recipe.IsUnlocked)continue;
                if (BuffIntroductionGuidance.Active && !BuffIntroductionGuidance.IsFirstEcho(recipe)) continue;
                var row=new VisualElement{name="recipe-"+recipe.name};row.AddToClassList("buff-entry");scroll.Add(row);
                var line=new VisualElement();line.AddToClassList("buff-entry-heading");row.Add(line);
                var frame=new VisualElement();frame.AddToClassList("buff-entry-icon");line.Add(frame);NativeIcon(frame,recipe.buffIcon);
                var title=Label(recipe.GetDisplayName(),9);title.AddToClassList("buff-entry-title");line.Add(title);
                var assign=MakeButton("assign-"+recipe.name,()=>{if(!inRun){selected=selected==recipe?null:recipe;Refresh();}},null);assign.AddToClassList("buff-entry-assign");line.Add(assign);
                var effects=Label("",7);effects.AddToClassList("buff-entry-effects");row.Add(effects);
                var timing=Label("",7);timing.AddToClassList("buff-entry-timing");row.Add(timing);
                rows.Add((recipe,row,assign,effects,timing));
            }
            SizeRows();scroll.scrollOffset=offset;
        }
        private void SizeRows(){if(scroll==null)return;var width=scroll.contentViewport.contentRect.width;if(width<=0)return;var columns=width>=560?2:1;var size=Mathf.Floor((width-(columns-1)*12-1)/columns);for(int i=0;i<rows.Count;i++){rows[i].root.style.width=size;rows[i].root.style.marginRight=i%columns==columns-1?0:12;}}
        private void AssignSlot(int index){var manager=BuffManager.Instance;if(!manager)return;if(!inRun&&selected&&manager.IsSlotUnlocked(index)){manager.AssignBuff(index,selected);if(BuffIntroductionGuidance.Active&&BuffIntroductionGuidance.IsFirstEcho(manager.GetAssigned(index))){Blindsided.Oracle.oracle.saveData.General.BuffTutorialStage=3;Blindsided.EventHandler.SaveData();rebuild=true;}}else manager.ToggleSlotAutoCast(index);selected=null;Refresh();}
        private void Refresh()
        {
            var manager=BuffManager.Instance;if(!manager||!IsOpen)return;
            instruction.text=inRun?ToolkitLocalization.Text("buffs.instructions-running", "Assignments are locked during runs. Select an equipped slot to toggle autocast."):selected?ToolkitLocalization.Text("buffs.instructions-select-slot", "Choose a slot for {0}. Select Cancel to finish without assigning.", selected.GetDisplayName()):ToolkitLocalization.Text("buffs.instructions-select-buff", "Select a buff to assign it. Select an equipped slot to toggle autocast.");
            if (BuffIntroductionGuidance.Active)
                instruction.text = selected ? ToolkitLocalization.Text("buffs.lesson.slot", "Click your first slot to equip this Echo.") : ToolkitLocalization.Text("buffs.lesson.choice", "Choose Task Echo to gather resources, or Combat Echo to fight enemies. Select Assign, then choose your first slot.");
            else if (!inRun && !selected && BuffIntroductionGuidance.Stage >= 3 && !manager.IsAutoSlotUnlocked(0))
                instruction.text = ToolkitLocalization.Text("buffs.lesson.casting", "Click your Echo's button during an adventure to summon it. Complete Echo Casting to unlock autocasting.");
            for(int i=0;i<5;i++)
            {
                var recipe=manager.GetAssigned(i);icons[i].sprite=recipe?recipe.buffIcon:null;
                var unlocked=manager.IsSlotUnlocked(i);
                slots[i].SetEnabled(selected?unlocked:manager.IsAutoSlotUnlocked(i)&&recipe);
                slots[i].EnableInClassList("awaiting-assignment",selected&&unlocked);
                slots[i].EnableInClassList("buff-slot-auto",manager.IsSlotAutoCasting(i));
                autoIcons[i].style.display=manager.IsSlotAutoCasting(i)?DisplayStyle.Flex:DisplayStyle.None;
                locked[i].text=!unlocked?ToolkitLocalization.Text("common.locked", "Locked"):"";
                slots[i].tooltip=!unlocked?ToolkitLocalization.Text("buffs.slot-locked", "Slot locked"):recipe?
                    manager.IsAutoSlotUnlocked(i)?manager.IsSlotAutoCasting(i)?ToolkitLocalization.Text("buffs.slot-autocast-on", "{0} · Autocast on", recipe.GetDisplayName()):ToolkitLocalization.Text("buffs.slot-autocast-off", "{0} · Autocast off", recipe.GetDisplayName()):ToolkitLocalization.Text("buffs.slot-autocast-locked", "{0} · Autocast locked", recipe.GetDisplayName()):ToolkitLocalization.Text("buffs.slot-empty", "Empty slot");
            }
            foreach(var row in rows)
            {
                int assigned=-1;for(int i=0;i<5;i++)if(manager.GetAssigned(i)==row.recipe){assigned=i;break;}
                row.root.EnableInClassList("buff-equipped",assigned>=0);
                row.root.EnableInClassList("buff-choosing",row.recipe==selected);
                row.assign.text=row.recipe==selected?ToolkitLocalization.Text("common.cancel", "Cancel"):assigned>=0?ToolkitLocalization.Text("buffs.assigned-slot", "Slot {0}", assigned+1):ToolkitLocalization.Text("buffs.assign", "Assign");
                row.assign.SetEnabled(!inRun);
                // Timing is identified structurally, never by an English text prefix.
                row.effects.text=string.Join("\n",row.recipe.GetDescriptionLines(includeTiming:false));
                row.timing.text=row.recipe.durationType==BuffDurationType.DistancePercent?ToolkitLocalization.Text("buffs.timing-distance", "Distance {0}%", Mathf.CeilToInt(row.recipe.GetDuration()*100)):ToolkitLocalization.Text("buffs.timing-duration", "Duration {0}   ·   Cooldown {1}", CalcUtils.FormatTime(row.recipe.GetDuration(),shortForm:true), CalcUtils.FormatTime(row.recipe.GetCooldown(),shortForm:true));
            }
            lessonHand.style.display=BuffIntroductionGuidance.Active&&selected&&definition.tutorialHand?DisplayStyle.Flex:DisplayStyle.None;
            if(BuffIntroductionGuidance.Active&&selected){var p=root.WorldToLocal(new Vector2(slots[0].worldBound.center.x,slots[0].worldBound.yMax));lessonHand.style.left=Mathf.Round(p.x-12);lessonHand.style.top=Mathf.Round(p.y+2+4*(1+Mathf.Cos(Time.unscaledTime*5)));lessonHand.BringToFront();}
        }
        private void LocalizationChanged(){rebuild=true;nextRefresh=0;}
        private void Loaded(){selected=null;rebuild=true;}
        private void QuestChanged(string _)=>rebuild=true;
        private void RunStarted(){inRun=true;selected=null;Refresh();}
        private void RunEnded(){inRun=false;Refresh();}
        private readonly ToolkitWindowLayout windowLayout=new();
        private void Layout(){var area=ToolkitWindowLayout.SafeArea;var width=Mathf.Min(744,area.width-24);windowLayout.Apply(root,new Rect(area.center.x-width/2,area.y+ToolkitResponsiveLayout.WindowTop,width,Mathf.Max(0,area.height-ToolkitResponsiveLayout.WindowTop-ToolkitResponsiveLayout.WindowBottom)));}
        private void Update(){if(!IsOpen)return;Layout();if(rebuild){rebuild=false;BuildRows();Refresh();}if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}}
        public void Hide(){ToolkitLocalization.Changed-=LocalizationChanged;Blindsided.EventHandler.OnLoadData-=Loaded;Blindsided.EventHandler.OnQuestHandin-=QuestChanged;Blindsided.EventHandler.OnRunStarted-=RunStarted;Blindsided.EventHandler.OnRunEnded-=RunEnded;root?.RemoveFromHierarchy();root=null;scroll=null;selected=null;rows.Clear();}
        private void OnDisable()=>Hide();
        private void OnDestroy(){Hide();if(panel)Destroy(panel);}
    }
}
