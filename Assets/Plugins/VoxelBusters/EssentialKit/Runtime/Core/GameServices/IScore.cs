using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    /// @deprecated Since V3.0.0
    [Obsolete("Use ILeaderboardScore for accessing the score of a leaderboard. For reporting scores, use ILeaderboard.", true)]
    public interface IScore
    {
        #region Properties

        /// <summary>
        /// An unique string used to identify the leaderboard across all the supported platforms. (read-only)
        /// </summary>
        string LeaderboardId { get; }

        /// <summary>
        /// A string used to identify the leaderboard in the current platform. (read-only)
        /// </summary>
        string LeaderboardPlatformId { get; }

        /// <summary>
        /// The player that earned the score. (read-only)
        /// </summary>
        IPlayer Player { get; }

        /// <summary>
        /// The position of the score in leaderboard. (read-only) 
        /// </summary>
        long Rank { get; }

        /// <summary>
        /// The score earned by the player.
        /// </summary>
        long Value { get; set; }

        /// <summary>
        /// The players score as a localized string. (read-only)
        /// </summary>
        string FormattedValue { get; }

        /// <summary>
        /// The date and time when score was reported. (read-only)
        /// </summary>
        DateTime LastReportedDate { get; }

        // value may contain no more than 64 URI-safe characters as defined by section 2.3 of RFC 3986.
        /// <summary>
        /// A tag string associated with the score.
        /// The value may contain no more than 64 URI-safe characters as defined by section 2.3 of RFC 3986.
        /// </summary>
        /// <remarks>
        /// This tag value is used to identify a unique score within the same leaderboard.
        /// You can use this tag to update or retrieve the score in the future.
        /// </remarks>
        string Tag { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Reports the score to game server.
        /// </summary>
        /// <param name="callback">Callback that will be called after operation is completed.</param>
        void ReportScore(CompletionCallback callback);

        #endregion
    }
}