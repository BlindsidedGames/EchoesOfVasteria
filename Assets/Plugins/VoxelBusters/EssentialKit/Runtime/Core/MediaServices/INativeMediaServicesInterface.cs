using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.MediaServicesCore
{
    public interface INativeMediaServicesInterface : INativeFeatureInterface
    {
        #region Methods
        
        GalleryAccessStatus GetGalleryAccessStatus(GalleryAccessMode mode);
        CameraAccessStatus GetCameraAccessStatus();


        void SelectMediaContent(MediaContentSelectOptions options, SelectMediaContentInternalCallback callback);
        void CaptureMediaContent(MediaContentCaptureOptions options, CaptureMediaContentInternalCallback callback);
        void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, SaveMediaContentInternalCallback callback);

        #endregion
    }
}