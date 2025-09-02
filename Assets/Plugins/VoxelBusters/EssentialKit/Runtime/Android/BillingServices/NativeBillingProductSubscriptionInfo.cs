#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingProductSubscriptionInfo : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingProductSubscriptionInfo(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingProductSubscriptionInfo(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBillingProductSubscriptionInfo(string groupId, string localizedGroupTitle, int level, NativeBillingPeriod period) : base(Native.kClassName ,(object)groupId, (object)localizedGroupTitle, (object)level, (object)period.NativeObject)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingProductSubscriptionInfo()
        {
            DebugLogger.Log("Disposing NativeBillingProductSubscriptionInfo");
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

        public string GetGroupId()
        {
            return Call<string>(Native.Method.kGetGroupId);
        }
        public int GetLevel()
        {
            return Call<int>(Native.Method.kGetLevel);
        }
        public string GetLocalizedGroupTitle()
        {
            return Call<string>(Native.Method.kGetLocalizedGroupTitle);
        }
        public NativeBillingPeriod GetPeriod()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetPeriod);
            NativeBillingPeriod data  = new  NativeBillingPeriod(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingProductSubscriptionInfo";

            internal class Method
            {
                internal const string kGetLevel = "getLevel";
                internal const string kGetLocalizedGroupTitle = "getLocalizedGroupTitle";
                internal const string kGetGroupId = "getGroupId";
                internal const string kGetPeriod = "getPeriod";
            }

        }
    }
}
#endif