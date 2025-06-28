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
            public bool UseScaledTimeForValues;
        }

        [HideReferenceObjectPicker]
        public class SkillProgress
        {
            public float CurrentXP;
            public int Level;
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