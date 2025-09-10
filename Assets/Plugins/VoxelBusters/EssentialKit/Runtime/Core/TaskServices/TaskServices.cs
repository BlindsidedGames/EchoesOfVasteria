using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.TaskServicesCore;

namespace VoxelBusters.EssentialKit
{

    /** 
     * @defgroup TaskServices  Task Services
     * @brief Provides cross-platform interface to schedule or continue tasks in background.
     */

    /// <summary>
    /// The TaskServices class provides cross-platform interface to schedule or continue tasks in background. This can be used for running tasks uninterruptedly.
    /// </summary>
    /// <description> 
    /// <para>
    /// iOS platform has limited support for background execution compared to Android platform.
    /// </para>
    /// </description>
    /// @ingroup TaskServices  
    public static class TaskServices
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeTaskServicesInterface    s_nativeInterface    = null;

        #endregion

        #region Static properties

        /// <summary>
        ///  The settings used for initialization.
        /// </summary>
        public static TaskServicesUnitySettings UnitySettings { get; private set; }

        #endregion
        
        #region Static methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        /// <summary>
        /// [Optional] Initialize the Task Services module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the behavior of the Task Services module.
        /// </para>
        /// </remarks>
        public static void Initialize(TaskServicesUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Set default properties
            UnitySettings                       = settings;

            // Configure interface
            s_nativeInterface                   = NativeFeatureActivator.CreateInterface<INativeTaskServicesInterface>(ImplementationSchema.TaskServices, true);
        }
        
        /// <summary>
        /// Allow application background processing until task completion. All tasks which are started other the passed task are also allowed to complete until this task completes."
        /// </summary>
        /// <param name="task"> Pass task to complete without interruption.</param>
        /// <param name="onBackgroundProcessingQuotaWillExpireCallback">Callback triggered when background processing quota about to expire. Cleanup any resources here if you want to.</param>
        /// <returns> Task to be awaited on.</returns>
        public static Task AllowRunningApplicationInBackgroundUntilTaskCompletion(Task task, Callback onBackgroundProcessingQuotaWillExpireCallback = null)
        {
            string taskId = StartTaskWithoutInterruption(onBackgroundProcessingQuotaWillExpireCallback);
            return task.ContinueWith(_ =>
            {
                s_nativeInterface.CancelTask(taskId);
            });
        }


        /// <summary>
        /// Allow application background processing until task completion. All tasks which are started other the passed task are also allowed to complete until this task completes."
        /// </summary>
        /// <param name="task"> Pass task which can return a result to complete without interruption.</param>
        /// <param name="onBackgroundProcessingQuotaWillExpireCallback">Callback triggered when background processing quota about to expire. Cleanup any resources here if you want to.</param>
        /// <returns> Task to be awaited on.</returns>
        public static Task<TResult> AllowRunningApplicationInBackgroundUntilTaskCompletion<TResult>(Task<TResult> task, Callback onBackgroundProcessingQuotaWillExpireCallback = null)
        {
            string taskId = StartTaskWithoutInterruption(onBackgroundProcessingQuotaWillExpireCallback);
            return task.ContinueWith<TResult>( t =>
            {
                s_nativeInterface.CancelTask(taskId);
                return t.Result;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        #endregion

        #region Extension methods
        
        /// <summary>
        /// Extension method for a task to allow application background processing until it's completion. All tasks which are started other the passed task are also allowed to complete until this task completes."
        /// </summary>
        /// <param name="task"> Call this method on a task to complete without interruption.</param>
        /// <param name="onBackgroundProcessingQuotaWillExpireCallback">Callback triggered when background processing quota about to expire. Cleanup any resources here if you want to.</param>
        /// <returns> Task to be awaited on.</returns>
        public static Task AllowRunningApplicationInBackgroundUntilCompletion(this Task task, Callback onBackgroundProcessingQuotaWillExpireCallback = null)
        {
            return AllowRunningApplicationInBackgroundUntilTaskCompletion(task, onBackgroundProcessingQuotaWillExpireCallback);
        }
        
        /// <summary>
        /// Extension method for a task to allow application background processing until it's completion. All tasks which are started other the passed task are also allowed to complete until this task completes."
        /// </summary>
        /// <param name="task"> Call this method on a task to complete without interruption.</param>
        /// <param name="onBackgroundProcessingQuotaWillExpireCallback">Callback triggered when background processing quota about to expire. Cleanup any resources here if you want to.</param>
        /// <returns> Task to be awaited on.</returns>
        public static Task<TResult> AllowRunningApplicationInBackgroundUntilCompletion<TResult>(this Task<TResult> task, Callback onBackgroundProcessingQuotaWillExpireCallback = null)
        {
            return AllowRunningApplicationInBackgroundUntilTaskCompletion<TResult>(task, onBackgroundProcessingQuotaWillExpireCallback);
        }
        #endregion


        #region Private methods

        private static string StartTaskWithoutInterruption(Callback onBackgroundProcessingQuotaWillExpireCallback)
        {

            string taskId = Guid.NewGuid().ToString();
            s_nativeInterface.StartTaskWithoutInterruption(taskId, () =>
            {
                s_nativeInterface.CancelTask(taskId);
                CallbackDispatcher.InvokeOnMainThread(onBackgroundProcessingQuotaWillExpireCallback);
            });

            return taskId;
        }

        #endregion
        
       
    }
}