#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeBillingTransaction : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeBillingTransaction(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBillingTransaction(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBillingTransaction()
        {
            DebugLogger.Log("Disposing NativeBillingTransaction");
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
        public NativeBillingEnvironment GetEnvironment()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetEnvironment);
            NativeBillingEnvironment data  = NativeBillingEnvironmentHelper.ReadFromValue(nativeObj);
            return data;
        }
        public int GetErrorCode()
        {
            return Call<int>(Native.Method.kGetErrorCode);
        }
        public string GetErrorDescription()
        {
            return Call<string>(Native.Method.kGetErrorDescription);
        }
        public string GetId()
        {
            return Call<string>(Native.Method.kGetId);
        }
        public string GetProductIdentifier()
        {
            return Call<string>(Native.Method.kGetProductIdentifier);
        }
        public NativeBillingProductType GetProductType()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetProductType);
            NativeBillingProductType data  = NativeBillingProductTypeHelper.ReadFromValue(nativeObj);
            return data;
        }
        public string GetPurchaseTag()
        {
            return Call<string>(Native.Method.kGetPurchaseTag);
        }
        public int GetPurchasedQuantity()
        {
            return Call<int>(Native.Method.kGetPurchasedQuantity);
        }
        public string GetRawData()
        {
            return Call<string>(Native.Method.kGetRawData);
        }
        public string GetReceipt()
        {
            return Call<string>(Native.Method.kGetReceipt);
        }
        public int GetRequestedQuantity()
        {
            return Call<int>(Native.Method.kGetRequestedQuantity);
        }
        public NativeBillingProductRevocationInfo GetRevocationInfo()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetRevocationInfo);
            NativeBillingProductRevocationInfo data  = new  NativeBillingProductRevocationInfo(nativeObj);
            return data;
        }
        public NativeBillingTransactionState GetState()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetState);
            NativeBillingTransactionState data  = NativeBillingTransactionStateHelper.ReadFromValue(nativeObj);
            return data;
        }
        public NativeBillingProductSubscriptionStatus GetSubscriptionStatus()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetSubscriptionStatus);
            NativeBillingProductSubscriptionStatus data  = new  NativeBillingProductSubscriptionStatus(nativeObj);
            return data;
        }
        public NativeBillingTransactionVerificationState GetVerificationState()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetVerificationState);
            NativeBillingTransactionVerificationState data  = NativeBillingTransactionVerificationStateHelper.ReadFromValue(nativeObj);
            return data;
        }
        public void SetVerificationState(NativeBillingTransactionVerificationState verificationState)
        {
            Call(Native.Method.kSetVerificationState, NativeBillingTransactionVerificationStateHelper.CreateWithValue(verificationState));
        }
        public override string ToString()
        {
            return Call<string>(Native.Method.kToString);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.common.BillingTransaction";

            internal class Method
            {
                internal const string kToString = "toString";
                internal const string kGetState = "getState";
                internal const string kGetErrorCode = "getErrorCode";
                internal const string kSetVerificationState = "setVerificationState";
                internal const string kGetPurchasedQuantity = "getPurchasedQuantity";
                internal const string kGetRequestedQuantity = "getRequestedQuantity";
                internal const string kGetProductIdentifier = "getProductIdentifier";
                internal const string kGetVerificationState = "getVerificationState";
                internal const string kGetErrorDescription = "getErrorDescription";
                internal const string kGetDate = "getDate";
                internal const string kGetSubscriptionStatus = "getSubscriptionStatus";
                internal const string kGetReceipt = "getReceipt";
                internal const string kGetRawData = "getRawData";
                internal const string kGetProductType = "getProductType";
                internal const string kGetEnvironment = "getEnvironment";
                internal const string kGetPurchaseTag = "getPurchaseTag";
                internal const string kGetRevocationInfo = "getRevocationInfo";
                internal const string kGetId = "getId";
            }

        }
    }
}
#endif