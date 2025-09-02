#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProductSubscriptionRenewalInfo : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProductSubscriptionRenewalInfo(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProductSubscriptionRenewalInfo(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingProductSubscriptionRenewalInfo(NativeBillingProductSubscriptionRenewalState state, string applicableOfferIdentifier, NativeBillingProductOfferCategory applicableOfferCategory, NativeDate lastRenewedDate, string lastRenewalId, bool isAutoRenewEnabled, NativeBillingProductSubscriptionExpirationReason expirationReason, NativeDate renewalDate, NativeDate gracePeriodExpirationDate, NativeBillingProductSubscriptionPriceIncreaseStatus priceIncreaseStatus) : base(Native.kClassName ,(object)state, (object)applicableOfferIdentifier, (object)applicableOfferCategory, (object)lastRenewedDate.NativeObject, (object)lastRenewalId, (object)isAutoRenewEnabled, (object)expirationReason, (object)renewalDate.NativeObject, (object)gracePeriodExpirationDate.NativeObject, (object)priceIncreaseStatus)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProductSubscriptionRenewalInfo()
        {
            DebugLogger.Log("Disposing NativeBillingProductSubscriptionRenewalInfo");
        }
#endif
        #endregion
        #region Static methods
        private static AndroidJavaClass GetClass()
        {
            if (m_nativeClass == null)
            {
                m_nativeClass = new AndroidJavaClass(Native.kClassName);
            }
            return m_nativeClass;
        }

        #endregion
        #region Public methods

        public NativeBillingProductOfferCategory GetApplicableOfferCategory()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetApplicableOfferCategory);
            NativeBillingProductOfferCategory data  = NativeBillingProductOfferCategoryHelper.ReadFromValue(nativeObj);
            return data;
        }
        public string GetApplicableOfferIdentifier()
        {
            return Call<string>(Native.Method.kGetApplicableOfferIdentifier);
        }
        public bool GetAutoRenewEnabled()
        {
            return Call<bool>(Native.Method.kGetAutoRenewEnabled);
        }
        public NativeBillingProductSubscriptionExpirationReason GetExpirationReason()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetExpirationReason);
            NativeBillingProductSubscriptionExpirationReason data  = NativeBillingProductSubscriptionExpirationReasonHelper.ReadFromValue(nativeObj);
            return data;
        }
        public NativeDate GetGracePeriodExpirationDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetGracePeriodExpirationDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public string GetLastRenewalId()
        {
            return Call<string>(Native.Method.kGetLastRenewalId);
        }
        public NativeDate GetLastRenewedDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetLastRenewedDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public NativeBillingProductSubscriptionPriceIncreaseStatus GetPriceIncreaseStatus()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPriceIncreaseStatus);
            NativeBillingProductSubscriptionPriceIncreaseStatus data  = NativeBillingProductSubscriptionPriceIncreaseStatusHelper.ReadFromValue(nativeObj);
            return data;
        }
        public NativeDate GetRenewalDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetRenewalDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public NativeBillingProductSubscriptionRenewalState GetState()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetState);
            NativeBillingProductSubscriptionRenewalState data  = NativeBillingProductSubscriptionRenewalStateHelper.ReadFromValue(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductSubscriptionRenewalInfo";

            internal class Method
            {
                internal const string kGetState = "getState";
                internal const string kGetPriceIncreaseStatus = "getPriceIncreaseStatus";
                internal const string kGetAutoRenewEnabled = "getAutoRenewEnabled";
                internal const string kGetExpirationReason = "getExpirationReason";
                internal const string kGetLastRenewedDate = "getLastRenewedDate";
                internal const string kGetRenewalDate = "getRenewalDate";
                internal const string kGetApplicableOfferCategory = "getApplicableOfferCategory";
                internal const string kGetLastRenewalId = "getLastRenewalId";
                internal const string kGetApplicableOfferIdentifier = "getApplicableOfferIdentifier";
                internal const string kGetGracePeriodExpirationDate = "getGracePeriodExpirationDate";
            }

        }
    }
}
#endif