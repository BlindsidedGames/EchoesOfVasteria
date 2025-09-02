#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal delegate void GameServicesLoadArrayNativeCallback(ref NativeArray nativeArray, NativeError error, IntPtr tagPtr);

    internal delegate void GameServicesLoadScoresNativeCallback(ref NativeArray nativeArray, IntPtr localPlayerScorePtr, NativeError error, IntPtr tagPtr);

    internal delegate void GameServicesReportNativeCallback(NativeError error, IntPtr tagPtr);

    internal delegate void GameServicesAuthStateChangeNativeCallback(GKLocalPlayerAuthState state, NativeError error);

    internal delegate void GameServicesLoadImageNativeCallback(IntPtr dataPtr, int dataLength, NativeError error, IntPtr tagPtr);

    internal delegate void GameServicesViewClosedNativeCallback(NativeError error, IntPtr tagPtr);

    internal delegate void GameServicesLoadServerCredentialsNativeCallback(string publicKeyUrl, IntPtr signaturePtr, int signatureDataLength, IntPtr saltPtr, int saltDataLength, long timestamp, NativeError error, IntPtr tagPtr);
}
#endif