using Blindsided;
using Blindsided.Utilities;
using System.Linq;
using TimelessEchoes.Characters;
using TimelessEchoes.Gear;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitCharacterCreator
    {
        private void CreatePanel(string name)
        {
            panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 1000;
            panelObject = new GameObject("Character creation panel", typeof(UIDocument));
            var document = panelObject.GetComponent<UIDocument>(); document.panelSettings = panel;
            root = new VisualElement { name = name }; root.AddToClassList("character-overlay");
            theme.Apply(root); Apply(root, theme); document.rootVisualElement.Add(root);
            BlocksWorldInput = true;
            sheet = E(root, "surface character-sheet");
        }

        private bool ShowMemorial()
        {
            if (!theme || !runtimeTheme || !textSettings || !CharacterCatalog.Load()) return false;
            owner = CharacterCreation.Current; memorial = true;
            CreatePanel("hardcore-memorial");
            sheet.AddToClassList("memorial-sheet");
            var heading = L(sheet, "", "heading"); heading.style.whiteSpace = WhiteSpace.Normal;
            ToolkitLocalization.Bind(heading, "hardcore.fallen-name", "{0} has fallen", owner.Name);
            var scroll = ToolkitControls.RecessedScroll(sheet, "hardcore-memorial-scroll", theme);
            scroll.style.flexGrow = 1; scroll.style.flexBasis = 0; scroll.style.minWidth = 0;
            var body = scroll.contentContainer;
            var data = Oracle.oracle.saveData;
            var death = data.HardcoreDeath;
            var header = E(body, "row memorial-header");
            var previewBox = E(header, "character-preview"); preview = new CharacterImage(previewBox, 1.5f);
            var details = E(header, "memorial-details");
            L(details, T("hardcore.mode", "Hardcore"), "strong");
            var place = L(details, T("hardcore.final-distance", "{0} · {1} m", ToolkitLocalization.Text("map." + death.Map, death.Map), CalcUtils.FormatNumber(death.Distance)), "strong");
            place.style.whiteSpace = WhiteSpace.Normal;
            L(details, T("hardcore.survived", "Time survived: {0}", CalcUtils.FormatTime(data.PlayTime, shortForm: true)), "small");
            var totals = data.General;
            var grid = E(body, "memorial-totals");
            MemorialStat(grid, "kills", runArt ? runArt.killsIcon : null, "Enemies killed", totals.TotalKills);
            MemorialStat(grid, "loot", runArt ? runArt.ResourceIcon(totals.TotalResourcesGathered) : null, "Loot gathered", totals.TotalResourcesGathered);
            MemorialStat(grid, "tasks", taskIcon, "Tasks completed", totals.TasksCompleted);
            MemorialStat(grid, "quests", navigationArt ? navigationArt.questAttention : null, "Quests completed", QuestManager.Instance ? QuestManager.Instance.CompletedQuestCount : 0);
            MemorialStat(grid, "distance", runArt ? runArt.stepsIcon : null, "Furthest distance", totals.LongestRun, " m");
            MemorialStat(grid, "reaps", runArt ? runArt.reaperIcon : null, "Times reaped", totals.TimesReaped);
            var equipment = EquipmentController.Instance;
            if (equipment)
            {
                var gear = E(body, "memorial-equipment");
                foreach (var slot in equipment.Slots)
                {
                    var item = equipment.GetEquipped(slot);
                    if (item == null) continue;
                    var entry = E(gear, "memorial-gear");
                    var row = E(entry, "row memorial-gear-heading");
                    var icon = Icon(row, equipmentArt ? equipmentArt.GearSprite(item, slot) : QuestEquipmentReward.GetIcon(item), 20);
                    icon.name = "hardcore-gear-" + slot;
                    var label = L(row, item.GetName(), "strong"); label.style.whiteSpace = WhiteSpace.Normal; label.style.flexShrink = 1;
                    foreach (var affix in item.affixes.Where(a => a?.stat).OrderBy(a => StatSortOrder.GetIndex(a.stat.heroMapping)))
                    {
                        var statRow = E(entry, "row memorial-gear-affix");
                        statRow.tooltip = affix.stat.GetName();
                        StatIconLookup.TryGetIcon(affix.stat.heroMapping, out var statIcon);
                        Icon(statRow, statIcon ? statIcon : affix.stat.icon, 14);
                        L(statRow, CalcUtils.FormatNumber(affix.value) + (affix.stat.isPercent ? LocaleNumbers.PercentSuffix : ""), "small");
                    }
                }
            }
            var footer = E(sheet, "row character-footer");
            var stats = B(footer, "", () => TownWindowManager.Instance?.OpenWindow(TownWindowManager.Window.Stats)); stats.name = "hardcore-statistics";
            ToolkitLocalization.Bind(stats, "hardcore.statistics", "Statistics");
            var files = B(footer, "", () =>
            {
                TownWindowManager.Instance?.OpenWindow(TownWindowManager.Window.Options);
                FindAnyObjectByType<ToolkitOptionsScreen>()?.ShowSaveFiles();
            }); files.name = "hardcore-save-files";
            ToolkitLocalization.Bind(files, "hardcore.save-files", "Save Files");
            ToolkitLocalization.Changed += LocaleChanged;
            return true;
        }

        private static void MemorialStat(VisualElement grid, string id, Sprite sprite, string english, double value, string suffix = "")
        {
            var cell = E(grid, "memorial-stat"); cell.name = "hardcore-total-" + id;
            L(cell, T("hardcore.total-" + id, english), "small memorial-stat-label");
            var row = E(cell, "row memorial-stat-value");
            Icon(row, sprite, 24);
            L(row, CalcUtils.FormatNumber(value, true) + suffix, "strong").name = "hardcore-value-" + id;
        }
    }
}
