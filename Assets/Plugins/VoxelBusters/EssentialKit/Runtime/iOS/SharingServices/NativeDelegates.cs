#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.SharingServicesCore.iOS
{
    internal delegate void MailComposerClosedNativeCallback(IntPtr nativePtr, MFMailComposeResult result, NativeError error);

    internal delegate void MessageComposerClosedNativeCallback(IntPtr nativePtr, MFMessageComposeResult result);

    internal delegate void ShareSheetClosedNativeCallback(IntPtr nativePtr, bool completed, NativeError error);
    
    internal delegate void SocialShareComposerClosedNativeCallback(IntPtr nativePtr, SLComposeViewControllerResult result);
}
#endif