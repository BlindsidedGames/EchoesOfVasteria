#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductSubscriptionRenewalState
    {
        Unknown = 0,
        Subscribed = 1,
        Expired = 2,
        InBillingRetryPeriod = 3,
        InGracePeriod = 4,
        Revoked = 5
    }
    public class NativeBillingProductSubscriptionRenewalStateHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductSubscriptionRenewalState";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductSubscriptionRenewalState value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductSubscriptionRenewalStateHelper : NativeBillingProductSubscriptionRenewalStateHelper][Method(CreateWithValue) : NativeBillingProductSubscriptionRenewalState]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductSubscriptionRenewalState ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductSubscriptionRenewalState)value.Call<int>("ordinal");
        }
    }
}
#endif