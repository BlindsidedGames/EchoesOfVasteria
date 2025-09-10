#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public class NativeRequestNotificationPermissionsListener : AndroidJavaProxy
    {
        #region Delegates

        public delegate void OnFailureDelegate(NativeErrorInfo error);
        public delegate void OnSuccessDelegate(NativeNotificationAccessState state);

        #endregion

        #region Public callbacks

        public OnFailureDelegate  onFailureCallback;
        public OnSuccessDelegate  onSuccessCallback;

        #endregion


        #region Constructors

        public NativeRequestNotificationPermissionsListener() : base("com.voxelbusters.essentialkit.notificationservices.INotificationServices$IRequestNotificationPermissionsListener")
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

        public void onFailure(AndroidJavaObject error)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "onFailure"  + " " + "[" + "error" + " : " + error +"]");
#endif
            if(onFailureCallback != null)
            {
                onFailureCallback(new NativeErrorInfo(error));
            }
        }
        public void onSuccess(AndroidJavaObject state)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Proxy : Callback] : " + "onSuccess"  + " " + "[" + "state" + " : " + state +"]");
#endif
            if(onSuccessCallback != null)
            {
                onSuccessCallback(NativeNotificationAccessStateHelper.ReadFromValue(state));
            }
        }

        #endregion
    }
}
#endif