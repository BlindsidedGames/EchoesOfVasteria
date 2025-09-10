using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.EssentialKit.DeepLinkServicesCore;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    /** @defgroup DeepLinkServices Deep Link Services
    *   @brief Cross-platform interface to handle deep links.
    */

    /// <summary>
    /// Provides cross-platform interface to handle deep links.    
    /// </summary>
    /// <description>
    /// This class provides cross-platform interface to handle deep links.
    /// </description>
    /// @ingroup DeepLinkServices
    public static class DeepLinkServices
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeDeepLinkServicesInterface    s_nativeInterface;

        #endregion

        #region Static properties

        public static DeepLinkServicesUnitySettings UnitySettings { get; private set; }

        public static IDeepLinkServicesDelegate Delegate { get; set; }

        #endregion

        #region Static events

        /// <summary>
        /// Event that will be called when url scheme is opened.
        /// </summary>
        public static event Callback<DeepLinkServicesDynamicLinkOpenResult> OnCustomSchemeUrlOpen;

        /// <summary>
        /// Event that will be called when universal link is opened.
        /// </summary>
        public static event Callback<DeepLinkServicesDynamicLinkOpenResult> OnUniversalLinkOpen;

        #endregion

        #region Setup methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        /// @name Advanced Usage
        /// @{

        /// <summary>
        /// Initializes the <see cref="DeepLinkServices"/> module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the behavior of the <see cref="DeepLinkServices"/> module.
        /// </para>
        /// </remarks>
        public static void Initialize(DeepLinkServicesUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Reset event properties
            OnCustomSchemeUrlOpen   = null;
            OnUniversalLinkOpen     = null;

            // Set properties
            UnitySettings           = settings;

            // Configure interface
            s_nativeInterface       = NativeFeatureActivator.CreateInterface<INativeDeepLinkServicesInterface>(ImplementationSchema.DeepLinkServices, true);
            s_nativeInterface.SetCanHandleCustomSchemeUrl(handler: CanHandleCustomSchemeUrl);
            s_nativeInterface.SetCanHandleUniversalLink(handler: CanHandleUniversalLink);
            s_nativeInterface.OnCustomSchemeUrlOpen    += HandleOnCustomSchemeUrlOpen;
            s_nativeInterface.OnUniversalLinkOpen      += HandleOnUniversalLinkOpen;
            s_nativeInterface.Init();
        }
        /// @}

        private static bool CanHandleCustomSchemeUrl(string url)
        {
            return (Delegate == null) || Delegate.CanHandleCustomSchemeUrl(new Uri(url));
        }

        private static bool CanHandleUniversalLink(string url)
        {
            return (Delegate == null) || Delegate.CanHandleUniversalLink(new Uri(url));
        }

        #endregion

        #region Callback methods

        private static void HandleOnCustomSchemeUrlOpen(string url)
        {
            DebugLogger.Log(EssentialKitDomain.Default, $"Detected url scheme: {url}");

            // notify listeners
            var     result      = new DeepLinkServicesDynamicLinkOpenResult(new Uri(url), url);

            if (OnCustomSchemeUrlOpen != null)
            {
                CallbackDispatcher.InvokeOnMainThread(OnCustomSchemeUrlOpen, result);
            }
            else
            {
                SurrogateCoroutine.WaitUntilAndInvoke(new WaitUntil(() => OnCustomSchemeUrlOpen != null), () =>
                {
                    CallbackDispatcher.InvokeOnMainThread(OnCustomSchemeUrlOpen, result);
                });
            }
        }

        private static void HandleOnUniversalLinkOpen(string url)
        {
            DebugLogger.Log(EssentialKitDomain.Default, $"Detected universal link: {url}");

            // notify listeners
            var     result      = new DeepLinkServicesDynamicLinkOpenResult(new Uri(url), url);

            if (OnUniversalLinkOpen != null)
            {
                CallbackDispatcher.InvokeOnMainThread(OnUniversalLinkOpen, result);
            }
            else
            {
                SurrogateCoroutine.WaitUntilAndInvoke(new WaitUntil(() => OnUniversalLinkOpen != null), () =>
                {
                    CallbackDispatcher.InvokeOnMainThread(OnUniversalLinkOpen, result);
                });
            }
        }

        #endregion
    }
}