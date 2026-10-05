using System;
using System.Collections.Generic;
using Blindsided;
using Blindsided.Utilities;
using TimelessEchoes.Audio;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitOptionsScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitOptionsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private TownWindowManager windows;
        [SerializeField] private ToolkitOptionsDialogs dialogsPrefab;
        private ToolkitOptionsDialogs dialogs;
        private PanelSettings panel;
        private VisualElement root, widthPreview;
        private ScrollView scroll;
        private VisualElement preferences, savePage, currentPage;
        private string selectedPage = "preferences";
        private Button preferencesTab, savesTab;
        private readonly List<ToolkitTextBinding> bindings = new();
        private readonly Dictionary<string, Button> buttons = new();
        private readonly Dictionary<string, Slider> sliders = new();
        private readonly Dictionary<string, Label> labels = new();
        private readonly SaveSlotPresentation[] slots = { new(), new(), new() };
        private readonly bool[] safety = new bool[3];
        private float nextRefresh;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings && dialogsPrefab;
        // The native input/dialog layer owns these requests; no hidden Canvas controls are invoked.
        private void OpenDialog(string mode) { if (dialogs) dialogs.Open(mode); }

        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured) return false;
            if (!dialogs) dialogs = Instantiate(dialogsPrefab);
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "options", pickingMode = PickingMode.Ignore };
            root.AddToClassList("eov-options"); theme.Apply(root); ToolkitGameplay.Apply(root,theme); root.AddToClassList("settings-page");
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "options-scroll" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scroll.AddToClassList("eov-options-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); root.Add(scroll);
            var heading = Text(root, "settings-title", 12); heading.AddToClassList("settings-heading"); root.Insert(0, heading);
            var tabs = Row(root, "settings-tabs"); tabs.AddToClassList("settings-tabs"); root.Insert(1, tabs);
            preferencesTab = ActionButton(tabs, "preferences-tab", () => SelectPage("preferences"), 24); Grow(preferencesTab);
            savesTab = ActionButton(tabs, "saves-tab", () => SelectPage("saves"), 24); Grow(savesTab);
            preferences = new VisualElement { name="settings-preferences" }; scroll.Add(preferences); currentPage=preferences;
            BuildAudio(); BuildDisplay(); BuildFloating();
            savePage = new VisualElement { name="settings-saves" }; scroll.Add(savePage); currentPage=savePage; BuildSaves();
            BuildFooter(); MarkRowEnds(); SelectPage(selectedPage);
            widthPreview = new VisualElement { name = "width-preview", pickingMode = PickingMode.Ignore };
            widthPreview.style.position = Position.Absolute; widthPreview.style.display = DisplayStyle.None;
            ToolkitTheme.Background(widthPreview, definition.widthPreview);
            document.rootVisualElement.Add(widthPreview); document.rootVisualElement.Add(root);
            sliders["width"].RegisterCallback<PointerDownEvent>(_ => widthPreview.style.display = DisplayStyle.Flex, TrickleDown.TrickleDown);
            sliders["width"].RegisterCallback<PointerUpEvent>(_ => widthPreview.style.display = DisplayStyle.None, TrickleDown.TrickleDown);
            sliders["width"].RegisterCallback<PointerCaptureOutEvent>(_ => widthPreview.style.display = DisplayStyle.None);
            Blindsided.EventHandler.OnLoadData += RefreshLoadedData;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
            RefreshLanguage(UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale);
            if (dialogs) dialogs.Imported += RefreshSlots;
            ToolkitLocalization.Changed += RefreshLocalizedValues;
            RefreshSlots(); RefreshValues(); Layout(); return true;
        }

        // These rows are rebuilt on each Show; mark every child after construction so
        // empty/single rows and a changed number of actions cannot retain stale ends.
        private void MarkRowEnds()
        {
            root.Query<VisualElement>().ForEach(row =>
            {
                if (!row.ClassListContains("settings-tabs") && !row.ClassListContains("settings-actions") &&
                    (row.childCount == 0 || !row[0].ClassListContains("settings-column"))) return;
                for (var i = 0; i < row.childCount; i++)
                    row[i].EnableInClassList("row-end", i == row.childCount - 1);
            });
        }

        private VisualElement Frame(string name)
        {
            var frame = new VisualElement { name = name }; frame.AddToClassList("eov-options-frame");
            currentPage.Add(frame);
            var title=Text(frame,name+"-title",9); title.AddToClassList("settings-section-title");
            return frame;
        }
        private static VisualElement Row(VisualElement parent, string name = null)
        {
            var row = new VisualElement { name = name }; row.AddToClassList("eov-options-row"); parent.Add(row); return row;
        }
        private static void Grow(VisualElement element) { element.style.flexGrow = 1; element.style.flexBasis = 0; element.style.minWidth = 0; }
        private Label Text(VisualElement parent, string id, float size = 8)
        {
            var label = new Label { name = id, pickingMode = PickingMode.Ignore }; label.AddToClassList("eov-options-label");
            label.style.fontSize = Mathf.Max(7,size); parent.Add(label);
            // Computed labels own their localized templates; an asynchronous definition
            // callback must not overwrite slot state, FPS or duration arguments.
            var computed = id.StartsWith("slot-") || id.StartsWith("save-") && id.EndsWith("-label")
                || id.StartsWith("load-") && id.EndsWith("-label") || id == "fps-label"
                || id == "drops-label" || id == "player-damage-label" || id == "enemy-damage-label"
                || id == "language-label";
            if (!computed) bindings.Add(new ToolkitTextBinding(label, definition.Text(id)));
            labels[id] = label; return label;
        }
        private Button ActionButton(VisualElement parent, string id, Action action, float height = 16, bool text = true)
        {
            var button = new Button(action) { name = id }; button.AddToClassList("eov-options-button");
            button.style.height = height; button.AddToClassList("button"); parent.Add(button); buttons[id] = button;
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) AudioManager.Instance?.PlayUIButtonClick(); });
            button.RegisterCallback<NavigationSubmitEvent>(_ => AudioManager.Instance?.PlayUIButtonClick());
            if (text) { var label = Text(button, id + "-label"); label.AddToClassList("eov-options-button-label"); }
            return button;
        }
        private Button Toggle(VisualElement parent, string id, Action action)
        {
            var button = ActionButton(parent, id, () => { action(); RefreshValues(); }, 16, false);
            button.style.flexShrink = 0; return button;
        }
        private Slider Slider(VisualElement parent, string id, Action<float> change, float height = 10)
        {
            var slider = ToolkitControls.Slider(id, 0, 1, change, definition.sliderTrack, definition.sliderFill, definition.sliderHandle, height);
            slider.AddToClassList("eov-options-slider");
            ToolkitGameplay.StyleSlider(slider); slider.Q("unity-dragger").style.height=12; sliders[id] = slider; parent.Add(slider); return slider;
        }
        private void SelectPage(string page)
        {
            selectedPage=page; preferences.style.display=page=="preferences"?DisplayStyle.Flex:DisplayStyle.None;
            savePage.style.display=page=="saves"?DisplayStyle.Flex:DisplayStyle.None;
            preferencesTab.EnableInClassList("active",page=="preferences"); savesTab.EnableInClassList("active",page=="saves");
            scroll.scrollOffset=Vector2.zero;
        }
        private Button LabelledToggle(VisualElement parent,string id,string labelId,Action action)
        {
            var toggle=Toggle(parent,id,action); Text(toggle,labelId,7); toggle.AddToClassList("settings-labelled-toggle"); return toggle;
        }
        private void BuildAudio()
        {
            var frame=Frame("audio"); var row=Row(frame);
            foreach(var id in new[]{"master","music","sfx"})
            {
                var column=new VisualElement();Grow(column);column.AddToClassList("settings-column");row.Add(column);
                Text(column,id+"-label");
                Slider(column,id,value=>{
                    if(id=="master"){MasterVolume=value;AudioManager.Instance?.SetMasterVolume(value);}
                    else if(id=="music"){MusicVolume=value;AudioManager.Instance?.SetMusicVolume(value);}
                    else {SfxVolume=value;AudioManager.Instance?.SetSfxVolume(value);}
                },22);
            }
            var mute=LabelledToggle(frame,"mute","mute-label",()=>{MuteWhenUnfocused=!MuteWhenUnfocused;AudioManager.Instance?.ApplyFocusMuteNow();});
            mute.style.display=Application.isMobilePlatform?DisplayStyle.None:DisplayStyle.Flex;
        }
        private void BuildDisplay()
        {
            var frame=Frame("display");
            var widthSetting=new VisualElement();widthSetting.style.display=Application.isMobilePlatform?DisplayStyle.None:DisplayStyle.Flex;frame.Add(widthSetting);
            Text(widthSetting,"width-label");Slider(widthSetting,"width",value=>{SafeAreaRatio=value;foreach(var safe in FindObjectsByType<ScreenSafeArea>())safe.RatioPreference=value;},22);
            var row=Row(frame);row.AddToClassList("settings-actions");
            var full=ActionButton(row,"fullscreen",()=>Screen.fullScreenMode=FullScreenMode.FullScreenWindow,22);Grow(full);
            var window=ActionButton(row,"windowed",()=>Screen.fullScreenMode=FullScreenMode.Windowed,22);Grow(window);
            full.style.display=window.style.display=Application.isMobilePlatform?DisplayStyle.None:DisplayStyle.Flex;
            var fps=ActionButton(row,"fps",()=>{TargetFps=TargetFps==30?60:TargetFps==60?120:30;ApplyFps();RefreshValues();},22);Grow(fps);
            LabelledToggle(row,"vsync","vsync-label",()=>{VSyncEnabled=!VSyncEnabled;ApplyFps();});
        }
        private static void ApplyFps()
        {
            if(TargetFps==0)TargetFps=60;
            QualitySettings.vSyncCount=VSyncEnabled?1:0;Application.targetFrameRate=VSyncEnabled?-1:TargetFps;
        }
        private void BuildFloating()
        {
            var frame=Frame("floating");
            foreach(var id in new[]{"drops","enemy-damage","player-damage"})
            {
                var row=Row(frame);row.AddToClassList("settings-floating-row");
                var toggle=Toggle(row,id+"-toggle",()=>{if(id=="drops")ItemDropFloatingText=!ItemDropFloatingText;else if(id=="player-damage")PlayerFloatingDamage=!PlayerFloatingDamage;else EnemyFloatingDamage=!EnemyFloatingDamage;});
                var label=Text(row,id+"-label");label.AddToClassList("settings-floating-label");
                var slider=Slider(row,id,value=>{if(id=="drops")DropFloatingTextDuration=value*10;else if(id=="player-damage")PlayerDamageTextDuration=value*2;else EnemyDamageTextDuration=value*2;RefreshDurations();},22);Grow(slider);
            }
        }
        private void BuildSaves()
        {
            var frame=Frame("saves");
            for(int i=0;i<3;i++)
            {
                int index=i;var row=Row(frame,"save-file-"+i);row.AddToClassList("settings-save-row");
                var details=new VisualElement();Grow(details);row.Add(details);
                Text(details,"slot-"+i+"-title",9);Text(details,"slot-"+i+"-playtime",7);Text(details,"slot-"+i+"-date",7);
                var actions=new VisualElement();actions.AddToClassList("settings-save-actions");row.Add(actions);
                var buttonsRow=Row(actions);buttonsRow.AddToClassList("settings-actions");
                Grow(ActionButton(buttonsRow,"save-"+i,()=>{if(CanSave(index)&&SaveSlotActions.SaveSlot(index))RefreshSlots();},22));
                Grow(ActionButton(buttonsRow,"load-"+i,()=>LoadOrDelete(index),22));
                LabelledToggle(actions,"safety-"+i,"save-unlock-label",()=>{safety[index]=!safety[index];RefreshSlotLabels();});
            }
            var transfers=Row(frame);transfers.AddToClassList("settings-actions");
            Grow(ActionButton(transfers,"import",()=>OpenDialog("import"),22));Grow(ActionButton(transfers,"export",()=>OpenDialog("export"),22));
            var note=Text(frame,"autosave-label",7);note.AddToClassList("settings-save-note");
        }
        private void BuildFooter()
        {
            var row=Row(root,"footer");row.AddToClassList("settings-footer");
            Grow(ActionButton(row,"credits",()=>windows?.OpenWindow(TownWindowManager.Window.Credits),22));
            var language=ActionButton(row,"language",()=>OpenDialog("language"),22);Grow(language);
            var folder=ActionButton(row,"folder",()=>Application.OpenURL($"file://{Application.persistentDataPath}"),22);Grow(folder);
            language.style.display=folder.style.display=Application.isMobilePlatform?DisplayStyle.None:DisplayStyle.Flex;
            Text(row,"version-label",7).AddToClassList("settings-version");
        }
        private bool CanSave(int index) => Oracle.oracle != null && Oracle.oracle.CanBeginSaveMutation && (index == Oracle.oracle.CurrentSlot || safety[index]);
        private bool CanLoad(int index) => Oracle.oracle != null && Oracle.oracle.CanBeginSaveMutation && (safety[index] || ((GameManager.Instance == null || GameManager.Instance.CurrentMap == null) && index != Oracle.oracle.CurrentSlot));
        private void LoadOrDelete(int index)
        {
            if (!CanLoad(index)) return;
            if (safety[index]) { if (!SaveSlotActions.DeleteSlot(index)) return; if (index == Oracle.oracle.CurrentSlot) Oracle.oracle.WipeAllData(replacingDeletedSlot: true); }
            else Oracle.oracle.SelectSlot(index);
            RefreshSlots();
        }
        private void RefreshLanguage(UnityEngine.Localization.Locale locale)
        {
            if (locale == null || !labels.TryGetValue("language-label",out var label)) return;
            var text=definition.Text("locale-"+locale.Identifier.Code+"-label");
            label.text=text.fallback.StartsWith("locale-")?locale.LocaleName:ToolkitLocalization.Text(text.key,text.fallback);
        }
        private void RefreshLocalizedValues() { if (IsOpen) { RefreshLanguage(UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale); RefreshSlotLabels(); RefreshValues(); } }
        private void RefreshLoadedData() { RefreshSlots(); if (IsOpen) RefreshValues(); }
        private void RefreshSlots() { for (var i = 0; i < 3; i++) slots[i].Refresh(i); RefreshSlotLabels(); }
        private void RefreshSlotLabels()
        {
            if (!IsOpen) return;
            for (var i = 0; i < 3; i++)
            {
                slots[i].Update(i); labels["slot-" + i + "-title"].text = "<b><smallcaps>" + slots[i].Title + "</smallcaps></b>";
                labels["slot-" + i + "-playtime"].text = slots[i].Playtime;
                labels["slot-" + i + "-date"].text = slots[i].LastPlayed;
                labels["save-" + i + "-label"].text = "<b><smallcaps>" + ToolkitLocalization.Text("options.save", "Save") + "</smallcaps></b>";
                labels["load-" + i + "-label"].text = "<b><smallcaps>" + (safety[i] ? ToolkitLocalization.Text("options.delete", "Delete") : ToolkitLocalization.Text("options.load", "Load")) + "</smallcaps></b>";
                buttons["save-" + i].SetEnabled(CanSave(i)); buttons["load-" + i].SetEnabled(CanLoad(i)); SetToggle("safety-" + i, safety[i]);
            }
        }
        private void SetToggle(string id, bool on) { ToolkitGameplay.SetToggle(buttons[id],on,!buttons[id].ClassListContains("settings-labelled-toggle"));buttons[id].tooltip=on?ToolkitLocalization.Text("common.on", "On"):ToolkitLocalization.Text("common.off", "Off"); }
        private void SetSlider(string id, float value) { sliders[id].SetValueWithoutNotify(value); sliders[id].Q("value-fill").style.width = Length.Percent(value * 100); }
        private void RefreshValues()
        {
            SetSlider("master", MasterVolume); SetSlider("music", MusicVolume); SetSlider("sfx", SfxVolume); SetSlider("width", SafeAreaRatio);
            SetSlider("drops", DropFloatingTextDuration / 10); SetSlider("player-damage", PlayerDamageTextDuration / 2); SetSlider("enemy-damage", EnemyDamageTextDuration / 2);
            SetToggle("mute", MuteWhenUnfocused); SetToggle("vsync", VSyncEnabled); SetToggle("drops-toggle", ItemDropFloatingText);
            SetToggle("player-damage-toggle", PlayerFloatingDamage); SetToggle("enemy-damage-toggle", EnemyFloatingDamage);
            buttons["fps"].SetEnabled(!VSyncEnabled); labels["fps-label"].text = "<b><smallcaps>" + (VSyncEnabled ? ToolkitLocalization.Text("options.fps-vsync", "FPS: VSync") : ToolkitLocalization.Text("options.fps", "FPS: {0}", TargetFps)) + "</smallcaps></b>";
            RefreshDurations();
        }
        private void RefreshDurations()
        {
            labels["drops-label"].text = ToolkitLocalization.Text("options.drops-duration", "Drops \u00b7 {0}", CalcUtils.FormatTime(DropFloatingTextDuration, true, shortForm: true));
            labels["player-damage-label"].text = ToolkitLocalization.Text("options.incoming-duration", "Incoming damage \u00b7 {0}", CalcUtils.FormatTime(PlayerDamageTextDuration, true, shortForm: true));
            labels["enemy-damage-label"].text = ToolkitLocalization.Text("options.outgoing-duration", "Outgoing damage \u00b7 {0}", CalcUtils.FormatTime(EnemyDamageTextDuration, true, shortForm: true));
        }
        private void Layout()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);
            widthPreview.style.left = area.x; widthPreview.style.top = area.y; widthPreview.style.width = area.width; widthPreview.style.height = area.height;
            root.style.left = area.center.x; root.style.top = area.y + 44; root.style.width = Mathf.Min(600, area.width-24); root.style.height = Mathf.Max(0,area.height - 56);
        }
        private void Update() { if (!IsOpen) return; Layout(); if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 1; RefreshSlotLabels(); } }
        public void Hide()
        {
            ToolkitLocalization.Changed -= RefreshLocalizedValues;
            Blindsided.EventHandler.OnLoadData -= RefreshLoadedData;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
            if (dialogs) dialogs.Imported -= RefreshSlots;
            foreach (var binding in bindings) binding.Dispose(); bindings.Clear(); buttons.Clear(); sliders.Clear(); labels.Clear();
            dialogs?.Hide(); Array.Clear(safety,0,safety.Length);
            root?.RemoveFromHierarchy(); widthPreview?.RemoveFromHierarchy(); root = null; widthPreview = null; scroll = null;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (panel) Destroy(panel); if (dialogs) Destroy(dialogs.gameObject); }
    }
}


