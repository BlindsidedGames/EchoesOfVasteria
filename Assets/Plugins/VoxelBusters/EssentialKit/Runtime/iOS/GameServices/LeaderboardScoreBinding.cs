#if UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal static class LeaderboardScoreBinding
    {
        [DllImport("__Internal")]
        public static extern  long NPLeaderboardScoreEntryGetRank(IntPtr entryPtr);

        [DllImport("__Internal")]
        public static extern  long NPLeaderboardScoreEntryGetValue(IntPtr entryPtr);

        [DllImport("__Internal")]
        public static extern  string NPLeaderboardScoreEntryGetLastReportedDate(IntPtr entryPtr);

        [DllImport("__Internal")]
        public static extern  IntPtr NPLeaderboardScoreEntryGetPlayer(IntPtr entryPtr);

        [DllImport("__Internal")]
        public static extern  string NPLeaderboardScoreEntryGetTag(IntPtr entryPtr);
    }
}
#endif