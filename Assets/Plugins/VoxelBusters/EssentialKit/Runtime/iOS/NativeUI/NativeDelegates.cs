#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.NativeUICore.iOS
{
    internal delegate void AlertActionSelectNativeCallback(IntPtr nativePtr, int selectedButtonIndex, ref NativeArray inputValues);
    internal delegate void DatePickerControllerNativeCallback(long selectedValue, IntPtr tagPtr);
}
#endif