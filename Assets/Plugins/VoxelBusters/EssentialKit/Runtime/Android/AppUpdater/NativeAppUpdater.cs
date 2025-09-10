#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.AppUpdaterCore.Android
{
    public class NativeAppUpdater : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion

        #region Constructor

        public NativeAppUpdater(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
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

        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeAppUpdater][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public void PromptUpdate(NativePromptUpdateOptions options, NativeOnPromptUpdateComplete listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeAppUpdater][Method : PromptUpdate]");
#endif
            Call(Native.Method.kPromptUpdate, new object[] { options?.NativeObject, listener } );
        }
        public void RequestUpdateInfo(NativeOnRequestUpdateInfoComplete listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeAppUpdater][Method : RequestUpdateInfo]");
#endif
            Call(Native.Method.kRequestUpdateInfo, new object[] { listener } );
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.appupdater.AppUpdater";

            internal class Method
            {
                internal const string kPromptUpdate = "promptUpdate";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kRequestUpdateInfo = "requestUpdateInfo";
            }

        }
    }
}
#endif