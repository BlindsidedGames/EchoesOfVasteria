using System;
using System.Globalization;
using System.IO;
using Blindsided;
using Blindsided.Utilities;
using UnityEngine;
using TimelessEchoes.UI.Toolkit;

namespace TimelessEchoes.UI
{
    /// <summary>Slot metadata and labels without references to either UI framework.</summary>
    public sealed class SaveSlotPresentation
    {
        public string Title { get; private set; }
        public string Playtime { get; private set; }
        public string LastPlayed { get; private set; }
        private float completion;
        private double savedPlaytime;
        private string createdVersion;
        private DateTime? lastPlayed;

        public void Refresh(int index)
        {
            var oracle = Oracle.oracle;
            if (oracle == null) return;
            completion = 0;
            savedPlaytime = 0;
            if (PlayerPrefs.GetInt(oracle.GetSlotDeletedKey(index), 0) == 1)
            {
                lastPlayed = null; createdVersion = string.Empty; Playtime = ToolkitLocalization.Text("save.playtime-none", "Playtime: None");
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
                        savedPlaytime = playtime;
                        Playtime = FormatPlaytime(playtime);
                        createdVersion = meta?.createdVersion ?? string.Empty;
                        lastPlayed = DateTime.TryParse(meta?.timestampUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                            ? date.ToUniversalTime() : null;
                    }
                    else { lastPlayed = null; createdVersion = string.Empty; Playtime = ToolkitLocalization.Text("save.playtime-none", "Playtime: None"); }
                }
                catch (Exception ex)
                {
                    lastPlayed = null; createdVersion = string.Empty; Playtime = ToolkitLocalization.Text("save.playtime-none", "Playtime: None");
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
            if (!current) Playtime = FormatPlaytime(savedPlaytime);
            if (current)
            {
                if (!oracle.HasCurrentSlotData)
                {
                    Title = ToolkitLocalization.Text("save.recovery-title", "File {0} - Recovery Required", index + 1); Playtime = ToolkitLocalization.Text("save.playtime-unavailable", "Playtime: Unavailable"); return;
                }
                completion = oracle.saveData.CompletionPercentage;
                Playtime = FormatPlaytime(oracle.saveData.PlayTime);
                lastPlayed = DateTime.TryParse(PlayerPrefs.GetString(SaveSlotActions.SlotKey(index, "Date"), string.Empty),
                    CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var date)
                    ? date.ToUniversalTime() : null;
                LastPlayed = lastPlayed.HasValue ? ToolkitLocalization.Text("save.last-save", "Last Save: {0}", CalcUtils.FormatTime((DateTime.UtcNow-lastPlayed.Value).TotalSeconds, shortForm: true)) : ToolkitLocalization.Text("save.last-save-never", "Last Save: Never");
            }
            else
                LastPlayed = lastPlayed.HasValue
                    ? ToolkitLocalization.Text("save.last-played", "Last Played: {0} \u2022 {1} ago", lastPlayed.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture), CalcUtils.FormatTime((DateTime.UtcNow-lastPlayed.Value).TotalSeconds, shortForm: true))
                    : ToolkitLocalization.Text("save.last-played-never", "Last Played: Never");
            Title = current ? ToolkitLocalization.Text("save.active-title", "File {0} | {1:0}% - Active", index + 1, completion) : ToolkitLocalization.Text("save.title", "File {0} | {1:0}%", index + 1, completion);
            var version = current ? oracle.saveData.GameVersionCreated : createdVersion;
            LastPlayed += "\n<size=80%>" + ToolkitLocalization.Text("save.created-version", "Created with version: {0}", string.IsNullOrEmpty(version) ? ToolkitLocalization.Text("common.unknown", "Unknown") : version) + "</size>";
        }

        private static string FormatPlaytime(double value) => value > 0
            ? ToolkitLocalization.Text("save.playtime", "Playtime: {0}", CalcUtils.FormatTime(value, shortForm: true)) : ToolkitLocalization.Text("save.playtime-none", "Playtime: None");

        [Serializable] private sealed class Metadata
        {
            public string timestampUtc = null, integrity = null, createdVersion = null;
            public float completion = 0;
            public double playTime = 0;
        }
    }
}
