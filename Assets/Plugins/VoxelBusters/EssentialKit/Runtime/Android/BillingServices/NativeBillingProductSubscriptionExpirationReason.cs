#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductSubscriptionExpirationReason
    {
        None = 0,
        Unknown = 1,
        AutoRenewDisabled = 2,
        BillingError = 3,
        DidNotConsentToPriceIncrease = 4,
        ProductUnavailable = 5
    }
    public class NativeBillingProductSubscriptionExpirationReasonHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductSubscriptionExpirationReason";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductSubscriptionExpirationReason value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductSubscriptionExpirationReasonHelper : NativeBillingProductSubscriptionExpirationReasonHelper][Method(CreateWithValue) : NativeBillingProductSubscriptionExpirationReason]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductSubscriptionExpirationReason ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductSubscriptionExpirationReason)value.Call<int>("ordinal");
        }
    }
}
#endif