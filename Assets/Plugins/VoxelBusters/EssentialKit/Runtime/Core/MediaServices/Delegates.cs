using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.MediaServicesCore
{
    public delegate void RequestGalleryAccessInternalCallback(GalleryAccessStatus status, Error error);

    public delegate void RequestCameraAccessInternalCallback(CameraAccessStatus status, Error error);

    public delegate void SelectImageInternalCallback(byte[] imageData, Error error);

    public delegate void SelectMediaContentInternalCallback(IMediaContent[] contents, Error error);

    public delegate void CaptureMediaContentInternalCallback(IMediaContent content, Error error);

    public delegate void SaveImageToGalleryInternalCallback(bool success, Error error);

    public delegate void SaveMediaContentInternalCallback(bool success, Error error);
}