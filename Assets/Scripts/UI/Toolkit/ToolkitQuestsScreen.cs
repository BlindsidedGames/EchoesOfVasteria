using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitQuestsScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitQuestsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private QuestManager manager;
        private VisualElement root;
        private ScrollView scroll;
        private ToolkitTextBinding instructionBinding;
        private readonly Dictionary<string, Action<float>> progress = new();
        private readonly HashSet<string> dirtyProgress = new();
        private bool dirty;
        public float CompanionWidth { get; set; } = 178;
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;

        public bool Show()
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = QuestManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "quests" }; root.AddToClassList("eov-quests");
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface");  document.rootVisualElement.Add(root);
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "quest-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-buffs-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); root.Add(scroll);
            manager.RefreshNoticeboard();
            manager.NoticeboardChanged += Changed; manager.QuestProgressChanged += ProgressChanged;
            LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
            Rebuild(); Layout(); return true;
        }
        private void LocaleChanged(UnityEngine.Localization.Locale _) { manager.RefreshNoticeboard(); dirty = true; }
        private void Changed() => dirty = true;
        private void ProgressChanged(QuestData quest, float value) { if (quest) dirtyProgress.Add(quest.questId); }
        private void Rebuild()
        {
            var offset = scroll.scrollOffset; instructionBinding?.Dispose(); scroll.Clear(); progress.Clear(); dirtyProgress.Clear();
            var instructions = Text(scroll, "", 4.4f); instructions.AddToClassList("eov-quest-instructions");
            instructionBinding = new ToolkitTextBinding(instructions, definition.instructions);
            var entries = manager.GetNoticeboardEntries();
            foreach (var category in (QuestNoticeboardCategory[])Enum.GetValues(typeof(QuestNoticeboardCategory)))
            {
                var items = entries.Where(e => e.Category == category).ToArray(); if (items.Length == 0) continue;
                var group = new VisualElement(); group.AddToClassList("eov-quest-category"); scroll.Add(group);
                var header = new Button { name = "quest-category-" + category }; header.AddToClassList("eov-chapter-header"); header.AddToClassList("button");
                var title = new Label((category == QuestNoticeboardCategory.Ready ? "Complete" : category == QuestNoticeboardCategory.Completed ? "Quest History" : category.ToString()) + " | " + items.Length);
                title.text = "<b><smallcaps>" + title.text + "</smallcaps></b>"; title.AddToClassList("eov-chapter-title"); title.pickingMode = PickingMode.Ignore; header.Add(title);
                var icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit }; icon.AddToClassList("eov-chapter-icon"); header.Add(icon);
                var content = new VisualElement(); content.AddToClassList("eov-quest-entries");
                var expanded = category != QuestNoticeboardCategory.Completed;
                void Expand(bool value) { expanded = value; content.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; icon.sprite = value ? theme.collapse : theme.expand; }
                header.clicked += () => { Expand(!expanded); Audio.AudioManager.Instance?.PlayUIButtonClick(); };
                group.Add(header); group.Add(content); Expand(expanded);
                foreach (var entry in items) AddQuest(content, entry);
                content.ElementAt(content.childCount - 1).style.marginBottom = 0;
            }
            scroll.scrollOffset = offset;
        }
        private void AddQuest(VisualElement parent, QuestNoticeboardEntry entry)
        {
            var quest = entry.Quest;
            var row = new VisualElement { name = "quest-" + quest.questId }; row.AddToClassList("eov-quest-row"); 
            row.style.unityBackgroundImageTintColor = new Color(1, 1, 1, entry.Completed ? .7f : 1); parent.Add(row);
            var top = new VisualElement(); top.AddToClassList("eov-quest-top"); row.Add(top);
            var texts = new VisualElement(); texts.AddToClassList("eov-quest-texts"); top.Add(texts);
            var pinned = Blindsided.Oracle.oracle?.saveData?.PinnedQuests?.Contains(quest.questId) == true;
            Text(texts, "<b>" + quest.questName.GetLocalizedString() + (entry.Completed ? " | Completed" : pinned ? " | Pinned" : "") + "</b>", 8);
            var description = Text(texts, quest.description.GetLocalizedString(), 6); description.style.marginTop = 2;
            Button turnIn = null;
            if (!entry.Completed)
            {
                var controls = new VisualElement(); controls.AddToClassList("eov-quest-controls"); top.Add(controls);
                turnIn = ToolkitBuffsScreen.MakeButton("turn-in-" + quest.questId, () => manager.TryTurnInQuest(quest.questId), definition.button);
                turnIn.AddToClassList("eov-quest-turn-in");
                var first = quest.requirements?.FirstOrDefault()?.type;
                Text(turnIn, "<b>" + (first == QuestData.RequirementType.Instant ? "Okay" : first == QuestData.RequirementType.Meet ? "Done" : "Turn In") + "</b>", 8);
                controls.Add(turnIn); turnIn.SetEnabled(entry.Progress >= 1);
            }
            var requirements = new List<(QuestData.Requirement req, Label text, VisualElement fill)>();
            var requirementGroup = new VisualElement(); requirementGroup.style.marginTop = 2; row.Add(requirementGroup);
            if (!entry.Completed && quest.requirements != null)
                foreach (var req in quest.requirements.Where(r => r != null && r.type != QuestData.RequirementType.Instant))
                {
                    var requirement = new VisualElement(); requirementGroup.Add(requirement);
                    var label = Text(requirement, "", 8); label.userData = 6.4f;
                    var track = new VisualElement(); track.style.height = 4; track.AddToClassList("track"); requirement.Add(track);
                    var fill = new VisualElement(); fill.style.height = 4; fill.style.overflow = Overflow.Hidden; track.Add(fill);
                    var sprite = new VisualElement { pickingMode = PickingMode.Ignore }; sprite.style.position = Position.Absolute; sprite.style.height = 4; sprite.AddToClassList("fill"); fill.Add(sprite);
                    track.RegisterCallback<GeometryChangedEvent>(_ => sprite.style.width = track.contentRect.width);
                    requirements.Add((req, label, fill));
                }
            var reward = Text(row, "<b>Reward: " + quest.rewardDescription.GetLocalizedString() + "</b>", 6); reward.style.marginTop = 2;
            var canPin = !entry.Completed && quest.requirements?.Any(r => r != null && r.type == QuestData.RequirementType.Instant) != true;
            if (canPin)
            {
                void Pin() { manager.TogglePinned(quest.questId); Audio.AudioManager.Instance?.PlayUIButtonClick(); }
                texts.pickingMode = PickingMode.Position; reward.pickingMode = PickingMode.Position;
                texts.RegisterCallback<ClickEvent>(_ => Pin()); reward.RegisterCallback<ClickEvent>(_ => Pin());
                texts.focusable = true; texts.tabIndex = 0; texts.RegisterCallback<NavigationSubmitEvent>(evt => { Pin(); evt.StopPropagation(); });
            }
            void UpdateProgress(float value)
            {
                turnIn?.SetEnabled(value >= 1);
                foreach (var item in requirements)
                {
                    var presentation = QuestRequirementPresentation.Build(quest, item.req);
                    item.text.text = "<b>" + presentation.text + "</b>"; item.fill.style.width = Length.Percent(presentation.progress * 100);
                }
            }
            progress[quest.questId] = UpdateProgress; UpdateProgress(entry.Progress);
        }
        private static Label Text(VisualElement parent, string value, float size)
        {
            var label = new Label(value) { pickingMode = PickingMode.Ignore, enableRichText = true }; label.AddToClassList("eov-quest-text"); label.style.fontSize = Mathf.Max(7,size);
 parent.Add(label); return label;
        }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void Layout() => windowLayout.Fill(root, theme, CompanionWidth);
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (dirty) { dirty = false; Rebuild(); }
            else if (dirtyProgress.Count > 0)
            {
                foreach (var entry in manager.GetNoticeboardEntries()) if (dirtyProgress.Contains(entry.Quest.questId) && progress.TryGetValue(entry.Quest.questId, out var update)) update(entry.Progress);
                dirtyProgress.Clear();
            }
        }
        public void Hide()
        {
            if (manager) { manager.NoticeboardChanged -= Changed; manager.QuestProgressChanged -= ProgressChanged; }
            LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            instructionBinding?.Dispose(); instructionBinding = null; root?.RemoveFromHierarchy(); root = null; progress.Clear(); dirtyProgress.Clear(); dirty = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
