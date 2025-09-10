// internal namespace

using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public class AppUpdaterDemo : DemoActionPanelBase<AppUpdaterDemoAction, AppUpdaterDemoActionType>
	{
        #region Fields
        
        [SerializeField]
        private Toggle m_isForceUpdateToggle = null;

        [SerializeField] private Toggle m_allowInstallationIfDownloaded = null;
        #endregion
        
		#region Base methods

        protected override void OnActionSelectInternal(AppUpdaterDemoAction selectedAction)
        {
            switch (selectedAction.ActionType)
            {
                case AppUpdaterDemoActionType.RequestUpdateInfo:
					Log("Requesting update info."); 
                    AppUpdater.RequestUpdateInfo((result, error) => {
                        if(error == null)
                        {
                            Log("Received updated info: " + result);
                        }
                        else
                        {
                            Log("Failed to receive updated info: " + error);
                        }
                    });
                    break;

                case AppUpdaterDemoActionType.PromptUpdate:
					Log("Prompting an update."); 
                    PromptUpdateOptions options = new PromptUpdateOptions.Builder()
                                                    .SetIsForceUpdate(m_isForceUpdateToggle.isOn)
                                                    .SetPromptTitle("Update Available")
                                                    .SetPromptMessage("There is an update available. Do you want to update?")
                                                    .SetAllowInstallationIfDownloaded(m_allowInstallationIfDownloaded.isOn) //Or you can just check the AppUpdaterUpdateStatus.Downloaded then set this to true for installation.
                                                    .Build();

                    AppUpdater.PromptUpdate(options, (progress, error) => {
                        if(error == null)
                        {
                            Log("Update download progress : " + progress);
                        }
                        else
                        {
                            Log("Failed to update: " + error);
                        }
                    });
                    break;
                case AppUpdaterDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kAppUpdater);
                    break;

                default:
                    break;
            }
        }
        
        #endregion
	}
}