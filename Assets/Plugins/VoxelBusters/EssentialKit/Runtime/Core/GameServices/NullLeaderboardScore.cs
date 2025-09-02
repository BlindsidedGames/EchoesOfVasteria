using System;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore
{
    internal sealed class NullLeaderboardScore : LeaderboardScoreBase
    {
        #region Constructors

        public NullLeaderboardScore(string leaderboardId, string leaderboardPlatformId) : base(leaderboardId, leaderboardPlatformId)
        { }

        #endregion

        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("LeaderboardScore");
        }

        #endregion

        #region Base class methods

        protected override IPlayer GetPlayerInternal()
        {
            LogNotSupported();

            return new NullPlayer();
        }

        protected override long GetRankInternal()
        {
            LogNotSupported();

            return 0;
        }

        protected override long GetValueInternal()
        {
            LogNotSupported();

            return 0;
        }

        protected override DateTime GetLastReportedDateInternal()
        {
            LogNotSupported();

            return default(DateTime);
        }

        protected override string GetTagInternal()
        {
            return null;
        }

        #endregion
    }
}