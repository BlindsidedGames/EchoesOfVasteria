using System;
using System.Threading;
using System.Threading.Tasks;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.TaskServicesCore
{
    public interface INativeTaskServicesInterface : INativeFeatureInterface
    {
        #region Methods
        void StartTaskWithoutInterruption(string taskId, Action onBackgroundProcessingQuotaWillExpire);
        void CancelTask(string taskId);
        #endregion
    }
}