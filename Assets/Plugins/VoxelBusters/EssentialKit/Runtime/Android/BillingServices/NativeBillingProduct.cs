#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProduct : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProduct(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProduct(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProduct()
        {
            DebugLogger.Log("Disposing NativeBillingProduct");
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

        public string GetIdentifier()
        {
            return Call<string>(Native.Method.kGetIdentifier);
        }
        public string GetLocalizedDescription()
        {
            return Call<string>(Native.Method.kGetLocalizedDescription);
        }
        public string GetLocalizedTitle()
        {
            return Call<string>(Native.Method.kGetLocalizedTitle);
        }
        public NativeList<NativeBillingProductOffer> GetOffers()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetOffers);
            NativeList<NativeBillingProductOffer> data  = new  NativeList<NativeBillingProductOffer>(nativeObj);
            return data;
        }
        public NativeBillingPrice GetPrice()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPrice);
            NativeBillingPrice data  = new  NativeBillingPrice(nativeObj);
            return data;
        }
        public NativeBillingProductSubscriptionInfo GetSubscriptionInfo()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetSubscriptionInfo);
            NativeBillingProductSubscriptionInfo data  = new  NativeBillingProductSubscriptionInfo(nativeObj);
            return data;
        }
        public bool IsAvailable()
        {
            return Call<bool>(Native.Method.kIsAvailable);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProduct";

            internal class Method
            {
                internal const string kGetPrice = "getPrice";
                internal const string kGetIdentifier = "getIdentifier";
                internal const string kGetSubscriptionInfo = "getSubscriptionInfo";
                internal const string kIsAvailable = "isAvailable";
                internal const string kGetOffers = "getOffers";
                internal const string kGetLocalizedDescription = "getLocalizedDescription";
                internal const string kGetLocalizedTitle = "getLocalizedTitle";
            }

        }
    }
}
#endif