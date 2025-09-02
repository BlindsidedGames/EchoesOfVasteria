#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.MediaServicesCore.Android
{
    public class NativeMediaContentBase : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeMediaContentBase(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeMediaContentBase(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeMediaContentBase() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeMediaContentBase()
        {
            DebugLogger.Log("Disposing NativeMediaContentBase");
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

        public void AsFilePath(string destinationDirectory, string fileName, NativeMediaContentPathCallback onComplete)
        {
            Call(Native.Method.kAsFilePath, destinationDirectory, fileName, onComplete);
        }
        public void AsRawMediaData(NativeMediaContentRawDataCallback onComplete)
        {
            Call(Native.Method.kAsRawMediaData, onComplete);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.mediaservices.MediaContentBase";

            internal class Method
            {
                internal const string kAsRawMediaData = "AsRawMediaData";
                internal const string kAsFilePath = "AsFilePath";
            }

        }
    }
}
#endif