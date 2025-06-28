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

        public static int ItemShards
        {
            get => oracle.saveData.ItemShards;
            set => oracle.saveData.ItemShards = value;
        }


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

        public static bool DevSpeed
        {
            get => oracle.saveData.DevOptions.DevSpeed;
            set => oracle.saveData.DevOptions.DevSpeed = value;
        }

        public static Preferences SavedPreferences => oracle.saveData.SavedPreferences;
        public static Dictionary<string, bool> Foldouts => oracle.saveData.SavedPreferences.Foldouts;

        public static Statistics Stats => oracle.saveData.Stats;

        public static TimeSpentInRealms TimeSpentInRealms => Stats.TimeSpentInRealms;
        public static TimeSpentInRealms ScaledTimeSpentInRealms => Stats.ScaledTimeSpentInRealms;

        public static event Action ShowLevelTextChanged;
    }
}