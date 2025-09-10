#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.TaskServicesCore.Android
{
    public class NativeBackgroundTaskOptions : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public bool ExecuteOnCallingThread
        {
            get
            {
                return Get<bool>("executeOnCallingThread");
            }

            set
            {
                Set<bool>("executeOnCallingThread", value);
            }
        }


        public float Delay
        {
            get
            {
                return Get<float>("delay");
            }

            set
            {
                Set<float>("delay", value);
            }
        }


        public float RepeatUntilCancelledWithInterval
        {
            get
            {
                return Get<float>("repeatUntilCancelledWithInterval");
            }

            set
            {
                Set<float>("repeatUntilCancelledWithInterval", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativeBackgroundTaskOptions(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeBackgroundTaskOptions(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeBackgroundTaskOptions() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeBackgroundTaskOptions()
        {
            DebugLogger.Log("Disposing NativeBackgroundTaskOptions");
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
            internal const string kClassName = "com.voxelbusters.essentialkit.taskservices.BackgroundTaskOptions";

            internal class Method
            {
            }

        }
    }
}
#endif