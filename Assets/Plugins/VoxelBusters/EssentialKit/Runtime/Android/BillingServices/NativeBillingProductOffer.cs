#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProductOffer : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProductOffer(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProductOffer(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingProductOffer(string id, NativeBillingProductOfferCategory category, NativeList<NativeBillingProductOfferPricingPhase> pricingPhases) : base(Native.kClassName ,(object)id, (object)category, (object)pricingPhases)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProductOffer()
        {
            DebugLogger.Log("Disposing NativeBillingProductOffer");
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

        public NativeBillingProductOfferCategory GetCategory()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetCategory);
            NativeBillingProductOfferCategory data  = NativeBillingProductOfferCategoryHelper.ReadFromValue(nativeObj);
            return data;
        }
        public string GetId()
        {
            return Call<string>(Native.Method.kGetId);
        }
        public NativeList<NativeBillingProductOfferPricingPhase> GetPricingPhases()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPricingPhases);
            NativeList<NativeBillingProductOfferPricingPhase> data  = new  NativeList<NativeBillingProductOfferPricingPhase>(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductOffer";

            internal class Method
            {
                internal const string kGetCategory = "getCategory";
                internal const string kGetPricingPhases = "getPricingPhases";
                internal const string kGetId = "getId";
            }

        }
    }
}
#endif