using System.Collections.Generic;
using References.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blindsided.Utilities;
using UnityEngine.EventSystems;

namespace TimelessEchoes.Skills
{
    public class SkillUIManager : MonoBehaviour
    {
        [SerializeField] private SkillController controller;
        [SerializeField] private List<SkillUIReferences> skillSelectors = new();
        [SerializeField] private List<Skill> skills = new();
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TMP_Text skillTitle;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text experienceText;
        [SerializeField] private SlicedFilledImage experienceBar;
        [SerializeField] private Button bonusesButton;
        [SerializeField] private MilestoneBonusUI bonusUI;

        private int selectedIndex = -1;

        private Skill CurrentSkill => selectedIndex >= 0 && selectedIndex < skills.Count ? skills[selectedIndex] : null;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<SkillController>();
            if (popupPanel == null)
                popupPanel = gameObject;
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

            if (bonusesButton != null)
                bonusesButton.onClick.AddListener(OpenBonuses);

            if (popupPanel != null)
                popupPanel.SetActive(false);
            DeselectSkill();
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.OnExperienceGained += OnExperienceGained;
                controller.OnLevelUp += OnLevelUp;
            }
            if (selectedIndex < 0)
            {
                DeselectSkill();
                if (popupPanel != null)
                    popupPanel.SetActive(false);
            }
            else
            {
                UpdateSelectedSkillUI();
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnExperienceGained -= OnExperienceGained;
                controller.OnLevelUp -= OnLevelUp;
            }
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
                selector.levelText.text = $"Lvl {level}";

            bool popupShowing = popupPanel != null && popupPanel.activeSelf && selectedIndex == index;
            if (!popupShowing && selector != null && selector.highlightImage != null)
                selector.highlightImage.enabled = true;
        }

        private void SelectSkill(int index)
        {
            selectedIndex = Mathf.Clamp(index, 0, skillSelectors.Count - 1);
            for (int i = 0; i < skillSelectors.Count; i++)
                if (skillSelectors[i] != null)
                {
                    if (skillSelectors[i].selectionImage != null)
                        skillSelectors[i].selectionImage.enabled = i == selectedIndex;
                    if (skillSelectors[i].highlightImage != null && i == selectedIndex)
                        skillSelectors[i].highlightImage.enabled = false;
                }

            if (popupPanel != null && selectedIndex >= 0 && selectedIndex < skillSelectors.Count)
            {
                var pos = popupPanel.transform.position;
                var target = skillSelectors[selectedIndex].transform.position;
                popupPanel.transform.position = new Vector3(pos.x, target.y, pos.z);
                popupPanel.SetActive(true);
            }
            UpdateSelectedSkillUI();
        }

        private void OpenBonuses()
        {
            if (bonusUI != null && CurrentSkill != null)
            {
                bonusUI.PopulateMilestones(CurrentSkill);
                bonusUI.gameObject.SetActive(true);
            }
        }

        private void UpdateSelectedSkillUI()
        {
            var skill = CurrentSkill;
            if (skill == null || controller == null)
                return;

            var prog = controller.GetProgress(skill);
            int lvl = prog != null ? prog.Level : 1;
            float current = prog != null ? prog.CurrentXP : 0f;
            float needed = skill.xpForFirstLevel * Mathf.Pow(lvl, skill.xpLevelMultiplier);

            if (skillTitle != null)
                skillTitle.text = skill.skillName;
            if (levelText != null)
                levelText.text = $"Lvl {lvl}";
            if (experienceText != null)
                experienceText.text = $"{current:0.#} / {needed:0.#}";
            if (experienceBar != null)
                experienceBar.fillAmount = needed > 0 ? Mathf.Clamp01(current / needed) : 0f;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (popupPanel != null && popupPanel.activeSelf)
                    popupPanel.SetActive(false);
                DeselectSkill();
            }
        }

        private void DeselectSkill()
        {
            selectedIndex = -1;
            foreach (var selector in skillSelectors)
                if (selector != null && selector.selectionImage != null)
                    selector.selectionImage.enabled = false;
        }
    }
}
