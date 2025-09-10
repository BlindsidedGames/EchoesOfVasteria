using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    public static class ProductResources
    {
        #region Constants

        private     const   string      kAddressBookPage                        = "https://assetstore.essentialkit.voxelbusters.com/features/address-book";

        private     const   string      kAppShortcutsPage                       = "https://assetstore.essentialkit.voxelbusters.com/features/app-shortcuts";

        private     const   string      kAppUpdaterPage                         = "https://assetstore.essentialkit.voxelbusters.com/features/app-updater";
        
        private     const   string      kTaskServicesPage                       = "https://assetstore.essentialkit.voxelbusters.com/features/background-task-services";

        private     const   string      kBillingServicesPage                    = "https://assetstore.essentialkit.voxelbusters.com/features/billing-services";

        private     const   string      kCloudServicesPage                      = "https://assetstore.essentialkit.voxelbusters.com/features/cloud-services";

        private     const   string      kExtrasPage                             = "https://assetstore.essentialkit.voxelbusters.com/features/utilities";
        
        private     const   string      kGameServicesPage                       = "https://assetstore.essentialkit.voxelbusters.com/features/game-services";

        private     const   string      kMediaServicesPage                      = "https://assetstore.essentialkit.voxelbusters.com/features/media-services";

        private     const   string      kNetworkServicesPage                    = "https://assetstore.essentialkit.voxelbusters.com/features/network-services";

        private     const   string      kNotificationServicesPage               = "https://assetstore.essentialkit.voxelbusters.com/features/notification-services";

        private     const   string      kRateMyAppPage                          = "https://assetstore.essentialkit.voxelbusters.com/features/rate-my-app";

        private     const   string      kSharingPage                            = "https://assetstore.essentialkit.voxelbusters.com/features/sharing";

        private     const   string      kUIPage                                 = "https://assetstore.essentialkit.voxelbusters.com/features/native-ui";
        
        private     const   string      kWebViewPage                            = "https://assetstore.essentialkit.voxelbusters.com/features/web-view";

        private     const   string      kDeepLinkServicesPage                   = "https://assetstore.essentialkit.voxelbusters.com/features/deep-link-services";
        
        #endregion

        #region Public static methods

        public static void OpenResourcePage(string feature)
        {
            string resourcePage = null;
            switch (feature)
            {
                case NativeFeatureType.kAddressBook:
                    resourcePage    = kAddressBookPage;
                    break;
                
                case NativeFeatureType.kAppShortcuts:
                    resourcePage    = kAppShortcutsPage;
                    break;
                
                case NativeFeatureType.kAppUpdater:
                    resourcePage    = kAppUpdaterPage;
                    break;

                case NativeFeatureType.kBillingServices:
                    resourcePage    = kBillingServicesPage;
                    break;

                case NativeFeatureType.kCloudServices:
                    resourcePage    = kCloudServicesPage;
                    break;

                case NativeFeatureType.kDeepLinkServices:
                    resourcePage    = kDeepLinkServicesPage;
                    break;

                case NativeFeatureType.kExtras:
                    resourcePage    = kExtrasPage;
                    break;

                case NativeFeatureType.kGameServices:
                    resourcePage    = kGameServicesPage;
                    break;

                case NativeFeatureType.kMediaServices:
                    resourcePage    = kMediaServicesPage;
                    break;

                case NativeFeatureType.kNetworkServices:
                    resourcePage    = kNetworkServicesPage;
                    break;

                case NativeFeatureType.kNotificationServices:
                    resourcePage    = kNotificationServicesPage;
                    break;

                case NativeFeatureType.kRateMyApp:
                    resourcePage    = kRateMyAppPage;
                    break;

                case NativeFeatureType.kSharingServices:
                    resourcePage    = kSharingPage;
                    break;

                case NativeFeatureType.kNativeUI:
                    resourcePage    = kUIPage;
                    break;
                
                case NativeFeatureType.kTaskServices:
                    resourcePage    = kTaskServicesPage;
                    break;

                case NativeFeatureType.kWebView:
                    resourcePage    = kWebViewPage;
                    break;
            }

            // open link
            if (resourcePage != null)
            {
                Application.OpenURL(resourcePage);
            }
        }

        #endregion
    }
}