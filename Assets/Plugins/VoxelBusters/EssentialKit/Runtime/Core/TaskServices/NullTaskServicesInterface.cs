using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.TaskServicesCore
{
    internal class NullTaskServicesInterface : NativeFeatureInterfaceBase, INativeTaskServicesInterface
    {
        #region Constructors

        public NullTaskServicesInterface(): base(isAvailable: false)
        { }

        #endregion

        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("TaskServices");
        }

        #endregion

        #region INativeTaskServicesInterface implementation methods

        public void StartTaskWithoutInterruption(string taskId, Action onBackgroundProcessingQuotaWillExpire)
        {
            LogNotSupported();
        }
        public void CancelTask(string taskId)
        {
            LogNotSupported();
        }

        #endregion
    }
}