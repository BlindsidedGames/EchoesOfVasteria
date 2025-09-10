using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

using SPlayer = VoxelBusters.EssentialKit.GameServicesCore.Simulator.Player;

namespace VoxelBusters.EssentialKit.GameServicesCore.Simulator
{
    internal sealed class LeaderboardScore : LeaderboardScoreBase
    {
        #region Fields

        private     string          m_playerId; 

        private     long            m_rank; 

        private     long            m_value;

        private     string          m_tag;

        #endregion

        #region Constructors

        public LeaderboardScore(string leaderboardId, string leaderboardPlatformId) 
            : base(leaderboardId, leaderboardPlatformId)
        {
            // set default values
            m_rank          = 0;
            m_value         = 0;
        }

        public LeaderboardScore(string leaderboardPlatformId, string playerId, long rank, long value, string tag) 
            : base(leaderboardPlatformId)
        {
            // set properties
            m_playerId      = playerId; 
            m_rank          = rank;
            m_value         = value;
            m_tag           = tag;
        }

        #endregion

        #region Create methods

        internal static LeaderboardScore CreateScoreFromData(ScoreData data)
        {
            return (data != null) ? new LeaderboardScore(data.LeaderboardPlatformId, data.PlayerId, 1, data.Value, data.Tag) : null; 
        }

        #endregion

        #region Base class methods

        protected override IPlayer GetPlayerInternal()
        {
            // create player object using data
            var     data    = GameServicesSimulator.Instance.FindPlayerWithId(m_playerId);
            return SPlayer.CreatePlayerFromData(data);
        }

        protected override long GetRankInternal()
        {
            return m_rank;
        }

        protected override long GetValueInternal()
        {
            return m_value;
        }

        protected override DateTime GetLastReportedDateInternal()
        {
            return default(DateTime);
        }

        protected override string GetTagInternal()
        {
            return m_tag;
        }

        #endregion
    }
}