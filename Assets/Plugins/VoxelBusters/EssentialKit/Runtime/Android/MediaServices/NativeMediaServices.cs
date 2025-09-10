#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.MediaServicesCore.Android
{
    public class NativeMediaServices : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Private properties
        private NativeActivity Activity
        {
            get;
            set;
        }
        #endregion

        #region Constructor

        public NativeMediaServices(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
        {
            Activity    = new NativeActivity(context);
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

        public void CaptureMediaContent(NativeMediaCaptureType captureType, string title, string fileName, NativeMediaContentCaptureListener listener)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeMediaServices][Method(RunOnUiThread) : CaptureMediaContent]");
#endif
                Call(Native.Method.kCaptureMediaContent, new object[] { NativeMediaCaptureTypeHelper.CreateWithValue(captureType), title, fileName, listener } );
            });
        }
        public NativeCameraAccessStatus GetCameraAccessStatus()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeMediaServices][Method : GetCameraAccessStatus]");
#endif
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetCameraAccessStatus);
            NativeCameraAccessStatus data  = NativeCameraAccessStatusHelper.ReadFromValue(nativeObj);
            return data;
        }
        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeMediaServices][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public NativeGalleryAccessStatus GetGalleryReadAccessStatus()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeMediaServices][Method : GetGalleryReadAccessStatus]");
#endif
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetGalleryReadAccessStatus);
            NativeGalleryAccessStatus data  = NativeGalleryAccessStatusHelper.ReadFromValue(nativeObj);
            return data;
        }
        public NativeGalleryAccessStatus GetGalleryReadWriteAccessStatus()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeMediaServices][Method : GetGalleryReadWriteAccessStatus]");
#endif
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetGalleryReadWriteAccessStatus);
            NativeGalleryAccessStatus data  = NativeGalleryAccessStatusHelper.ReadFromValue(nativeObj);
            return data;
        }
        public void SaveMediaContent(NativeBytesWrapper data, string mimeType, string targetDirectoryName, string targetFileName, NativeMediaContentSaveListener listener)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeMediaServices][Method(RunOnUiThread) : SaveMediaContent]");
#endif
                Call(Native.Method.kSaveMediaContent, new object[] { data?.NativeObject, mimeType, targetDirectoryName, targetFileName, listener } );
            });
        }
        public void SelectMediaContent(string title, string mimeType, int maxAllowed, NativeMediaContentSelectionListener listener)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeMediaServices][Method(RunOnUiThread) : SelectMediaContent]");
#endif
                Call(Native.Method.kSelectMediaContent, new object[] { title, mimeType, maxAllowed, listener } );
            });
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.mediaservices.MediaServices";

            internal class Method
            {
                internal const string kCaptureMediaContent = "captureMediaContent";
                internal const string kGetGalleryReadWriteAccessStatus = "getGalleryReadWriteAccessStatus";
                internal const string kSelectMediaContent = "selectMediaContent";
                internal const string kGetCameraAccessStatus = "getCameraAccessStatus";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kGetGalleryReadAccessStatus = "getGalleryReadAccessStatus";
                internal const string kSaveMediaContent = "saveMediaContent";
            }

        }
    }
}
#endif