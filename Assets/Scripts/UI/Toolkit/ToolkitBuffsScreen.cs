using System.Collections.Generic;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.Stats;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitBuffsScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitBuffsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private ToolkitProspectorPicker pickerPrefab;
        private ToolkitProspectorPicker picker;
        private PanelSettings panel;
        private VisualElement root;
        private ScrollView scroll;
        private ToolkitTextBinding instructionBinding;
        private readonly List<Button> assignButtons = new();
        private readonly Button[] slots = new Button[5];
        private readonly Image[] icons = new Image[5], autoIcons = new Image[5];
        private readonly Label[] locked = new Label[5];
        private Button targetButton;
        private Label targetText;
        private Image targetIcon;
        private BuffRecipe selected;
        private bool inRun, rebuild;
        private float nextRefresh;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings && pickerPrefab;

        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured || !BuffManager.Instance) return false;
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 100; }
            var doc = GetComponent<UIDocument>(); doc.panelSettings = panel; doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "buffs" }; root.AddToClassList("eov-buffs"); theme.Apply(root);ToolkitGameplay.Apply(root,theme); 
            doc.rootVisualElement.Add(root);
            var header = new VisualElement(); header.AddToClassList("eov-buffs-header"); root.Add(header);
            var slotFrame = new VisualElement { name = "assignment-slots" }; slotFrame.AddToClassList("eov-buffs-slots");  header.Add(slotFrame);
            for (var i = 0; i < 5; i++)
            {
                var index = i; var button = MakeButton("assign-slot-" + i, () => AssignSlot(index), definition.slot);
                button.AddToClassList("eov-buffs-slot"); slotFrame.Add(button); slots[i] = button;
                icons[i] = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit }; icons[i].AddToClassList("eov-buffs-slot-icon"); button.Add(icons[i]);
                locked[i] = Label("", 5); locked[i].AddToClassList("eov-buffs-slot-locked"); button.Add(locked[i]);
                autoIcons[i] = new Image { sprite = definition.autoCast, scaleMode=ScaleMode.ScaleToFit, tintColor = definition.autoCastTint, pickingMode = PickingMode.Ignore }; autoIcons[i].AddToClassList("eov-buffs-auto"); button.Add(autoIcons[i]);
            }
            var instruction = new VisualElement(); instruction.AddToClassList("eov-buffs-instructions");  header.Add(instruction);
            var text = Label("", 6.7f); text.style.unityFontStyleAndWeight = FontStyle.Normal; text.style.letterSpacing = .134f; text.style.whiteSpace = WhiteSpace.Normal; instruction.Add(text);
            instructionBinding = new ToolkitTextBinding(text, definition.instructions);
            var inset = new VisualElement(); inset.AddToClassList("eov-buffs-inset");  root.Add(inset);
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "buff-recipes", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-buffs-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); inset.Add(scroll);
            inRun = GameplayStatTracker.Instance?.RunInProgress == true;
            Blindsided.EventHandler.OnLoadData += Loaded;
            Blindsided.EventHandler.OnQuestHandin += QuestChanged;
            Blindsided.EventHandler.OnRunStarted += RunStarted;
            Blindsided.EventHandler.OnRunEnded += RunEnded;
            BuildRows(); Layout(); Refresh(); return true;
        }

        internal static Label Label(string text, float size)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore }; label.AddToClassList("eov-buffs-label"); label.style.fontSize = Mathf.Max(7,size); return label;
        }
        internal static Button MakeButton(string name, System.Action click, Sprite sprite)
        {
            var button = ToolkitControls.Button(name, click, sprite);
            button.AddToClassList("eov-buffs-button");button.AddToClassList("button");button.style.backgroundImage=StyleKeyword.None;
            return button;
        }
        private void BuildRows()
        {
            scroll.Clear(); assignButtons.Clear(); targetButton = null; targetText = null; targetIcon = null;
            var manager = BuffManager.Instance; if (!manager) return;
            foreach (var recipe in manager.Recipes)
            {
                if (!recipe || (recipe.requiredQuest && (QuestManager.Instance == null || !QuestManager.Instance.IsQuestCompleted(recipe.requiredQuest)))) continue;
                var row = new VisualElement { name = "recipe-" + recipe.name }; row.AddToClassList("eov-buff-row");  scroll.Add(row);
                var line = new VisualElement(); line.AddToClassList("eov-buff-line"); row.Add(line);
                var iconFrame = new VisualElement(); iconFrame.AddToClassList("eov-buff-icon-frame");  line.Add(iconFrame);
                var icon = new Image { sprite = recipe.buffIcon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; icon.AddToClassList("eov-buffs-slot-icon"); iconFrame.Add(icon);
                var texts = new VisualElement(); texts.AddToClassList("eov-buff-texts"); line.Add(texts);
                var name = Label(recipe.GetDisplayName(), 6); name.style.height = StyleKeyword.Auto;name.style.fontSize=9; texts.Add(name);
                var description = Label(string.Join("\n", recipe.GetDescriptionLines()), 4); description.style.height = StyleKeyword.Auto; description.style.whiteSpace = WhiteSpace.Normal; texts.Add(description);
                var assign = MakeButton("assign-" + recipe.name, () => { if (!inRun) { selected = recipe; Refresh(); } }, definition.button);
                assign.AddToClassList("eov-buff-assign"); var label = Label("Assign", 8); assign.Add(label); line.Add(assign); assignButtons.Add(assign);
                if (recipe.HasEffect(BuffEffectType.ProspectorWeightPercent))
                {
                    targetButton = MakeButton("prospector-target", OpenPicker, definition.button); targetButton.AddToClassList("eov-buff-target"); row.Add(targetButton);
                    targetIcon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; targetIcon.AddToClassList("eov-buff-target-icon"); targetButton.Add(targetIcon);
                    targetText = Label("", 6); targetText.style.unityFontStyleAndWeight = FontStyle.Normal; targetText.AddToClassList("eov-buff-target-label"); targetButton.Add(targetText);
                }
            }
        }
        private void OpenPicker()
        {
            if (inRun) return;
            if (!picker) picker = Instantiate(pickerPrefab);
            picker.Show(() => IsOpen, Refresh, targetButton);
        }
        private void AssignSlot(int index)
        {
            var manager = BuffManager.Instance; if (!manager) return;
            if (!inRun && selected && manager.IsSlotUnlocked(index)) manager.AssignBuff(index, selected);
            else manager.ToggleSlotAutoCast(index);
            selected = null; Refresh();
        }
        private void Refresh()
        {
            var manager = BuffManager.Instance; if (!manager || !IsOpen) return;
            for (var i = 0; i < 5; i++)
            {
                var recipe = manager.GetAssigned(i); icons[i].sprite = recipe ? recipe.buffIcon : null;
                icons[i].tintColor = new Color(1, 1, 1, recipe ? manager.IsSlotUnlocked(i) ? 1 : .4f : 0);
                slots[i].SetEnabled(selected ? manager.IsSlotUnlocked(i) : manager.IsAutoSlotUnlocked(i));
                autoIcons[i].style.display = manager.IsSlotAutoCasting(i) ? DisplayStyle.Flex : DisplayStyle.None;
                locked[i].text = manager.IsSlotUnlocked(i) ? "" : "Locked";
            }
            foreach (var button in assignButtons) button.SetEnabled(!inRun);
            if (targetButton != null)
            {
                targetButton.SetEnabled(!inRun); var task = manager.ProspectorTarget;
                targetText.text = task ? "Target: " + task.taskName + "  \u2022 Change" : "Choose target";
                targetIcon.sprite = task ? task.taskIcon : null; targetIcon.style.display = targetIcon.sprite ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        private void Loaded() { selected = null; if (picker) picker.Hide(); rebuild = true; }
        private void QuestChanged(string _) => rebuild = true;
        private void RunStarted() { inRun = true; selected = null; if (picker) picker.Hide(); Refresh(); }
        private void RunEnded() { inRun = false; Refresh(); }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout() => windowLayout.Fill(root, theme);
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (rebuild) { rebuild = false; BuildRows(); Refresh(); }
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .1f; Refresh(); }
        }
        public void Hide()
        {
            Blindsided.EventHandler.OnLoadData -= Loaded; Blindsided.EventHandler.OnQuestHandin -= QuestChanged;
            Blindsided.EventHandler.OnRunStarted -= RunStarted; Blindsided.EventHandler.OnRunEnded -= RunEnded;
            if (picker) picker.Hide(); instructionBinding?.Dispose(); instructionBinding = null;
            root?.RemoveFromHierarchy(); root = null; scroll = null; selected = null; assignButtons.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (panel) Destroy(panel); if (picker) Destroy(picker.gameObject); }
    }
}

