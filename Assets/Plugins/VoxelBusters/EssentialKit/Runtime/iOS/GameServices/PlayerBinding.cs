#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal static class PlayerBinding
    {
        [DllImport("__Internal")]
        public static extern void NPPlayerRegisterCallbacks(GameServicesLoadArrayNativeCallback loadPlayersCallback, GameServicesLoadImageNativeCallback loadPlayerImageCallback, GameServicesViewClosedNativeCallback friendProfileViewClosedCallback);

        [DllImport("__Internal")]
        public static extern void NPPlayerLoadPlayers(string[] playerIds, int count, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern void NPLocalPlayerLoadFriends(IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern void NPLocalPlayerShowAddFriendRequestView(string requestedPlayerId, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern string NPPlayerGetId(IntPtr playerPtr);

        [DllImport("__Internal")]
        public static extern string NPPlayerGetDeveloperScopeId(IntPtr playerPtr);

        [DllImport("__Internal")]
        public static extern string NPPlayerGetLegacyId(IntPtr playerPtr);

        [DllImport("__Internal")]
        public static extern string NPPlayerGetAlias(IntPtr playerPtr);

        [DllImport("__Internal")]
        public static extern string NPPlayerGetDisplayName(IntPtr playerPtr);

        [DllImport("__Internal")]
        public static extern void NPPlayerLoadImage(IntPtr playerPtr, IntPtr tagPtr);

        [DllImport("__Internal")]
        public static extern void NPLocalPlayerRegisterCallbacks(GameServicesAuthStateChangeNativeCallback authChangeCallback);

        [DllImport("__Internal")]
        public static extern IntPtr NPLocalPlayerGetLocalPlayer();

        [DllImport("__Internal")]
        public static extern void NPLocalPlayerAuthenticate(bool interactive);

        [DllImport("__Internal")]
        public static extern bool NPLocalPlayerIsAuthenticated();

        [DllImport("__Internal")]
        public static extern bool NPLocalPlayerIsUnderage();
    }
}
#endif