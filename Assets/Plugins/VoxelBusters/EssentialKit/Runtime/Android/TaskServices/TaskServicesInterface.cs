#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.TaskServicesCore.Android
{

    public sealed class TaskServicesInterface : NativeFeatureInterfaceBase, INativeTaskServicesInterface, IApplicationLifecycleListener
    {
        #region Private fields
        private const string kBackgroundProcessingTaskId = "task-for-background-processing"; 
        private ManualPlayerLoopSystem m_playerLoopSystem;
        
        private readonly NativeTaskServices m_instance;
        private readonly Dictionary<string, Action> m_tasksCache = new Dictionary<string, Action>();
        
        #endregion
        
        #region Constructors

        public TaskServicesInterface() : base(isAvailable: true)
        {
            m_instance = new NativeTaskServices(NativeUnityPluginUtility.GetContext());
            ApplicationLifecycleObserver.Instance.AddListener(this);
            CreateManualPlayerLoopSystem();
        }


        #endregion
        
        #region Public methods

        public void StartTaskWithoutInterruption(string taskId, Action onBackgroundProcessingQuotaWillExpire)
        {
            m_tasksCache.Add(taskId, onBackgroundProcessingQuotaWillExpire);
        }
        public void CancelTask(string taskId)
        {
            m_tasksCache.Remove(taskId);
        }

        #endregion

        #region IApplicationLifecycleListener implementation
        public void OnApplicationFocus(bool hasFocus)
        {
            
        }
        public void OnApplicationPause(bool pauseStatus)
        {
            if (m_tasksCache.Keys.Count > 0)
            {
                if (pauseStatus)
                {
                    StartBackgroundProcessingInternal();    
                }
                else
                {
                    StopBackgroundProcessingInternal();    
                }
            }
        }
        private void StopBackgroundProcessingInternal()
        {
            m_instance.CancelTask(kBackgroundProcessingTaskId);
        }
        private void StartBackgroundProcessingInternal()
        {
            m_instance.StartTaskWithoutInterruption(kBackgroundProcessingTaskId, new NativeBackgroundProcessingListener()
            {
                onBeginCallback = () => {},
                onUpdateCallback = () =>
                {
                    Debug.Log("called update"); 
                    m_playerLoopSystem.Process(); 
                },
                onCancelCallback = () => {},
                onExpiryCallback = () =>
                {
                    var keys = m_tasksCache.Keys;
                    foreach (var key in keys)
                    {
                        var action = m_tasksCache[key];
                        action?.Invoke();
                    }
                }
            });
        }
        public void OnApplicationQuit()
        {
            m_instance?.CancelAllTasks();
        }
        #endregion

        #region Helpers

        private void CreateManualPlayerLoopSystem()
        {

            List<object> requiredSubSystems = new List<object>
            {
                new EarlyUpdate.UnityWebRequestUpdate(),
                new EarlyUpdate.ExecuteMainThreadJobs(),
                new Update.ScriptRunDelayedTasks(),
            };

            m_playerLoopSystem = new ManualPlayerLoopSystem(requiredSubSystems);
        }
        
        #endregion
    }
}
#endif