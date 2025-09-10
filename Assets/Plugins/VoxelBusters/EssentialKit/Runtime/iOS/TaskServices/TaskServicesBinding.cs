#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.TaskServicesCore.iOS
{
    internal static class TaskServicesBinding
    {

        [DllImport("__Internal")]
        public static extern void NPTaskServicesSetBackgrounProcessingUpdateLoopCallback(BackgroundProcessingUpdateNativeCallback callback); 

        
        [DllImport("__Internal")]
        public static extern void NPTaskServicesStartTaskWithoutInterruption(string taskId, BackgroundQuotaWillExpireNativeCallback callback, IntPtr tagPtr); 
        
        [DllImport("__Internal")]
        public static extern void NPTaskServicesCancelTask(string taskId); 

    }
}
#endif