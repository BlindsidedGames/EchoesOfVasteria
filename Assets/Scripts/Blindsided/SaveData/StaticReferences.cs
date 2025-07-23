using System;
using System.Collections.Generic;
using static Blindsided.Oracle;
using static Blindsided.SaveData.GameData;

namespace Blindsided.SaveData
{
    public static class StaticReferences
    {
        public static Dictionary<string, int> UpgradeLevels => oracle.saveData.UpgradeLevels;
        public static Dictionary<string, ResourceEntry> Resources => oracle.saveData.Resources;
        public static Dictionary<string, double> EnemyKills => oracle.saveData.EnemyKills;
        public static HashSet<string> CompletedNpcTasks => oracle.saveData.CompletedNpcTasks;
        /// <summary>
        ///     Runtime tracking of NPC meetings that are currently active.
        ///     These are not persisted and are cleared when the game restarts.
        /// </summary>
        public static HashSet<string> ActiveNpcMeetings { get; } = new();


        public static BuyMode PurchaseMode
        {
            get => oracle.saveData.SavedPreferences.BuyMode;
            set => oracle.saveData.SavedPreferences.BuyMode = value;
        }

        public static bool RoundedBulkBuy
        {
            get => oracle.saveData.SavedPreferences.RoundedBulkBuy;
            set => oracle.saveData.SavedPreferences.RoundedBulkBuy = value;
        }

        public static NumberTypes Notation
        {
            get => oracle.saveData.SavedPreferences.Notation;
            set => oracle.saveData.SavedPreferences.Notation = value;
        }

        public static bool ExtraBuyOptions
        {
            get => oracle.saveData.SavedPreferences.ExtraBuyOptions;
            set => oracle.saveData.SavedPreferences.ExtraBuyOptions = value;
        }

        public static Tab LayerTab
        {
            get => oracle.saveData.SavedPreferences.LayerTab;
            set => oracle.saveData.SavedPreferences.LayerTab = value;
        }

        public static float TimeScale
        {
            get => oracle.saveData.TimeScale;
            set => oracle.saveData.TimeScale = value;
        }

        public static float CurrentTime
        {
            get => oracle.saveData.CurrentTime;
            set => oracle.saveData.CurrentTime = value;
        }

        public static double OfflineTime
        {
            get => oracle.saveData.OfflineTime;
            set => oracle.saveData.OfflineTime = value;
        }

        public static double OfflineTimeScaleMultiplier
        {
            get => oracle.saveData.OfflineTimeScaleMultiplier;
            set => oracle.saveData.OfflineTimeScaleMultiplier = value;
        }

        public static bool OfflineTimeActive
        {
            get => oracle.saveData.SavedPreferences.OfflineTimeActive;
            set => oracle.saveData.SavedPreferences.OfflineTimeActive = value;
        }

        public static bool OfflineTimeAutoDisable
        {
            get => oracle.saveData.SavedPreferences.OfflineTimeAutoDisable;
            set => oracle.saveData.SavedPreferences.OfflineTimeAutoDisable = value;
        }

        public static bool UseScaledTimeForValues
        {
            get => oracle.saveData.SavedPreferences.UseScaledTimeForValues;
            set => oracle.saveData.SavedPreferences.UseScaledTimeForValues = value;
        }

        public static float MasterVolume
        {
            get => oracle.saveData.SavedPreferences.MasterVolume;
            set => oracle.saveData.SavedPreferences.MasterVolume = value;
        }

        public static float MusicVolume
        {
            get => oracle.saveData.SavedPreferences.MusicVolume;
            set => oracle.saveData.SavedPreferences.MusicVolume = value;
        }

        public static float SfxVolume
        {
            get => oracle.saveData.SavedPreferences.SfxVolume;
            set => oracle.saveData.SavedPreferences.SfxVolume = value;
        }

        public static bool ShowLevelText
        {
            get => oracle.saveData.SavedPreferences.ShowLevelText;
            set
            {
                if (oracle.saveData.SavedPreferences.ShowLevelText != value)
                {
                    oracle.saveData.SavedPreferences.ShowLevelText = value;
                    ShowLevelTextChanged?.Invoke();
                }
            }
        }

        /// <summary>
        ///     Indicates whether autobuff is enabled. The saved preference can
        ///     temporarily be disabled for the remainder of a run without
        ///     affecting the persisted value.
        /// </summary>
        public static bool AutoBuff
        {
            get => oracle.saveData.AutoBuff && !autoBuffDisabledThisRun;
            set
            {
                if (oracle.saveData.AutoBuff != value)
                {
                    oracle.saveData.AutoBuff = value;
                    AutoBuffChanged?.Invoke();
                }
            }
        }

        private static bool autoBuffDisabledThisRun;

        /// <summary>
        ///     Temporarily enable or disable autobuff until the next run starts.
        /// </summary>
        /// <param name="disabled">True to disable autobuff for the current run.</param>
        public static void SetAutoBuffRunDisabled(bool disabled)
        {
            if (autoBuffDisabledThisRun != disabled)
            {
                autoBuffDisabledThisRun = disabled;
                AutoBuffChanged?.Invoke();
            }
        }


        public static Preferences SavedPreferences => oracle.saveData.SavedPreferences;
        public static Dictionary<string, bool> Foldouts => oracle.saveData.SavedPreferences.Foldouts;
        public static event Action ShowLevelTextChanged;
        public static event Action AutoBuffChanged;
    }}