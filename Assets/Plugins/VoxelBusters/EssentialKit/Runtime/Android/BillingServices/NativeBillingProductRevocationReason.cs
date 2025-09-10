#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductRevocationReason
    {
        None = 0,
        Unknown = 1,
        DeveloperIssue = 2
    }
    public class NativeBillingProductRevocationReasonHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductRevocationReason";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductRevocationReason value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductRevocationReasonHelper : NativeBillingProductRevocationReasonHelper][Method(CreateWithValue) : NativeBillingProductRevocationReason]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductRevocationReason ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductRevocationReason)value.Call<int>("ordinal");
        }
    }
}
#endif