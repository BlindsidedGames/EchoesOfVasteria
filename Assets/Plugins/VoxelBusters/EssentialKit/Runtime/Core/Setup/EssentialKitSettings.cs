using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    public class EssentialKitDomain
    {
        public static string Default => "VoxelBusters.CoreLibrary.EssentialKit";
    }

    public class EssentialKitSettings : SettingsObject
    {
        #region Static fields

        [ClearOnReload]
        private static EssentialKitSettings s_sharedInstance;

        [ClearOnReload]
        private static UnityPackageDefinition s_package;

        #endregion

        #region Fields

        [SerializeField]
        private ApplicationSettings m_applicationSettings = new ApplicationSettings();

        [SerializeField]
        private AddressBookUnitySettings m_addressBookSettings = new AddressBookUnitySettings();

        [SerializeField]
        private AppUpdaterUnitySettings m_appUpdaterSettings = new AppUpdaterUnitySettings();

        [SerializeField]
        private AppShortcutsUnitySettings m_appShortcutsSettings = new AppShortcutsUnitySettings();


        [SerializeField]
        private NativeUIUnitySettings m_nativeUISettings = new NativeUIUnitySettings();

        [SerializeField]
        private SharingServicesUnitySettings m_sharingServicesSettings = new SharingServicesUnitySettings();

        [SerializeField]
        private CloudServicesUnitySettings m_cloudServicesSettings = new CloudServicesUnitySettings();

        [SerializeField]
        private GameServicesUnitySettings m_gameServicesSettings = new GameServicesUnitySettings();

        [SerializeField]
        private BillingServicesUnitySettings m_billingServicesSettings = new BillingServicesUnitySettings();

        [SerializeField]
        private NetworkServicesUnitySettings m_networkServicesSettings = new NetworkServicesUnitySettings();

        [SerializeField]
        private NotificationServicesUnitySettings m_notificationServicesSettings = new NotificationServicesUnitySettings();

        [SerializeField]
        private MediaServicesUnitySettings m_mediaServicesSettings = new MediaServicesUnitySettings();

        [SerializeField]
        private DeepLinkServicesUnitySettings m_deepLinkServicesSettings = new DeepLinkServicesUnitySettings();

        [SerializeField]
        private RateMyAppUnitySettings m_rateMyAppSettings = new RateMyAppUnitySettings();

        [SerializeField]
        private TaskServicesUnitySettings m_taskServicesSettings = new TaskServicesUnitySettings();

        [SerializeField]
        private UtilityUnitySettings m_utilitySettings = new UtilityUnitySettings();

        [SerializeField]
        private WebViewUnitySettings m_webViewSettings = new WebViewUnitySettings();


        #endregion

        #region Static properties

        internal static UnityPackageDefinition Package => ObjectHelper.CreateInstanceIfNull(
            ref s_package,
            () =>
            {
                return new UnityPackageDefinition(name: "com.voxelbusters.essentialkit",
                                                  displayName: "Essential Kit",
                                                  version: "3.5.0",
                                                  defaultInstallPath: $"Assets/Plugins/VoxelBusters/EssentialKit",
                                                  dependencies: CoreLibrarySettings.Package);
            });

        public static string PackageName => Package.Name;

        public static string DisplayName => Package.DisplayName;

        public static string Version => Package.Version;

        public static string DefaultSettingsAssetName => "EssentialKitSettings";

        public static string DefaultSettingsAssetPath => $"{Package.GetMutableResourcesPath()}/{DefaultSettingsAssetName}.asset";

        public static string PersistentDataPath => Package.PersistentDataPath;

        public static EssentialKitSettings Instance => GetSharedInstanceInternal();

        #endregion

        #region Properties

        public ApplicationSettings ApplicationSettings
        {
            get => m_applicationSettings;
            set => m_applicationSettings = value;
        }

        public AddressBookUnitySettings AddressBookSettings
        {
            get => m_addressBookSettings;
            set => m_addressBookSettings = value;
        }

        public AppShortcutsUnitySettings AppShortcutsSettings
        {
            get => m_appShortcutsSettings;
            set => m_appShortcutsSettings = value;
        }

        public AppUpdaterUnitySettings AppUpdaterSettings
        {
            get => m_appUpdaterSettings;
            set => m_appUpdaterSettings = value;
        }

        public TaskServicesUnitySettings TaskServicesSettings
        {
            get => m_taskServicesSettings;
            set => m_taskServicesSettings = value;
        }

        public NativeUIUnitySettings NativeUISettings
        {
            get => m_nativeUISettings;
            set => m_nativeUISettings = value;
        }

        public SharingServicesUnitySettings SharingServicesSettings
        {
            get => m_sharingServicesSettings;
            set => m_sharingServicesSettings = value;
        }

        public CloudServicesUnitySettings CloudServicesSettings
        {
            get => m_cloudServicesSettings;
            set => m_cloudServicesSettings = value;
        }

        public GameServicesUnitySettings GameServicesSettings
        {
            get => m_gameServicesSettings;
            set => m_gameServicesSettings = value;
        }

        public BillingServicesUnitySettings BillingServicesSettings
        {
            get => m_billingServicesSettings;
            set => m_billingServicesSettings = value;
        }

        public NetworkServicesUnitySettings NetworkServicesSettings
        {
            get => m_networkServicesSettings;
            set => m_networkServicesSettings = value;
        }

        public WebViewUnitySettings WebViewSettings
        {
            get => m_webViewSettings;
            set => m_webViewSettings = value;
        }

        public NotificationServicesUnitySettings NotificationServicesSettings
        {
            get => m_notificationServicesSettings;
            set => m_notificationServicesSettings = value;
        }

        public MediaServicesUnitySettings MediaServicesSettings
        {
            get => m_mediaServicesSettings;
            set => m_mediaServicesSettings = value;
        }

        public DeepLinkServicesUnitySettings DeepLinkServicesSettings
        {
            get => m_deepLinkServicesSettings;
            set => m_deepLinkServicesSettings = value;
        }

        public UtilityUnitySettings UtilitySettings
        {
            get => m_utilitySettings;
            set => m_utilitySettings = value;
        }

        public RateMyAppUnitySettings RateMyAppSettings
        {
            get => m_rateMyAppSettings;
            set => m_rateMyAppSettings = value;
        }

        #endregion

        #region Static methods

        public static void SetSettings(EssentialKitSettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // set properties
            s_sharedInstance = settings;
        }

        private static EssentialKitSettings GetSharedInstanceInternal(bool throwError = true)
        {
            if (null == s_sharedInstance)
            {
                // check whether we are accessing in edit or play mode
                var assetPath = DefaultSettingsAssetName;
                var settings = Resources.Load<EssentialKitSettings>(assetPath);
                if (throwError && (null == settings))
                {
                    throw Diagnostics.PluginNotConfiguredException("Essential Kit");
                }

                // store reference
                s_sharedInstance = settings;
            }

            return s_sharedInstance;
        }

        #endregion

        #region Base class methods

        protected override void UpdateLoggerSettings()
        {
#if NATIVE_PLUGINS_DEBUG
            DebugLogger.SetLogLevel(ApplicationSettings.LogLevel, EssentialKitDomain.Default, CoreLibraryDomain.NativePlugins, CoreLibraryDomain.Default, "VoxelBusters");
#else
            DebugLogger.SetLogLevel(ApplicationSettings.LogLevel, EssentialKitDomain.Default);
#endif
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            UpdateLoggerSettings();
            SyncSettings();
        }

        #endregion

        #region Private methods

        private string[] GetAvailableFeatureNames()
        {
            return new string[]
            {
                NativeFeatureType.kAddressBook,
                NativeFeatureType.kAppShortcuts,
                NativeFeatureType.kAppUpdater,
                NativeFeatureType.kBillingServices,
                NativeFeatureType.kCloudServices,
                NativeFeatureType.kDeepLinkServices,
                NativeFeatureType.kGameServices,
                NativeFeatureType.kMediaServices,
                NativeFeatureType.kNativeUI,
                NativeFeatureType.kNetworkServices,
                NativeFeatureType.kNotificationServices,
                NativeFeatureType.kRateMyApp,
                NativeFeatureType.kSharingServices,
                NativeFeatureType.kTaskServices,
                NativeFeatureType.kWebView,
                NativeFeatureType.kExtras
            };
        }

        private string[] GetUsedFeatureNames()
        {
            var usedFeatures = new List<string>();
            if (m_addressBookSettings.IsEnabled && !IsPlatformTarget(NativePlatform.tvOS))
            {
                usedFeatures.Add(NativeFeatureType.kAddressBook);
            }
            if (m_appShortcutsSettings.IsEnabled && !IsPlatformTarget(NativePlatform.tvOS))
            {
                usedFeatures.Add(NativeFeatureType.kAppShortcuts);
            }
            if (m_appUpdaterSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kAppUpdater);
            }
            if (m_taskServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kTaskServices);
            }
            if (m_billingServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kBillingServices);
            }
            if (m_cloudServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kCloudServices);
            }
            if (m_gameServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kGameServices);
            }
            if (m_mediaServicesSettings.IsEnabled && !IsPlatformTarget(NativePlatform.tvOS))
            {
                usedFeatures.Add(NativeFeatureType.kMediaServices);
            }
            if (m_nativeUISettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kNativeUI);
            }
            if (m_networkServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kNetworkServices);
            }
            if (m_notificationServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kNotificationServices);
            }
            if (m_sharingServicesSettings.IsEnabled && !IsPlatformTarget(NativePlatform.tvOS))
            {
                usedFeatures.Add(NativeFeatureType.kSharingServices);
            }
            if (m_webViewSettings.IsEnabled && !IsPlatformTarget(NativePlatform.tvOS))
            {
                usedFeatures.Add(NativeFeatureType.kWebView);
            }
            if (m_deepLinkServicesSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kDeepLinkServices);
            }
            if (m_rateMyAppSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kRateMyApp);
            }
            if (usedFeatures.Count > 0)
            {
                usedFeatures.Add(NativeFeatureType.kNativeUI);//Required for showing confirmation dialog
            }
            if (m_utilitySettings.IsEnabled || m_rateMyAppSettings.IsEnabled)
            {
                usedFeatures.Add(NativeFeatureType.kExtras);
            }

            return usedFeatures.ToArray();
        }

        private bool IsPlatformTarget(NativePlatform nativePlatform)
        {
            return PlatformMappingServices.GetActivePlatform() == nativePlatform;
        }


        private void InitialiseFeatureIfRequired(string feature, Action initialiseAction)
        {
            if (IsFeatureUsed(feature))
            {
                initialiseAction.Invoke();
            }
        }

        private void SyncSettings()
        {
            //Cloud Services Play Services Application Id
            if (m_cloudServicesSettings.IsEnabled)
            {
                m_cloudServicesSettings.AndroidProperties.PlayServicesApplicationId = m_gameServicesSettings.AndroidProperties.PlayServicesApplicationId;
            }
        }

        #endregion

        #region Public methods

        public void InitialiseFeatures()
        {
            // Initialize required features
            InitialiseFeatureIfRequired(NativeFeatureType.kAddressBook, () => AddressBook.Initialize(m_addressBookSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kAppShortcuts, () => AppShortcuts.Initialize(m_appShortcutsSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kAppUpdater, () => AppUpdater.Initialize(m_appUpdaterSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kBillingServices, () => BillingServices.Initialize(m_billingServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kCloudServices, () => CloudServices.Initialize(m_cloudServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kDeepLinkServices, () => DeepLinkServices.Initialize(m_deepLinkServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kGameServices, () => GameServices.Initialize(m_gameServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kMediaServices, () => MediaServices.Initialize(m_mediaServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kNetworkServices, () => NetworkServices.Initialize(m_networkServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kNotificationServices, () => NotificationServices.Initialize(m_notificationServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kNativeUI, () => NativeUI.Initialize(m_nativeUISettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kRateMyApp, () => RateMyApp.Initialize(m_rateMyAppSettings, ApplicationSettings.GetAppStoreIdForActivePlatform()));
            InitialiseFeatureIfRequired(NativeFeatureType.kSharingServices, () => SharingServices.Initialize(m_sharingServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kExtras, () => Utilities.Initialize(m_utilitySettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kTaskServices, () => TaskServices.Initialize(m_taskServicesSettings));
            InitialiseFeatureIfRequired(NativeFeatureType.kWebView, () => WebView.Initialize(m_webViewSettings));
        }

        public bool IsFeatureUsed(string name)
        {
            return Array.Exists(GetUsedFeatureNames(), (item) => string.Equals(item, name));
        }

        #endregion
    }
}