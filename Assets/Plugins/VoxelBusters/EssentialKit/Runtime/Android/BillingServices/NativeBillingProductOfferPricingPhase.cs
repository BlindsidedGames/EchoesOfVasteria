#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProductOfferPricingPhase : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProductOfferPricingPhase(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProductOfferPricingPhase(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingProductOfferPricingPhase(NativeBillingPrice price, NativeBillingProductOfferPaymentMode paymentMode, NativeBillingPeriod period, int repeatCount) : base(Native.kClassName ,(object)price.NativeObject, (object)paymentMode, (object)period.NativeObject, (object)repeatCount)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProductOfferPricingPhase()
        {
            DebugLogger.Log("Disposing NativeBillingProductOfferPricingPhase");
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

        public NativeBillingProductOfferPaymentMode GetPaymentMode()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPaymentMode);
            NativeBillingProductOfferPaymentMode data  = NativeBillingProductOfferPaymentModeHelper.ReadFromValue(nativeObj);
            return data;
        }
        public NativeBillingPeriod GetPeriod()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPeriod);
            NativeBillingPeriod data  = new  NativeBillingPeriod(nativeObj);
            return data;
        }
        public NativeBillingPrice GetPrice()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPrice);
            NativeBillingPrice data  = new  NativeBillingPrice(nativeObj);
            return data;
        }
        public int GetRepeatCount()
        {
            return Call<int>(Native.Method.kGetRepeatCount);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductOfferPricingPhase";

            internal class Method
            {
                internal const string kGetPrice = "getPrice";
                internal const string kGetPeriod = "getPeriod";
                internal const string kGetPaymentMode = "getPaymentMode";
                internal const string kGetRepeatCount = "getRepeatCount";
            }

        }
    }
}
#endif