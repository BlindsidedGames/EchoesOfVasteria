using System;
using System.Globalization;
using System.IO;
using Blindsided;
using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>Slot metadata and labels without references to either UI framework.</summary>
    public sealed class SaveSlotPresentation
    {
        public string Title { get; private set; }
        public string Playtime { get; private set; }
        public string LastPlayed { get; private set; }
        private float completion;
        private string createdVersion;
        private DateTime? lastPlayed;

        public void Refresh(int index)
        {
            var oracle = Oracle.oracle;
            if (oracle == null) return;
            completion = 0;
            if (PlayerPrefs.GetInt(oracle.GetSlotDeletedKey(index), 0) == 1)
            {
                lastPlayed = null; createdVersion = string.Empty; Playtime = "Playtime: None";
            }
            else if (index != oracle.CurrentSlot || !oracle.HasCurrentSlotData)
            {
                try
                {
                    var path = Path.Combine(Application.persistentDataPath, "Saves", oracle.GetSlotDirectoryName(index), "meta.json");
                    if (File.Exists(path))
                    {
                        var meta = JsonUtility.FromJson<Metadata>(File.ReadAllText(path));
                        var verified = meta != null && string.Equals(meta.integrity, "sha256", StringComparison.Ordinal);
                        completion = verified ? meta.completion : PlayerPrefs.GetFloat(SaveSlotActions.SlotKey(index, "Completion"), 0);
                        var playtime = verified ? meta.playTime : PlayerPrefs.GetFloat(SaveSlotActions.SlotKey(index, "Playtime"), 0);
                        Playtime = FormatPlaytime(playtime);
                        createdVersion = meta?.createdVersion ?? string.Empty;
                        lastPlayed = DateTime.TryParse(meta?.timestampUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                            ? date.ToUniversalTime() : null;
                    }
                    else { lastPlayed = null; createdVersion = string.Empty; Playtime = "Playtime: None"; }
                }
                catch (Exception ex)
                {
                    lastPlayed = null; createdVersion = string.Empty; Playtime = "Playtime: None";
                    Debug.LogError($"Failed to refresh slot {index}: {ex}");
                }
            }
            Update(index);
        }

        public void Update(int index)
        {
            var oracle = Oracle.oracle;
            if (oracle == null) return;
            var current = index == oracle.CurrentSlot;
            if (current)
            {
                if (!oracle.HasCurrentSlotData)
                {
                    Title = $"File {index + 1} - Recovery Required"; Playtime = "Playtime: Unavailable"; return;
                }
                completion = oracle.saveData.CompletionPercentage;
                Playtime = FormatPlaytime(oracle.saveData.PlayTime);
                lastPlayed = DateTime.TryParse(PlayerPrefs.GetString(SaveSlotActions.SlotKey(index, "Date"), string.Empty),
                    CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var date)
                    ? date.ToUniversalTime() : null;
                LastPlayed = lastPlayed.HasValue ? $"Last Save: {CalcUtils.FormatTime((DateTime.UtcNow-lastPlayed.Value).TotalSeconds, shortForm: true)}" : "Last Save: Never";
            }
            else
                LastPlayed = lastPlayed.HasValue
                    ? $"Last Played: {lastPlayed.Value.ToLocalTime():g} • {CalcUtils.FormatTime((DateTime.UtcNow-lastPlayed.Value).TotalSeconds, shortForm: true)} ago"
                    : "Last Played: Never";
            Title = $"File {index + 1} | {completion:0}%" + (current ? " - Active" : string.Empty);
            var version = current ? oracle.saveData.GameVersionCreated : createdVersion;
            LastPlayed += $"\n<size=80%>Created with version: {(string.IsNullOrEmpty(version) ? "Unknown" : version)}</size>";
        }

        private static string FormatPlaytime(double value) => value > 0
            ? $"Playtime: {CalcUtils.FormatTime(value, shortForm: true)}" : "Playtime: None";

        [Serializable] private sealed class Metadata
        {
            public string timestampUtc = null, integrity = null, createdVersion = null;
            public float completion = 0;
            public double playTime = 0;
        }
    }
}
