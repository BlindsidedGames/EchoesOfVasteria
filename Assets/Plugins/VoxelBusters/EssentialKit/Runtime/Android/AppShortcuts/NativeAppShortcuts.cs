#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.AppShortcutsCore.Android
{
    public class NativeAppShortcuts : NativeAndroidJavaObjectWrapper
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

        public NativeAppShortcuts(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
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
        public static void SetLaunchedShortcut(string identifier)
        {
            GetClass().CallStatic(Native.Method.kSetLaunchedShortcut, new object[] { identifier } );
        }

        #endregion
        #region Public methods

        public void Add(NativeAppShortcutItem item)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeAppShortcuts][Method(RunOnUiThread) : Add]");
#endif
                Call(Native.Method.kAdd, new object[] { item?.NativeObject } );
            });
        }
        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeAppShortcuts][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public void Initialize(NativeAppShortcutClickListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeAppShortcuts][Method : Initialize]");
#endif
            Call(Native.Method.kInitialize, new object[] { listener } );
        }
        public void Remove(string itemIdentifier)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeAppShortcuts][Method(RunOnUiThread) : Remove]");
#endif
                Call(Native.Method.kRemove, new object[] { itemIdentifier } );
            });
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.appshortcuts.AppShortcuts";

            internal class Method
            {
                internal const string kInitialize = "Initialize";
                internal const string kRemove = "Remove";
                internal const string kSetLaunchedShortcut = "setLaunchedShortcut";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kAdd = "Add";
            }

        }
    }
}
#endif