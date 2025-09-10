#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProductRevocationInfo : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProductRevocationInfo(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProductRevocationInfo(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingProductRevocationInfo(NativeDate date, NativeBillingProductRevocationReason reason) : base(Native.kClassName ,(object)date.NativeObject, (object)reason)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProductRevocationInfo()
        {
            DebugLogger.Log("Disposing NativeBillingProductRevocationInfo");
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

        public NativeDate GetDate()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetDate);
            NativeDate data  = new  NativeDate(nativeObj);
            return data;
        }
        public NativeBillingProductRevocationReason GetReason()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetReason);
            NativeBillingProductRevocationReason data  = NativeBillingProductRevocationReasonHelper.ReadFromValue(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductRevocationInfo";

            internal class Method
            {
                internal const string kGetDate = "getDate";
                internal const string kGetReason = "getReason";
            }

        }
    }
}
#endif