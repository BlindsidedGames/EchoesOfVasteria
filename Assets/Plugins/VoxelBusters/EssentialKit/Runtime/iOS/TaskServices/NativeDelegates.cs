#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit;

namespace VoxelBusters.EssentialKit.TaskServicesCore.iOS
{
    internal delegate void BackgroundProcessingUpdateNativeCallback();
    internal delegate void BackgroundQuotaWillExpireNativeCallback(IntPtr tagPtr);
}
#endif