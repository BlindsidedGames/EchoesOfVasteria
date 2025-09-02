using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.AppShortcutsCore;

namespace VoxelBusters.EssentialKit
{

    /** 
     * @defgroup AppShortcuts  App Shortcuts
     * @brief Provides cross-platform interface to facilitate updating the current version of the application.
     */

    /// <summary>
    /// The AppShortcuts feature allowes to create shortcuts for the application.
    /// </summary>
    /// <description> 
    /// <para>
    /// It uses Quick Actions and App Shortcuts libraries internally on iOS and Android platforms respectively. 
    /// </para>
    /// </description>
    /// @ingroup AppShortcuts  
    public static class AppShortcuts
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeAppShortcutsInterface    s_nativeInterface    = null;

        #endregion

        #region Static properties

        /// <summary>
        ///  The settings used for initialization.
        /// </summary>
        public static AppShortcutsUnitySettings UnitySettings { get; private set; }

        #endregion

        #region Static events

        /// <summary>
        /// Fired when a shortcut is clicked and provides the shortcut identifier to identify the shortcut.
        /// </summary>
        public static event Callback<string> OnShortcutClicked;

        #endregion
        
        #region Static methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        /// <summary>
        /// Initialize the App Shortcuts module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the behavior of the App Shortcuts module.
        /// </para>
        /// </remarks>
        public static void Initialize(AppShortcutsUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Set default properties
            UnitySettings                       = settings;
            
            // Configure interface
            s_nativeInterface                   = NativeFeatureActivator.CreateInterface<INativeAppShortcutsInterface>(ImplementationSchema.AppShortcuts, true);
            
            //Registering the callback ahead as when initializing the interface, there can be some events that need to be fired(for ex: when a shortcut is clicked which triggers an app launch).
            s_nativeInterface.OnShortcutClicked += (string shortcutId) =>
            {
                CallbackDispatcher.InvokeOnMainThread(() =>
                {
                    SurrogateCoroutine.WaitUntilAndInvoke(new WaitUntil(() => (OnShortcutClicked != null)), () =>
                    {
                        OnShortcutClicked?.Invoke(shortcutId);
                    });
                });
            };
        }
        
        /// <summary>
        /// Adds an app shortcut.
        /// </summary>
        /// <param name="item"> The item to be added. Pass an instance of <see cref="AppShortcutItem"/> class which is created with <see cref="AppShortcutItem.Builder"/>.</param>
        public static void Add(AppShortcutItem item)
        {
            s_nativeInterface.Add(item);
        }

        
        /// <summary>
        ///  Removes an app shortcut.
        /// </summary>
        /// <param name="shortcutItemId"> The identifier of the shortcut to be removed.</param>
        public static void Remove(string shortcutItemId)
        {
            s_nativeInterface.Remove(shortcutItemId);
        }
        
        #endregion
       
    }
}