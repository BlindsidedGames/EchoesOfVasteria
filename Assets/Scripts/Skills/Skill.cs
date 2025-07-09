using System.Collections.Generic;
using Sirenix.OdinInspector;
using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes.Skills
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Skill", menuName = "SO/Skill")]
    public class Skill : SerializedScriptableObject
    {
        public string skillName;
        public Sprite skillIcon;
        public float xpForFirstLevel = 10f;
        public float xpLevelMultiplier = 1.5f;
        public float taskSpeedPerLevel = 0.01f;
        public List<MilestoneBonus> milestones = new();
    }}