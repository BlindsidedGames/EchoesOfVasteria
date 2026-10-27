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
            public VisualElement root, known;
            public Label unknownCount;
            public readonly List<Entry> entries = new();
        }
        private readonly TaskStatisticsPresentation presentation;
        private readonly GameplayStatTracker tracker;
        private readonly VisualElement host, grid;
        private readonly ScrollView scroll;
        private readonly List<Group> groups = new();
        private List<Group> orderedGroups = new();
        private readonly List<VisualElement> columns = new();
        private string columnLayout;
        private TaskData detailTask;
        private VisualElement detailOverlay;
        private Label detailText;
        private Button detailToggle;

        public ToolkitTaskPreferences(VisualElement parent, VisualElement modalHost, ToolkitTheme theme, Sprite inset,
            TaskStatisticsPresentation source, GameplayStatTracker stats)
        {
            host = modalHost; presentation = source; tracker = stats;
            scroll = ToolkitControls.RecessedScroll(parent, "task-statistics", theme, inset);
            grid = ToolkitGameplay.E(scroll.contentContainer, "task-preferences-grid");
            foreach (var grouping in source.Ordered(TaskStatsPanelUI.SortMode.Default, stats)
                .Where(t => t.associatedSkill).GroupBy(t => t.associatedSkill))
            {
                var group = new Group { skill = grouping.Key };
                groups.Add(group);
                group.root = ToolkitGameplay.E(grid, "task-preferences-group");
                group.root.name = "task-group-" + group.skill.name;
                var heading = ToolkitGameplay.E(group.root, "task-preferences-heading");
                var image = ToolkitControls.Icon(group.skill.skillIcon); heading.Add(image);
                ToolkitGameplay.L(heading, ToolkitLocalization.Name(group.skill, "skill"), "task-preferences-skill-name");
                group.known = ToolkitGameplay.E(group.root, "task-preferences-known");
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
                group.unknownCount = ToolkitGameplay.L(group.root, "", "task-preferences-unknown-count");
            }
            Refresh();
        }

        public void Layout()
        {
            bool portrait = ToolkitResponsiveLayout.IsPortrait;
            var count = portrait ? 1 : Mathf.Clamp(Mathf.FloorToInt((grid.contentRect.width + 12) / 220), 1, 3);
            grid.EnableInClassList("task-preferences-multi", count > 1);
            var signature = count + ":" + string.Join(",", orderedGroups.Select(g => g.skill.name+":"+g.known.childCount));
            if (columnLayout != signature)
            {
                columnLayout = signature; grid.Clear(); columns.Clear();
                var heights = new float[count];
                for (var i = 0; i < count; i++)
                {
                    var column = ToolkitGameplay.E(grid, "task-preferences-column");
                    column.style.width = Length.Percent(100f / count); columns.Add(column);
                }
                foreach (var group in orderedGroups)
                {
                    var index = Array.IndexOf(heights, heights.Min());
                    columns[index].Add(group.root);
                    heights[index] += 60 + 38 * (group.known.childCount);
                }
            }
            foreach (var group in groups)
            {
                group.root.style.width = Length.Percent(100);
            }
        }

        public void Refresh()
        {
            presentation.Refresh(tracker);
            orderedGroups=groups.OrderByDescending(group=>presentation.MapWeight(group.skill)).ToList();
            var ordered = presentation.Ordered(TaskStatsPanelUI.SortMode.Default, tracker);
            foreach (var group in groups)
            {
                var unknownCount = 0;
                var knownIndex = 0;
                foreach (var task in ordered.Where(t => t.associatedSkill == group.skill))
                {
                    var data = presentation.Describe(task, tracker);
                    var entry = group.entries.First(e => e.task == task);
                    if (!data.toggleVisible)
                    {
                        unknownCount++;
                        entry.root.RemoveFromHierarchy();
                        continue;
                    }
                    if (entry.root.parent != group.known || group.known.IndexOf(entry.root) != knownIndex)
                        group.known.Insert(knownIndex, entry.root);
                    knownIndex++;
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
                group.unknownCount.style.display = unknownCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                group.unknownCount.text = ToolkitLocalization.Text("statistics.undiscovered-count", "Undiscovered ({0})", unknownCount);
            }
            RefreshDetails(); Layout();
        }

        public Button Reveal(TaskData task)
        {
            var group = groups.FirstOrDefault(g => g.skill == task.associatedSkill);
            var entry = group?.entries.FirstOrDefault(e => e.task == task);
            if (entry == null) return null;
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
