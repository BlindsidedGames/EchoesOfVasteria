using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blindsided.UGS;
using Blindsided.Utilities;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class LeaderboardSnapshot
    {
        public IList<LeaderboardEntry> Entries = Array.Empty<LeaderboardEntry>();
        public LeaderboardEntry Player;
        public int? Total;
        public string PlayerName = "Player";
    }
    public interface IToolkitLeaderboardSource
    {
        Task<LeaderboardSnapshot> Load(bool top, bool tasks);
    }
    /// <summary>Read-only adapter; score submission remains owned by the existing game services.</summary>
    public sealed class ToolkitLeaderboardSource : IToolkitLeaderboardSource
    {
        public async Task<LeaderboardSnapshot> Load(bool top, bool tasks)
        {
            var id = tasks ? UgsLeaderboardIds.Tasks : UgsLeaderboardIds.CompletionTime;
            var snapshot = new LeaderboardSnapshot();
            snapshot.Player = await LeaderboardClient.GetMyScoreAsync(id);
            if (top || snapshot.Player == null)
                snapshot.Entries = (await LeaderboardClient.GetTopAsync(50, id))?.Results ?? new List<LeaderboardEntry>();
            else
            {
                snapshot.Entries = (await LeaderboardClient.GetAroundPlayerAsync(50, id))?.Results ?? new List<LeaderboardEntry>();
                snapshot.Total = await LeaderboardClient.GetTotalCountAsync(id);
            }
            if (snapshot.Player != null && string.IsNullOrWhiteSpace(snapshot.Player.PlayerName))
            {
                try { snapshot.PlayerName = await LocalProfile.GetDisplayNameOrGeneratedAsync() ?? "Player"; }
                catch { snapshot.PlayerName = "Player"; }
            }
            return snapshot;
        }
    }
    public static class LeaderboardPresentation
    {
        public readonly struct Row
        {
            public readonly LeaderboardEntry Entry;
            public readonly bool IsPlayer;
            public Row(LeaderboardEntry entry, bool player) { Entry = entry; IsPlayer = player; }
        }
        public static List<Row> Rows(LeaderboardSnapshot snapshot, bool top)
        {
            var ordered = snapshot.Entries.Where(e => e != null).OrderBy(e => e.Rank).ToList();
            var my = snapshot.Player;
            if (my == null || (top && !ordered.Any(e => e.Rank == my.Rank)))
                return ordered.Take(50).Select(e => new Row(e, false)).ToList();
            var above = ordered.Where(e => e.Rank < my.Rank).ToList();
            var below = ordered.Where(e => e.Rank > my.Rank).ToList();
            var limit = top ? 49 : 50;
            var takeAbove = Math.Min(above.Count, limit / 2);
            var takeBelow = Math.Min(below.Count, limit - takeAbove);
            takeAbove = Math.Min(above.Count, limit - takeBelow);
            var rows = above.Skip(above.Count - takeAbove).Select(e => new Row(e, false)).ToList();
            rows.Add(new Row(my, true)); rows.AddRange(below.Take(takeBelow).Select(e => new Row(e, false))); return rows;
        }
        public static string Score(double value, bool tasks) => tasks ? Math.Floor(value).ToString("N0") : CalcUtils.FormatTime(Math.Max(0, value), mspace: false, shortForm: true).Replace("</color>", "").Trim();
        [Serializable] private class VersionMetadata { public string GameVersionCreated; public string LastGameVersion; }
        public static string Version(object metadata)
        {
            string created = null, last = null;
            try
            {
                if (metadata is IDictionary<string, object> values)
                { values.TryGetValue("GameVersionCreated", out var a); values.TryGetValue("LastGameVersion", out var b); created = a?.ToString(); last = b?.ToString(); }
                else if (metadata is string json)
                { var value = JsonUtility.FromJson<VersionMetadata>(json); created = value?.GameVersionCreated; last = value?.LastGameVersion; }
            }
            catch { return ""; }
            return string.IsNullOrEmpty(created) && string.IsNullOrEmpty(last) ? "" : $"Save created with V{created}\nScore submitted with V{last}";
        }
    }
}
