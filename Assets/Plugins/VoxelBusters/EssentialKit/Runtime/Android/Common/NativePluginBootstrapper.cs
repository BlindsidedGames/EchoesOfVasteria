#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.Common.Android
{
    public class NativePluginBootstrapper : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativePluginBootstrapper(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativePluginBootstrapper(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativePluginBootstrapper() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativePluginBootstrapper()
        {
            DebugLogger.Log("Disposing NativePluginBootstrapper");
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
        public static void Initialise(NativeContext context, bool debug)
        {
            GetClass().CallStatic(Native.Method.kInitialise, context.NativeObject, debug);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.utilities.PluginBootstrapper";

            internal class Method
            {
                internal const string kInitialise = "initialise";
            }

        }
    }
}
#endif