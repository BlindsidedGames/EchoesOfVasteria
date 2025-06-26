using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    public class MilestoneBonusUI : MonoBehaviour
    {
        [SerializeField] private Transform entryParent;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private SkillController controller;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<SkillController>();
        }

        public void PopulateMilestones(Skill skill)
        {
            if (skill == null || entryParent == null || entryPrefab == null)
                return;

            foreach (Transform child in entryParent)
                Destroy(child.gameObject);

            var prog = controller ? controller.GetProgress(skill) : null;
            int level = prog != null ? prog.Level : 1;

            foreach (var milestone in skill.milestones)
            {
                var entry = Instantiate(entryPrefab, entryParent);
                var texts = entry.GetComponentsInChildren<TMP_Text>();
                foreach (var t in texts)
                    t.text = $"Lv {milestone.levelRequirement}: {milestone.bonusDescription}";

                var img = entry.GetComponentInChildren<UnityEngine.UI.Image>();
                if (img != null)
                    img.color = level >= milestone.levelRequirement ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }
    }
}
