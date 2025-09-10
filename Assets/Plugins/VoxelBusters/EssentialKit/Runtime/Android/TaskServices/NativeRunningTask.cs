#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.TaskServicesCore.Android
{
    public class NativeRunningTask : AndroidJavaProxy
    {
        #region Delegates

        public delegate void CancelDelegate();
        public delegate void IsCompletedDelegate();

        #endregion

        #region Public callbacks

        public CancelDelegate  cancelCallback;
        public IsCompletedDelegate  isCompletedCallback;

        #endregion


        #region Constructors

        public NativeRunningTask() : base("com.voxelbusters.essentialkit.taskservices.IRunningTask")
        {
        }

        #endregion


        #region Public methods
#if NATIVE_PLUGINS_DEBUG_ENABLED
        public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs)
        {
            DebugLogger.Log("**************************************************");
            DebugLogger.Log("[Generic Invoke : " +  methodName + "]" + " Args Length : " + (javaArgs != null ? javaArgs.Length : 0));
            if(javaArgs != null)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();

                foreach(AndroidJavaObject each in javaArgs)
                {
                    if(each != null)
                    {
                        builder.Append(string.Format("[Type : {0} Value : {1}]", each.Call<AndroidJavaObject>("getClass").Call<string>("getName"), each.Call<string>("toString")));
                        builder.Append("\n");
                    }
                    else
                    {
                        builder.Append("[Value : null]");
                        builder.Append("\n");
                    }
                }

                DebugLogger.Log(builder.ToString());
            }
            DebugLogger.Log("-----------------------------------------------------");
            return base.Invoke(methodName, javaArgs);
        }
#endif

        public void cancel()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "cancel" );
#endif
            if(cancelCallback != null)
            {
                cancelCallback();
            }
        }
        public void isCompleted()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "isCompleted" );
#endif
            if(isCompletedCallback != null)
            {
                isCompletedCallback();
            }
        }

        #endregion
    }
}
#endif