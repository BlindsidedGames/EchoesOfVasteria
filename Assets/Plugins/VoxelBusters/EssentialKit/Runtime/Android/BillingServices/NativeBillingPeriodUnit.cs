#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingPeriodUnit
    {
        Day = 0,
        Week = 1,
        Month = 2,
        Year = 3
    }
    public class NativeBillingPeriodUnitHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingPeriodUnit";

        public static AndroidJavaObject CreateWithValue(NativeBillingPeriodUnit value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingPeriodUnitHelper : NativeBillingPeriodUnitHelper][Method(CreateWithValue) : NativeBillingPeriodUnit]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingPeriodUnit ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingPeriodUnit)value.Call<int>("ordinal");
        }
    }
}
#endif