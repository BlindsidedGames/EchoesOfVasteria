#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AOT;
using VoxelBusters.EssentialKit;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary;
using System;
using System.Runtime.InteropServices;
using UnityEngine.PlayerLoop;

namespace VoxelBusters.EssentialKit.TaskServicesCore.iOS
{
    public sealed class TaskServicesInterface : TaskServicesInterfaceBase
    {
        #region Constructors

        public TaskServicesInterface() 
            : base(isAvailable: true)
        {
            TaskServicesBinding.NPTaskServicesSetBackgrounProcessingUpdateLoopCallback(HandleBackgroundProcessingUpdateNativeCallback);
            CreateManualPlayerLoopSystem();
        }

        #endregion

        #region INativeTaskServicesInterface implementation methods
        
        public override void StartTaskWithoutInterruption(string taskId, Action onBackgroundProcessingQuotaWillExpire)
        {
            TaskServicesBinding.NPTaskServicesStartTaskWithoutInterruption(taskId, HandleOnBackgroundProcessingQuotaWillExpireNativeCallback, MarshalUtility.GetIntPtr(onBackgroundProcessingQuotaWillExpire));
        }

        public override void CancelTask(string taskId)
        {
            TaskServicesBinding.NPTaskServicesCancelTask(taskId);
        }

        #endregion

        #region Native callback methods
        
        [MonoPInvokeCallback(typeof(BackgroundQuotaWillExpireNativeCallback))]
        private static void HandleOnBackgroundProcessingQuotaWillExpireNativeCallback(IntPtr tagptr)
        {
            var     tagHandle       = GCHandle.FromIntPtr(tagptr);
            try
            {
                ((Action)tagHandle.Target).Invoke();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }
        
        [MonoPInvokeCallback(typeof(BackgroundProcessingUpdateNativeCallback))]
        private static void HandleBackgroundProcessingUpdateNativeCallback()
        {
            m_playerLoopSystem.Process();
        }

        #endregion
    }
}
#endif