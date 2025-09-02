#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.TaskServicesCore.Android
{
    public class NativeBackgroundProcessingListener : AndroidJavaProxy
    {
        #region Delegates

        public delegate void OnBeginDelegate();
        public delegate void OnCancelDelegate();
        public delegate void OnExpiryDelegate();
        public delegate void OnUpdateDelegate();

        #endregion

        #region Public callbacks

        public OnBeginDelegate  onBeginCallback;
        public OnCancelDelegate  onCancelCallback;
        public OnExpiryDelegate  onExpiryCallback;
        public OnUpdateDelegate  onUpdateCallback;

        #endregion


        #region Constructors

        public NativeBackgroundProcessingListener() : base("com.voxelbusters.essentialkit.taskservices.IBackgroundProcessingListener")
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

        public void onBegin()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "onBegin" );
#endif
            if(onBeginCallback != null)
            {
                onBeginCallback();
            }
        }
        public void onCancel()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "onCancel" );
#endif
            if(onCancelCallback != null)
            {
                onCancelCallback();
            }
        }
        public void onExpiry()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "onExpiry" );
#endif
            if(onExpiryCallback != null)
            {
                onExpiryCallback();
            }
        }
        public void onUpdate()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "onUpdate" );
#endif
            if(onUpdateCallback != null)
            {
                onUpdateCallback();
            }
        }

        #endregion
    }
}
#endif