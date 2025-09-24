using System.Collections;
using System.Collections.Generic;
using System.Linq;
using References.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.Utilities;
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
                activeSlotsText.text = $"Active Slots: {controller.ActiveSlotsUsed}({controller.TotalActiveSlots})";

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

            if (controller == null)
                return;

            var summaries = controller.EnumerateActiveSets()
                .Where(s => s.Definition != null)
                .OrderByDescending(s => s.ActiveCount)
                .ToList();

            if (summaries.Count == 0)
                return;

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

        private void UpdateTotalsPanel()
        {
            if (totalSkillIncreasesText == null)
                return;

            if (controller == null || CurrentSkill == null)
            {
                totalSkillIncreasesText.text = string.Empty;
                return;
            }

            var lines = new List<string>();

            float instantTaskChance = controller.Aggregator.GetProcChance(CurrentSkill, MilestoneProcType.InstantTask) * 100f;
            if (instantTaskChance > 0f)
                lines.Add($"{instantTaskChance:0.#}% Chance to Instantly Complete Tasks");

            float doubleResourceChance = controller.Aggregator.GetProcChance(CurrentSkill, MilestoneProcType.DoubleResources) * 100f;
            if (doubleResourceChance > 0f)
                lines.Add($"{doubleResourceChance:0.#}% Chance to Double Resources");

            float doubleXpChance = controller.Aggregator.GetProcChance(CurrentSkill, MilestoneProcType.DoubleXP) * 100f;
            if (doubleXpChance > 0f)
                lines.Add($"{doubleXpChance:0.#}% Chance to Double XP");

            var spawnEntries = controller.GetSpawnEntries(CurrentSkill);
            if (spawnEntries != null && spawnEntries.Count > 0)
            {
                float spawnChance = 0f;
                int maxCount = 0;
                bool mixedCounts = false;
                int? lastCount = null;

                foreach (var entry in spawnEntries)
                {
                    spawnChance += entry.Chance;
                    if (entry.Count > maxCount)
                        maxCount = entry.Count;
                    if (lastCount.HasValue && lastCount.Value != entry.Count)
                        mixedCounts = true;
                    lastCount = entry.Count;
                }

                if (spawnChance > 0f)
                {
                    string label = $"{spawnChance * 100f:0.#}% Echo Spawn Chance";
                    if (maxCount > 1)
                    {
                        label += mixedCounts
                            ? $" (spawns up to {maxCount} Echoes)"
                            : $" (spawns {maxCount} {(maxCount == 1 ? "Echo" : "Echoes")})";
                    }
                    lines.Add(label);
                }
            }

            totalSkillIncreasesText.text = lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
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

