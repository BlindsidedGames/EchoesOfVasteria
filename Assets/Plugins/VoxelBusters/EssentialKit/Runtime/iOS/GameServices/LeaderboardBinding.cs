#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal static class LeaderboardBinding
    {
        [DllImport("__Internal")]
        public static extern void NPLeaderboardRegisterCallbacks(GameServicesLoadArrayNativeCallback loadLeaderboardsCallback, GameServicesLoadScoresNativeCallback loadScoresCallback, GameServicesLoadImageNativeCallback loadImageCallback, GameServicesReportNativeCallback reportNativeCallback);

        [DllImport("__Internal")]
        public static extern void NPLeaderboardLoadLeaderboards(string[] leaderboardIds, int length, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern IntPtr NPLeaderboardCreate(string id);

        [DllImport("__Internal")]
        public static extern string NPLeaderboardGetId(IntPtr leaderboardPtr);

        [DllImport("__Internal")]
        public static extern string NPLeaderboardGetTitle(IntPtr leaderboardPtr);

        [DllImport("__Internal")]
        public static extern void NPLeaderboardLoadScores(IntPtr leaderboardPtr, GKLeaderboardPlayerScope playerScope, GKLeaderboardTimeScope timeScope, long startIndex, int count, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern void NPLeaderboardLoadImage(IntPtr leaderboardPtr, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern void NPLeaderboardShowView(string leaderboardID, GKLeaderboardTimeScope timeScope, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern void NPLeaderboardReportScore(IntPtr leaderboardPtr, long score, string context, IntPtr tagPtr);
    }
}
#endif