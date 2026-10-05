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
        private ToolkitResourceInventoryScreen companionInventory;
        private ScrollView scroll;
        private ToolkitTextBinding instructionBinding;
        private readonly Dictionary<string, Action<float>> progress = new();
        private readonly HashSet<string> dirtyProgress = new();
        private bool dirty;
        private readonly Dictionary<QuestNoticeboardCategory,bool> expandedCategories=new();
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;

        public bool Show() => Show(null);
        public bool Show(ToolkitResourceInventoryScreen inventoryTemplate)
        {
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = QuestManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "quests" }; root.AddToClassList("eov-quests");
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface");root.AddToClassList("quests-reviewed");  document.rootVisualElement.Add(root);
            var columns = new VisualElement { name = "quests-columns" };
            columns.AddToClassList("quests-columns"); root.Add(columns);
            var quests = new VisualElement { name = "quests-content" };
            quests.AddToClassList("quests-content"); columns.Add(quests);
            Text(quests,ToolkitLocalization.Text("quests.heading", "Quests"),12).AddToClassList("quests-heading");
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "quest-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-buffs-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); quests.Add(scroll);
            if (inventoryTemplate && inventoryTemplate.IsConfigured)
            {
                var inventoryHost = new VisualElement { name = "quests-inventory" };
                inventoryHost.AddToClassList("quests-inventory"); columns.Add(inventoryHost);
                companionInventory = inventoryTemplate.CreateCompanionView();
                if (!companionInventory.ShowInQuests(inventoryHost))
                {
                    Destroy(companionInventory.gameObject); companionInventory = null;
                    inventoryHost.RemoveFromHierarchy();
                }
            }
            manager.RefreshNoticeboard();
            manager.NoticeboardChanged += Changed; manager.QuestProgressChanged += ProgressChanged;
            LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
            ToolkitLocalization.Changed += LocalizationChanged;
            Rebuild(); Layout(); return true;
        }
        private void LocaleChanged(UnityEngine.Localization.Locale _) { manager.RefreshNoticeboard(); dirty = true; }
        public bool TryHighlightResource(Upgrades.Resource resource, bool scrollToSlot)
        {
            if (!IsOpen || !companionInventory || !companionInventory.IsOpen) return false;
            companionInventory.HighlightResource(resource, scrollToSlot);
            return true;
        }
        private void LocalizationChanged() { if (manager) manager.RefreshNoticeboard(); dirty = true; }
        private static string CategoryName(QuestNoticeboardCategory category) => category switch
        {
            QuestNoticeboardCategory.Ready => ToolkitLocalization.Text("quests.category-ready", "Complete"),
            QuestNoticeboardCategory.Completed => ToolkitLocalization.Text("quests.category-completed", "Quest History"),
            _ => ToolkitLocalization.Text("quests.category-active", "Active")
        };
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
                var title = new Label(ToolkitLocalization.Text("quests.category-count", "{0} · {1}", CategoryName(category), items.Length));
                title.text = "<b>" + title.text + "</b>"; title.AddToClassList("eov-chapter-title"); title.pickingMode = PickingMode.Ignore; header.Add(title);
                var icon = new Label { pickingMode = PickingMode.Ignore }; icon.AddToClassList("book-disclosure"); header.Add(icon);
                var content = new VisualElement(); content.AddToClassList("eov-quest-entries");
                var expanded = expandedCategories.TryGetValue(category,out var saved)?saved:category != QuestNoticeboardCategory.Completed;
                void Expand(bool value) { expanded = value; expandedCategories[category]=value; content.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; icon.text = value ? "−" : "+";header.EnableInClassList("expanded",value); }
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
            row.EnableInClassList("quest-completed",entry.Completed); row.EnableInClassList("quest-ready",!entry.Completed&&entry.Progress>=1); parent.Add(row);
            var top = new VisualElement(); top.AddToClassList("eov-quest-top"); row.Add(top);
            var texts = new VisualElement(); texts.AddToClassList("eov-quest-texts"); top.Add(texts);
            var pinned = Blindsided.Oracle.oracle?.saveData?.PinnedQuests?.Contains(quest.questId) == true;
            Text(texts, "<b>" + quest.questName.GetLocalizedString() + "</b>", 9).AddToClassList("quest-name");
            var description = Text(texts, quest.description.GetLocalizedString(), 6); description.AddToClassList("quest-description");
            Button turnIn = null;
            if (!entry.Completed)
            {
                var controls = new VisualElement(); controls.AddToClassList("eov-quest-controls"); top.Add(controls);
                turnIn = ToolkitBuffsScreen.MakeButton("turn-in-" + quest.questId, () => manager.TryTurnInQuest(quest.questId), definition.button);
                turnIn.AddToClassList("eov-quest-turn-in");
                var first = quest.requirements?.FirstOrDefault()?.type;
                Text(turnIn, "<b>" + (first == QuestData.RequirementType.Instant ? ToolkitLocalization.Text("quests.okay", "Okay") : first == QuestData.RequirementType.Meet ? ToolkitLocalization.Text("quests.done", "Done") : ToolkitLocalization.Text("quests.turn-in", "Turn In")) + "</b>", 8);
                controls.Add(turnIn); turnIn.SetEnabled(entry.Progress >= 1);
            }
            var requirements = new List<(QuestData.Requirement req, Label text, VisualElement fill)>();
            var requirementGroup = new VisualElement(); requirementGroup.style.marginTop = 2; row.Add(requirementGroup);
            if (!entry.Completed && quest.requirements != null)
                foreach (var req in quest.requirements.Where(r => r != null && r.type != QuestData.RequirementType.Instant))
                {
                    var requirement = new VisualElement();requirement.AddToClassList("quest-requirement"); requirementGroup.Add(requirement);
                    var label = Text(requirement, "", 8); label.userData = 6.4f;
                    var track = new VisualElement(); track.style.height = 3; track.AddToClassList("track"); requirement.Add(track);
                    var fill = new VisualElement(); fill.style.height = 3; fill.style.overflow = Overflow.Hidden; track.Add(fill);
                    var sprite = new VisualElement { pickingMode = PickingMode.Ignore }; sprite.style.position = Position.Absolute; sprite.style.height = 3; sprite.AddToClassList("fill"); fill.Add(sprite);
                    track.RegisterCallback<GeometryChangedEvent>(_ => sprite.style.width = track.contentRect.width);
                    requirements.Add((req, label, fill));
                }
            var reward = Text(row, ToolkitLocalization.Text("quests.reward", "Reward: {0}", quest.rewardDescription.GetLocalizedString()), 7); reward.AddToClassList("quest-reward");
            var canPin = !entry.Completed && quest.requirements?.Any(r => r != null && r.type == QuestData.RequirementType.Instant) != true;
            if (canPin)
            {
                void Pin() { manager.TogglePinned(quest.questId); Audio.AudioManager.Instance?.PlayUIButtonClick(); }
                var pin=new Button(Pin){name="pin-"+quest.questId,text=pinned?ToolkitLocalization.Text("quests.pinned", "Pinned"):ToolkitLocalization.Text("quests.pin", "Pin")};pin.AddToClassList("button");pin.AddToClassList("quest-pin");pin.EnableInClassList("active",pinned);top.Q(className:"eov-quest-controls").Add(pin);
                texts.pickingMode = PickingMode.Position; reward.pickingMode = PickingMode.Position;
                texts.RegisterCallback<ClickEvent>(_ => Pin()); reward.RegisterCallback<ClickEvent>(_ => Pin());
                texts.focusable = true; texts.tabIndex = 0; texts.RegisterCallback<NavigationSubmitEvent>(evt => { Pin(); evt.StopPropagation(); });
            }
            void UpdateProgress(float value)
            {
                turnIn?.SetEnabled(value >= 1);row.EnableInClassList("quest-ready",!entry.Completed&&value>=1);
                foreach (var item in requirements)
                {
                    var presentation = QuestRequirementPresentation.Build(quest, item.req);
                    item.text.text = presentation.text; item.fill.style.width = Length.Percent(presentation.progress * 100);
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
        private void Layout() => windowLayout.Fill(root, theme);
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (dirty) { dirty = false; root.Q<Label>(className: "quests-heading").text = ToolkitLocalization.Text("quests.heading", "Quests"); Rebuild(); }
            else if (dirtyProgress.Count > 0)
            {
                foreach (var entry in manager.GetNoticeboardEntries()) if (dirtyProgress.Contains(entry.Quest.questId) && progress.TryGetValue(entry.Quest.questId, out var update)) update(entry.Progress);
                dirtyProgress.Clear();
            }
        }
        public void Hide()
        {
            if (companionInventory)
            {
                companionInventory.Hide(); Destroy(companionInventory.gameObject); companionInventory = null;
            }
            if (manager) { manager.NoticeboardChanged -= Changed; manager.QuestProgressChanged -= ProgressChanged; }
            LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            ToolkitLocalization.Changed -= LocalizationChanged;
            instructionBinding?.Dispose(); instructionBinding = null; root?.RemoveFromHierarchy(); root = null; progress.Clear(); dirtyProgress.Clear(); dirty = false;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
