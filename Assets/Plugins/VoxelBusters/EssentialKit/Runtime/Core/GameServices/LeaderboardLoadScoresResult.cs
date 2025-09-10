using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when load scores operation is completed.
    /// </summary>
    /// @ingroup GameServices
    public class LeaderboardLoadScoresResult
    {
        #region Properties

        /// <summary>
        /// An array of score values.
        /// </summary>
        public ILeaderboardScore[] Scores { get; private set; }

        /// <summary>
        /// The current player's score information.
        /// </summary>
        public ILeaderboardScore LocalPlayerScore { get; private set; }

        #endregion

        #region Constructors

        public LeaderboardLoadScoresResult(ILeaderboardScore[] scores, ILeaderboardScore localPlayerScore)
        {
            // Set properties
            Scores  = scores;
            LocalPlayerScore = localPlayerScore;
        }

        #endregion
    }
}