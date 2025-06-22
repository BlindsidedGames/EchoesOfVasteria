using System.Collections.Generic;
using TimelessEchoes.Gear;
using Sirenix.OdinInspector;

namespace Blindsided.SaveData
{
    public class SaveData
    {
        public float CurrentTime = 0;
        public string DateQuitString;
        public string DateStarted;
        [TabGroup("Devoptions")] public Devoptions DevOptions = new();

        [HideReferenceObjectPicker] [TabGroup("Codex")]
        public Dictionary<string, int> GlobalKillCounts = new();

        [HideReferenceObjectPicker] [TabGroup("Gear")]
        public Dictionary<string, HeroGearState> HeroGear = new();

        [HideReferenceObjectPicker] [TabGroup("Heroes")]
        public Dictionary<string, HeroState> HeroStates = new();

        [TabGroup("Gear")] public int ItemShards;
        public double OfflineTime = 0;
        public double OfflineTimeCap = 3600f;
        public double OfflineTimeScaleMultiplier = 2f;
        public double PlayTime;

        [TabGroup("Preferences")] public Preferences SavedPreferences = new();

        [HideReferenceObjectPicker] [TabGroup("Statistics")]
        public Statistics Stats = new();

        public float TimeScale = 0f;


        [HideReferenceObjectPicker] [TabGroup("UpgradeSystem")]
        public Dictionary<string, int> UpgradeLevels = new();

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

            public bool StatsFoldout;
            public bool TransparentUi;
            public bool Tutorial;
            public bool UseScaledTimeForValues;
        }

        [HideReferenceObjectPicker]
        public class Statistics
        {
            public TimeSpentInRealms ScaledTimeSpentInRealms = new();
            public TimeSpentInRealms TimeSpentInRealms = new();
        }

        [HideReferenceObjectPicker]
        public class TimeSpentInRealms
        {
            public float ChronicleArchives;
            public float CollapseOfTime;
            public float EnginesOfExpansion;

            public float EventHorizon;
            public float FoundationOfProduction;
            public float RealmOfResearch;
            public float TemporalRifts;
            public float VoidLull;

            public float Total => EventHorizon + FoundationOfProduction + RealmOfResearch + EnginesOfExpansion +
                                  CollapseOfTime + ChronicleArchives + TemporalRifts + VoidLull;
        }

        [HideReferenceObjectPicker]
        public class HeroState
        {
            public int CurrentXP;
            public int Level;
        }

        [HideReferenceObjectPicker]
        public class Devoptions
        {
            public bool DevSpeed;
        }

        [HideReferenceObjectPicker]
        public class HeroGearState
        {
            public GearItem Brooch;
            public GearItem Necklace;
            public GearItem Pocket;
            public GearItem Ring;
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