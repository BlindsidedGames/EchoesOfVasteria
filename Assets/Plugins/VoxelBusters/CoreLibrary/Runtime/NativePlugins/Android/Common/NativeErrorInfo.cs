#if UNITY_ANDROID
using UnityEngine;

namespace VoxelBusters.CoreLibrary.NativePlugins.Android
{
    public class NativeErrorInfo : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeErrorInfo(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeErrorInfo(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeErrorInfo(int code, string description) : base(Native.kClassName ,code, description)
        {
        }
        public NativeErrorInfo(string description) : base(Native.kClassName ,description)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeErrorInfo()
        {
            DebugLogger.Log("Disposing NativeErrorInfo");
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

        public int GetCode()
        {
            return Call<int>(Native.Method.kGetCode);
        }
        public string GetDescription()
        {
            return Call<string>(Native.Method.kGetDescription);
        }
        public override string ToString()
        {
            return Call<string>(Native.Method.kToString);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.utilities.common.ErrorInfo";

            internal class Method
            {
                internal const string kToString = "toString";
                internal const string kGetCode = "getCode";
                internal const string kGetDescription = "getDescription";
            }

        }
    }
}
#endif