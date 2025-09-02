using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.TaskServicesCore.Simulator
{

    public sealed class TaskServicesInterface : NativeFeatureInterfaceBase, INativeTaskServicesInterface, IApplicationLifecycleListener
    {
        #region Fields

        [ClearOnReload]
        private readonly Dictionary<string, Action> m_taskWorkersCache;

        private const float kMaxBackgroundProcessingTimeAllowedInSeconds = 30f;

        private CancellationTokenSource m_currentCancellationTokenSource;

        [ClearOnReload]
        private readonly bool m_cachedRunInBackgroundStatus;
        
        [ClearOnReload]
        private static int s_backgroundProcessingTaskIdCounter = 0;
        
        #endregion
        
        #region Constructors

        public TaskServicesInterface()
            : base(isAvailable: true)
        {
            m_taskWorkersCache = new ();
            ApplicationLifecycleObserver.Instance.AddListener(this);
            m_cachedRunInBackgroundStatus = Application.runInBackground;
        }

        #endregion

        #region INativeTaskServicesInterface implementation methods
        public void StartTaskWithoutInterruption(string taskId, Action onBackgroundProcessingQuotaWillExpire)
        {
            m_taskWorkersCache[taskId] = onBackgroundProcessingQuotaWillExpire;
            Application.runInBackground = true;
            s_backgroundProcessingTaskIdCounter++;
        }
        public void CancelTask(string taskId)
        {
            m_taskWorkersCache.Remove(taskId);
            s_backgroundProcessingTaskIdCounter--;

            if (s_backgroundProcessingTaskIdCounter == 0)
            {
                Application.runInBackground = m_cachedRunInBackgroundStatus;
            }
        }

        #endregion
        
        #region Static methods
        
        public static void Reset()
        {
           
        }

        #endregion

        #region IApplicationLifecycleListener implementation

        public void OnApplicationFocus(bool hasFocus)
        {
        }
        public void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                m_currentCancellationTokenSource?.Cancel();
                m_currentCancellationTokenSource = new CancellationTokenSource();

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay((int)(kMaxBackgroundProcessingTimeAllowedInSeconds * 1000), m_currentCancellationTokenSource.Token);
                        ReportBackgroundProcessingExpiry();
                    }
                    catch(OperationCanceledException)
                    {
                    }
                }, m_currentCancellationTokenSource.Token);
            }
            else
            {
                m_currentCancellationTokenSource?.Cancel();
                m_currentCancellationTokenSource = null;
            }
        }
        public void OnApplicationQuit()
        {
            ReportBackgroundProcessingExpiry();
        }

        #endregion

        #region Private methods
        private void ReportBackgroundProcessingExpiry()
        {
            var keys = m_taskWorkersCache.Keys;
            foreach (var eachTaskId in keys)
            {
                var callback = m_taskWorkersCache[eachTaskId];
                callback();
            }
        }
        
        #endregion       
    }
}