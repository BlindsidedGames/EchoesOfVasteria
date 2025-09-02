using System;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore
{
    public abstract class LeaderboardScoreBase : NativeObjectBase, ILeaderboardScore
    {
        #region Constructors

        protected LeaderboardScoreBase(string leaderboardId, string leaderboardPlatformId)
        {
            // set properties
            LeaderboardId           = leaderboardId;
            LeaderboardPlatformId   = leaderboardPlatformId;
        }

        protected LeaderboardScoreBase(string leaderboardPlatformId)
        {
            var     settings        = GameServices.FindLeaderboardDefinitionWithPlatformId(leaderboardPlatformId);
            Assert.IsFalse(null == settings, "Could not find settings for specified platform id: " + leaderboardPlatformId);
            
            // set properties
            LeaderboardId           = settings.Id;
            LeaderboardPlatformId   = leaderboardPlatformId;
        }

        #endregion

        #region Abstract methods

        protected abstract IPlayer GetPlayerInternal();
        protected abstract long GetRankInternal();
        protected abstract long GetValueInternal();
        protected abstract DateTime GetLastReportedDateInternal();
        protected abstract string GetTagInternal();

        #endregion

        #region Base class methods

        public override string ToString()
        {
            return $"[LeaderboardId={LeaderboardId}, LeaderboardPlatformId={LeaderboardPlatformId}, Player={Player}, Rank={Rank}, Value={Value}, LastReportedDate={LastReportedDate}, Tag={Tag}]";
        }

        #endregion

        #region IGameServicesScore implementation

        public string LeaderboardId { get; internal set; }

        public string LeaderboardPlatformId { get; internal set; }

        public IPlayer Player => GetPlayerInternal();

        public long Rank => GetRankInternal();

        public long Value
        {
            get => GetValueInternal();
        }

        public string FormattedValue => GetValueInternal().ToString();

        public DateTime LastReportedDate => GetLastReportedDateInternal();

        public string Tag
        {
            get => GetTagInternal();
        }

        #endregion
    }
}