using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents an object containing additional information related to game leaderboard.
    /// </summary>
    /// @ingroup GameServices
    [Serializable]
    public partial class LeaderboardDefinition
    {
        #region Fields

        [SerializeField]
        private     string                      m_id;

        [SerializeField]
        private     string                      m_platformId;

        [SerializeField]
        private     RuntimePlatformConstantSet  m_platformIdOverrides;

        [SerializeField]
        private     string                      m_title;

        [SerializeField, HideInInspector]
        private     IosPlatformProperties       m_iosProperties;

        [SerializeField, HideInInspector]
        private     AndroidPlatformProperties   m_androidProperties;

        #endregion

        #region Properties

        /// <summary>
        /// The string that identifies the leaderboard within Unity environment. (read-only)
        /// </summary>
        public string Id => PropertyHelper.GetValueOrDefault(m_id);

        /// <summary>
        /// The name of the leaderboard. (read-only)
        /// </summary>
        public string Title => PropertyHelper.GetValueOrDefault(m_title);

        public IosPlatformProperties IosProperties => m_iosProperties;

        public AndroidPlatformProperties AndroidProperties => m_androidProperties;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardDefinition"/> with specified properties.
        /// </summary>
        /// <param name="id">The string that identifies the leaderboard within Unity environment.</param>
        /// <param name="platformId">The leaderboard identifier for active platform.</param>
        /// <param name="platformIdOverrides">The set of platform-specific leaderboard identifiers which overrides <paramref name="platformId"/>.</param>
        /// <param name="title">The name of the leaderboard.</param>
        /// <param name="iosProperties">The leaderboard identifier for iOS platform.</param>
        /// <param name="androidProperties">The leaderboard identifier for Android platform.</param>
        public LeaderboardDefinition(string id = null, string platformId = null,
            RuntimePlatformConstantSet platformIdOverrides = null, string title = null,
            IosPlatformProperties iosProperties = null, AndroidPlatformProperties androidProperties = null)
        {
            // set default values
            m_id                    = id;
            m_platformId            = platformId;
            m_platformIdOverrides   = platformIdOverrides ?? new RuntimePlatformConstantSet();
            m_title                 = title;
            m_iosProperties         = iosProperties ?? new IosPlatformProperties(); 
            m_androidProperties     = androidProperties ?? new AndroidPlatformProperties(); 
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Returns the leaderboard identifier for active platform.
        /// </summary>
        public string GetPlatformIdForActivePlatform()
        {
            return m_platformIdOverrides.GetConstantForActivePlatform(m_platformId);
        }

        #endregion
    }
}