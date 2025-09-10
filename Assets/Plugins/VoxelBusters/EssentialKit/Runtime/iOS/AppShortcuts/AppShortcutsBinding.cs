#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.AppShortcutsCore.iOS
{
    internal static class AppShortcutsBinding
    {
        [DllImport("__Internal")]
        public static extern void NPAppShortcutsSetShortcutClickedCallback(OnShortcutClickedNativeCallback callback);
        
        [DllImport("__Internal")]
        public static extern void NPAppShortcutsAddShortcut(NativeAppShortcutItem item); 
        
        [DllImport("__Internal")]
        public static extern void NPAppShortcutsRemoveShortcut(string shortcutId); 

    }

}
#endif