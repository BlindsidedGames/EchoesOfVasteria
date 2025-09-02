#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingEnvironment
    {
        Unknown = 0,
        Production = 1,
        Sandbox = 2,
        Local = 3
    }
    public class NativeBillingEnvironmentHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingEnvironment";

        public static AndroidJavaObject CreateWithValue(NativeBillingEnvironment value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingEnvironmentHelper : NativeBillingEnvironmentHelper][Method(CreateWithValue) : NativeBillingEnvironment]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingEnvironment ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingEnvironment)value.Call<int>("ordinal");
        }
    }
}
#endif