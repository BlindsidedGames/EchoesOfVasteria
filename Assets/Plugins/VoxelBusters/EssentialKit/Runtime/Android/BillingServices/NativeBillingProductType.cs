#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingProductType
    {
        Consumable = 0,
        NonConsumable = 1,
        Subscription = 2
    }
    public class NativeBillingProductTypeHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductType";

        public static AndroidJavaObject CreateWithValue(NativeBillingProductType value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingProductTypeHelper : NativeBillingProductTypeHelper][Method(CreateWithValue) : NativeBillingProductType]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingProductType ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingProductType)value.Call<int>("ordinal");
        }
    }
}
#endif