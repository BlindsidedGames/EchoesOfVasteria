#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.Common.Android
{
    public class NativeAsset : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeAsset(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeAsset(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeAsset()
        {
            DebugLogger.Log("Disposing NativeAsset");
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

        public void Copy(string destinationFolder, string destinationFileName, NativeCopyAssetListener listener)
        {
            Call(Native.Method.kCopy, destinationFolder, destinationFileName, listener);
        }
        public string GetMimeType()
        {
            return Call<string>(Native.Method.kGetMimeType);
        }
        public string GetUri()
        {
            return Call<string>(Native.Method.kGetUri);
        }
        public bool IsValid()
        {
            return Call<bool>(Native.Method.kIsValid);
        }
        public void Load(NativeLoadAssetListener listener)
        {
            Call(Native.Method.kLoad, listener);
        }
        public override string ToString()
        {
            return Call<string>(Native.Method.kToString);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.utilities.common.Asset";

            internal class Method
            {
                internal const string kToString = "toString";
                internal const string kIsValid = "isValid";
                internal const string kGetMimeType = "getMimeType";
                internal const string kGetUri = "getUri";
                internal const string kCopy = "copy";
                internal const string kLoad = "load";
            }

        }
    }
}
#endif