#if UNITY_ANDROID
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.Android
{
    public sealed class AppUpdaterInterface : NativeFeatureInterfaceBase, INativeAppUpdaterInterface
    {
#region Private fields

        private NativeAppUpdater m_instance;

#endregion

#region Constructors

        public AppUpdaterInterface() : base(isAvailable: true)
        {

            m_instance = new NativeAppUpdater(NativeUnityPluginUtility.GetContext());
        }

        #endregion


        #region Public methods

        public void RequestUpdateInfo(EventCallback<AppUpdaterUpdateInfo> callback)
        {
            m_instance.RequestUpdateInfo(new NativeOnRequestUpdateInfoComplete() {
                onSuccessCallback = (nativeUpdateInfo) => {
                    AppUpdaterUpdateStatus status = (AppUpdaterUpdateStatus)nativeUpdateInfo.Status;
                    AppUpdaterUpdateInfo result = new AppUpdaterUpdateInfo(status, nativeUpdateInfo.BuildTag);
                    callback(result, null);
                },
                onFailureCallback = (errorInfo) => {
                    callback(null, new Error(null, errorInfo.GetCode(), errorInfo.GetDescription()));
                }
            });
        }

        public void PromptUpdate(PromptUpdateOptions options, EventCallback<float> callback)
        {
            NativePromptUpdateOptions nativeOptions = new NativePromptUpdateOptions
            {
                IsForceUpdate = options.IsForceUpdate,
                Title = options.Title,
                Message = options.Message,
                AllowInstallationIfDownloaded = options.AllowInstallationIfDownloaded
            };

            m_instance.PromptUpdate(nativeOptions, new NativeOnPromptUpdateComplete() {
                
                onDownloadProgressUpdateCallback = (progress) => {
                    callback(progress, null);
                },
                onFailureCallback = (errorInfo) => {
                    callback(0f, new Error(null, errorInfo.GetCode(), errorInfo.GetDescription()));
                }
            });
        }

        #endregion
    }
}
#endif