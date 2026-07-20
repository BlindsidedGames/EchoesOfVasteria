using System;
using System.Collections.Generic;
using UnityEngine;
using static Blindsided.Oracle;
using static Blindsided.SaveData.GameData;

namespace Blindsided.SaveData
{
    public static class StaticReferences
    {
        // Most writable static properties in this class are façades over save data
        // or PlayerPrefs, not retained static state. Unity's domain-reload analyzer
        // cannot see through those accessors; the real retained state is reset below.
#pragma warning disable UDR0001, UDR0002
        private const string MasterVolumeKey = "MasterVolume";
        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private const string DropFloatingTextDurationKey = "DropFloatingTextDuration";
        private const string PlayerDamageTextDurationKey = "PlayerDamageTextDuration";
        private const string EnemyDamageTextDurationKey = "EnemyDamageTextDuration";
        private const string PlayerFloatingDamageKey = "PlayerFloatingDamage";
        private const string EnemyFloatingDamageKey = "EnemyFloatingDamage";
        private const string ItemDropFloatingTextKey = "ItemDropFloatingText";
        private const string AutoPinActiveQuestsKey = "AutoPinActiveQuests";
        private const string StopAutocraftOnVastiumKey = "StopAutocraftOnVastium";
        private const string LockAutocraftStatSetKey = "LockAutocraftStatSet";
        private const string TargetFpsKey = "TargetFps";
        private const string VSyncEnabledKey = "VSyncEnabled";
        private const string SafeAreaRatioKey = "SafeAreaRatio";
        private const string MuteWhenUnfocusedKey = "MuteWhenUnfocused";
        private const string FastCraftModeKey = "FastCraftMode";
        public static Dictionary<string, int> UpgradeLevels => oracle.saveData.UpgradeLevels;
        public static Dictionary<string, ResourceEntry> Resources => oracle.saveData.Resources;
        public static Dictionary<string, double> EnemyKills => oracle.saveData.EnemyKills;
        public static HashSet<string> CompletedNpcTasks => oracle.saveData.CompletedNpcTasks;
        /// <summary>
        ///     Runtime tracking of NPC meetings that are currently active.
        ///     These are not persisted and are cleared when the game restarts.
        /// </summary>
        public static HashSet<string> ActiveNpcMeetings { get; } = new();

        // Runtime-only bonus contributed by AE section tiers; not persisted. Updated by Collections UI.
        private static float disciplePercentCollectionsBonus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveNpcMeetings.Clear();
            disciplePercentCollectionsBonus = 0f;
            ShowLevelTextChanged = null;
            AutoBuffChanged = null;
        }

        public static float DisciplePercent
        {
            get => oracle.saveData.DisciplePercent + Mathf.Max(0f, disciplePercentCollectionsBonus);
            set => oracle.saveData.DisciplePercent = value;
        }

        // Expose the runtime collections bonus for controlled updates
        public static float DisciplePercentCollectionsBonus
        {
            get => Mathf.Max(0f, disciplePercentCollectionsBonus);
            set => disciplePercentCollectionsBonus = Mathf.Max(0f, value);
        }


        public static BuyMode PurchaseMode
        {
            get => oracle.saveData.SavedPreferences.BuyMode;
            set => oracle.saveData.SavedPreferences.BuyMode = value;
        }

        // Forge conversion mode persistence (0=Single, 1=Half, 2=All)
        public static int IngotCraftMode
        {
            get => oracle.saveData.SavedPreferences.IngotCraftMode;
            set => oracle.saveData.SavedPreferences.IngotCraftMode = Mathf.Clamp(value, 0, 2);
        }

        public static int CrystalCraftMode
        {
            get => oracle.saveData.SavedPreferences.CrystalCraftMode;
            set => oracle.saveData.SavedPreferences.CrystalCraftMode = Mathf.Clamp(value, 0, 2);
        }

        public static int ChunkCraftMode
        {
            get => oracle.saveData.SavedPreferences.ChunkCraftMode;
            set => oracle.saveData.SavedPreferences.ChunkCraftMode = Mathf.Clamp(value, 0, 2);
        }

        public static double ForgeIngotCraftAmount
        {
            get => ClampForgeCraftAmount(ref oracle.saveData.SavedPreferences.IngotCraftAmount);
            set => oracle.saveData.SavedPreferences.IngotCraftAmount = ClampForgeCraftAmount(value);
        }

        public static double ForgeCrystalCraftAmount
        {
            get => ClampForgeCraftAmount(ref oracle.saveData.SavedPreferences.CrystalCraftAmount);
            set => oracle.saveData.SavedPreferences.CrystalCraftAmount = ClampForgeCraftAmount(value);
        }

        public static double ForgeChunkCraftAmount
        {
            get => ClampForgeCraftAmount(ref oracle.saveData.SavedPreferences.ChunkCraftAmount);
            set => oracle.saveData.SavedPreferences.ChunkCraftAmount = ClampForgeCraftAmount(value);
        }

        public static double ForgeCoreCraftAmount
        {
            get => ClampForgeCraftAmount(ref oracle.saveData.SavedPreferences.CoreCraftAmount);
            set => oracle.saveData.SavedPreferences.CoreCraftAmount = ClampForgeCraftAmount(value);
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
            get
            {
                if (oracle?.saveData?.SavedPreferences != null)
                    oracle.saveData.SavedPreferences.OfflineTimeActive = true;
                return true;
            }
            set
            {
                if (oracle?.saveData?.SavedPreferences != null)
                    oracle.saveData.SavedPreferences.OfflineTimeActive = true;
            }
        }

