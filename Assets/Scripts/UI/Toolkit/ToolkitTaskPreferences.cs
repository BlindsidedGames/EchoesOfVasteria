using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Task preferences grouped by skill, with lifetime statistics behind an explicit tap.</summary>
    public sealed class ToolkitTaskPreferences
    {
        private sealed class Entry
        {
            public TaskData task;
            public VisualElement root;
            public Button name, toggle;
            public Label title, chance;
            public Image icon, check;
        }
        private sealed class Group
        {
            public Skill skill;
            public VisualElement root, known, unknown;
            public Button selector, unknownButton;
            public bool expanded;
            public readonly List<Entry> entries = new();
        }
        private readonly TaskStatisticsPresentation presentation;
        private readonly GameplayStatTracker tracker;
        private readonly VisualElement host, parent, toolbar, selectors, grid;
        private readonly ScrollView scroll;
        private readonly Label context;
        private readonly List<Group> groups = new();
        private readonly List<VisualElement> columns = new();
        private string columnLayout;
        private readonly Action<TaskStatsPanelUI.SortMode> setSort;
        private readonly Action<Skill> selectSkill;
        private Group selected;
        private TaskData detailTask;
        private VisualElement detailOverlay;
        private Label detailText;
        private Button detailToggle;
        private TaskStatsPanelUI.SortMode sort;

        public ToolkitTaskPreferences(VisualElement parent, VisualElement modalHost, ToolkitTheme theme, Sprite inset,
            TaskStatisticsPresentation source, GameplayStatTracker stats, TaskStatsPanelUI.SortMode sortMode,
            string selectedSkill, Action<TaskStatsPanelUI.SortMode> sortChanged, Action<Skill> selectionChanged)
        {
            host = modalHost; this.parent = parent; presentation = source; tracker = stats; sort = sortMode;
            setSort = sortChanged; selectSkill = selectionChanged;
            toolbar = ToolkitGameplay.E(parent, "task-preferences-toolbar");
            selectors = ToolkitGameplay.E(toolbar, "task-skill-selectors");
            var sortOptions = Enum.GetValues(typeof(TaskStatsPanelUI.SortMode)).Cast<TaskStatsPanelUI.SortMode>().ToList();
            var sortPicker = new DropdownField(sortOptions.Select(SortLabel).ToList(), (int)sortMode) { name = "task-preferences-sort" };
            sortPicker.AddToClassList("task-preferences-sort");
            ToolkitLocalization.BindTooltip(sortPicker, "statistics.sort-label", "Sort");
            sortPicker.RegisterValueChangedCallback(e =>
            {
                var index = sortPicker.choices.IndexOf(e.newValue);
                if (index >= 0) { sort = sortOptions[index]; setSort(sort); }
            });
            toolbar.Add(sortPicker);
            context = ToolkitGameplay.L(parent, "", "task-preferences-context");
            scroll = ToolkitControls.RecessedScroll(parent, "task-statistics", theme, inset);
            grid = ToolkitGameplay.E(scroll.contentContainer, "task-preferences-grid");
            foreach (var grouping in source.Ordered(TaskStatsPanelUI.SortMode.Default, stats)
                .Where(t => t.associatedSkill).GroupBy(t => t.associatedSkill))
            {
                var group = new Group { skill = grouping.Key };
                groups.Add(group);
                group.selector = ToolkitGameplay.B(selectors, "", () => Select(group));
                group.selector.name = "task-skill-" + group.skill.name;
                group.selector.AddToClassList("task-skill-selector");
                group.selector.tooltip = ToolkitLocalization.Name(group.skill, "skill");
                var skillIcon = ToolkitControls.Icon(group.skill.skillIcon); group.selector.Add(skillIcon);
                group.root = ToolkitGameplay.E(grid, "task-preferences-group");
                group.root.name = "task-group-" + group.skill.name;
                var heading = ToolkitGameplay.E(group.root, "task-preferences-heading");
                var image = ToolkitControls.Icon(group.skill.skillIcon); heading.Add(image);
                ToolkitGameplay.L(heading, ToolkitLocalization.Name(group.skill, "skill"), "task-preferences-skill-name");
                ToolkitGameplay.L(heading, ToolkitLocalization.Text("statistics.boost", "Favour"), "task-preferences-favour-label");
                group.known = ToolkitGameplay.E(group.root, "task-preferences-known");
                group.unknownButton = ToolkitGameplay.B(group.root, "", () =>
                {
                    group.expanded = !group.expanded;
                    Refresh();
                });
                group.unknownButton.AddToClassList("task-preferences-unknown-toggle");
                group.unknown = ToolkitGameplay.E(group.root, "task-preferences-unknown");
                foreach (var task in grouping)
                {
                    var entry = new Entry { task = task };
                    group.entries.Add(entry);
                    entry.root = ToolkitGameplay.E(group.known, "task-preference-row");
                    entry.root.name = "task-stat-" + task.name;
                    entry.name = ToolkitGameplay.B(entry.root, "", () => OpenDetails(task));
                    entry.name.AddToClassList("task-preference-name");
                    entry.icon = ToolkitControls.Icon(null); entry.name.Add(entry.icon);
                    entry.title = ToolkitGameplay.L(entry.name, "", "task-preference-title");
                    entry.chance = ToolkitGameplay.L(entry.root, "", "task-preference-chance");
                    entry.toggle = ToolkitGameplay.B(entry.root, "", () => TaskWeightService.SetToggle(task, !TaskWeightService.IsToggleEnabled(task)));
                    entry.toggle.name = "toggle-task-stat-" + task.name;
                    entry.toggle.AddToClassList("task-preference-toggle");
                    entry.check = ToolkitControls.Icon(null); entry.toggle.Add(entry.check);
                }
            }
            selected = groups.FirstOrDefault(g => g.skill.name == selectedSkill) ?? groups.FirstOrDefault();
            Refresh();
        }

        private static string SortLabel(TaskStatsPanelUI.SortMode mode) => mode switch
        {
            TaskStatsPanelUI.SortMode.Completions => ToolkitLocalization.Text("statistics.sort-completions", "Completions"),
            TaskStatsPanelUI.SortMode.TaskTime => ToolkitLocalization.Text("statistics.sort-tasktime", "Task Time"),
            TaskStatsPanelUI.SortMode.Unknown => ToolkitLocalization.Text("statistics.sort-unknown", "Unknown"),
            _ => ToolkitLocalization.Text("statistics.sort-default", "Default")
        };

        private void Select(Group group)
        {
            selected = group; selectSkill(group.skill); scroll.scrollOffset = Vector2.zero; Layout();
        }

        public void Layout()
        {
            bool portrait = ToolkitResponsiveLayout.IsPortrait;
            if (portrait && context.parent != parent) parent.Insert(1, context);
            else if (!portrait && context.parent != toolbar) toolbar.Insert(0, context);
            context.style.flexGrow = portrait ? 0 : 1;
            context.style.marginBottom = portrait ? 6 : 0;
            selectors.style.display = portrait ? DisplayStyle.Flex : DisplayStyle.None;
            var count = portrait ? 1 : Mathf.Clamp(Mathf.FloorToInt((grid.contentRect.width + 12) / 220), 1, 3);
            grid.EnableInClassList("task-preferences-multi", count > 1);
            var signature = count + ":" + string.Join(",", groups.Select(g => g.known.childCount + (g.expanded ? g.unknown.childCount : 0)));
            if (columnLayout != signature)
            {
                columnLayout = signature; grid.Clear(); columns.Clear();
                var heights = new float[count];
                for (var i = 0; i < count; i++)
                {
                    var column = ToolkitGameplay.E(grid, "task-preferences-column");
                    column.style.width = Length.Percent(100f / count); columns.Add(column);
                }
                foreach (var group in groups)
                {
                    var index = Array.IndexOf(heights, heights.Min());
                    columns[index].Add(group.root);
                    heights[index] += 60 + 38 * (group.known.childCount + (group.expanded ? group.unknown.childCount : 0));
                }
            }
            foreach (var group in groups)
            {
                group.root.style.display = !portrait || group == selected ? DisplayStyle.Flex : DisplayStyle.None;
                group.root.style.width = Length.Percent(100);
                group.selector.EnableInClassList("active", group == selected);
            }
        }

        public void Refresh()
        {
            presentation.Refresh(tracker);
            context.text = tracker && tracker.RunInProgress
                ? ToolkitLocalization.Text("statistics.chance-this-run", "Chance in this run") : "";
            context.style.display = string.IsNullOrEmpty(context.text) ? DisplayStyle.None : DisplayStyle.Flex;
            var ordered = presentation.Ordered(sort, tracker);
            foreach (var group in groups)
            {
                if (sort == TaskStatsPanelUI.SortMode.Unknown)
                {
                    group.expanded = true;
                    group.unknownButton.PlaceBehind(group.known);
                    group.unknown.PlaceBehind(group.known);
                }
                else
                {
                    group.known.PlaceBehind(group.unknownButton);
                    group.unknown.PlaceInFront(group.unknownButton);
                }
                var unknownCount = 0; var knownIndex = 0; var unknownIndex = 0;
                foreach (var task in ordered.Where(t => t.associatedSkill == group.skill))
                {
                    var entry = group.entries.First(e => e.task == task);
                    var data = presentation.Describe(task, tracker);
                    var destination = data.toggleVisible ? group.known : group.unknown;
                    var index = data.toggleVisible ? knownIndex++ : unknownIndex++;
                    if (entry.root.parent != destination || destination.IndexOf(entry.root) != index) destination.Insert(index, entry.root);
                    if (!data.toggleVisible) unknownCount++;
                    entry.title.text = data.title; entry.icon.sprite = data.icon;
                    entry.name.SetEnabled(data.toggleVisible);
                    entry.chance.text = data.toggleVisible && presentation.TryChance(task, out var chance)
                        ? (chance * 100).ToString("0.#", LocaleNumbers.Culture) + LocaleNumbers.PercentSuffix : "";
                    entry.chance.style.display = string.IsNullOrEmpty(entry.chance.text) ? DisplayStyle.None : DisplayStyle.Flex;
                    entry.toggle.style.display = data.toggleVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    ToolkitGameplay.SetToggle(entry.check, data.toggle);
                    entry.toggle.tooltip = ToolkitLocalization.Text(data.toggle ? "statistics.task-boost-enabled" : "statistics.task-boost-hint",
                        data.toggle ? "Favoured. Select again to turn off." : "Find this task more often.");
                    entry.root.EnableInClassList("favoured", data.toggleVisible && data.toggle);
                }
                group.unknownButton.style.display = unknownCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                group.unknownButton.text = (group.expanded ? "− " : "+ ") + ToolkitLocalization.Text("statistics.undiscovered-count", "Undiscovered ({0})", unknownCount);
                group.unknown.style.display = group.expanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
            RefreshDetails(); Layout();
        }

        public Button Reveal(TaskData task)
        {
            var group = groups.FirstOrDefault(g => g.skill == task.associatedSkill);
            var entry = group?.entries.FirstOrDefault(e => e.task == task);
            if (entry == null) return null;
            if (selected != group) Select(group);
            scroll.ScrollTo(entry.root);
            return entry.toggle;
        }

        public Button ToggleFor(TaskData task) => groups.SelectMany(g => g.entries).FirstOrDefault(e => e.task == task)?.toggle;

        private void OpenDetails(TaskData task)
        {
            CloseDetails(); detailTask = task;
            detailOverlay = ToolkitGameplay.E(host, "task-details-overlay");
            detailOverlay.name = "task-details";
            var backdrop = ToolkitGameplay.B(detailOverlay, "", CloseDetails); backdrop.AddToClassList("task-details-backdrop");
            var panel = ToolkitGameplay.E(detailOverlay, "task-details-panel"); panel.AddToClassList("surface");
            var heading = ToolkitGameplay.E(panel, "task-details-heading");
            heading.Add(ToolkitControls.Icon(task.taskIcon));
            ToolkitGameplay.L(heading, ToolkitLocalization.Name(task), "task-details-title");
            var close = ToolkitGameplay.B(heading, "×", CloseDetails); close.name = "task-details-close";
            ToolkitLocalization.BindTooltip(close, "common.close", "Close");
            var content = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            ToolkitGameplay.StyleScroll(content); panel.Add(content);
            detailText = ToolkitGameplay.L(content, "", "task-details-text");
            detailToggle = ToolkitGameplay.B(panel, "", () => { TaskWeightService.SetToggle(task, !TaskWeightService.IsToggleEnabled(task)); Refresh(); });
            detailToggle.name = "task-details-favour";
            detailOverlay.focusable = true;
            detailOverlay.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { CloseDetails(); e.StopPropagation(); } });
            detailOverlay.Focus(); RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (!detailTask || detailOverlay == null) return;
            var data = presentation.Describe(detailTask, tracker);
            detailText.text = data.totals + "\n\n" + data.detail;
            detailToggle.text = ToolkitLocalization.Text("statistics.boost", "Favour");
            detailToggle.EnableInClassList("active", data.toggle);
        }
        public void CloseDetails() { detailOverlay?.RemoveFromHierarchy(); detailOverlay = null; detailTask = null; }
    }
}
