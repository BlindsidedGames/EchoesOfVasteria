using System;
using System.Collections.Generic;
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

		[InfoBox("Roll Curve maps a normalized quantile t ∈ [0,1] to a value between Min and Max. Rarity Bands clamp the t-range per rarity, and Within-Tier Curve biases samples inside that band.")]
        [Tooltip("Distribution curve for random rolls in [0,1]. Value is remapped to [minRoll,maxRoll].")]
        public AnimationCurve rollCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Serializable]
        public class RarityBand
        {
            [Title("Rarity Band")] public RaritySO rarity;

            [PropertyRange(0f, 1f)] [LabelText("Min Quantile")] public float minQuantile = 0f;
            [PropertyRange(0f, 1f)] [LabelText("Max Quantile")] public float maxQuantile = 1f;

			[InfoBox("Within-Tier Curve shapes sampling inside [MinQ, MaxQ]. For example, an ease-out curve biases towards the higher end of the band's range.")]
            [LabelText("Within-Tier Curve")] public AnimationCurve withinTierCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            public float GetClampedMin() => Mathf.Clamp01(Mathf.Min(minQuantile, maxQuantile));
            public float GetClampedMax() => Mathf.Clamp01(Mathf.Max(minQuantile, maxQuantile));
        }

        [Title("Rarity Bands")]
        [ListDrawerSettings(ShowPaging = true, DraggableItems = true)]
        public List<RarityBand> rarityBands = new();

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

        public RarityBand GetBandForRarity(RaritySO r)
        {
            if (r == null) return null;
            for (int i = 0; i < rarityBands.Count; i++)
            {
                var b = rarityBands[i];
                if (b != null && b.rarity == r)
                    return b;
            }
            return null;
        }

#if UNITY_EDITOR
        [PropertySpace]
        [Title("Auto-Distribution (Editor)")]
        [InfoBox("Auto-distribute uniform bands with overlap. Optionally reserve the very top quantile slice exclusively for the highest rarity tier.")]
        [PropertyRange(0f, 0.5f)] public float defaultOverlapPercent = 0.10f;
        [PropertyRange(0f, 0.25f)] public float topReservePercent = 0.02f;
        public bool reserveTopForHighest = true;

        [Button("Auto-Distribute Bands Across Rarities")]
        private void AutoDistributeBands()
        {
            var list = new List<RaritySO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RaritySO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var r = UnityEditor.AssetDatabase.LoadAssetAtPath<RaritySO>(path);
                if (r != null) list.Add(r);
            }
            list.Sort((a, b) => a.tierIndex.CompareTo(b.tierIndex));
            if (list.Count == 0) return;

            int n = list.Count;
            float baseWidth = 1f / n;
            float overlap = Mathf.Clamp01(defaultOverlapPercent) * baseWidth;
            float topReserve = reserveTopForHighest ? Mathf.Clamp01(topReservePercent) : 0f;

            // Ensure we have entries for all rarities
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                var band = GetBandForRarity(r);
                if (band == null)
                {
                    band = new RarityBand { rarity = r };
                    rarityBands.Add(band);
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                float start = i * baseWidth - overlap;
                float end = (i + 1) * baseWidth + overlap;
                if (i == list.Count - 1 && reserveTopForHighest)
                {
                    // Make sure last tier covers to 1 and owns the topReserve slice
                    end = 1f;
                }
                float minQ = Mathf.Clamp01(start);
                float maxQ = Mathf.Clamp01(end);
                // Clip others away from the reserved top slice, if requested
                if (reserveTopForHighest && i < list.Count - 1)
                {
                    maxQ = Mathf.Min(maxQ, 1f - topReserve);
                }
                var band = GetBandForRarity(list[i]);
                if (band != null)
                {
                    band.minQuantile = minQ;
                    band.maxQuantile = maxQ;
                    if (band.withinTierCurve == null || band.withinTierCurve.length == 0)
                        band.withinTierCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}


