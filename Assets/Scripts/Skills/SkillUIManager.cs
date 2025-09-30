using System.Collections;
using System.Collections.Generic;
using System.Linq;
using References.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine.EventSystems;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.EventHandler;

namespace TimelessEchoes.Skills
{
    public class SkillUIManager : MonoBehaviour
    {
        [SerializeField] private SkillController controller;
        [SerializeField] private List<SkillUIReferences> skillSelectors = new();
        [SerializeField] private List<Skill> skills = new();
        [SerializeField] private TMP_Text skillTitle;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text experienceText;
        [SerializeField] private SlicedFilledImage experienceBar;
        [SerializeField] private MilestoneBonusUI bonusUI;

        [Header("Set Bonus Panel")]
        [SerializeField] private TMP_Text primarySetNameText;
        [SerializeField] private TMP_Text primarySetEffectText;
        [SerializeField] private TMP_Text secondarySetNameText;
        [SerializeField] private TMP_Text secondarySetEffectText;
        [SerializeField] private GameObject setBonusPanel;

        [Header("Active Slots Panel")]
        [SerializeField] private TMP_Text activeSlotsText;
        [SerializeField] private TMP_Text activeSlotListText;

        [Header("Totals Panel")]
        [SerializeField] private TMP_Text totalSkillIncreasesText;

        [Header("Milestone Toggle Sprites")]
        [SerializeField] private Sprite activeMilestoneToggleSprite;
        [SerializeField] private Sprite inactiveMilestoneToggleSprite;

        private int selectedIndex = -1;

        private Skill CurrentSkill => selectedIndex >= 0 && selectedIndex < skills.Count ? skills[selectedIndex] : null;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<SkillController>();
            if (skillSelectors.Count == 0)
                skillSelectors.AddRange(GetComponentsInChildren<SkillUIReferences>(true));

            for (int i = 0; i < skillSelectors.Count; i++)
            {
                int index = i;
                var selector = skillSelectors[i];
                if (selector == null) continue;
                if (selector.selectButton != null)
                    selector.selectButton.onClick.AddListener(() => SelectSkill(index));
                selector.PointerEnter += _ =>
                {
                    if (selector.highlightImage != null)
                        selector.highlightImage.enabled = false;
                };
                selector.PointerClick += (_, __) =>
                {
                    if (selector.highlightImage != null)
                        selector.highlightImage.enabled = false;
                };
            }

            if (bonusUI != null && !bonusUI.gameObject.activeSelf)
                bonusUI.gameObject.SetActive(true);

            ApplyToggleSprites();
            SelectSkill(0);
            UpdateSkillSelectorLevels();
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.OnExperienceGained += OnExperienceGained;
                controller.OnLevelUp += OnLevelUp;
                controller.OnMilestoneDataChanged += OnMilestoneDataChanged;
                controller.OnActiveSlotsChanged += OnActiveSlotsChanged;
            }
            ShowLevelTextChanged += OnShowLevelTextChanged;
            OnLoadData += OnLoadDataHandler;

