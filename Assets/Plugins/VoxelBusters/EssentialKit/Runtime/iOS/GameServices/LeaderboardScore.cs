#if UNITY_IOS || UNITY_TVOS
using System;
  using VoxelBusters.CoreLibrary.NativePlugins.iOS;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal sealed class LeaderboardScore : LeaderboardScoreBase
    {
        #region Constructors

        static LeaderboardScore()
        {
        }

        public LeaderboardScore(string leaderboardId, string leaderboardPlatformId, IntPtr nativePtr)
            : base(leaderboardId, leaderboardPlatformId)
        {            
            // set properties
            NativeObjectRef     = new IosNativeObjectRef(nativePtr);
        }

        ~LeaderboardScore()
        {
            Dispose(false);
        }

        #endregion

        #region Base class methods

        protected override IPlayer GetPlayerInternal()
        {
            var     playerPtr   = LeaderboardScoreBinding.NPLeaderboardScoreEntryGetPlayer(AddrOfNativeObject());
            return new Player(playerPtr, false);
        }

        protected override long GetRankInternal()
        {
            return LeaderboardScoreBinding.NPLeaderboardScoreEntryGetRank(AddrOfNativeObject());
        }

        protected override long GetValueInternal()
        {
            return LeaderboardScoreBinding.NPLeaderboardScoreEntryGetValue(AddrOfNativeObject());
        }

        protected override DateTime GetLastReportedDateInternal()
        {
            string  dateStr     = LeaderboardScoreBinding.NPLeaderboardScoreEntryGetLastReportedDate(AddrOfNativeObject());
            return IosNativePluginsUtility.ParseDateTimeStringInUTCFormat(dateStr);
        }

        protected override string GetTagInternal()
        {
            return LeaderboardScoreBinding.NPLeaderboardScoreEntryGetTag(AddrOfNativeObject());
        }

        #endregion
    }
}
#endif