using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;


namespace VoxelBusters.EssentialKit.Demo
{
	public class TaskServicesDemo : DemoActionPanelBase<TaskServicesDemoAction, TaskServicesDemoActionType>
	{
        #region Fields

        private  CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

        #endregion
        
		#region Base methods
        protected override void OnActionSelectInternal(TaskServicesDemoAction selectedAction)
        {
            switch (selectedAction.ActionType)
            {
                case TaskServicesDemoActionType.AllowApplicationBackgroundProcessingUntilTaskCompletion:
					Log("Allowing application background processing until task completion. When app goes to background, whole app gets background processing time until the passed task completes.");

                    Task<string> task = GetSampleTask();
                    task = TaskServices.AllowRunningApplicationInBackgroundUntilTaskCompletion(task, onBackgroundProcessingQuotaWillExpireCallback: () =>
                    {
                        Log("Received warning on background processing quota. This means no more background processing quota is available for the app. You can ignore this warning if you don't have anything to handle this scenario. Once app resumes you work will continue as usual.");
                        //Can use this callback to notify user (via a notification) that the current action is pausing and he needs to put the app to foreground to complete.
                    });
                    
                    /* //or
                    task.AllowRunningApplicationInBackgroundUntilCompletion(onBackgroundProcessingQuotaWillExpireCallback: () =>
                    {
                        Log("Received warning on background processing quota. This means no more background processing quota is available for the app. You can ignore this warning if you don't have anything to handle this scenario. Once app resumes you work will continue as usual.");
                    });
                    */
                    
                    //await task;

                    LogResult(task);
                    break;
                
                case TaskServicesDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kTaskServices);
                    break;

                default:    
                    break;
            }
        }
        
        #endregion

        #region Private methods
        
        private Task<string> GetSampleTask()
        {
            UnityWebRequest request = UnityWebRequest.Get("https://httpbin.org/drip?duration=20&numbytes=1000&code=200&delay=1");
            return request.ToTask(m_cancellationTokenSource.Token); //ToTask uses extension method in UnityWebRequestUtility. You need to import VoxelBusters.CoreLibrary namespace.
        }
        
        private void LogResult(Task<string> task)
        {
            Debug.Log("Before Current thread: " + Thread.CurrentThread.ManagedThreadId);
            task.ContinueWith((Task<string> forwardedTask) =>
            {
                try
                {
                    Debug.Log("After Current thread: " + Thread.CurrentThread);
                    Log("Result : " + forwardedTask.Result);
                }
                catch(Exception exception)
                {
                    Debug.Log("Exception " + exception);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnDestroy()
        {
            m_cancellationTokenSource.Cancel();
        }

        #endregion
	}

}