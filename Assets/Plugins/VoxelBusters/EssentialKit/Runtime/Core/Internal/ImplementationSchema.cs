using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    public static class AssemblyName
    {
        public  const   string      kMain                   = "VoxelBusters.EssentialKit";
        
        public  const   string      kIos                    = "VoxelBusters.EssentialKit.iOSModule";
        
        public  const   string      kAndroid                = "VoxelBusters.EssentialKit.AndroidModule";
        
        public  const   string      kSimulator              = "VoxelBusters.EssentialKit.SimulatorModule";
    }

    public static class NamespaceName
    {
        public  const   string      kRoot                   = "VoxelBusters.EssentialKit";
        
        public  const   string      kAddressBook            = kRoot + ".AddressBookCore";
        
        public  const   string      kAppShortcuts           = kRoot + ".AppShortcutsCore";
        
        public  const   string      kAppUpdater             = kRoot + ".AppUpdaterCore";
        
        public  const   string      kBillingServices        = kRoot + ".BillingServicesCore";
        
        public  const   string      kCloudServices          = kRoot + ".CloudServicesCore";
        
        public  const   string      kGameServices           = kRoot + ".GameServicesCore";
        
        public  const   string      kMediaServices          = kRoot + ".MediaServicesCore";
        
        public  const   string      kNativeUI               = kRoot + ".NativeUICore";
        
        public  const   string      kNetworkServices        = kRoot + ".NetworkServicesCore";
        
        public  const   string      kNotificationServices   = kRoot + ".NotificationServicesCore";
        
        public  const   string      kSharingServices        = kRoot + ".SharingServicesCore";
        
        public  const   string      kWebView                = kRoot + ".WebViewCore";
        
        public  const   string      kExtras                 = kRoot + ".ExtrasCore";
        
        public  const   string      kDeepLinkServices       = kRoot + ".DeepLinkServicesCore";

        public  const   string      kRateMyApp              = kRoot + ".RateMyAppCore";
        
        public  const   string      kTaskServices           = kRoot + ".TaskServicesCore";
    }

    internal static class ImplementationSchema
    {
        #region Static fields

        private static Dictionary<string, NativeFeatureRuntimeConfiguration>    s_configurationMap;

        #endregion

        #region Static properties

        public static NativeFeatureRuntimeConfiguration AddressBook 
        { 
            get => GetRuntimeConfiguration(NativeFeatureType.kAddressBook);
        }
        
        public static NativeFeatureRuntimeConfiguration AppShortcuts 
        { 
            get => GetRuntimeConfiguration(NativeFeatureType.kAppShortcuts);
        }


        public static NativeFeatureRuntimeConfiguration AppUpdater 
        { 
            get => GetRuntimeConfiguration(NativeFeatureType.kAppUpdater);
        }

        public static NativeFeatureRuntimeConfiguration BillingServices 
        { 
            get => GetRuntimeConfiguration(NativeFeatureType.kBillingServices);
        }

        public static NativeFeatureRuntimeConfiguration CloudServices 
        { 
            get => GetRuntimeConfiguration(NativeFeatureType.kCloudServices);
        }
        
        public static NativeFeatureRuntimeConfiguration GameServices
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kGameServices);
        }
        
        public static NativeFeatureRuntimeConfiguration MediaServices
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kMediaServices);
        }
        
        public static NativeFeatureRuntimeConfiguration NativeUI
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kNativeUI);
        }
        
        public static NativeFeatureRuntimeConfiguration NetworkServices
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kNetworkServices);
        }
        
        public static NativeFeatureRuntimeConfiguration NotificationServices
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kNotificationServices);
        }
        
        public static NativeFeatureRuntimeConfiguration SharingServices
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kSharingServices);
        }
        
        public static NativeFeatureRuntimeConfiguration WebView
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kWebView);
        }
        
        public static NativeFeatureRuntimeConfiguration Extras
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kExtras);
        }
        
        public static NativeFeatureRuntimeConfiguration DeepLinkServices
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kDeepLinkServices);
        }

        public static NativeFeatureRuntimeConfiguration RateMyApp
        {
            get => GetRuntimeConfiguration(NativeFeatureType.kRateMyApp);
        }
        
        public static NativeFeatureRuntimeConfiguration TaskServices 
        { 
            get => GetRuntimeConfiguration(NativeFeatureType.kTaskServices);
        }

        
        #endregion

        #region Constructors

        static ImplementationSchema()
        {
            s_configurationMap  = new Dictionary<string, NativeFeatureRuntimeConfiguration>()
            {
                // Address Book
                {
                    NativeFeatureType.kAddressBook,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kAddressBook}.iOS",
                                                                                            nativeInterfaceType: "AddressBookInterface",
                                                                                            bindingTypes: new string[] { "AddressBookBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kAddressBook}.Android",
                                                                                                nativeInterfaceType: "AddressBookInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kAddressBook}.Simulator",
                                                                                                                nativeInterfaceType: "AddressBookInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kAddressBook,
                                                                                                               nativeInterfaceType: "NullAddressBookInterface"))
                },
                
                // App Shortcuts
                {
                    NativeFeatureType.kAppShortcuts,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kAppShortcuts}.iOS",
                                                                                            nativeInterfaceType: NativeFeatureType.kAppShortcuts + "Interface",
                                                                                            bindingTypes: new string[] { NativeFeatureType.kAppShortcuts + "Binding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kAppShortcuts}.Android",
                                                                                                nativeInterfaceType: NativeFeatureType.kAppShortcuts + "Interface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kAppShortcuts}.Simulator",
                                                                                                                nativeInterfaceType: NativeFeatureType.kAppShortcuts + "Interface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kAppShortcuts,
                                                                                                               nativeInterfaceType: "Null" + NativeFeatureType.kAppShortcuts + "Interface"))
                },

                // App Updater
                {
                    NativeFeatureType.kAppUpdater,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kAppUpdater}.iOS",
                                                                                            nativeInterfaceType: "AppUpdaterInterface",
                                                                                            bindingTypes: new string[] { "AppUpdaterBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kAppUpdater}.Android",
                                                                                                nativeInterfaceType: "AppUpdaterInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kAppUpdater}.Simulator",
                                                                                                                nativeInterfaceType: "AppUpdaterInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kAppUpdater,
                                                                                                               nativeInterfaceType: "NullAppUpdaterInterface"))
                },
                
                // Billing Services
                {
                    NativeFeatureType.kBillingServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kBillingServices}.iOS",
                                                                                            nativeInterfaceType: "BillingServicesInterface",
                                                                                            bindingTypes: new string[] { "BillingServicesBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kBillingServices}.Android",
                                                                                                nativeInterfaceType: "BillingServicesInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kBillingServices}.Simulator",
                                                                                                                nativeInterfaceType: "BillingServicesInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kBillingServices,
                                                                                                               nativeInterfaceType: "NullBillingServicesInterface"))
                },

                // Cloud Services
                {
                    NativeFeatureType.kCloudServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kCloudServices}.iOS",
                                                                                            nativeInterfaceType: "CloudServicesInterface",
                                                                                            bindingTypes: new string[] { "CloudServicesBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kCloudServices}.Android",
                                                                                                nativeInterfaceType: "CloudServicesInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kCloudServices}.Simulator",
                                                                                                                nativeInterfaceType: "CloudServicesInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kCloudServices,
                                                                                                               nativeInterfaceType: "NullCloudServicesInterface"))
                },

                // Game Services
                {
                    NativeFeatureType.kGameServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kGameServices}.iOS",
                                                                                            nativeInterfaceType: "GameCenterInterface",
                                                                                            bindingTypes: new string[] { "LeaderboardScoreBinding", "PlayerBinding", "LeaderboardBinding", "GameCenterBinding", "AchievementDescriptionBinding", "AchievementBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kGameServices}.Android",
                                                                                                nativeInterfaceType: "GameServicesInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kGameServices}.Simulator",
                                                                                                                nativeInterfaceType: "GameServicesInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kGameServices,
                                                                                                               nativeInterfaceType: "NullGameServicesInterface"))
                },

                // Media Services
                {
                    NativeFeatureType.kMediaServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kMediaServices}.iOS",
                                                                                            nativeInterfaceType: "MediaServicesInterface",
                                                                                            bindingTypes: new string[] { "MediaServicesBinding", "MediaContentBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kMediaServices}.Android",
                                                                                                nativeInterfaceType: "MediaServicesInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kMediaServices}.Simulator",
                                                                                                                nativeInterfaceType: "MediaServicesInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kMediaServices,
                                                                                                               nativeInterfaceType: "NullMediaServicesInterface"))
                },

                // Native UI
                {
                    NativeFeatureType.kNativeUI,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kNativeUI}.iOS",
                                                                                            nativeInterfaceType: "NativeUIInterface",
                                                                                            bindingTypes: new string[] { "DatePickerControllerBinding", "AlertControllerBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kNativeUI}.Android",
                                                                                                nativeInterfaceType: "UIInterface"),
                                                          },
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kNativeUI,
                                                                                                               nativeInterfaceType: "UnityUIInterface"))
                },

                // Network Services
                { 
                    NativeFeatureType.kNetworkServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kNetworkServices}.iOS",
                                                                                            nativeInterfaceType: "NetworkServicesInterface",
                                                                                            bindingTypes: new string[] { "NetworkServicesBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kNetworkServices}.Android",
                                                                                                nativeInterfaceType: "NetworkServicesInterface"),
                                                          },
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kNetworkServices,
                                                                                                               nativeInterfaceType: "UnityNetworkServicesInterface"))
                },

                // Notification Services
                {
                    NativeFeatureType.kNotificationServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kNotificationServices}.iOS",
                                                                                            nativeInterfaceType: "NotificationCenterInterface",
                                                                                            bindingTypes: new string[] { "NotificationCenterBinding", "NotificationBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kNotificationServices}.Android",
                                                                                                nativeInterfaceType: "NotificationCenterInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kNotificationServices}.Simulator",
                                                                                                                nativeInterfaceType: "NotificationCenterInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kNotificationServices,
                                                                                                               nativeInterfaceType: "NullNotificationCenterInterface"))
                },

                // Sharing Services
                {
                    NativeFeatureType.kSharingServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kSharingServices}.iOS",
                                                                                            nativeInterfaceType: "NativeSharingInterface",
                                                                                            bindingTypes: new string[] { "SocialShareComposerBinding", "ShareSheetBinding", "MessageComposerBinding", "MailComposerBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kSharingServices}.Android",
                                                                                                nativeInterfaceType: "SharingServicesInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kSharingServices}.Simulator",
                                                                                                                nativeInterfaceType: "NativeSharingInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kSharingServices,
                                                                                                               nativeInterfaceType: "NullSharingInterface"))
                },
                
                // Task Services
                {
                    NativeFeatureType.kTaskServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kTaskServices}.iOS",
                                                                                            nativeInterfaceType: "TaskServicesInterface",
                                                                                            bindingTypes: new string[] { "TaskServicesBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kTaskServices}.Android",
                                                                                                nativeInterfaceType: "TaskServicesInterface"),
                                                          },
                                                          simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kTaskServices}.Simulator",
                                                                                                                nativeInterfaceType: "TaskServicesInterface"),
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kTaskServices,
                                                                                                               nativeInterfaceType: "NullTaskServicesInterface"))
                },

                // Webview
                {
                    NativeFeatureType.kWebView,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kWebView}.iOS",
                                                                                            nativeInterfaceType: "NativeWebView",
                                                                                            bindingTypes: new string[] { "WebViewBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kWebView}.Android",
                                                                                                nativeInterfaceType: "WebView"),
                                                          },
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kWebView,
                                                                                                               nativeInterfaceType: "NullNativeWebView"))
                },

                // Extras
                {
                    NativeFeatureType.kExtras,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kExtras}.iOS",
                                                                                            nativeInterfaceType: "NativeUtilityInterface",
                                                                                            bindingTypes: new string[] { "NativeUtilityInterface" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kExtras}.Android",
                                                                                                nativeInterfaceType: "UtilityInterface"),
                                                          },
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kExtras,
                                                                                                               nativeInterfaceType: "NullNativeUtilityInterface"))
                },

                // Deep Link Services
                {
                    NativeFeatureType.kDeepLinkServices,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kDeepLinkServices}.iOS",
                                                                                            nativeInterfaceType: "DeepLinkServicesInterface",
                                                                                            bindingTypes: new string[] { "DeepLinkServicesBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kDeepLinkServices}.Android",
                                                                                                nativeInterfaceType: "DeepLinkServicesInterface"),
                                                          },
                                                          fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kDeepLinkServices,
                                                                                                               nativeInterfaceType: "NullDeepLinkServicesInterface"))
                },

                // RateMyApp
                {
                    NativeFeatureType.kRateMyApp,
                    new NativeFeatureRuntimeConfiguration(packages: new NativeFeatureRuntimePackage[]
                                                          {
                                                            NativeFeatureRuntimePackage.iOS(assembly: AssemblyName.kIos,
                                                                                            ns: $"{NamespaceName.kRateMyApp}.iOS",
                                                                                            nativeInterfaceType: "NativeRateMyAppInterface",
                                                                                            bindingTypes: new string[] { "RateMyAppBinding" }),
                                                            NativeFeatureRuntimePackage.Android(assembly: AssemblyName.kAndroid,
                                                                                                ns: $"{NamespaceName.kRateMyApp}.Android",
                                                                                                nativeInterfaceType: "RateMyAppInterface"),
                                                          },
                                                            simulatorPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kSimulator,
                                                                                                                ns: $"{NamespaceName.kRateMyApp}.Simulator",
                                                                                                                nativeInterfaceType: "RateMyAppInterface"),
                                                                                                                   
                                                            fallbackPackage: NativeFeatureRuntimePackage.Generic(assembly: AssemblyName.kMain,
                                                                                                               ns: NamespaceName.kRateMyApp,
                                                                                                               nativeInterfaceType: "NullRateMyAppInterface"))
                }
            };
        }
            
        #endregion

        #region Public static methods

        public static KeyValuePair<string, NativeFeatureRuntimeConfiguration>[] GetAllRuntimeConfigurations(bool includeInactive = true, EssentialKitSettings settings = null)
        {
            Assert.IsTrue(includeInactive || (settings != null), "Arg settings is null.");

            var     configurations  = new List<KeyValuePair<string, NativeFeatureRuntimeConfiguration>>();
            foreach (var feature in s_configurationMap)
            {
                if (includeInactive || ((settings != null) && settings.IsFeatureUsed(feature.Key)))
                {
                    configurations.Add(feature);
                }
            }
            return configurations.ToArray();
        }

        public static NativeFeatureRuntimeConfiguration GetRuntimeConfiguration(string featureName)
        {
            s_configurationMap.TryGetValue(featureName, out NativeFeatureRuntimeConfiguration config);

            return config;
        }

        #endregion
    }
}