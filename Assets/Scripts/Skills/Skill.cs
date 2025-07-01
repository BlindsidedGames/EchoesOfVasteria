using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [CreateAssetMenu(fileName = "Skill", menuName = "SO/Skill")]
    public class Skill : SerializedScriptableObject
    {
        public string skillName;
        public Sprite skillIcon;
        public float xpForFirstLevel = 10f;
        public float xpLevelMultiplier = 1.5f;
        [TableList]
        public List<MilestoneBonus> milestones = new();
    }
}
