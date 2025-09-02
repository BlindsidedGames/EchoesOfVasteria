#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public enum NativeNotificationPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Max = 3
    }
    public class NativeNotificationPriorityHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.notificationservices.datatypes.NotificationPriority";

        public static AndroidJavaObject CreateWithValue(NativeNotificationPriority value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeNotificationPriorityHelper : NativeNotificationPriorityHelper][Method(CreateWithValue) : NativeNotificationPriority]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeNotificationPriority ReadFromValue(AndroidJavaObject value)
        {
            return (NativeNotificationPriority)value.Call<int>("ordinal");
        }
    }
}
#endif