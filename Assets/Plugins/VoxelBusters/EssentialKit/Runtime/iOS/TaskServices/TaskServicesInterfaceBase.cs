using System;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
namespace VoxelBusters.EssentialKit.TaskServicesCore
{
    public abstract class TaskServicesInterfaceBase : NativeFeatureInterfaceBase, INativeTaskServicesInterface
    {
        protected static ManualPlayerLoopSystem m_playerLoopSystem;

        #region Constructors

        public TaskServicesInterfaceBase(bool isAvailable) : base(isAvailable)
        {
            CreateManualPlayerLoopSystem();
        }

        #endregion


        #region Base methods
        protected void CreateManualPlayerLoopSystem()
        {
            if (m_playerLoopSystem == null)
            {
                List<object> requiredSubSystems = new List<object>
                {
                    new EarlyUpdate.UnityWebRequestUpdate(),
                    new EarlyUpdate.ExecuteMainThreadJobs(),
                    new Update.ScriptRunDelayedTasks(),
                };

                m_playerLoopSystem = new ManualPlayerLoopSystem(requiredSubSystems);
            }
        }
        
        public abstract void StartTaskWithoutInterruption(string taskId, Action onBackgroundProcessingQuotaWillExpire);
        public abstract void CancelTask(string taskId);
        
        #endregion
    }
}