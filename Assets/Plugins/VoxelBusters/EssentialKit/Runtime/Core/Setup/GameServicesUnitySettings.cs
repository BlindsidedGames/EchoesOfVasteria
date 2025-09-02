using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    /// <summary>
    /// The GameServicesUnitySettings class is used to configure Game Services module of Essential Kit.
    /// </summary>
    /// @ingroup GameServices
    public partial class GameServicesUnitySettings : SettingsPropertyGroup
    {
        #region Nested types

        [SerializeField, FormerlySerializedAs("m_leaderboardMetaArray")]
        [Tooltip ("Array contains information of the leaderboards used within the game.")]
        public      LeaderboardDefinition[]     m_leaderboards;

        [SerializeField]
        [Tooltip ("Array contains information of the achievements used within the game.")]
        public      AchievementDefinition[]     m_achievements;

        [SerializeField]
        [Tooltip ("If enabled, a banner is displayed when an achievement is completed (iOS).")]
        private     bool                        m_showAchievementCompletionBanner;

        [SerializeField]
        [Tooltip ("If enabled, required permissions for accessing friends will be added.")]
        private     bool                        m_allowFriendsAccess;

        [SerializeField]
        [Tooltip("Android specific settings.")]
        private     AndroidPlatformProperties   m_androidProperties;

        #endregion

        #region Properties

        /// <summary>
        /// Gets an array of leaderboards used in the game.
        /// </summary>
        public LeaderboardDefinition[] Leaderboards => m_leaderboards;

        /// <summary>
        /// Gets an array of achievements used in the game.
        /// </summary>
        public AchievementDefinition[] Achievements => m_achievements;

        /// <summary>
        /// Gets a value indicating whether a banner should be displayed when an achievement is completed (iOS).
        /// </summary>
        public bool ShowAchievementCompletionBanner => m_showAchievementCompletionBanner;

        /// <summary>
        /// Gets a value indicating whether required permissions for accessing friends will be added.
        /// </summary>
        public bool AllowFriendsAccess => m_allowFriendsAccess;

        /// <summary>
        /// Gets Android specific settings for the Game Services module.
        /// </summary>
        public AndroidPlatformProperties AndroidProperties => m_androidProperties;

        #endregion

        #region Constructors

        public GameServicesUnitySettings(bool isEnabled = true, bool initializeOnStart = true,
            LeaderboardDefinition[] leaderboards = null, AchievementDefinition[] achievements = null,
            bool showAchievementCompletionBanner = true, bool allowFriendsAccess = true, AndroidPlatformProperties androidProperties = null)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kGameServices)
        {
            // set default values
            m_leaderboards                      = leaderboards ?? new LeaderboardDefinition[0];
            m_achievements                      = achievements ?? new AchievementDefinition[0];
            m_showAchievementCompletionBanner   = showAchievementCompletionBanner;
            m_allowFriendsAccess                = allowFriendsAccess;
            m_androidProperties                 = androidProperties ?? new AndroidPlatformProperties();
        }

        #endregion
    }
}