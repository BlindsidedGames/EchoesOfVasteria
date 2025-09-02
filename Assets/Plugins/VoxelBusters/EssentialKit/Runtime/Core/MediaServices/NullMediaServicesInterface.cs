using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.MediaServicesCore
{
    internal sealed class NullMediaServicesInterface : NativeMediaServicesInterfaceBase
    {
        #region Constructors

        public NullMediaServicesInterface()
            : base(isAvailable: false)
        { }

        #endregion
        
        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("MediaServices");
        }

        #endregion

        #region Base class methods

        public override GalleryAccessStatus GetGalleryAccessStatus(GalleryAccessMode mode)
        {
            LogNotSupported();

            return GalleryAccessStatus.Restricted;
        }
        
        public override CameraAccessStatus GetCameraAccessStatus()
        {
            LogNotSupported();

            return CameraAccessStatus.Restricted;
        }

        public override void SelectMediaContent(MediaContentSelectOptions options, SelectMediaContentInternalCallback callback)
        {
            LogNotSupported();
            
            callback(null, Diagnostics.kFeatureNotSupported);
        }

        public override void CaptureMediaContent(MediaContentCaptureOptions options, CaptureMediaContentInternalCallback callback)
        {
            LogNotSupported();
            
            callback(null, Diagnostics.kFeatureNotSupported);
        }

        public override void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, SaveMediaContentInternalCallback callback)
        {
            LogNotSupported();
            
            callback(false, Diagnostics.kFeatureNotSupported);
        }

        #endregion
    }
}