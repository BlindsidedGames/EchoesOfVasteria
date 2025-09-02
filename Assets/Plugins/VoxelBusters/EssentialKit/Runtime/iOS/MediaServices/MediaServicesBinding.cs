#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
{
    internal static class MediaServicesBinding
    {
        [DllImport("__Internal")]
        public static extern PHAuthorizationStatus NPMediaServicesGetPhotoLibraryAccessStatus();

        [DllImport("__Internal")]
        public static extern AVAuthorizationStatus NPMediaServicesGetCameraAccessStatus();
        
        [DllImport("__Internal")]
        public static extern void NPMediaServicesSelectMediaContent(NativeMediaContentSelectionOptionsData options, IntPtr tagPtr, SelectMediaContentInternalNativeCallback handleSelectMediaContentNativeCallback);

        [DllImport("__Internal")]
        public static extern void NPMediaServicesCaptureMediaContent(NativeMediaContentCaptureOptionsData options, IntPtr tagPtr, CaptureMediaContentInternalNativeCallback handleCaptureMediaContentNativeCallback);

        [DllImport("__Internal")]
        public static extern void NPMediaServicesSaveMediaContent(byte[] data, long length, string mimeType, NativeMediaContentSaveOptionsData options, IntPtr tagPtr, SaveMediaContentInternalNativeCallback handleSaveMediaContentNativeCallback);
    }
}
#endif