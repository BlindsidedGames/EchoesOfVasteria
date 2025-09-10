#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.Android
{
    public class NativeUpdateInfo : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public NativeUpdateStatus Status
        {
            get
            {
                return NativeUpdateStatusHelper.ReadFromValue(Get<AndroidJavaObject>("status"));
            }

            set
            {
                Set<AndroidJavaObject>("status", NativeUpdateStatusHelper.CreateWithValue(value));
            }
        }


        public int BuildTag
        {
            get
            {
                return Get<int>("buildTag");
            }

            set
            {
                Set<int>("buildTag", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativeUpdateInfo(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeUpdateInfo(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeUpdateInfo() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeUpdateInfo()
        {
            DebugLogger.Log("Disposing NativeUpdateInfo");
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

        public override string ToString()
        {
            return Call<string>(Native.Method.kToString);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.appupdater.UpdateInfo";

            internal class Method
            {
                internal const string kToString = "toString";
            }

        }
    }
}
#endif