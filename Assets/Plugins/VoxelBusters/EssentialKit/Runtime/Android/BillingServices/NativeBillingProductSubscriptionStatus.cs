#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProductSubscriptionStatus : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProductSubscriptionStatus(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProductSubscriptionStatus(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingProductSubscriptionStatus(string groupId, NativeBillingProductSubscriptionRenewalInfo renewalInfo, NativeDate expirationDate, bool isUpgraded, string appliedOfferId, NativeBillingProductOfferCategory offerCategory) : base(Native.kClassName ,(object)groupId, (object)renewalInfo.NativeObject, (object)expirationDate.NativeObject, (object)isUpgraded, (object)appliedOfferId, (object)offerCategory)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProductSubscriptionStatus()
        {
            DebugLogger.Log("Disposing NativeBillingProductSubscriptionStatus");
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

        public NativeBillingProductOfferCategory GetAppliedOfferCategory()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetAppliedOfferCategory);
            NativeBillingProductOfferCategory data  = NativeBillingProductOfferCategoryHelper.ReadFromValue(nativeObj);
            return data;
        }
        public string GetAppliedOfferId()
        {
            return Call<string>(Native.Method.kGetAppliedOfferId);
        }
        public NativeDate GetExpirationDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetExpirationDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public string GetGroupId()
        {
            return Call<string>(Native.Method.kGetGroupId);
        }
        public NativeBillingProductSubscriptionRenewalInfo GetRenewalInfo()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetRenewalInfo);
            NativeBillingProductSubscriptionRenewalInfo data  = new  NativeBillingProductSubscriptionRenewalInfo(nativeObj);
            return data;
        }
        public bool GetUpgraded()
        {
            return Call<bool>(Native.Method.kGetUpgraded);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductSubscriptionStatus";

            internal class Method
            {
                internal const string kGetUpgraded = "getUpgraded";
                internal const string kGetGroupId = "getGroupId";
                internal const string kGetAppliedOfferCategory = "getAppliedOfferCategory";
                internal const string kGetRenewalInfo = "getRenewalInfo";
                internal const string kGetAppliedOfferId = "getAppliedOfferId";
                internal const string kGetExpirationDate = "getExpirationDate";
            }

        }
    }
}
#endif