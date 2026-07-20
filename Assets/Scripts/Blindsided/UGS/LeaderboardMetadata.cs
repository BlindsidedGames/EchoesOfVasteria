using System.Collections.Generic;
using UnityEngine;

namespace Blindsided.UGS
{
    /// <summary>
    /// Builds compact JSON metadata for UGS leaderboard submissions.
    /// Contains GameVersionCreated and LastGameVersion.
    /// </summary>
    internal static class LeaderboardMetadata
    {
        // Returns a serializable object (dictionary) suitable for UGS metadata (expects JSON object, not string)
        public static object Build()
        {
            try
            {
                string created = null;
                string last = null;

                var oracle = Blindsided.Oracle.oracle;
                if (oracle != null && oracle.saveData != null)
                {
                    created = oracle.saveData.GameVersionCreated;
                    last = oracle.saveData.LastGameVersion;
                }

                if (string.IsNullOrEmpty(created)) created = Application.version;
                if (string.IsNullOrEmpty(last)) last = Application.version;

                var dict = new Dictionary<string, object>
                {
                    { "GameVersionCreated", created },
                    { "LastGameVersion", last }
                };
                return dict;
            }
            catch
            {
                // Best-effort fallback as dictionary
                var dict = new Dictionary<string, object>
                {
                    { "GameVersionCreated", Application.version },
                    { "LastGameVersion", Application.version }
                };
                return dict;
            }
        }
    }
}
