#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
{
    internal delegate void RequestPhotoLibraryAccessNativeCallback(PHAuthorizationStatus status, string error, IntPtr tagPtr);

    internal delegate void RequestCameraAccessNativeCallback(AVAuthorizationStatus status, string error, IntPtr tagPtr);

    internal delegate void PickImageNativeCallback(IntPtr imageDataPtr, PickImageFinishReason reasonCode, IntPtr tagPtr);

    internal delegate void SaveImageToAlbumNativeCallback(bool success, string error, IntPtr tagPtr);


    internal delegate void SelectMediaContentInternalNativeCallback(ref NativeArray contents, NativeError error, IntPtr tagPtr);
    internal delegate void CaptureMediaContentInternalNativeCallback(IntPtr content, NativeError error, IntPtr tagPtr);
    internal delegate void SaveMediaContentInternalNativeCallback(bool success, NativeError error, IntPtr tagPtr);

    internal delegate void MediaContentGetRawDataNativeCallback(IntPtr byteArrayPtr, int byteLength, string mime, NativeError error, IntPtr tagPtr);
    internal delegate void MediaContentAsFilePathNativeCallback(string path, NativeError error, IntPtr tagPtr);
}
#endif