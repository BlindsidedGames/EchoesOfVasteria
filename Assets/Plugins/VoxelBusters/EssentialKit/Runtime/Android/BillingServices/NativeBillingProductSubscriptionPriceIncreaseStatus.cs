#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductSubscriptionPriceIncreaseStatus
    {
        Unknown = 0,
        NoIncreasePending = 1,
        Agreed = 2,
        Pending = 3
    }
    public class NativeBillingProductSubscriptionPriceIncreaseStatusHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductSubscriptionPriceIncreaseStatus";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductSubscriptionPriceIncreaseStatus value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductSubscriptionPriceIncreaseStatusHelper : NativeBillingProductSubscriptionPriceIncreaseStatusHelper][Method(CreateWithValue) : NativeBillingProductSubscriptionPriceIncreaseStatus]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductSubscriptionPriceIncreaseStatus ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductSubscriptionPriceIncreaseStatus)value.Call<int>("ordinal");
        }
    }
}
#endif