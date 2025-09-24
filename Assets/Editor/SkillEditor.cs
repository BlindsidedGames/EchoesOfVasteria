#if UNITY_EDITOR
using System.Linq;
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

            var ordered = skill.milestones.Where(m => m != null)
                .OrderBy(m => m.UnlockLevel)
                .ToList();

            bool sequenceDiffers = ordered.Count != skill.milestones.Count ||
                                   skill.milestones.Where(m => m != null)
                                                   .Where((m, index) => index < ordered.Count && !ReferenceEquals(m, ordered[index]))
                                                   .Any();

            if (sequenceDiffers)
            {
                skill.milestones.Clear();
                skill.milestones.AddRange(ordered);
                EditorUtility.SetDirty(skill);
            }
        }
    }
}
#endif
