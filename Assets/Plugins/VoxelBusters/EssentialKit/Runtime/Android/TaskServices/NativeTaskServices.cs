#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.TaskServicesCore.Android
{
    public class NativeTaskServices : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion

        #region Constructor

        public NativeTaskServices(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
        {
        }

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

        public void CancelAllTasks()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeTaskServices][Method : CancelAllTasks]");
#endif
            Call(Native.Method.kCancelAllTasks);
        }
        public void CancelTask(string taskId)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeTaskServices][Method : CancelTask]");
#endif
            Call(Native.Method.kCancelTask, new object[] { taskId } );
        }
        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeTaskServices][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public void StartTaskWithoutInterruption(string taskId, NativeBackgroundProcessingListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeTaskServices][Method : StartTaskWithoutInterruption]");
#endif
            Call(Native.Method.kStartTaskWithoutInterruption, new object[] { taskId, listener } );
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.taskservices.TaskServices";

            internal class Method
            {
                internal const string kCancelTask = "cancelTask";
                internal const string kCancelAllTasks = "cancelAllTasks";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kStartTaskWithoutInterruption = "startTaskWithoutInterruption";
            }

        }
    }
}
#endif