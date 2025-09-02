#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingPrice : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingPrice(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingPrice(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingPrice(double price, string currencyCode, string currencySymbol, string localizedDisplay) : base(Native.kClassName ,(object)price, (object)currencyCode, (object)currencySymbol, (object)localizedDisplay)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingPrice()
        {
            DebugLogger.Log("Disposing NativeBillingPrice");
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

        public string GetCurrencyCode()
        {
            return Call<string>(Native.Method.kGetCurrencyCode);
        }
        public string GetCurrencySymbol()
        {
            return Call<string>(Native.Method.kGetCurrencySymbol);
        }
        public string GetLocalizedDisplay()
        {
            return Call<string>(Native.Method.kGetLocalizedDisplay);
        }
        public double GetPrice()
        {
            return Call<double>(Native.Method.kGetPrice);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingPrice";

            internal class Method
            {
                internal const string kGetPrice = "getPrice";
                internal const string kGetLocalizedDisplay = "getLocalizedDisplay";
                internal const string kGetCurrencyCode = "getCurrencyCode";
                internal const string kGetCurrencySymbol = "getCurrencySymbol";
            }

        }
    }
}
#endif