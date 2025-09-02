#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductOfferCategory
    {
        Unknown = 0,
        Introductory = 1,
        Promotional = 2,
        Code = 3
    }
    public class NativeBillingProductOfferCategoryHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductOfferCategory";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductOfferCategory value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductOfferCategoryHelper : NativeBillingProductOfferCategoryHelper][Method(CreateWithValue) : NativeBillingProductOfferCategory]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductOfferCategory ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductOfferCategory)value.Call<int>("ordinal");
        }
    }
}
#endif