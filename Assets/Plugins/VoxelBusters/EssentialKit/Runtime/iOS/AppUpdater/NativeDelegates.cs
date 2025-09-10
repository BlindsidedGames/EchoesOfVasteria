#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.iOS
{
    internal delegate void RequestUpdateInfoNativeCallback(NativeAppUpdaterUpdateInfoData nativeUpdateInfo, NativeError error, IntPtr tagPtr);
    internal delegate void PromptUpdateNativeCallback(float progress, NativeError error, IntPtr tagPtr);

}
#endif