#if UNITY_ANDROID
using System;
using System.Runtime.InteropServices;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    internal sealed class LeaderboardScore : LeaderboardScoreBase
    {
        #region Fields

        private NativeGameLeaderboardScore m_instance;

        #endregion

        #region Constructors

        public LeaderboardScore(string platformId) : base(platformId)
        {

        }

        public LeaderboardScore(NativeGameLeaderboardScore nativeScore) 
            : base(leaderboardPlatformId: nativeScore.GetLeaderboardId())
        {
            m_instance = nativeScore;
            DebugLogger.Log("Score constructor : " + LeaderboardPlatformId + "  " + nativeScore.GetLeaderboardId());
        }

        ~LeaderboardScore()
        {
            Dispose(false);
        }

        #endregion

        #region Base class methods

        protected override IPlayer GetPlayerInternal()
        {
            return new Player(m_instance.GetPlayer());
        }

        protected override long GetRankInternal()
        {
            return m_instance.GetRank();
        }

        protected override long GetValueInternal()
        {
            return m_instance.GetRawScore();
        }

        protected override DateTime GetLastReportedDateInternal()
        {
            return m_instance.GetLastReportedDate().GetDateTime();
        }

        protected override string GetTagInternal()
        {
            return m_instance.GetTag();
        }

        #endregion
    }
}
#endif