#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.AppShortcutsCore.Android
{
    public class NativeAppShortcutItem : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public string Identifier
        {
            get
            {
                return Get<string>("identifier");
            }

            set
            {
                Set<string>("identifier", value);
            }
        }


        public string Title
        {
            get
            {
                return Get<string>("title");
            }

            set
            {
                Set<string>("title", value);
            }
        }


        public string Subtitle
        {
            get
            {
                return Get<string>("subtitle");
            }

            set
            {
                Set<string>("subtitle", value);
            }
        }


        public string IconFileName
        {
            get
            {
                return Get<string>("iconFileName");
            }

            set
            {
                Set<string>("iconFileName", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativeAppShortcutItem(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeAppShortcutItem(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeAppShortcutItem() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeAppShortcutItem()
        {
            DebugLogger.Log("Disposing NativeAppShortcutItem");
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
            internal const string kClassName = "com.voxelbusters.essentialkit.appshortcuts.AppShortcutItem";

            internal class Method
            {
            }

        }
    }
}
#endif