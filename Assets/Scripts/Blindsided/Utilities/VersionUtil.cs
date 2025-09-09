using System;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Lightweight semantic-ish version comparator that compares sequences of numeric segments.
    /// Non-numeric suffixes (e.g., "f1") are ignored per segment.
    /// </summary>
    public static class VersionUtil
    {
        public static int Compare(string a, string b)
        {
            if (a == b) return 0;
            if (string.IsNullOrEmpty(a)) return -1;
            if (string.IsNullOrEmpty(b)) return 1;

            var pa = ParseVersionToIntArray(a);
            var pb = ParseVersionToIntArray(b);
            var len = Math.Max(pa.Length, pb.Length);
            for (var i = 0; i < len; i++)
            {
                var ai = i < pa.Length ? pa[i] : 0;
                var bi = i < pb.Length ? pb[i] : 0;
                if (ai < bi) return -1;
                if (ai > bi) return 1;
            }
            return 0;
        }

        private static int[] ParseVersionToIntArray(string v)
        {
            var parts = (v ?? string.Empty).Split('.');
            var list = new System.Collections.Generic.List<int>(parts.Length);
            foreach (var p in parts)
            {
                var val = 0;
                var any = false;
                for (var i = 0; i < p.Length; i++)
                {
                    if (char.IsDigit(p[i]))
                    {
                        any = true;
                        val = val * 10 + (p[i] - '0');
                    }
                    else break;
                }
                list.Add(any ? val : 0);
            }
            return list.ToArray();
        }
    }
}

