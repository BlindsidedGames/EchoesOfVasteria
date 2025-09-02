#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AddressBookCore.iOS
{
    internal delegate void ReadContactsNativeCallback(IntPtr contactsPtr, int count, int nextOffset, NativeError nativeError, IntPtr tagPtr);
}
#endif