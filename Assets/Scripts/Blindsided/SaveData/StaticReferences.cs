using System;
using System.Collections.Generic;
using UnityEngine;
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

        public static int TargetFps
        {
            get => oracle.saveData.SavedPreferences.TargetFps;
            set => oracle.saveData.SavedPreferences.TargetFps = value;
        }

        public static float SafeAreaRatio
        {
            get => oracle.saveData.SavedPreferences.SafeAreaRatio;
            set => oracle.saveData.SavedPreferences.SafeAreaRatio = Mathf.Clamp01(value);
        }

        public static float DropFloatingTextDuration
        {
            get => oracle.saveData.SavedPreferences.DropFloatingTextDuration;
            set => oracle.saveData.SavedPreferences.DropFloatingTextDuration = Mathf.Clamp(value, 0f, 10f);
        }

        public static float PlayerDamageTextDuration
        {
            get => oracle.saveData.SavedPreferences.PlayerDamageTextDuration;
            set => oracle.saveData.SavedPreferences.PlayerDamageTextDuration = Mathf.Clamp(value, 0f, 2f);
        }

        public static float EnemyDamageTextDuration
        {
            get => oracle.saveData.SavedPreferences.EnemyDamageTextDuration;
            set => oracle.saveData.SavedPreferences.EnemyDamageTextDuration = Mathf.Clamp(value, 0f, 2f);
        }

        public static bool PlayerFloatingDamage
        {
            get => oracle.saveData.SavedPreferences.PlayerFloatingDamage;
            set => oracle.saveData.SavedPreferences.PlayerFloatingDamage = value;
        }

        public static bool EnemyFloatingDamage
        {
            get => oracle.saveData.SavedPreferences.EnemyFloatingDamage;
            set => oracle.saveData.SavedPreferences.EnemyFloatingDamage = value;
        }

        public static bool ItemDropFloatingText
        {
            get => oracle.saveData.SavedPreferences.ItemDropFloatingText;
            set => oracle.saveData.SavedPreferences.ItemDropFloatingText = value;
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

        public static bool AutoPinActiveQuests
        {
            get => oracle.saveData.SavedPreferences.AutoPinActiveQuests;
            set => oracle.saveData.SavedPreferences.AutoPinActiveQuests = value;
        }

        public static bool ShowPinnedQuests
        {
            get => oracle.saveData.SavedPreferences.ShowPinnedQuests;
            set => oracle.saveData.SavedPreferences.ShowPinnedQuests = value;
        }

        public static int UnlockedAutoBuffSlots
        {
            get => oracle.saveData.UnlockedAutoBuffSlots;
            set => oracle.saveData.UnlockedAutoBuffSlots = Mathf.Clamp(value, 0, 5);
        }

        public static bool GetAutoBuffSlot(int index)
        {
            return index >= 0 && index < oracle.saveData.AutoBuffSlots.Count && oracle.saveData.AutoBuffSlots[index];
        }

        public static void SetAutoBuffSlot(int index, bool value)
        {
            if (index < 0 || index >= oracle.saveData.AutoBuffSlots.Count) return;
            if (oracle.saveData.AutoBuffSlots[index] != value)
            {
                oracle.saveData.AutoBuffSlots[index] = value;
                AutoBuffChanged?.Invoke();
            }
        }


        public static Preferences SavedPreferences => oracle.saveData.SavedPreferences;
        public static Dictionary<string, bool> Foldouts => oracle.saveData.SavedPreferences.Foldouts;
        public static event Action ShowLevelTextChanged;
        public static event Action AutoBuffChanged;

        public static void UpdateCompletionPercentage()
        {
            if (oracle == null) return;

            var completedQuests = 0;
            if (oracle.saveData.Quests != null)
                foreach (var q in oracle.saveData.Quests.Values)
                    if (q.Completed)
                        completedQuests++;

            var unlockedResources = 0;
            if (oracle.saveData.Resources != null)
                foreach (var r in oracle.saveData.Resources.Values)
                    if (r.Earned)
                        unlockedResources++;

            var totalQuests = UnityEngine.Resources.LoadAll<TimelessEchoes.Quests.QuestData>("Quests").Length;
            var totalResources = UnityEngine.Resources.LoadAll<TimelessEchoes.Upgrades.Resource>("").Length;

            var total = totalQuests + totalResources;
            var completed = completedQuests + unlockedResources;

            oracle.saveData.CompletionPercentage = total > 0 ? completed / (float)total * 100f : 0f;
        }
    }
}