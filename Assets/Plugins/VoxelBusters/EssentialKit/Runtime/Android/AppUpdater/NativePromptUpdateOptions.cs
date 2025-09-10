#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.Android
{
    public class NativePromptUpdateOptions : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public bool IsForceUpdate
        {
            get
            {
                return Get<bool>("isForceUpdate");
            }

            set
            {
                Set<bool>("isForceUpdate", value);
            }
        }


        public string Title
        {
            get
            {
                return Get<string>("title");
            }

            set
            {
                Set<string>("title", value);
            }
        }


        public string Message
        {
            get
            {
                return Get<string>("message");
            }

            set
            {
                Set<string>("message", value);
            }
        }


        public bool AllowInstallationIfDownloaded
        {
            get
            {
                return Get<bool>("allowInstallationIfDownloaded");
            }

            set
            {
                Set<bool>("allowInstallationIfDownloaded", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativePromptUpdateOptions(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativePromptUpdateOptions(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativePromptUpdateOptions() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativePromptUpdateOptions()
        {
            DebugLogger.Log("Disposing NativePromptUpdateOptions");
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

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.appupdater.PromptUpdateOptions";

            internal class Method
            {
            }

        }
    }
}
#endif