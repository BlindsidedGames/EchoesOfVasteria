using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "Skill", menuName = "SO/Skill")]
    public class Skill : ScriptableObject
    {
        public string skillName;
        public Sprite skillIcon;
        public float xpForFirstLevel = 10f;
        public float xpLevelMultiplier = 1.5f;
        public List<MilestoneBonus> milestones = new();
    }
}
