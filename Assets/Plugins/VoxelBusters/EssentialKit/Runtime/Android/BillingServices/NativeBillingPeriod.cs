#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingPeriod : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingPeriod(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingPeriod(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingPeriod(string isoPeriod) : base(Native.kClassName ,(object)isoPeriod)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingPeriod()
        {
            DebugLogger.Log("Disposing NativeBillingPeriod");
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

        public float GetDuration()
        {
            return Call<float>(Native.Method.kGetDuration);
        }
        public NativeBillingPeriodUnit GetUnit()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetUnit);
            NativeBillingPeriodUnit data  = NativeBillingPeriodUnitHelper.ReadFromValue(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingPeriod";

            internal class Method
            {
                internal const string kGetDuration = "getDuration";
                internal const string kGetUnit = "getUnit";
            }

        }
    }
}
#endif