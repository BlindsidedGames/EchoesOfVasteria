using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;


namespace VoxelBusters.EssentialKit.Demo
{
	public class AppShortcutsDemo : DemoActionPanelBase<AppShortcutsDemoAction, AppShortcutsDemoActionType>
	{
        #region Fields

        private const string kUninstallShortcutIdentifier   = "uninstall-shortcut";
        private const string kDailyRewardShortcutIdentifier = "daily-reward-shortcut";
        private const string kContinueShortcutIdentifier    = "continue-shortcut";

        #endregion
        
		#region Base methods

        protected override void OnEnable()
        {
            AppShortcuts.OnShortcutClicked += OnShortcutClicked;
        }

        protected override void OnDisable()
        {
            AppShortcuts.OnShortcutClicked -= OnShortcutClicked;
        }
        
        protected override void OnActionSelectInternal(AppShortcutsDemoAction selectedAction)
        {
            switch (selectedAction.ActionType)
            {
                case AppShortcutsDemoActionType.Add:
                    
                    AppShortcutItem uninstallShortcutItem = new AppShortcutItem.Builder(kUninstallShortcutIdentifier, "Any issues?")
                        .SetSubtitle("Please give us a chance to make it better!")
                        .SetIconFileName("support.png")//Make sure you refer the same icon texture in AppShortcuts settings inspector.
                        .Build();
                    AppShortcuts.Add(uninstallShortcutItem);
                    
					Log("Adding application shortcut item.");
                    AppShortcutItem dailyRewardShortcutItem = new AppShortcutItem.Builder(kDailyRewardShortcutIdentifier, "Daily Reward")
                        .SetSubtitle("Your rewards are waiting!")
                        .SetIconFileName("daily-reward.png")//Make sure you refer the same icon texture in AppShortcuts settings inspector.
                        .Build();
                    AppShortcuts.Add(dailyRewardShortcutItem);
                    
                    AppShortcutItem continueShortcutItem = new AppShortcutItem.Builder(kContinueShortcutIdentifier, "Continue Level 5")
                        .SetSubtitle("Pick up where you left off!")
                        .SetIconFileName("continue-game.png")
                        .Build();
                    AppShortcuts.Add(continueShortcutItem);
                    
                    break;
                
                case AppShortcutsDemoActionType.Remove:
                    Log("Removing application shortcut item.");
					AppShortcuts.Remove(kContinueShortcutIdentifier);
                    break;
                
                case AppShortcutsDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kAppShortcuts);
                    break;

                default:    
                    break;
            }
        }
        
        #endregion

        #region Private methods
        
        private void OnShortcutClicked(string shortcutId)
        {
            Log("Clicked shortcut item with id: " + shortcutId);
        }
        
        #endregion
	}

}