        public static bool OfflineTimeAutoDisable
        {
            get
            {
                if (oracle?.saveData?.SavedPreferences != null)
                    oracle.saveData.SavedPreferences.OfflineTimeAutoDisable = false;
                return false;
            }
            set
            {
                if (oracle?.saveData?.SavedPreferences != null)
                    oracle.saveData.SavedPreferences.OfflineTimeAutoDisable = false;
            }
        }

        public static bool UseScaledTimeForValues
        {
            get => oracle.saveData.SavedPreferences.UseScaledTimeForValues;
            set => oracle.saveData.SavedPreferences.UseScaledTimeForValues = value;
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.25f);
            set => PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.7f);
            set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public static int TargetFps
        {
            get => PlayerPrefs.GetInt(TargetFpsKey, 60);
            set
            {
                PlayerPrefs.SetInt(TargetFpsKey, Mathf.Clamp(value, 30, 1000));
                PlayerPrefs.Save();
            }
        }

        public static bool VSyncEnabled
        {
            get => PlayerPrefs.GetInt(VSyncEnabledKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(VSyncEnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
                QualitySettings.vSyncCount = value ? 1 : 0;
            }
        }

        public static float SafeAreaRatio
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SafeAreaRatioKey, 0f));
            set
            {
                PlayerPrefs.SetFloat(SafeAreaRatioKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static bool MuteWhenUnfocused
        {
            get => PlayerPrefs.GetInt(MuteWhenUnfocusedKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MuteWhenUnfocusedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static float DropFloatingTextDuration
        {
            get => PlayerPrefs.GetFloat(DropFloatingTextDurationKey, 1f);
            set => PlayerPrefs.SetFloat(DropFloatingTextDurationKey, Mathf.Clamp(value, 0f, 10f));
        }

        public static float PlayerDamageTextDuration
        {
            get => PlayerPrefs.GetFloat(PlayerDamageTextDurationKey, 0.5f);
            set => PlayerPrefs.SetFloat(PlayerDamageTextDurationKey, Mathf.Clamp(value, 0f, 2f));
        }

        public static float EnemyDamageTextDuration
        {
            get => PlayerPrefs.GetFloat(EnemyDamageTextDurationKey, 0.5f);
            set => PlayerPrefs.SetFloat(EnemyDamageTextDurationKey, Mathf.Clamp(value, 0f, 2f));
        }

        public static bool PlayerFloatingDamage
        {
            get => PlayerPrefs.GetInt(PlayerFloatingDamageKey, 1) == 1;
            set => PlayerPrefs.SetInt(PlayerFloatingDamageKey, value ? 1 : 0);
        }

        public static bool EnemyFloatingDamage
        {
            get => PlayerPrefs.GetInt(EnemyFloatingDamageKey, 1) == 1;
            set => PlayerPrefs.SetInt(EnemyFloatingDamageKey, value ? 1 : 0);
        }

        public static bool ItemDropFloatingText
        {
            get => PlayerPrefs.GetInt(ItemDropFloatingTextKey, 1) == 1;
            set => PlayerPrefs.SetInt(ItemDropFloatingTextKey, value ? 1 : 0);
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
            get => PlayerPrefs.GetInt(AutoPinActiveQuestsKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(AutoPinActiveQuestsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool StopAutocraftOnVastium
        {
            get => PlayerPrefs.GetInt(StopAutocraftOnVastiumKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(StopAutocraftOnVastiumKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // Stat Matching (formerly "Lock Stats"): when enabled, forge autocraft only stops
        // on upgrades that match the equipped item's affix stat set for that slot.
        // Now stored in SavedPreferences instead of PlayerPrefs. Default OFF for new saves.
        public static bool LockAutocraftStatSet
        {
            get => oracle.saveData.SavedPreferences.LockAutocraftStatSet;
            set => oracle.saveData.SavedPreferences.LockAutocraftStatSet = value;
        }

        /// <summary>
        /// Fast Craft Mode: when enabled, forge telemetry skips detailed stat tracking
        /// during autocrafting for maximum performance. Essential stats are still recorded.
        /// </summary>
        public static bool FastCraftMode
        {
            get => PlayerPrefs.GetInt(FastCraftModeKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FastCraftModeKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
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


        private static double ClampForgeCraftAmount(ref double value)
        {
            var clamped = ClampForgeCraftAmount(value);
            value = clamped;
            return clamped;
        }

        private static double ClampForgeCraftAmount(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 1d;
            var clamped = Math.Floor(value);
            return clamped < 1d ? 1d : clamped;
        }

        public static Preferences SavedPreferences => oracle.saveData.SavedPreferences;
        public static Dictionary<string, bool> Foldouts => oracle.saveData.SavedPreferences.Foldouts;
        public static event Action ShowLevelTextChanged;
        public static event Action AutoBuffChanged;
        public static void NotifyAutoBuffChanged()
        {
            AutoBuffChanged?.Invoke();
        }

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

            var totalQuests = Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Quests.QuestData>("Quests").Length;
            var totalResources = Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Upgrades.Resource>("").Length;

            var total = totalQuests + totalResources;
            var completed = completedQuests + unlockedResources;

            oracle.saveData.CompletionPercentage = total > 0 ? completed / (float)total * 100f : 0f;
        }
#pragma warning restore UDR0001, UDR0002
    }
}
