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

namespace VoxelBusters.EssentialKit.AppUpdaterCore.iOS
{
    public sealed class AppUpdaterInterface : NativeFeatureInterfaceBase, INativeAppUpdaterInterface
    {
        #region Constructors

        public AppUpdaterInterface() 
            : base(isAvailable: true)
        {
            AppUpdaterBinding.NPAppUpdaterCreate(EssentialKitSettings.Instance.ApplicationSettings.GetAppStoreIdForActivePlatform());
        }

        #endregion

        #region INativeAppUpdaterInterface implementation methods

        public void RequestUpdateInfo(EventCallback<AppUpdaterUpdateInfo> callback)
        {
            AppUpdaterBinding.NPAppUpdaterRequestUpdateInfo(MarshalUtility.GetIntPtr(callback), HandleRequestUpdateInfoNativeCallback);
        }

        public void PromptUpdate(PromptUpdateOptions options, EventCallback<float> callback)
        {
            NativeAppUpdaterPromptOptionsData nativeOptions = new NativeAppUpdaterPromptOptionsData()
            {
                IsForceUpdate   = options.IsForceUpdate,
                Title           = options.Title,
                Message         = options.Message,
                AllowInstallationIfDownloaded = options.AllowInstallationIfDownloaded
            };
            AppUpdaterBinding.NPAppUpdaterPromptUpdate(nativeOptions, MarshalUtility.GetIntPtr(callback), HandlePromptUpdateNativeCallback);
        }

        #endregion

        #region Native callback methods

        [MonoPInvokeCallback(typeof(RequestUpdateInfoNativeCallback))]
        private static void HandleRequestUpdateInfoNativeCallback(NativeAppUpdaterUpdateInfoData nativeUpdateInfo, NativeError error, IntPtr tagPtr)
        {
            var     tagHandle       = GCHandle.FromIntPtr(tagPtr);

            try
            {
                // send result
                AppUpdaterUpdateInfo   updateInfo   = new AppUpdaterUpdateInfo((AppUpdaterUpdateStatus)nativeUpdateInfo.Status, nativeUpdateInfo.BuildTag);
                var     errorObj    = error.Convert();
                ((EventCallback<AppUpdaterUpdateInfo>)tagHandle.Target).Invoke(updateInfo, errorObj);
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

        [MonoPInvokeCallback(typeof(PromptUpdateNativeCallback))]
        private static void HandlePromptUpdateNativeCallback(float progress, NativeError error, IntPtr tagPtr)
        {
            var     tagHandle       = GCHandle.FromIntPtr(tagPtr);

            try
            {
                // send result
                var     errorObj    = error.Convert();
                ((EventCallback<float>)tagHandle.Target).Invoke(progress, errorObj);
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

        #endregion
    }
}
#endif