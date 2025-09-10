using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.RateMyAppCore.Simulator
{
    public class RateMyAppInterface : NativeRateMyAppInterfaceBase
    {
        #region Constructors

        public RateMyAppInterface()
            : base(isAvailable: false)
        { }

        #endregion

        #region Base methods

        public override void RequestStoreReview(string storeId)
        {
            OpenAppStorePage(storeId);
        }

        #endregion
        
        #region Helper methods

        private void OpenAppStorePage(string storeId)
        {
            var     activePlatform  = PlatformMappingServices.GetActivePlatform();
            switch (activePlatform)
            {
                case NativePlatform.Android:
                    Application.OpenURL("https://play.google.com/store/apps/details?id=" + storeId); 
                    break;

                case NativePlatform.iOS:
                case NativePlatform.tvOS:
                    Application.OpenURL($"https://apps.apple.com/app/id{storeId}?action=write-review");
                    break;
                default:
                    DebugLogger.LogWarning(EssentialKitDomain.Default, "Cannot open app store page. Unsupported platform: " + activePlatform.ToString());
                    Application.OpenURL("https://google.com/search?" + storeId);
                    break;
            }
        }

        #endregion
    }
}