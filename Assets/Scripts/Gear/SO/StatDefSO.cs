using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    public enum HeroStatMapping
    {
        Damage,
        AttackRate,
        Defense,
        MaxHealth,
        HealthRegen,
        MoveSpeed,
        CritChance
    }

    [CreateAssetMenu(fileName = "StatDef", menuName = "SO/Gear/Stat Definition")]
    public class StatDefSO : ScriptableObject
    {
        [Title("Identity")]
        [Tooltip("Unique string id used for save data and lookups.")]
        public string id;

        [Tooltip("Readable name for UI; if empty the asset name will be shown.")]
        public string displayName;

        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite icon;

        [Title("Rolling")] public bool isPercent;

        [MinValue(0f)] public float minRoll = 0f;
        [MinValue(0f)] public float maxRoll = 1f;

        [Tooltip("Distribution curve for random rolls in [0,1]. Value is remapped to [minRoll,maxRoll].")]
        public AnimationCurve rollCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Title("Application")] public HeroStatMapping heroMapping;

        [Title("Comparison")]
        [Tooltip("How many comparison points per 1 unit of this stat. Example: 0.01 for AttackRate (per 1%), 1.0 for Damage per point.")]
        public float comparisonScale = 1f;

        public float RemapRoll(float t)
        {
            var v = Mathf.Clamp01(rollCurve != null ? rollCurve.Evaluate(Mathf.Clamp01(t)) : t);
            return Mathf.Lerp(minRoll, maxRoll, v);
        }

        public string GetName() => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}


