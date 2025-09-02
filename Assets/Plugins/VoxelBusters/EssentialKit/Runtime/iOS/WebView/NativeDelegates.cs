#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.WebViewCore.iOS
{
    internal delegate void WebViewNativeCallback(IntPtr nativePtr, NativeError error);

    internal delegate void WebViewRunJavaScriptNativeCallback(IntPtr nativePtr, string result, NativeError error, IntPtr tagPtr);

    internal delegate void WebViewURLSchemeMatchFoundNativeCallback(IntPtr nativePtr, string url);
}
#endif