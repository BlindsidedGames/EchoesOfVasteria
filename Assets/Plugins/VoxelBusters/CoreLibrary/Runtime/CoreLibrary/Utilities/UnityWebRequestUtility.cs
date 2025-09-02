using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace VoxelBusters.CoreLibrary
{
    public static class UnityWebRequestUtility
    {
        public static Task<string> ToTask(this UnityWebRequest request, CancellationToken cancellationToken)
        {
            var operation   = request.SendWebRequest();
            var tcs         = new TaskCompletionSource<string>();
            
            cancellationToken.Register(() =>
            {
                if (operation.isDone)
                    return;
                
                operation.webRequest.Abort(); // Abort the request
                tcs.SetCanceled();
            });
            
            
            operation.completed += _ =>
            {
                var webRequest = operation.webRequest;
                
                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        tcs.SetException(new Exception(webRequest.error));
                    }
                }
                else
                {
                    tcs.SetResult(webRequest.downloadHandler.text);
                }
            };
            
            return tcs.Task;
        }
    }
}