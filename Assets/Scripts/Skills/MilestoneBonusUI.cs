using TMPro;
using UnityEngine;
using UnityEngine.UI;
using References.UI;
using TimelessEchoes;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Skills
{
    public class MilestoneBonusUI : MonoBehaviour
    {
        [SerializeField] private GameObject window;
        [SerializeField] private Transform entryParent;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private SkillController controller;

        public void SetEntryParent(Transform parent)
        {
            entryParent = parent;
        }

        public void OpenWindow()
        {
            if (window == null)
                window = gameObject;
            window.SetActive(true);
        }

        public void CloseWindow()
        {
            if (window == null)
                window = gameObject;
            window.SetActive(false);
        }

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<SkillController>();
            if (window == null)
                window = gameObject;
        }

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.HeroDied += CloseWindow;
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.HeroDied -= CloseWindow;
        }

        public void PopulateMilestones(Skill skill)
        {
            if (skill == null || entryPrefab == null)
                return;

            if (entryParent == null)
            {
                var group = GetComponentInChildren<VerticalLayoutGroup>(true);
                if (group != null)
                    entryParent = group.transform;
            }

            if (entryParent == null)
                return;

            UIUtils.ClearChildren(entryParent);


            foreach (var milestone in skill.milestones)
            {
                var entry = Instantiate(entryPrefab, entryParent);
                var refs = entry.GetComponent<MilestoneEntryUIReferences>();

                if (refs != null)
                {
                    if (refs.levelText != null)
                        refs.levelText.text = $"Lv {milestone.levelRequirement}";

                    string desc = milestone.GetDescription(skill.skillName);
                    if (refs.descriptionText != null)
                        refs.descriptionText.text = desc;

                }

                var img = entry.GetComponentInChildren<Image>();
                if (img != null)
                {
                    bool unlocked = controller && controller.IsMilestoneUnlocked(skill, milestone);
                    img.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
    }
}
