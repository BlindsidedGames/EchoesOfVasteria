#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.GameServicesCore.Android
{
    public enum NativeServerCredentialAdditionalScope
    {
        None = 0,
        Email = 1,
        OpenId = 2,
        Profile = 3
    }
    public class NativeServerCredentialAdditionalScopeHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.gameservices.ServerCredentialAdditionalScope";

        public static AndroidJavaObject CreateWithValue(NativeServerCredentialAdditionalScope value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeServerCredentialAdditionalScopeHelper : NativeServerCredentialAdditionalScopeHelper][Method(CreateWithValue) : NativeServerCredentialAdditionalScope]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeServerCredentialAdditionalScope ReadFromValue(AndroidJavaObject value)
        {
            return (NativeServerCredentialAdditionalScope)value.Call<int>("ordinal");
        }
    }
}
#endif