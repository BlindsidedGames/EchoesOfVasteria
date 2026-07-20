#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace VinTools.BetterRuleTiles.Editor.Utilities
{
    public class TextUtils
    {
        /// <summary>
        /// Find the longest common substring from specified list
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        public static string FindLongestCommonSubstring(IList<string> strings)
        {
            if (strings == null || !strings.Any() || strings.Any(s => string.IsNullOrEmpty(s)))
                return string.Empty;

            if (strings.Count == 1)
                return strings.First();

            string shortest = strings.OrderBy(s => s.Length).First();
            return Enumerable.Range(0, shortest.Length)
                .SelectMany(start => Enumerable.Range(1, shortest.Length - start)
                    .Select(length => shortest.Substring(start, length)))
                .OrderByDescending(s => s.Length)
                .FirstOrDefault(sub => strings.All(s => s.Contains(sub))) ?? string.Empty;
        }

        public static string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.IndexOf(Find);
            if (Place < 0) return Source;

            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }
    }
}
#endif