using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.MediaServicesCore;
using System;

namespace VoxelBusters.EssentialKit
{
    /** @defgroup MediaServices Media Services
    *   @brief Provides cross-platform interface to access devices's media gallery and camera for picking/saving images/videos/documents etc.
    */

    /// <summary>
    /// Provides cross-platform interface to access devices's media gallery and camera for picking/saving media content.
    /// </summary>
    /// @ingroup MediaServices
    public static class MediaServices
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeMediaServicesInterface   s_nativeInterface    = null;

        #endregion

        #region Static properties

        public static MediaServicesUnitySettings UnitySettings { get; private set; }

        #endregion

        #region Setup methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        /// @name Advanced Usage
        ///@{
        
        /// <summary>
        /// Initializes the media services module with the given settings. 
        /// @note This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the behavior of the media services module.
        /// </para>
        /// </remarks>
        public static void Initialize(MediaServicesUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Set properties
            UnitySettings           = settings;

            // Configure interface
            s_nativeInterface       = NativeFeatureActivator.CreateInterface<INativeMediaServicesInterface>(ImplementationSchema.MediaServices, true);
        }
        /// @}

        #endregion


        #region Public methods

        /// <summary>
        /// Returns the current authorization status provided to access the gallery.
        /// </summary>
        /// <description>
        /// To see different authorization status, see <see cref="GalleryAccessStatus"/>.
        /// </description>
        /// <param name="mode">The access mode your app is requesting.</param>
        public static GalleryAccessStatus GetGalleryAccessStatus(GalleryAccessMode mode)
        {
            try
            {
                // make request
                return s_nativeInterface.GetGalleryAccessStatus(mode);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return GalleryAccessStatus.NotDetermined;
            }
        }

        /// <summary>
        /// Returns the current authorization status provided to access the camera.
        /// </summary>
        /// <description>
        /// To see different authorization status, see <see cref="CameraAccessStatus"/>.
        /// </description>
        public static CameraAccessStatus GetCameraAccessStatus()
        {
            try
            {
                // make request
                return s_nativeInterface.GetCameraAccessStatus();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return CameraAccessStatus.NotDetermined;
            }
        }

        /// <summary>
        /// Select media content from the gallery.
        /// </summary>
        /// <description>
        /// Selects media content as per the options specified in <see cref="MediaContentSelectOptions"/>
        /// </description>
        /// <param name="options">A set of options that customize the behavior of the method.</param>
        /// <param name="callback">A callback which will be invoked after the user selects media content.</param>
        public static void SelectMediaContent(MediaContentSelectOptions options, EventCallback<IMediaContent[]> callback)
        {
            Assert.IsArgNotNull(options, nameof(options));

            // Make request
            s_nativeInterface.SelectMediaContent(options, (contents,  error) => SendSelectMediaContentResult(callback, contents, error));
        }

        /// <summary>
        /// Capture media content from a camera.
        /// </summary>
        /// <description>
        /// Captures media content as per the options specified in <see cref="MediaContentCaptureOptions"/>
        /// </description>
        /// <param name="options">A set of options that customize the behavior of the method.</param>
        /// <param name="callback">A callback which will be invoked after the user captures media content.</param>
        public static void CaptureMediaContent(MediaContentCaptureOptions options, EventCallback<IMediaContent> callback)
        {
            Assert.IsArgNotNull(options, nameof(options));

            // Make request
            s_nativeInterface.CaptureMediaContent(options, (contents,  error) => SendCaptureMediaContentResult(callback, contents, error));
        }

        /// <summary>
        /// Save media content to the gallery.
        /// </summary>
        /// <description>
        /// Saves media content as per the options specified in <see cref="MediaContentSaveOptions"/>
        /// </description>
        /// <param name="data">The data to save to the gallery.</param>
        /// <param name="mimeType">The mime type of the data.</param>
        /// <param name="options">A set of options that customize the behavior of the method.</param>
        /// <param name="callback">A callback which will be invoked after the data is saved to the gallery.</param>
        public static void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, EventCallback<bool> callback)
        {
            Assert.IsArgNotNull(data, nameof(data));
            Assert.IsArgNotNull(mimeType, nameof(mimeType));
            Assert.IsArgNotNull(options, nameof(options));

            // Make request
            s_nativeInterface.SaveMediaContent(data, mimeType, options, (success, error) => SendSaveMediaContentResult(callback, success, error));
        }

        #endregion

        #region Helper methods
        private static void SendSelectMediaContentResult(EventCallback<IMediaContent[]> callback, IMediaContent[] contents, Error error)
        {
            // send result to caller object
            CallbackDispatcher.InvokeOnMainThread(callback, contents, error);
        }

        private static void SendCaptureMediaContentResult(EventCallback<IMediaContent> callback, IMediaContent content, Error error)
        {
            // send result to caller object
            CallbackDispatcher.InvokeOnMainThread(callback, content, error);
        }

        private static void SendSaveMediaContentResult(EventCallback<bool> callback, bool success, Error error)
        {
            // send result to caller object
            CallbackDispatcher.InvokeOnMainThread(callback, success, error);
        }
        #endregion

        #region Obsolete methods

        [Obsolete("Use SelectMediaContent instead. If a permission is required, SelectMediaContent shows up the permission dialog.", true)]  //Obsolete:2024
        private static void RequestGalleryAccess(GalleryAccessMode mode, bool showPrepermissionDialog = true, EventCallback<MediaServicesRequestGalleryAccessResult> callback = null)
        {}

        
        [Obsolete("Use CaptureMediaContent instead. If a permission is required, CaptureMediaContent shows up the permission dialog.", true)]  //Obsolete:2024            
        private static void RequestCameraAccess(bool showPrepermissionDialog = true, EventCallback<MediaServicesRequestCameraAccessResult> callback = null)
        {}

        [Obsolete("Use GetGalleryAccessStatus instead.", true)]  //Obsolete:2024
        private static bool CanSelectImageFromGallery()
        {
            return false;
        }

        [Obsolete("Use SelectMediaContent instead.", true)]  //Obsolete:2024
        private static void SelectImageFromGallery(bool canEdit, EventCallback<TextureData> callback)
        {}

        [Obsolete("Use GetCameraAccessStatus instead.", true)]  //Obsolete:2024
        private static bool CanCaptureImageFromCamera()
        {
            return false;
        }
        

        [Obsolete("Use CaptureMediaContent instead.", true)]  //Obsolete:2024            
        public static void CaptureImageFromCamera(bool canEdit, EventCallback<TextureData> callback)
        {}

        [Obsolete("This method is obsolete. Use SaveMediaContent instead.", true)]  //Obsolete:2024
        private static bool CanSaveImageToGallery()
        {
            return false;
        }


        [Obsolete("This method is obsolete. Use SaveMediaContent instead.", true)]  //Obsolete:2024
        public static void SaveImageToGallery(UnityEngine.Texture2D image, EventCallback<MediaServicesSaveImageToGalleryResult> callback)
        {}

        [Obsolete("This method is obsolete. Use SaveMediaContent instead.", true)]  //Obsolete:2024
        public static void SaveImageToGallery(string albumName, UnityEngine.Texture2D image, EventCallback<MediaServicesSaveImageToGalleryResult> callback)
        {}

        #endregion
    }
}