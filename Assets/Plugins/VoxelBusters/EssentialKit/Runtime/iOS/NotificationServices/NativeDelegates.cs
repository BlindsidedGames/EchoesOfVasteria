#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.iOS
{
    internal delegate void RequestAuthorizationNativeCallback(UNAuthorizationStatus status, NativeError error, IntPtr tagPtr);

    internal delegate void GetSettingsNativeCallback(ref UNNotificationSettingsData settings, IntPtr tagPtr);

    internal delegate void ScheduleNotificationNativeCallback(NativeError error, IntPtr tagPtr);

    internal delegate void GetScheduledNotificationsNativeCallback(ref NativeArray arrayPtr, NativeError error, IntPtr tagPtr);

    internal delegate void GetDeliveredNotificationsNativeCallback(ref NativeArray arrayPtr, NativeError error, IntPtr tagPtr);

    internal delegate void RegisterForRemoteNotificationsNativeCallback(string deviceToken, NativeError error, IntPtr tagPtr);

    internal delegate void NotificationReceivedNativeCallback(IntPtr nativePtr, bool isLaunchNotification);
}
#endif