            OnShowLevelTextChanged();
            if (selectedIndex >= 0)
                UpdateSelectedSkillUI();
            else
                DeselectSkill();
            UpdateSkillSelectorLevels();
            ApplyToggleSprites();
            UpdateAllPanels();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnExperienceGained -= OnExperienceGained;
                controller.OnLevelUp -= OnLevelUp;
                controller.OnMilestoneDataChanged -= OnMilestoneDataChanged;
                controller.OnActiveSlotsChanged -= OnActiveSlotsChanged;
            }
            ShowLevelTextChanged -= OnShowLevelTextChanged;
            OnLoadData -= OnLoadDataHandler;
        }

        private void OnExperienceGained(Skill skill, float current, float required)
        {
            if (skill == CurrentSkill)
                UpdateSelectedSkillUI();
        }

        private void OnLevelUp(Skill skill, int level)
        {
            int index = skills.IndexOf(skill);
            if (index < 0 || index >= skillSelectors.Count)
                return;

            var selector = skillSelectors[index];
            if (selector != null && selector.levelText != null)
                selector.levelText.text = ShowLevelText ? $"Lvl: {level}" : string.Empty;

            if (selector != null && selector.highlightImage != null && selectedIndex != index)
                selector.highlightImage.enabled = true;

            UpdateSkillSelectorLevels();
            ApplyToggleSprites();
            UpdateAllPanels();
        }

        private void OnMilestoneDataChanged()
        {
            UpdateAllPanels(excludeBonusList: true);
        }

        private void OnActiveSlotsChanged(int used, int total)
        {
            UpdateActiveSlotPanel();
        }

        private void SelectSkill(int index)
        {
            if (skillSelectors.Count == 0)
                return;

            selectedIndex = Mathf.Clamp(index, 0, skillSelectors.Count - 1);
            for (int i = 0; i < skillSelectors.Count; i++)
            {
                var selector = skillSelectors[i];
                if (selector == null)
                    continue;

                if (selector.selectionImage != null)
                    selector.selectionImage.enabled = i == selectedIndex;
                if (selector.highlightImage != null && i == selectedIndex)
                    selector.highlightImage.enabled = false;
            }

            UpdateSelectedSkillUI();
            ApplyToggleSprites();
            UpdateAllPanels();
        }

        private void UpdateSelectedSkillUI()
        {
            var skill = CurrentSkill;
            if (skill == null || controller == null)
                return;

            var prog = controller.GetProgress(skill);
            int level = prog != null ? prog.Level : 1;
            float current = prog != null ? prog.CurrentXP : 0f;
            float required = skill.xpForFirstLevel * Mathf.Pow(level, skill.xpLevelMultiplier);

            if (skillTitle != null)
                skillTitle.text = skill.skillName;
            if (levelText != null)
                levelText.text = $"{skill.skillName} | Lvl {level}";
            if (experienceText != null)
                experienceText.text = $"{current:N0} / {required:N0}";
            if (experienceBar != null)
                experienceBar.fillAmount = required > 0f ? Mathf.Clamp01(current / required) : 0f;
        }

        private void UpdateSkillSelectorLevels()
        {
            for (int i = 0; i < skillSelectors.Count && i < skills.Count; i++)
            {
                var selector = skillSelectors[i];
                var skill = skills[i];
                if (selector == null || selector.levelText == null)
                    continue;

                if (ShowLevelText)
                {
                    int level = 1;
                    if (controller != null)
                    {
                        var prog = controller.GetProgress(skill);
                        if (prog != null)
                            level = prog.Level;
                    }
                    selector.levelText.text = $"Lvl: {level}";
                }
                else
                {
                    selector.levelText.text = string.Empty;
                }
            }
        }

        private void ApplyToggleSprites()
        {
            if (bonusUI != null)
                bonusUI.SetToggleSprites(activeMilestoneToggleSprite, inactiveMilestoneToggleSprite);
        }

        private void UpdateAllPanels(bool excludeBonusList = false)
        {
            if (!excludeBonusList && bonusUI != null && bonusUI.gameObject.activeSelf)
                bonusUI.PopulateMilestones(CurrentSkill);

            UpdateActiveSlotPanel();
            UpdateSetPanel();
            UpdateTotalsPanel();
        }

        private void UpdateActiveSlotPanel()
        {
            if (controller == null)
            {
                if (activeSlotsText != null)
                    activeSlotsText.text = string.Empty;
                if (activeSlotListText != null)
                    activeSlotListText.text = string.Empty;
                return;
            }

            if (activeSlotsText != null)
            {
                activeSlotsText.text = controller.TotalActiveSlots > 0
                    ? string.Format("Active Slots: {0}({1})", controller.ActiveSlotsUsed, controller.TotalActiveSlots)
                    : string.Empty;
            }

            if (activeSlotListText != null)
            {
                var lines = new List<string>();
                foreach (var info in controller.EnumerateActiveMilestones())
                {
                    if (info.Definition == null)
                        continue;

                    string label = info.Definition.DisplayName;
                    if (info.Skill != null && info.Skill != CurrentSkill)
                        label = $"{label} ({info.Skill.skillName})";

                    lines.Add(label);
                }

                activeSlotListText.text = lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
            }
        }

        private void UpdateSetPanel()
        {
            if (primarySetNameText != null) primarySetNameText.text = string.Empty;
            if (primarySetEffectText != null) primarySetEffectText.text = string.Empty;
            if (secondarySetNameText != null) secondarySetNameText.text = string.Empty;
            if (secondarySetEffectText != null) secondarySetEffectText.text = string.Empty;

            bool hasActiveSet = false;

            if (controller != null)
            {
                var summaries = controller.EnumerateActiveSets()
                    .Where(s => s.Definition != null)
                    .OrderByDescending(s => s.ActiveCount)
                    .ToList();

                if (summaries.Count > 0)
                {
                    hasActiveSet = true;

                    var primary = summaries[0];
                    if (primary.Definition != null)
                    {
                        if (primarySetNameText != null)
                            primarySetNameText.text = primary.Definition.DisplayName;

                        if (primarySetEffectText != null)
                        {
                            var effects = new List<string>();
                            if (primary.ThreePieceActive && !string.IsNullOrEmpty(primary.Definition.ThreePieceDescription))
                                effects.Add(primary.Definition.ThreePieceDescription);
                            if (primary.SixPieceActive && !string.IsNullOrEmpty(primary.Definition.SixPieceDescription))
                                effects.Add(primary.Definition.SixPieceDescription);
                            primarySetEffectText.text = effects.Count > 0 ? string.Join("\n", effects) : string.Empty;
                        }
                    }

                    var secondary = summaries.Skip(1).FirstOrDefault(s => s.Definition != null && s.Definition != primary.Definition && s.ThreePieceActive);

                    if (secondary.Definition != null)
                    {
                        if (secondarySetNameText != null)
                            secondarySetNameText.text = secondary.Definition.DisplayName;

                        if (secondarySetEffectText != null)
                        {
                            var effects = new List<string>();
                            if (secondary.ThreePieceActive && !string.IsNullOrEmpty(secondary.Definition.ThreePieceDescription))
                                effects.Add(secondary.Definition.ThreePieceDescription);
                            if (secondary.SixPieceActive && !string.IsNullOrEmpty(secondary.Definition.SixPieceDescription))
                                effects.Add(secondary.Definition.SixPieceDescription);
                            secondarySetEffectText.text = effects.Count > 0 ? string.Join("\n", effects) : string.Empty;
                        }
                    }
                }
            }

            if (setBonusPanel != null)
                setBonusPanel.SetActive(hasActiveSet);
        }


        private void UpdateTotalsPanel()
        {
            if (totalSkillIncreasesText == null)
                return;

            if (controller == null)
            {
                totalSkillIncreasesText.text = string.Empty;
                return;
            }

            var aggregator = controller.Aggregator;
            if (aggregator == null)
            {
                totalSkillIncreasesText.text = string.Empty;
                return;
            }

            var totalContributions = new Dictionary<BaseStat, StatContribution>();
            foreach (var kvp in aggregator.EnumerateTotalFlatBonuses())
                AccumulateStatContribution(totalContributions, kvp.Key, kvp.Value, 0f);
            foreach (var kvp in aggregator.EnumerateTotalPercentBonuses())
                AccumulateStatContribution(totalContributions, kvp.Key, 0f, kvp.Value);

            var statLines = BuildStatLines(totalContributions);

            float globalResourceBonus = aggregator.GetGlobalResourceBonus();

            var resourceLines = new List<string>();
            if (globalResourceBonus > 0f)
                resourceLines.Add($"{globalResourceBonus * 100f:0.#}% Bonus Resources (All Tasks)");

            var perSkillResourceEntries = new List<(string SkillName, string Line)>();
            var procEntries = new List<(int Order, string SkillName, string Line)>();
            var echoEntries = new List<(string SkillName, string Line)>();

            foreach (var pair in aggregator.EnumerateSkillSummaries())
            {
                var summary = pair.Value;
                if (summary == null)
                    continue;

                string skillName = ResolveSkillName(pair.Key);

                if (summary.InstantTaskChance > 0f)
                    procEntries.Add((0, skillName, $"{summary.InstantTaskChance * 100f:0.#}% Chance to Instantly Complete Tasks"));

                if (summary.InstantKillChance > 0f)
                    procEntries.Add((1, skillName, $"{summary.InstantKillChance * 100f:0.#}% Chance to Instantly Kill"));

                if (summary.DoubleResourceChance > 0f)
                    procEntries.Add((2, skillName, $"{summary.DoubleResourceChance * 100f:0.#}% Chance to Double Resources"));

                if (summary.DoubleXpChance > 0f)
                    procEntries.Add((3, skillName, $"{summary.DoubleXpChance * 100f:0.#}% Chance to Double XP"));

                if (summary.ResourceBonusPercent > 0f)
                {
                    float totalPercent = summary.ResourceBonusPercent + globalResourceBonus;
                    string skillLabel = !string.IsNullOrWhiteSpace(skillName) ? skillName : "Associated Skill";
                    perSkillResourceEntries.Add((skillLabel, $"{totalPercent * 100f:0.#}% Bonus Resources ({skillLabel})"));
                }

                foreach (var entry in summary.SpawnEchoes)
                {
                    if (entry.Chance <= 0f)
                        continue;

                    string label = BuildEchoSpawnLine(entry, pair.Key);
                    if (!string.IsNullOrEmpty(label))
                        echoEntries.Add((skillName, label));
                }
            }

            perSkillResourceEntries.Sort((left, right) => string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));
            foreach (var entry in perSkillResourceEntries)
                resourceLines.Add(entry.Line);

            procEntries.Sort((left, right) =>
            {
                int orderCompare = left.Order.CompareTo(right.Order);
                if (orderCompare != 0)
                    return orderCompare;

                return string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
            });

            var procLines = procEntries.Select(entry => entry.Line).ToList();

            echoEntries.Sort((left, right) =>
            {
                int skillCompare = string.Compare(left.SkillName ?? string.Empty, right.SkillName ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
                if (skillCompare != 0)
                    return skillCompare;

                return string.Compare(left.Line, right.Line, System.StringComparison.OrdinalIgnoreCase);
            });

            var echoLines = echoEntries.Select(entry => entry.Line).ToList();

            var allLines = new List<string>();
            if (statLines.Count > 0)
                allLines.AddRange(statLines);
            if (resourceLines.Count > 0)
                allLines.AddRange(resourceLines);
            if (procLines.Count > 0)
                allLines.AddRange(procLines);
            if (echoLines.Count > 0)
                allLines.AddRange(echoLines);

            totalSkillIncreasesText.text = allLines.Count > 0 ? string.Join(System.Environment.NewLine, allLines) : string.Empty;
        }
        private List<string> BuildSkillSectionLines(Skill skill, SkillMilestoneSummary summary)
        {
            var lines = new List<string>();
            if (summary == null)
                return lines;

            var contributions = new Dictionary<BaseStat, StatContribution>();
            if (summary.FlatStatBonuses != null && summary.FlatStatBonuses.Count > 0)
            {
                foreach (var kvp in summary.FlatStatBonuses)
                    AccumulateStatContribution(contributions, kvp.Key, kvp.Value, 0f);
            }

            if (summary.PercentStatBonuses != null && summary.PercentStatBonuses.Count > 0)
            {
                foreach (var kvp in summary.PercentStatBonuses)
                    AccumulateStatContribution(contributions, kvp.Key, 0f, kvp.Value);
            }

            var statLines = BuildStatLines(contributions);
            if (statLines.Count > 0)
                lines.AddRange(statLines);

            if (summary.ResourceBonusPercent > 0f)
            {
                float totalPercent = summary.ResourceBonusPercent;
                if (controller != null && controller.Aggregator != null)
                    totalPercent += controller.Aggregator.GetGlobalResourceBonus();
                lines.Add($"{totalPercent * 100f:0.#}% Bonus Resources");
            }

            foreach (var entry in summary.SpawnEchoes)
            {
                if (entry.Chance <= 0f)
                    continue;

                string label = BuildEchoSpawnLine(entry, skill);
                if (!string.IsNullOrEmpty(label))
                    lines.Add(label);
            }

            return lines;
        }
        private static List<string> BuildStatLines(Dictionary<BaseStat, StatContribution> source)
        {
            var lines = new List<string>();
            if (source == null || source.Count == 0)
                return lines;

            foreach (var pair in source.OrderBy(p => p.Key != null ? p.Key.name : string.Empty))
            {
                string formatted = FormatStatLine(pair.Key, pair.Value);
                if (!string.IsNullOrEmpty(formatted))
                    lines.Add(formatted);
            }

            return lines;
        }

        private static string ResolveStatDisplayName(BaseStat stat)
        {
            if (stat == null)
                return "Stat";

            var def = stat.AssociatedStat;
            if (def != null && !string.IsNullOrWhiteSpace(def.displayName))
                return def.displayName;

            return !string.IsNullOrWhiteSpace(stat.name) ? stat.name : "Stat";
        }

        private static string GetIconTagFor(BaseStat stat)
        {
            if (stat == null)
                return string.Empty;

            var def = stat.AssociatedStat;
            if (def != null)
            {
                var iconFromMapping = StatIconLookup.GetIconTag(def.heroMapping);
                if (!string.IsNullOrEmpty(iconFromMapping))
                    return iconFromMapping;

                if (!string.IsNullOrWhiteSpace(def.displayName))
                {
                    var iconFromDisplay = StatIconLookup.GetIconTag(def.displayName);
                    if (!string.IsNullOrEmpty(iconFromDisplay))
                        return iconFromDisplay;
                }
            }

            if (!string.IsNullOrWhiteSpace(stat.name))
            {
                var iconFromName = StatIconLookup.GetIconTag(stat.name);
                if (!string.IsNullOrEmpty(iconFromName))
                    return iconFromName;
            }

            return string.Empty;
        }

        private static string ResolveSkillName(Skill skill)
        {
            if (skill == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(skill.skillName))
                return skill.skillName;

            return !string.IsNullOrWhiteSpace(skill.name) ? skill.name : string.Empty;
        }

        private static string BuildEchoSpawnLine(SpawnEchoEntry entry, Skill sourceSkill)
        {
            if (entry.Chance <= 0f)
                return string.Empty;

            int count = entry.Count > 0 ? entry.Count : 1;
            string target = GetEchoSpawnTargetLabel(entry, sourceSkill);
            string noun = count == 1 ? "Echo" : "Echoes";
            string descriptor = string.IsNullOrEmpty(target) ? noun : $"{target} {noun}";
            string subject = count > 1 ? $"{count} {descriptor}" : FormatSingleEchoDescriptor(descriptor);

            string triggerSkill = GetEchoSpawnTriggerSkillLabel(entry, sourceSkill);
            string triggerSuffix = string.IsNullOrEmpty(triggerSkill) ? string.Empty : $" when {triggerSkill}";

            return $"{entry.Chance * 100f:0.#}% Chance to spawn {subject}{triggerSuffix}";
        }

        private static string GetEchoSpawnTriggerSkillLabel(SpawnEchoEntry entry, Skill sourceSkill)
        {
            var config = entry.Config;

            if (config != null && config.capableSkills != null && config.capableSkills.Count > 0)
            {
                var names = new List<string>();
                foreach (var skill in config.capableSkills)
                {
                    var name = NormalizeTriggerLabel(ResolveSkillName(skill));
                    if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                        names.Add(name);
                }

                if (names.Count == 1)
                    return names[0];

                if (names.Count > 1)
                    return string.Join("/", names);
            }

            var fallback = NormalizeTriggerLabel(ResolveSkillName(sourceSkill));
            return !string.IsNullOrEmpty(fallback) ? fallback : string.Empty;
        }

        private static string NormalizeTriggerLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            if (string.Equals(label, "Combat", System.StringComparison.OrdinalIgnoreCase))
                return "Killing";

            return label;
        }

        private static string FormatSingleEchoDescriptor(string descriptor)
        {
            if (string.IsNullOrWhiteSpace(descriptor))
                return "an Echo";

            string trimmed = descriptor.Trim();
            if (trimmed.Length == 0)
                return "an Echo";

            char first = char.ToLowerInvariant(trimmed[0]);
            bool useAn = first == 'a' || first == 'e' || first == 'i' || first == 'o' || first == 'u';
            string article = useAn ? "an" : "a";
            return $"{article} {trimmed}";
        }

        private static string GetEchoSpawnTargetLabel(SpawnEchoEntry entry, Skill sourceSkill)
        {
            var config = entry.Config;

            if (config != null && config.capableSkills != null && config.capableSkills.Count > 0)
            {
                var names = new List<string>();
                foreach (var skill in config.capableSkills)
                {
                    var name = ResolveSkillName(skill);
                    if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                        names.Add(name);
                }

                if (names.Count == 1)
                    return names[0];
                if (names.Count > 1)
                    return "Selective";
            }

            if (entry.UseAssociatedSkillFallback)
            {
                var fallback = ResolveSkillName(sourceSkill);
                if (!string.IsNullOrEmpty(fallback))
                    return fallback;
            }

            if (config != null)
            {
                switch (config.echoType)
                {
                    case TimelessEchoes.Hero.EchoType.Combat:
                        return "Combat";
                    case TimelessEchoes.Hero.EchoType.TaskOnly:
                        return "Task";
                    case TimelessEchoes.Hero.EchoType.All:
                        return string.Empty;
                    case TimelessEchoes.Hero.EchoType.Selective:
                        return "Selective";
                }
            }

            var defaultName = ResolveSkillName(sourceSkill);
            return !string.IsNullOrEmpty(defaultName) ? defaultName : string.Empty;
        }

        private static string FormatStatLine(BaseStat stat, StatContribution contribution)
        {
            bool hasFlat = !Mathf.Approximately(contribution.Flat, 0f);
            bool hasPercent = !Mathf.Approximately(contribution.Percent, 0f);

            if (!hasFlat && !hasPercent)
                return string.Empty;

            var parts = new List<string>();
            if (hasFlat)
                parts.Add($"{(contribution.Flat >= 0f ? "+" : string.Empty)}{contribution.Flat:0.##}");
            if (hasPercent)
                parts.Add($"{(contribution.Percent >= 0f ? "+" : string.Empty)}{(contribution.Percent * 100f):0.##}%");

            string iconTag = GetIconTagFor(stat);
            if (!string.IsNullOrEmpty(iconTag))
                return $"{iconTag} {string.Join(" ", parts)}";

            string label = ResolveStatDisplayName(stat);
            return $"{label}: {string.Join(" ", parts)}";
        }

        private static void AccumulateStatContribution(Dictionary<BaseStat, StatContribution> map, BaseStat stat, float flatDelta, float percentDelta)
        {
            if (!map.TryGetValue(stat, out var contribution))
                contribution = default;

            contribution.Flat += flatDelta;
            contribution.Percent += percentDelta;
            map[stat] = contribution;
        }

        private struct StatContribution
        {
            public float Flat;
            public float Percent;
        }

        private void OnShowLevelTextChanged()
        {
            UpdateSkillSelectorLevels();
            if (selectedIndex >= 0)
                UpdateSelectedSkillUI();
        }

        private void OnLoadDataHandler()
        {
            StartCoroutine(DelayedUpdate());
        }

        private IEnumerator DelayedUpdate()
        {
            yield return null;
            OnShowLevelTextChanged();
            ApplyToggleSprites();
            UpdateAllPanels();
        }

        private void DeselectSkill()
        {
            selectedIndex = -1;
            foreach (var selector in skillSelectors)
            {
                if (selector != null && selector.selectionImage != null)
                    selector.selectionImage.enabled = false;
            }
        }
    }
}



