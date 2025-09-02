#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductOfferPaymentMode
    {
        Unknown = 0,
        FreeTrial = 1,
        PayAsYouGo = 2,
        PayUpFront = 3
    }
    public class NativeBillingProductOfferPaymentModeHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductOfferPaymentMode";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductOfferPaymentMode value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductOfferPaymentModeHelper : NativeBillingProductOfferPaymentModeHelper][Method(CreateWithValue) : NativeBillingProductOfferPaymentMode]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductOfferPaymentMode ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductOfferPaymentMode)value.Call<int>("ordinal");
        }
    }
}
#endif