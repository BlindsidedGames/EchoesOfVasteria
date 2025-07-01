#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace TimelessEchoes.Skills
{
    [CustomEditor(typeof(Skill))]
    public class SkillEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var skill = (Skill)target;
            if (skill == null || skill.milestones == null)
                return;

            bool changed = false;
            foreach (var milestone in skill.milestones)
            {
                if (milestone == null) continue;
                string expected = $"{skill.skillName.ToLowerInvariant().Replace(" ", string.Empty)}{milestone.levelRequirement}";
                if (milestone.bonusID != expected)
                {
                    milestone.bonusID = expected;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(skill);
            }
        }
    }
}
#endif
