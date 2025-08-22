using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Blindsided.SaveData
{
    public class GameData
    {
        public int SchemaVersion = 1;
        [ShowInInspector] [TabGroup("GameDataTabs", "Preferences")]
        public Preferences SavedPreferences = new();

        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Skills")]
        public Dictionary<string, SkillProgress> SkillData = new();

        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "UpgradeSystem")]
        public Dictionary<string, int> UpgradeLevels = new();

        [TabGroup("GameDataTabs", "UpgradeSystem")]
        public bool StatUpgradesMigratedToGear;

        [TabGroup("GameDataTabs", "Time")] public float CurrentTime;
        [TabGroup("GameDataTabs", "Time")] public string DateQuitString;
        [TabGroup("GameDataTabs", "Time")] public string DateStarted;
        [TabGroup("GameDataTabs", "Time")] public double OfflineTime;
        [TabGroup("GameDataTabs", "Time")] public double OfflineTimeCap = 3600f;
        [TabGroup("GameDataTabs", "Time")] public double OfflineTimeScaleMultiplier = 2f;
        [TabGroup("GameDataTabs", "Time")] public double PlayTime;
        [TabGroup("GameDataTabs", "Time")] public float TimeScale;

        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Resources")]
        public Dictionary<string, ResourceEntry> Resources = new();

        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Resources")]
        public Dictionary<string, double> EnemyKills = new();

        // Start with the Echo Tasks buff assigned to the first slot by default
        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Buffs")]
        public List<string> BuffSlots = new() { "Echo Tasks", null, null, null, null };

        [TabGroup("GameDataTabs", "Buffs")] public int UnlockedBuffSlots = 1;
        [TabGroup("GameDataTabs", "Buffs")] public int UnlockedAutoBuffSlots;

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Buffs")]
        public List<bool> AutoBuffSlots = new() { false, false, false, false, false };

        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Tasks")]
        public HashSet<string> CompletedNpcTasks = new();

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Disciples")]
        public Dictionary<string, DiscipleGenerationRecord> Disciples = new();

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Quests")]
        public Dictionary<string, QuestRecord> Quests = new();

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Quests")]
        public HashSet<string> RetroQuestRewardsApplied = new();

        [TabGroup("GameDataTabs", "Quests")] public bool RetroQuestRewardsPassRan;

        // --- Gear system (phase 1) ---
        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Gear")]
        public Dictionary<string, GearItemRecord> EquipmentBySlot = new();

        [TabGroup("GameDataTabs", "Gear")] public int CraftingMasteryLevel; // Ivan's level
        [TabGroup("GameDataTabs", "Gear")] public float CraftingMasteryXP; // Ivan's current XP toward next level

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Quests")]
        public List<string> PinnedQuests = new();

        // --- Forge stats ---
        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Forge")]
        public ForgeStats Forge = new();

        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Tasks")]
        public Dictionary<int, TaskRecord> TaskRecords = new();

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Stats")]
        public Dictionary<string, ResourceRecord> ResourceStats = new();

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Stats")]
        public Dictionary<string, MapStatistics> MapStats = new();

        [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "Stats")]
        public GeneralStats General = new();

        [TabGroup("GameDataTabs", "Stats")] public float CompletionPercentage;

        [TabGroup("GameDataTabs", "Disciples")]
        public float DisciplePercent = 0.01f;

        // --- Cauldron (Stew/Collections) ---
        [TabGroup("GameDataTabs", "Cauldron")] public double CauldronStew;
        [TabGroup("GameDataTabs", "Cauldron")] public int CauldronEvaLevel = 1;
        [TabGroup("GameDataTabs", "Cauldron")] public double CauldronEvaXp;
        [ShowInInspector] [HideReferenceObjectPicker]
        [TabGroup("GameDataTabs", "Cauldron")]
        public Dictionary<string, int> CauldronCardCounts = new();
        [TabGroup("GameDataTabs", "Cauldron")] public bool CauldronShowAllCards;

        // Cauldron persisted totals (not per-session)
        [HideReferenceObjectPicker]
        [TabGroup("GameDataTabs", "Cauldron")] public CauldronTotalsRecord CauldronTotals = new();


        [HideReferenceObjectPicker]
        public class ResourceEntry
        {
            public double Amount;
            public bool Earned;
            public double BestPerMinute;
        }

        [HideReferenceObjectPicker]
        public class CauldronTotalsRecord
        {
            public int TotalTastings;
            public int TotalCards;
            public int GainedNothing;
            public int AlterEcho;
            public int Buffs;
            public int LowCards;
            public int EvasBlessing;
            public int VastSurge;
            // Alter-Echo subcategory totals
            public int AEFarming;
            public int AEFishing;
            public int AEMining;
            public int AEWoodcutting;
            public int AELooting;
            public int AECombat;
        }

        [HideReferenceObjectPicker]
        public class Preferences
        {
            public BuyMode BuyMode = BuyMode.BuyMax;
            public bool ExtraBuyOptions = true;
            public Dictionary<string, bool> Foldouts = new();
            public bool InvertMenu;
            public Tab LayerTab = Tab.Zero;
            public bool Music = true;
            public NumberTypes Notation;
            public bool OfflineTimeActive;
            public bool OfflineTimeAutoDisable;

            public bool RoundedBulkBuy = true;

            // Persist Forge conversion mode selections (0=Single, 1=Half, 2=All)
            public int IngotCraftMode = 0;
            public int CrystalCraftMode = 0;
            public int ChunkCraftMode = 0;
            public bool SettingsFoldout;
            public bool ShopFoldout = false;

            public bool ShortLongCurrencyDisplay;
            public bool ShowLevelText = true;

            public bool StatsFoldout;
            public bool TransparentUi;
            public bool Tutorial;

            /// <summary>
            ///     Automatically pin new quests when they become active.
            /// </summary>
            [Obsolete("Use PlayerPrefs via StaticReferences instead.")]
            public bool AutoPinActiveQuests = true;

            /// <summary>
            ///     Whether the pinned quest panel is visible.
            /// </summary>
            public bool ShowPinnedQuests = true;

            public bool UseScaledTimeForValues;

            /// <summary>
            ///     Desired target frame rate for the game.
            /// </summary>
            [Obsolete("Use PlayerPrefs via StaticReferences instead.")]
            public int TargetFps = 60;

            /// <summary>
            ///     Normalised screen aspect ratio for the safe area limiter.
            ///     0 → 16:9, 1 → 32:9.
            /// </summary>
            [Obsolete("Use PlayerPrefs via StaticReferences instead.")]
            public float SafeAreaRatio;
        }

        [HideReferenceObjectPicker]
        public class SkillProgress
        {
            public float CurrentXP;
            public int Level;
            public List<string> Milestones = new();
        }

        [HideReferenceObjectPicker]
        public class TaskRecord
        {
            public int TotalCompleted;
            public float TimeSpent;
            public float XpGained;
        }

        [HideReferenceObjectPicker]
        public class ResourceRecord
        {
            public int TotalReceived;
            public int TotalSpent;
        }

        [HideReferenceObjectPicker]
        public class DiscipleGenerationRecord
        {
            public Dictionary<string, double> StoredResources = new();
            public Dictionary<string, double> TotalCollected = new();
            public float Progress;
            public double LastGenerationTime;
        }

        [HideReferenceObjectPicker]
        public class QuestRecord
        {
            public bool Completed;
            public Dictionary<string, double> KillBaseline = new();
            public Dictionary<string, double> KillProgress = new();
            public float DistanceBaseline;
            public bool DistanceBaselineSet;
            public int BuffCastBaseline;
            public bool BuffCastBaselineSet;
        }

        [HideReferenceObjectPicker]
        public class RunRecord
        {
            public int RunNumber;
            public string MapType;
            public float Duration;
            public float Distance;
            public int TasksCompleted;
            public double ResourcesCollected;
            public double BonusResourcesCollected;
            public int EnemiesKilled;
            public float DamageDealt;
            public float DamageTaken;
            public bool Died;
            public bool Reaped;
            public bool Abandoned;
        }

        [HideReferenceObjectPicker]
        public class GeneralStats
        {
            public float DistanceTravelled;
            public float HighestDistance;
            public int TotalKills;
            public int SlimesKilled;
            public int TasksCompleted;
            public int Deaths;
            public float DamageDealt;
            public float DamageTaken;
            public int TimesReaped;
            public int BuffsCast;
            public double TotalResourcesGathered;

            // Records for the most recent runs. Limited to the last 50.
            public List<RunRecord> RecentRuns = new();
            public float LongestRun;
            public float ShortestRun;
            public float AverageRun;
            public float MaxRunDistance = 50f;
            public int NextRunNumber = 1;
        }

        [HideReferenceObjectPicker]
        public class MapStatistics
        {
            public float Steps;
            public float LongestTrek;
            public int TasksCompleted;
            public double ResourcesGathered;
            public int Kills;
            public float DamageDealt;
            public int Deaths;
            public float DamageTaken;
        }


        [HideReferenceObjectPicker]
        public class ForgeStats
        {
            // Top-level totals
            public int TotalCrafts;
            public int TotalEquippedFromCraft;
            public int TotalSalvaged;
            public int TotalAutocraftSessions;
            public int TotalCraftUntilUpgradeSessions;
            public int TotalFailedCraftAttempts;

            // Resource usage and returns
            public Dictionary<string, double> ResourcesSpent = new(); // ingots/cores/chunks/crystals spends
            public Dictionary<string, double> ResourcesGainedFromSalvage = new();
            public Dictionary<string, double> CoresSpentByCore = new(); // coreName -> cores spent
            public Dictionary<string, double> IngotsSpentByCore = new(); // coreName -> ingots spent

            // Distributions (what was crafted)
            public Dictionary<string, int> CraftsByCore = new();
            public Dictionary<string, int> CraftsBySlot = new();
            public Dictionary<string, int> CraftsByRarity = new();
            public Dictionary<string, Dictionary<string, int>> RarityCountsByCore = new(); // core -> rarity -> count
            public Dictionary<string, Dictionary<string, int>> SlotCountsByCore = new(); // core -> slot -> count
            public Dictionary<int, int> AffixCountDistribution = new();

            // Upgrade outcomes
            public Dictionary<string, int> UpgradesBySlot = new();
            public Dictionary<string, int> UpgradesByRarity = new();
            public int CraftsSinceLastUpgrade;
            public int MaxCraftsBetweenUpgrades;
            public int TotalUpgradeEvents;
            public int CumulativeCraftsBetweenUpgrades;
            public float AverageCraftsPerUpgrade; // derived but cached for convenience
            public Dictionary<string, FloatAgg> UpgradeScoreDeltaBySlot = new(); // slot -> {sum,count}

            // Affix/stat roll quality
            public Dictionary<string, StatAgg> StatRolls = new(); // statId -> agg
            public Dictionary<string, Dictionary<string, StatAgg>> StatRollsByRarity = new(); // rarity -> statId -> agg
            public Dictionary<string, Dictionary<string, StatAgg>> StatRollsBySlot = new(); // slot -> statId -> agg
            public Dictionary<string, int> HighRollsByStat = new(); // statId -> count above threshold
            public float HighRollTopPercentThreshold = 0.9f; // default top 10%

            public Dictionary<string, double>
                CumulativeStatTotalsByStat = new(); // statId -> running sum across all crafts

            public Dictionary<string, float> HighestRollByStat = new(); // statId -> highest single affix roll value

            // Ivan progression (forge mastery)
            public int IvanLevelAtCraft;
            public float IvanXpAtCraft;
            public double IvanXpGainedTotal;
            public int IvanLevelUpsFromCrafts;
            public Dictionary<string, double> IvanXpByCore = new();
            public Dictionary<string, double> IvanXpByRarity = new();

            // Autocraft specifics
            public int AutocraftCrafts;

            public Dictionary<string, int>
                AutocraftStopReasons = new(); // {Upgraded,OutOfResources,Cancelled,MaxIterations}

            public Dictionary<string, int> AutocraftBestRarityTierBySlot = new(); // slot -> highest rarity tier index

            // Salvage specifics
            public Dictionary<string, int> SalvagesByRarity = new();
            public Dictionary<string, int> SalvagesByCore = new();
            public int SalvageItems; // number of items salvaged
            public int SalvageEntries; // total individual entries awarded across all salvages
            public Dictionary<string, ResourceAgg> SalvageYieldPerResource = new(); // resName -> {sum,count}

            // Conversion actions (forge side-panels)
            public int IngotConversions; // actions performed
            public double CrystalCrafted; // total units produced (legacy total)
            public double ChunksCrafted; // total units produced (legacy total)
            public Dictionary<string, double> ConversionSpentByResource = new();
            public Dictionary<string, double> CrystalsCraftedByResource = new();
            public Dictionary<string, double> ChunksCraftedByResource = new();
            public Dictionary<string, double> IngotsCraftedByResource = new();

            // Best single-piece scores
            public Dictionary<string, float> BestPieceScoreBySlot = new(); // slot -> highest score (absolute)
            public Dictionary<string, float> BestPieceScoreByCore = new(); // coreName -> highest score (absolute)
            public Dictionary<string, float> MinPieceScoreByCore = new(); // coreName -> min observed piece score
            public Dictionary<string, float> MaxPieceScoreByCore = new(); // coreName -> max observed piece score
            public Dictionary<string, float> BestPieceScoreByRarity = new(); // rarityName -> highest score

            // Per-slot totals
            public Dictionary<string, int> EquipsBySlot = new();
            public Dictionary<string, int> SalvagesBySlot = new();
            public Dictionary<string, int> CraftsBySlotTotals = new();

            [HideReferenceObjectPicker]
            public class StatAgg
            {
                public int count;
                public double sum;
                public float min = float.PositiveInfinity;
                public float max = float.NegativeInfinity;
            }

            [HideReferenceObjectPicker]
            public class FloatAgg
            {
                public int count;
                public double sum;
            }

            [HideReferenceObjectPicker]
            public class ResourceAgg
            {
                public int count;
                public double sum;
            }
        }


        #region Enums

        #region UI

        public enum Tab
        {
            Zero,
            RealmOfResearch,
            FoundationOfProduction,
            CollapseOfTime,
            EnginesOfExpansion,
            TemporalRifts,
            ChronicleArchives
        }

        public enum BuyMode
        {
            Buy1,
            Buy10,
            Buy50,
            Buy100,
            BuyMax
        }

        public enum NumberTypes
        {
            Standard,
            Scientific,
            Engineering
        }

        public enum SideSetting
        {
            Left,
            Right
        }

        public enum SaveFile
        {
            Unset,
            Standard,
            Hardcore,
            Duplicator,
            Replicator
        }

        public enum Tier
        {
            Tier1,
            Tier2,
            Tier3,
            Tier4,
            Tier5,
            Tier6
        }

        public enum StoryState
        {
            Prologue,
            Chapter1,
            Chapter2,
            Chapter3,
            Chapter4,
            Chapter5,
            Chapter6,
            Chapter7,
            Chapter8,
            End
        }

        #endregion

        #endregion
    }

    [Serializable]
    public class GearAffixRecord
    {
        public string statId;
        public float value;
    }

    [Serializable]
    public class GearItemRecord
    {
        public string slot;
        public string rarity; // rarity asset name
        public List<GearAffixRecord> affixes = new();
    }
}