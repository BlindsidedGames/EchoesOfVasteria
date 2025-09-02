#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.MediaServicesCore.Android
{
    public enum NativeMediaCaptureType
    {
        Image = 0,
        Video = 1
    }
    public class NativeMediaCaptureTypeHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.mediaservices.MediaCaptureType";

        public static AndroidJavaObject CreateWithValue(NativeMediaCaptureType value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeMediaCaptureTypeHelper : NativeMediaCaptureTypeHelper][Method(CreateWithValue) : NativeMediaCaptureType]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeMediaCaptureType ReadFromValue(AndroidJavaObject value)
        {
            return (NativeMediaCaptureType)value.Call<int>("ordinal");
        }
    }
}
#endif