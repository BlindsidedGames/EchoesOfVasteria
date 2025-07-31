using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Blindsided.SaveData
{
    public class GameData
    {
        [TabGroup("Preferences")] public Preferences SavedPreferences = new();

        [HideReferenceObjectPicker] [TabGroup("Skills")]
        public Dictionary<string, SkillProgress> SkillData = new();

        [HideReferenceObjectPicker] [TabGroup("UpgradeSystem")]
        public Dictionary<string, int> UpgradeLevels = new();

        public float CurrentTime = 0;
        public string DateQuitString;
        public string DateStarted;
        public double OfflineTime = 0;
        public double OfflineTimeCap = 3600f;
        public double OfflineTimeScaleMultiplier = 2f;
        public double PlayTime;
        public float TimeScale = 0f;
        [HideReferenceObjectPicker] public Dictionary<string, ResourceEntry> Resources = new();
        [HideReferenceObjectPicker] public Dictionary<string, double> EnemyKills = new();
        // Start with the MoveSpeed buff assigned to the first slot by default
        [HideReferenceObjectPicker]
        public List<string> BuffSlots = new() { "MoveSpeed", null, null, null, null };
        public int UnlockedBuffSlots = 1;
        public int UnlockedAutoBuffSlots = 0;
        [HideReferenceObjectPicker]
        public List<bool> AutoBuffSlots = new() { false, false, false, false, false };

        [HideReferenceObjectPicker] public HashSet<string> CompletedNpcTasks = new();

        [HideReferenceObjectPicker] public Dictionary<string, DiscipleGenerationRecord> Disciples = new();

        [HideReferenceObjectPicker] public Dictionary<string, QuestRecord> Quests = new();

        [HideReferenceObjectPicker] public HashSet<string> PinnedQuests = new();

        [HideReferenceObjectPicker] public Dictionary<int, TaskRecord> TaskRecords = new();

        [HideReferenceObjectPicker] public Dictionary<string, ResourceRecord> ResourceStats = new();

        [HideReferenceObjectPicker] public Dictionary<string, MapStatistics> MapStats = new();

        [HideReferenceObjectPicker] public GeneralStats General = new();


        [HideReferenceObjectPicker]
        public class ResourceEntry
        {
            public double Amount;
            public bool Earned;
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
            public bool AutoPinActiveQuests = true;
            /// <summary>
            ///     Whether the pinned quest panel is visible.
            /// </summary>
            public bool ShowPinnedQuests = true;
            public bool UseScaledTimeForValues;
            public float MasterVolume = 1f;
            public float MusicVolume = 0.25f;
            public float SfxVolume = 0.7f;

            /// <summary>
            ///     Desired target frame rate for the game.
            /// </summary>
            public int TargetFps = 60;

            /// <summary>
            ///     Normalised screen aspect ratio for the safe area limiter.
            ///     0 → 16:9, 1 → 32:9.
            /// </summary>
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
}