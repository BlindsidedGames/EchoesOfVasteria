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
            BuildAudio(); BuildDisplay(); BuildFloating(); BuildSaves(); BuildFooter();
            widthPreview = new VisualElement { name = "width-preview", pickingMode = PickingMode.Ignore };
            widthPreview.style.position = Position.Absolute; widthPreview.style.display = DisplayStyle.None;
            ToolkitTheme.Background(widthPreview, definition.widthPreview);
            document.rootVisualElement.Add(widthPreview); document.rootVisualElement.Add(root);
            sliders["width"].RegisterCallback<PointerDownEvent>(_ => widthPreview.style.display = DisplayStyle.Flex, TrickleDown.TrickleDown);
            sliders["width"].RegisterCallback<PointerUpEvent>(_ => widthPreview.style.display = DisplayStyle.None, TrickleDown.TrickleDown);
            sliders["width"].RegisterCallback<PointerCaptureOutEvent>(_ => widthPreview.style.display = DisplayStyle.None);
            Blindsided.EventHandler.OnLoadData += RefreshLoadedData;
            if (dialogs) dialogs.Imported += RefreshSlots;
            RefreshSlots(); RefreshValues(); Layout(); return true;
        }

        private VisualElement Frame(string name)
        {
            var frame = new VisualElement { name = name }; frame.AddToClassList("eov-options-frame");
             scroll.Add(frame); return frame;
        }
        private static VisualElement Row(VisualElement parent, string name = null)
        {
            var row = new VisualElement { name = name }; row.AddToClassList("eov-options-row"); parent.Add(row); return row;
        }
        private static void Grow(VisualElement element) { element.style.flexGrow = 1; element.style.flexBasis = 0; element.style.minWidth = 0; }
        private Label Text(VisualElement parent, string id, float size = 8)
        {
            var label = new Label { name = id, pickingMode = PickingMode.Ignore }; label.AddToClassList("eov-options-label");
            label.style.fontSize = Mathf.Max(7,size); parent.Add(label); bindings.Add(new ToolkitTextBinding(label, definition.Text(id))); labels[id] = label; return label;
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
            ToolkitGameplay.StyleSlider(slider); sliders[id] = slider; parent.Add(slider); return slider;
        }
        private void BuildAudio()
        {
            var row = Row(Frame("audio"));
            foreach (var id in new[] { "master", "music", "sfx" })
            {
                var column = new VisualElement(); Grow(column); column.AddToClassList("eov-options-spaced"); row.Add(column);
                var label = Text(column, id + "-label"); label.style.height = 12.54f;
                Slider(column, id, value => { if (id == "master") { MasterVolume = value; AudioManager.Instance?.SetMasterVolume(value); }
                    else if (id == "music") { MusicVolume = value; AudioManager.Instance?.SetMusicVolume(value); }
                    else { SfxVolume = value; AudioManager.Instance?.SetSfxVolume(value); } }, 11.64f);
            }
            var mute = new VisualElement(); mute.style.display = Application.isMobilePlatform ? DisplayStyle.None : DisplayStyle.Flex; row.Add(mute);
            var title = Text(mute, "mute-label", 6); title.style.height = 8.18f; title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Toggle(mute, "mute", () => { MuteWhenUnfocused = !MuteWhenUnfocused; AudioManager.Instance?.ApplyFocusMuteNow(); });
        }
        private void BuildDisplay()
        {
            var frame = Frame("display");
            var widthSetting = new VisualElement(); widthSetting.style.display = Application.isMobilePlatform ? DisplayStyle.None : DisplayStyle.Flex; frame.Add(widthSetting);
            Text(widthSetting, "width-label").style.height = 10.9f;
            Slider(widthSetting, "width", value =>
            {
                SafeAreaRatio = value;
                // Update the remaining Canvas roots during the staged migration as well.
                foreach (var safe in FindObjectsByType<ScreenSafeArea>()) safe.RatioPreference = value;
            });
            var row = Row(frame); row.style.marginTop = 2;
            var full = ActionButton(row, "fullscreen", () => Screen.fullScreenMode = FullScreenMode.FullScreenWindow); Grow(full); full.AddToClassList("eov-options-spaced");
            var window = ActionButton(row, "windowed", () => Screen.fullScreenMode = FullScreenMode.Windowed); Grow(window); window.AddToClassList("eov-options-spaced");
            var fps = ActionButton(row, "fps", () => { TargetFps = TargetFps == 30 ? 60 : TargetFps == 60 ? 120 : 30; ApplyFps(); RefreshValues(); }); Grow(fps); fps.AddToClassList("eov-options-spaced");
            Toggle(row, "vsync", () => { VSyncEnabled = !VSyncEnabled; if (VSyncEnabled) Application.targetFrameRate = -1; else ApplyFps(); });
            var label = Text(row, "vsync-label", 6); label.style.width = 26; label.style.marginLeft = 2; label.style.unityTextAlign = TextAnchor.MiddleCenter;
        }
        private static void ApplyFps()
        {
            if (TargetFps == 0) TargetFps = 60;
            QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
            Application.targetFrameRate = VSyncEnabled ? -1 : TargetFps;
        }
        private void BuildFloating()
        {
            var frame = Frame("floating"); var heading = Text(frame, "floating-label", 10); heading.style.height = 13.63f; heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            var row = Row(frame); row.style.marginTop = 2;
            foreach (var id in new[] { "drops", "enemy-damage", "player-damage" })
            {
                var entry = Row(row); Grow(entry); if (id != "player-damage") entry.style.marginRight = 8;
                var column = new VisualElement(); Grow(column); entry.Add(column); Text(column, id + "-label").style.height = 10.9f;
                Slider(column, id, value => { if (id == "drops") DropFloatingTextDuration = value * 10; else if (id == "player-damage") PlayerDamageTextDuration = value * 2; else EnemyDamageTextDuration = value * 2; RefreshDurations(); });
                var toggle = Toggle(entry, id + "-toggle", () => { if (id == "drops") ItemDropFloatingText = !ItemDropFloatingText; else if (id == "player-damage") PlayerFloatingDamage = !PlayerFloatingDamage; else EnemyFloatingDamage = !EnemyFloatingDamage; });
                toggle.style.alignSelf = Align.FlexEnd;
            }
        }
        private void BuildSaves()
        {
            var frame = Frame("saves"); var row = Row(frame);
            for (var i = 0; i < 3; i++)
            {
                var index = i; var slot = new VisualElement(); Grow(slot); slot.AddToClassList("eov-options-spaced"); row.Add(slot);
                Text(slot, "slot-" + i + "-title").style.height = 10.9f;
                var inset = new VisualElement(); inset.AddToClassList("eov-options-slot");  slot.Add(inset);
                Text(inset, "slot-" + i + "-playtime", 6).style.height = 8.18f;
                Text(inset, "slot-" + i + "-date", 6).style.height = 16;
                var actions = Row(inset);
                var save = ActionButton(actions, "save-" + i, () => { if (CanSave(index) && SaveSlotActions.SaveSlot(index)) RefreshSlots(); }); Grow(save); save.AddToClassList("eov-options-spaced");
                var load = ActionButton(actions, "load-" + i, () => LoadOrDelete(index)); Grow(load); load.AddToClassList("eov-options-spaced");
                Toggle(actions, "safety-" + i, () => { safety[index] = !safety[index]; RefreshSlotLabels(); });
            }
            var transfers = new VisualElement(); transfers.AddToClassList("eov-options-transfers");  row.Add(transfers);
            ActionButton(transfers, "import", () => OpenDialog("import"), 25.04f).style.marginBottom = 2;
            ActionButton(transfers, "export", () => OpenDialog("export"), 25.04f);
            var note = Text(frame, "autosave-label", 5.1f); note.style.height = StyleKeyword.Auto; note.style.marginTop = 8; note.style.marginLeft = 0;
        }
        private void BuildFooter()
        {
            var row = Row(scroll, "footer"); row.style.minHeight = 22;
            var credits = ActionButton(row, "credits", () => windows?.OpenWindow(TownWindowManager.Window.Credits), 19); Grow(credits); credits.AddToClassList("eov-options-spaced");
            var language = ActionButton(row, "language", () => OpenDialog("language"), 19); language.style.minWidth = 56.42f; language.style.paddingLeft = 4; language.style.paddingRight = 4; language.AddToClassList("eov-options-spaced");
            var folder = ActionButton(row, "folder", () => Application.OpenURL($"file://{Application.persistentDataPath}"), 19); folder.style.width = 100; folder.AddToClassList("eov-options-spaced");
            language.style.display = folder.style.display = Application.isMobilePlatform ? DisplayStyle.None : DisplayStyle.Flex;
            var version = new VisualElement(); version.style.width = 40;  row.Add(version);
            var label = Text(version, "version-label", 7); label.style.flexGrow = 1; label.style.unityTextAlign = TextAnchor.MiddleCenter;
            foreach (var id in new[] { "credits-label", "language-label", "folder-label" }) labels[id].style.fontSize = 7;
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
        private void RefreshLoadedData() { RefreshSlots(); if (IsOpen) RefreshValues(); }
        private void RefreshSlots() { for (var i = 0; i < 3; i++) slots[i].Refresh(i); RefreshSlotLabels(); }
        private void RefreshSlotLabels()
        {
            if (!IsOpen) return;
            for (var i = 0; i < 3; i++)
            {
                slots[i].Update(i); labels["slot-" + i + "-title"].text = "<b><smallcaps>" + slots[i].Title + "</smallcaps></b>";
                labels["slot-" + i + "-playtime"].text = "<b>" + slots[i].Playtime + "</b>";
                labels["slot-" + i + "-date"].text = "<b>" + slots[i].LastPlayed + "</b>";
                labels["save-" + i + "-label"].text = "<b><smallcaps>Save</smallcaps></b>";
                labels["load-" + i + "-label"].text = "<b><smallcaps>" + (safety[i] ? "Delete" : "Load") + "</smallcaps></b>";
                buttons["save-" + i].SetEnabled(CanSave(i)); buttons["load-" + i].SetEnabled(CanLoad(i)); SetToggle("safety-" + i, safety[i]);
            }
        }
        private void SetToggle(string id, bool on) { ToolkitGameplay.SetToggle(buttons[id],on);buttons[id].tooltip=on?"On":"Off"; }
        private void SetSlider(string id, float value) { sliders[id].SetValueWithoutNotify(value); sliders[id].Q("value-fill").style.width = Length.Percent(value * 100); }
        private void RefreshValues()
        {
            SetSlider("master", MasterVolume); SetSlider("music", MusicVolume); SetSlider("sfx", SfxVolume); SetSlider("width", SafeAreaRatio);
            SetSlider("drops", DropFloatingTextDuration / 10); SetSlider("player-damage", PlayerDamageTextDuration / 2); SetSlider("enemy-damage", EnemyDamageTextDuration / 2);
            SetToggle("mute", MuteWhenUnfocused); SetToggle("vsync", VSyncEnabled); SetToggle("drops-toggle", ItemDropFloatingText);
            SetToggle("player-damage-toggle", PlayerFloatingDamage); SetToggle("enemy-damage-toggle", EnemyFloatingDamage);
            buttons["fps"].SetEnabled(!VSyncEnabled); labels["fps-label"].text = "<b><smallcaps>" + (VSyncEnabled ? "FPS: VSync" : $"FPS: {TargetFps}") + "</smallcaps></b>";
            RefreshDurations();
        }
        private void RefreshDurations()
        {
            labels["drops-label"].text = "<b><smallcaps>Drops | " + CalcUtils.FormatTime(DropFloatingTextDuration, true, shortForm: true) + "</smallcaps></b>";
            labels["player-damage-label"].text = "<b><smallcaps>Enemies damage | " + CalcUtils.FormatTime(PlayerDamageTextDuration, true, shortForm: true) + "</smallcaps></b>";
            labels["enemy-damage-label"].text = "<b><smallcaps>Caleb's damage | " + CalcUtils.FormatTime(EnemyDamageTextDuration, true, shortForm: true) + "</smallcaps></b>";
        }
        private void Layout()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);
            widthPreview.style.left = area.x; widthPreview.style.top = area.y; widthPreview.style.width = area.width; widthPreview.style.height = area.height;
            root.style.left = area.center.x; root.style.top = area.y + 44; root.style.width = Mathf.Min(600, area.width-24); root.style.height = Mathf.Max(0, Mathf.Min(320,area.height - 56));
        }
        private void Update() { if (!IsOpen) return; Layout(); if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 1; RefreshSlotLabels(); } }
        public void Hide()
        {
            Blindsided.EventHandler.OnLoadData -= RefreshLoadedData;
            if (dialogs) dialogs.Imported -= RefreshSlots;
            foreach (var binding in bindings) binding.Dispose(); bindings.Clear(); buttons.Clear(); sliders.Clear(); labels.Clear();
            root?.RemoveFromHierarchy(); widthPreview?.RemoveFromHierarchy(); root = null; widthPreview = null; scroll = null;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (panel) Destroy(panel); if (dialogs) Destroy(dialogs.gameObject); }
    }
}


