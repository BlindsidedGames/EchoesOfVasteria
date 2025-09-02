#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.iOS
{
    internal static class AppUpdaterBinding
    {

        [DllImport("__Internal")]
        public static extern void NPAppUpdaterCreate(string appId); 

        [DllImport("__Internal")]
        public static extern void NPAppUpdaterRequestUpdateInfo(IntPtr tagPtr, RequestUpdateInfoNativeCallback callback);       

        [DllImport("__Internal")]
        public static extern void NPAppUpdaterPromptUpdate(NativeAppUpdaterPromptOptionsData options, IntPtr tagPtr, PromptUpdateNativeCallback callback);       
    }
}
#endif