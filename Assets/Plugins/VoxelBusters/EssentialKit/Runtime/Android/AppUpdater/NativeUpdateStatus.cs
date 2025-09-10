#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.Android
{
    public enum NativeUpdateStatus
    {
        Unknown = 0,
        UpdateAvailable = 1,
        UpdateNotAvailable = 2,
        UpdateInProgress = 3,
        UpdateDownloaded = 4
    }
    public class NativeUpdateStatusHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.appupdater.UpdateStatus";

        public static AndroidJavaObject CreateWithValue(NativeUpdateStatus value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeUpdateStatusHelper : NativeUpdateStatusHelper][Method(CreateWithValue) : NativeUpdateStatus]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeUpdateStatus ReadFromValue(AndroidJavaObject value)
        {
            return (NativeUpdateStatus)value.Call<int>("ordinal");
        }
    }
}
#endif