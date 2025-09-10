#if UNITY_ANDROID
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public enum NativeBillingTransactionState
    {
        Unknown = 0,
        Purchasing = 1,
        Purchased = 2,
        Failed = 3,
        Restored = 4,
        Deferred = 5,
        Refunded = 6
    }
    public class NativeBillingTransactionStateHelper
    {
        internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingTransactionState";

        public static AndroidJavaObject CreateWithValue(NativeBillingTransactionState value)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[NativeBillingTransactionStateHelper : NativeBillingTransactionStateHelper][Method(CreateWithValue) : NativeBillingTransactionState]");
#endif
            AndroidJavaClass javaClass = new AndroidJavaClass(kClassName);
            AndroidJavaObject[] values = javaClass.CallStatic<AndroidJavaObject[]>("values");
            return values[(int)value];
        }

        public static NativeBillingTransactionState ReadFromValue(AndroidJavaObject value)
        {
            return (NativeBillingTransactionState)value.Call<int>("ordinal");
        }
    }
}
#endif