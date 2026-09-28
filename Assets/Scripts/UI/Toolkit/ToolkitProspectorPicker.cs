using System;
using System.Linq;
using System.Collections.Generic;
using TimelessEchoes.Buffs;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitProspectorPicker : MonoBehaviour
    {
        [SerializeField] private ToolkitBuffsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, frame, inset;
        private ScrollView scroll;
        private Button previous, next, cancel, confirm;
        private Label skill, selectedLabel;
        private TaskData[] tasks;
        private string[] skills;
        private int filter;
        private TaskData selected;
        private Func<bool> ownerOpen;
        private Action confirmed;
        private VisualElement previousFocus;
        private readonly List<(Button button, TaskData task, VisualElement crop, Image icon, Label label)> rows = new();
        public static bool IsOpen { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;

        public void Show(Func<bool> owner, Action onConfirmed, VisualElement returnFocus)
        {
            Hide(); ownerOpen = owner; confirmed = onConfirmed; previousFocus = returnFocus;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 300; }
            var doc = GetComponent<UIDocument>(); doc.panelSettings = settings; doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "prospector-picker" }; root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.AddToClassList("eov-prospector"); root.style.backgroundColor = definition.pickerDimmer; theme.Apply(root);ToolkitGameplay.Apply(root,theme); doc.rootVisualElement.Add(root);
            frame = new VisualElement(); frame.style.position = Position.Absolute;frame.AddToClassList("surface");  root.Add(frame);
            previous = Button("previous-skill", "‹", () => Filter(-1)); next = Button("next-skill", "›", () => Filter(1));
            skill = ToolkitBuffsScreen.Label("", 22); skill.style.position = Position.Absolute; skill.style.unityTextAlign = TextAnchor.MiddleCenter; frame.Add(skill);
            inset = new VisualElement(); inset.style.position = Position.Absolute;  frame.Add(inset);
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "prospector-tasks", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.style.position = Position.Absolute; scroll.style.left = 2; scroll.style.right = 2; scroll.style.top = 2; scroll.style.bottom = 1;
            scroll.contentContainer.style.paddingTop = 1; inset.Add(scroll);
            selectedLabel = ToolkitBuffsScreen.Label("", 20); selectedLabel.style.position = Position.Absolute; selectedLabel.style.unityTextAlign = TextAnchor.MiddleLeft; frame.Add(selectedLabel);
            cancel = Button("cancel-target", "Cancel", Hide); confirm = Button("confirm-target", "Confirm", Confirm);
            tasks = Resources.LoadAll<TaskData>("Tasks").Where(t => t && t.weight > 0 && TaskWeightService.IsTaskUnlocked(t)).OrderBy(t => t.taskName).ToArray();
            skills = new[] { "All skills" }.Concat(tasks.Select(SkillName).Distinct().OrderBy(s => s)).ToArray(); filter = 0;
            selected = BuffManager.Instance ? BuffManager.Instance.ProspectorTarget : null; IsOpen = true; BuildRows(); Layout(); cancel.Focus();
        }
        private Button Button(string name, string text, Action action)
        {
            var button = ToolkitBuffsScreen.MakeButton(name, action, definition.pickerButton); button.style.position = Position.Absolute;
            button.Add(ToolkitBuffsScreen.Label(text, 22)); frame.Add(button); return button;
        }
        private static string SkillName(TaskData task) => task.associatedSkill ? task.associatedSkill.name : "Other";
        private void Filter(int delta) { filter = (filter + delta + skills.Length) % skills.Length; BuildRows(); Layout(); }
        private void BuildRows()
        {
            scroll.Clear(); rows.Clear(); skill.text = skills[filter];
            foreach (var task in tasks.Where(t => filter == 0 || SkillName(t) == skills[filter]))
            {
                var button = ToolkitBuffsScreen.MakeButton("task-" + task.name, () => { selected = task; Selection(); }, definition.pickerRow);
                button.style.flexShrink = 0; button.style.flexDirection = FlexDirection.Row; button.style.justifyContent = Justify.FlexStart;
                var crop = new VisualElement { pickingMode = PickingMode.Ignore }; crop.style.overflow = Overflow.Hidden; crop.style.flexShrink = 0; crop.style.alignItems = Align.Center; crop.style.justifyContent = Justify.Center;
                 button.Add(crop);
                var icon = new Image { sprite = task.taskIcon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; icon.style.flexShrink = 0; crop.Add(icon);
                var label = ToolkitBuffsScreen.Label(task.taskName, 22); label.style.flexGrow = 1; label.style.minWidth = 0; label.style.unityTextAlign = TextAnchor.MiddleLeft; button.Add(label);
                rows.Add((button, task, crop, icon, label)); scroll.Add(button);
            }
            scroll.scrollOffset = Vector2.zero; Selection();
        }
        private void Selection()
        {
            selectedLabel.text = selected ? "Target: " + selected.taskName : "Choose a task";
            confirm.SetEnabled(selected && TaskWeightService.IsTaskUnlocked(selected));
            foreach (var row in rows) row.button.EnableInClassList("active",selected==row.task);
        }
        private void Confirm()
        {
            if (BuffManager.Instance && BuffManager.Instance.SetProspectorTarget(selected)) { confirmed?.Invoke(); Hide(); }
        }
        private static void Box(VisualElement element, float x, float y, float width, float height)
        { element.style.left = x; element.style.top = y; element.style.width = width; element.style.height = height; }
        private void Layout()
        {
            var scale = 432f / Mathf.Max(1, Screen.height); var safe = Screen.safeArea;
            var area = new Rect(safe.x * scale, (Screen.height - safe.yMax) * scale, safe.width * scale, safe.height * scale);
            var density = Application.isMobilePlatform && Screen.dpi > 0 ? Mathf.Clamp(Screen.dpi / 160, 1, 5) : Mathf.Max(1, Screen.height / 1080f);
            var unit = density * scale;
            var width = Mathf.Min(640, area.width / unit - 24); var height = Mathf.Min(720, area.height / unit - 24);
            var compact = height < 500 && width > 520;
            Box(frame, area.center.x - width * unit / 2, area.center.y - height * unit / 2, width * unit, height * unit);
            Box(previous, 16 * unit, 16 * unit, 64 * unit, 56 * unit); Box(next, (width - 80) * unit, 16 * unit, 64 * unit, 56 * unit);
            Box(skill, 84 * unit, 16 * unit, (width - 168) * unit, 56 * unit); skill.style.fontSize = 22 * unit;
            Box(inset, 16 * unit, 84 * unit, (width - 32) * unit, (height - 84 - (compact ? 112 : 130)) * unit);
            Box(selectedLabel, 16 * unit, (height - (compact ? 110 : 126)) * unit, (width - 32) * unit, (compact ? 32 : 48) * unit); selectedLabel.style.fontSize = 20 * unit;
            Box(cancel, 16 * unit, (height - 76) * unit, (width / 2 - 24) * unit, 60 * unit); Box(confirm, (width / 2 + 8) * unit, (height - 76) * unit, (width / 2 - 24) * unit, 60 * unit);
            foreach (var button in new[] { previous, next, cancel, confirm }) button.Q<Label>().style.fontSize = 22 * unit;
            foreach (var row in rows)
            {
                row.button.style.height = 64 * unit; row.button.style.marginBottom = 4 * unit;
                row.crop.style.width = row.crop.style.height = 48 * unit; row.crop.style.marginLeft = 8 * unit;
                row.label.style.marginLeft = 12 * unit; row.label.style.fontSize = 22 * unit;
                var sprite = row.task.taskIcon;
                if (sprite) { row.icon.style.width=48*unit;row.icon.style.height=48*unit; }
            }
        }
        private void Update()
        {
            if (root == null) return;
            if (ownerOpen?.Invoke() != true || Stats.GameplayStatTracker.Instance?.RunInProgress == true || Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Hide(); return; }
            Layout();
        }
        public void Hide()
        {
            if (root == null) return; root.RemoveFromHierarchy(); root = null; rows.Clear(); IsOpen = false;
            previousFocus?.Focus(); previousFocus = null; confirmed = null; ownerOpen = null;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
