using System;
using System.Linq;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    /// <summary>
    /// Helpers for converting between absolute stat values and normalized quantile quality in [0,1].
    /// </summary>
    public static class StatRollMath
    {
        /// <summary>
        /// Convert an absolute stat value to a normalized quantile by linear normalization across [minRoll,maxRoll].
        /// If rarity is supplied and the observed value equals the rarity floor (within epsilon), returns the floor quantile.
        /// </summary>
        public static double ToNormalizedQuantileLinear(StatDefSO def, float observedValue, RaritySO rarityForFloor = null)
        {
            if (def == null) return 0.0;
            if (Mathf.Approximately(def.minRoll, def.maxRoll)) return 0.0;

            if (rarityForFloor != null)
            {
                float floorQ = Mathf.Clamp01(rarityForFloor.floorPercent / 100f);
                float floorVal = Mathf.Lerp(def.minRoll, def.maxRoll, floorQ);
                if (Approximately(observedValue, floorVal, def))
                    return floorQ;
            }

            float clamped = Mathf.Clamp(observedValue, def.minRoll, def.maxRoll);
            return Mathf.InverseLerp(def.minRoll, def.maxRoll, clamped);
        }

        /// <summary>
        /// Invert the StatDefSO.RemapRoll mapping to estimate the pre-floor normalized quantile t in [0,1].
        /// If a rarity is provided and the observed value equals the rarity floor (within epsilon), returns
        /// the quantile that maps to the floor value (best-effort for legacy floor-clamped rolls).
        /// </summary>
        public static double InvertQuantileFromValue(StatDefSO def, float observedValue, RaritySO rarityForFloor = null)
        {
            if (def == null) return 0.0;

            // Degenerate case: no range
            if (Mathf.Approximately(def.minRoll, def.maxRoll))
                return 0.0;

            // If the value appears to be exactly the rarity floor, prefer the quantile mapping to that floor value
            if (rarityForFloor != null)
            {
                float floorQ = Mathf.Clamp01(rarityForFloor.floorPercent / 100f);
                float floorVal = Mathf.Lerp(def.minRoll, def.maxRoll, floorQ);
                if (Approximately(observedValue, floorVal, def))
                {
                    return InvertQuantileFromValue(def, floorVal);
                }
            }

            return InvertQuantileFromValue(def, observedValue);
        }

        /// <summary>
        /// Invert without floor context. Uses bisection over t in [0,1] to minimize |RemapRoll(t) - value|.
        /// Robust to non-linear roll curves. Returns a value in [0,1].
        /// </summary>
        public static double InvertQuantileFromValue(StatDefSO def, float value)
        {
            if (def == null) return 0.0;
            if (Mathf.Approximately(def.minRoll, def.maxRoll)) return 0.0;

            // Clamp target to [min,max] to make search stable
            float target = Mathf.Clamp(value, def.minRoll, def.maxRoll);

            // Simple bisection on t in [0,1]
            double lo = 0.0, hi = 1.0;
            const int iters = 48;
            for (int i = 0; i < iters; i++)
            {
                double mid = 0.5 * (lo + hi);
                float fmid = def.RemapRoll((float)mid);
                if (fmid < target)
                    lo = mid;
                else
                    hi = mid;
            }
            double t = 0.5 * (lo + hi);
            // Final clamp and return
            if (t < 0.0) t = 0.0; else if (t > 1.0) t = 1.0;
            return t;
        }

        private static bool Approximately(float a, float b, StatDefSO def)
        {
            float range = Mathf.Max(1e-6f, Mathf.Abs(def.maxRoll - def.minRoll));
            float eps = 1e-5f * range;
            return Mathf.Abs(a - b) <= eps;
        }
    }
}
